using System;
using System.Collections.Generic;
using FrontlineMap.Core;
using FrontlineMap.Diagnostics;

namespace FrontlineMap.Runtime
{
    public enum StepOutcome
    {
        Idle, Working, Complete
    }
    public enum InvalidationReason
    {
        Team, Map, Model, Hidden, Expired, Failure, Shutdown
    }
    public sealed class FieldResult
    {
        public readonly ObservationSnapshot Snapshot;
        public readonly InfluenceBuffers Field;
        public readonly Pixel[] Pixels;
        public double ScreenWidth, ScreenHeight;
        public FieldResult(ObservationSnapshot snapshot, InfluenceBuffers field, Pixel[] pixels)
        {
            Snapshot = snapshot;
            Field = field;
            Pixels = pixels;
        }
    }
    public sealed class StepScheduler : IDisposable
    {
        private sealed class Request
        {
            public ObservationSnapshot Snapshot; public ModelSettings Settings; public double Width, Height; public InfluenceBuffers Existing;
        }
        private readonly Func<double> clock;
        private Request active, pending;
        private IEnumerator<int> work;
        private FieldResult completed;
        private WorkStage stage;
        public readonly OverlayMetrics Metrics = new OverlayMetrics();
        public Action<WorkStage, int> ChunkCompleted;
        public long ExpiredCount
        {
            get; private set;
        }
        public long PendingReplacementCount
        {
            get; private set;
        }
        public bool Busy => active != null;
        public int LastChunkCells
        {
            get; private set;
        }
        public long PendingSequence => pending == null ? 0 : pending.Snapshot.Identity.Sequence;
        public StepScheduler(Func<double> clock)
        {
            this.clock = clock;
        }
        public void Begin(ObservationSnapshot snapshot, ModelSettings settings, double screenWidth, double screenHeight)
        {
            var request = new Request { Snapshot = snapshot, Settings = settings, Width = screenWidth, Height = screenHeight };
            if (active != null || completed != null || pending != null)
            {
                if (pending != null)
                    PendingReplacementCount++;
                pending = request;
                return;
            }
            active = request;
            Metrics.Reset();
            work = Calculate(request).GetEnumerator();
        }
        public void BeginRepaint(FieldResult result, ModelSettings settings, double width, double height)
        {
            if (active != null || pending != null || completed != null)
                return;
            Metrics.Reset();
            active = new Request { Snapshot = result.Snapshot, Settings = settings, Width = width, Height = height, Existing = result.Field };
            work = Calculate(active).GetEnumerator();
        }
        public void DiscardExpired(double now)
        {
            if (pending != null && now >= pending.Snapshot.ExpiresAt)
            {
                pending = null;
                ExpiredCount++;
            }
            if (active != null && now >= active.Snapshot.ExpiresAt)
            {
                work?.Dispose();
                work = null;
                active = null;
                ExpiredCount++;
            }
            if (completed != null && now >= completed.Snapshot.ExpiresAt)
            {
                completed = null;
                ExpiredCount++;
            }
        }
        public StepOutcome Step(double deadlineMonotonicSeconds)
        {
            DiscardExpired(clock());
            if (completed != null)
                return StepOutcome.Complete;
            if (active == null && pending != null)
            {
                var next = pending;
                pending = null;
                Begin(next.Snapshot, next.Settings, next.Width, next.Height);
            }
            if (active == null)
                return StepOutcome.Idle;
            try
            {
                while (clock() < deadlineMonotonicSeconds)
                {
                    double started = clock();
                    bool more = work.MoveNext();
                    Metrics.Record(stage, more ? work.Current : 0, clock() - started);
                    if (!more)
                    {
                        work.Dispose();
                        work = null;
                        active = null;
                        return StepOutcome.Complete;
                    }
                    LastChunkCells = work.Current;
                    ChunkCompleted?.Invoke(stage, LastChunkCells);
                }
                return StepOutcome.Working;
            }
            catch { Cancel(); throw; }
        }
        public bool TryTakeCompleted(out FieldResult result)
        {
            result = completed;
            completed = null;
            return result != null;
        }
        public void Cancel()
        {
            work?.Dispose();
            work = null;
            active = null;
            pending = null;
            completed = null;
        }
        public void Dispose() => Cancel();
        private IEnumerable<int> Calculate(Request request)
        {
            // Allocation/zeroing is indivisible; measured separately from the 128-cell compute chunks.
            var s = request.Snapshot;
            var settings = request.Settings;
            var g = s.Geometry;
            stage = WorkStage.Allocation;
            var field = request.Existing ?? new InfluenceBuffers(g);
            var pixels = new Pixel[g.Count];
            var columnDistanceSquared = request.Existing == null ? new double[g.Columns] : null;
            yield return 0;
            stage = WorkStage.Stamp;
            double radius = settings.Radius, radiusSquared = radius * radius;
            double cellX = g.CellX, cellZ = g.CellZ;
            if (request.Existing == null)
                foreach (var stamp in s.Stamps)
                {
                    // At extreme origin/cell ratios rounded cell centers can coincide. Coordinate bounds
                    // can then miss oracle-supported cells, so retain exact full-grid predicates instead.
                    bool fullGrid = Math.Max(Math.Abs(g.MinX), Math.Abs(stamp.X)) / cellX >= 1e12
                        || Math.Max(Math.Abs(g.MinZ), Math.Abs(stamp.Z)) / cellZ >= 1e12;
                    if (!fullGrid && (stamp.X + radius < g.MinX || stamp.Z + radius < g.MinZ || stamp.X - radius > g.MinX + g.Width || stamp.Z - radius > g.MinZ + g.Height))
                        continue;
                    int minX = fullGrid ? 0 : ClampIndex(Math.Floor((stamp.X - radius - g.MinX) / cellX), g.Columns);
                    int maxX = fullGrid ? g.Columns - 1 : ClampIndex(Math.Ceiling((stamp.X + radius - g.MinX) / cellX), g.Columns);
                    int minZ = fullGrid ? 0 : ClampIndex(Math.Floor((stamp.Z - radius - g.MinZ) / cellZ), g.Rows);
                    int maxZ = fullGrid ? g.Rows - 1 : ClampIndex(Math.Ceiling((stamp.Z + radius - g.MinZ) / cellZ), g.Rows);
                    var target = stamp.Side == Side.Friendly ? field.Friendly : field.Enemy;
                    int cells = 0;
                    // The same scratch is overwritten for each stamp; never shared with a displayed field.
                    for (int x = minX; x <= maxX; x++)
                    {
                        double dx = g.MinX + (x + .5) * cellX - stamp.X;
                        columnDistanceSquared[x] = dx * dx;
                        if (++cells == 128)
                        {
                            yield return cells;
                            cells = 0;
                        }
                    }
                    // One-cell padding covers rounding in the circle-to-grid conversion at ordinary scales.
                    // Extreme radius/cell ratios keep the rectangle; unsafe coordinates use the full grid.
                    bool clipRows = !fullGrid && radius / cellX < 1e6;
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        // Preserve the oracle's operations/order, but hoist row invariants.
                        double dz = g.MinZ + (z + .5) * cellZ - stamp.Z;
                        double dzSquared = dz * dz;
                        int row = z * g.Columns;
                        int rowMinX = minX, rowMaxX = maxX;
                        if (clipRows)
                        {
                            // Addition of a nonnegative dx² cannot satisfy the strict predicate in these rows.
                            if (dzSquared >= radiusSquared)
                            {
                                if (++cells == 128)
                                {
                                    yield return cells;
                                    cells = 0;
                                }
                                continue;
                            }
                            double span = Math.Sqrt(radiusSquared - dzSquared);
                            rowMinX = Math.Max(minX, ClampIndex(Math.Floor((stamp.X - span - g.MinX) / cellX) - 1, g.Columns));
                            rowMaxX = Math.Min(maxX, ClampIndex(Math.Ceiling((stamp.X + span - g.MinX) / cellX) + 1, g.Columns));
                        }
                        for (int x = rowMinX; x <= rowMaxX; x++)
                        {
                            double distance = columnDistanceSquared[x] + dzSquared;
                            if (distance < radiusSquared)
                                target[row + x] += stamp.Weight * (1 - distance / radiusSquared);
                            if (++cells == 128)
                            {
                                yield return cells;
                                cells = 0;
                            }
                        }
                    }
                    if (cells > 0)
                        yield return cells;
                }
            stage = WorkStage.Raster;
            var sampler = new Rasterizer.Sampler(field, settings, request.Width, request.Height);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = sampler.Sample(i);
                if ((i + 1) % 128 == 0)
                    yield return 128;
            }
            if (pixels.Length % 128 != 0)
                yield return pixels.Length % 128;
            completed = new FieldResult(s, field, pixels) { ScreenWidth = request.Width, ScreenHeight = request.Height };
        }
        private static int ClampIndex(double value, int length) => (int)Math.Max(0, Math.Min(length - 1, value));
    }
}
