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
            yield return 0;
            stage = WorkStage.Stamp;
            double radius = settings.Radius, radiusSquared = radius * radius;
            if (request.Existing == null)
                foreach (var stamp in s.Stamps)
                {
                    if (stamp.X + radius < g.MinX || stamp.Z + radius < g.MinZ || stamp.X - radius > g.MinX + g.Width || stamp.Z - radius > g.MinZ + g.Height)
                        continue;
                    int minX = ClampIndex(Math.Floor((stamp.X - radius - g.MinX) / g.CellX), g.Columns);
                    int maxX = ClampIndex(Math.Ceiling((stamp.X + radius - g.MinX) / g.CellX), g.Columns);
                    int minZ = ClampIndex(Math.Floor((stamp.Z - radius - g.MinZ) / g.CellZ), g.Rows);
                    int maxZ = ClampIndex(Math.Ceiling((stamp.Z + radius - g.MinZ) / g.CellZ), g.Rows);
                    var target = stamp.Side == Side.Friendly ? field.Friendly : field.Enemy;
                    int cells = 0;
                    for (int z = minZ; z <= maxZ; z++)
                        for (int x = minX; x <= maxX; x++)
                        {
                            double dx = g.X(x) - stamp.X, dz = g.Z(z) - stamp.Z;
                            double distance = dx * dx + dz * dz;
                            if (distance < radiusSquared)
                                target[z * g.Columns + x] += stamp.Weight * (1 - distance / radiusSquared);
                            if (++cells == 128)
                            {
                                yield return cells;
                                cells = 0;
                            }
                        }
                    if (cells > 0)
                        yield return cells;
                }
            stage = WorkStage.Raster;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Rasterizer.Sample(field, settings, i, request.Width, request.Height);
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
