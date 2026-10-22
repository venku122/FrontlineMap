using System;
using System.Collections.Generic;

namespace FrontlineMap.Core
{
    /// <summary>Immutable public static-map samples; unknown space never carries influence.</summary>
    public sealed class TerrainCostGrid
    {
        readonly double[] heights;
        readonly bool[] known, land, water;
        readonly double maximumSlope;
        readonly ushort[] landSteps, waterSteps;
        readonly double[] centerX, centerZ, stepLength;
        readonly int[] cellColumns, cellRows, stepOffset;
        public MapGeometry Geometry { get; }
        public double SeaLevel { get; }
        public double MaxSlopeDegrees { get; }
        public IReadOnlyList<double> Heights { get; }
        public IReadOnlyList<bool> Known { get; }
        public IReadOnlyList<bool> Land { get; }
        public IReadOnlyList<bool> Water { get; }

        public TerrainCostGrid(MapGeometry geometry, double[] heights, bool[] known, double seaLevel = 0, double maxSlopeDegrees = 35)
        {
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));
            if (heights == null || known == null || heights.Length != geometry.Count || known.Length != geometry.Count)
                throw new ArgumentException("Sample counts must match geometry.");
            if (!ModelSettings.Finite(seaLevel) || !ModelSettings.Finite(maxSlopeDegrees) || maxSlopeDegrees < 0 || maxSlopeDegrees >= 90)
                throw new ArgumentOutOfRangeException(nameof(maxSlopeDegrees));
            Geometry = geometry;
            SeaLevel = seaLevel; MaxSlopeDegrees = maxSlopeDegrees;
            this.heights = (double[])heights.Clone();
            this.known = (bool[])known.Clone();
            land = new bool[geometry.Count]; water = new bool[geometry.Count];
            maximumSlope = Math.Tan(maxSlopeDegrees * Math.PI / 180);
            for (int i = 0; i < geometry.Count; i++)
            {
                this.known[i] &= ModelSettings.Finite(this.heights[i]);
                land[i] = this.known[i] && this.heights[i] > seaLevel;
                water[i] = this.known[i] && !land[i];
            }
            // The sampled terrain is immutable: validate cell transitions once, not per raster pixel.
            landSteps = new ushort[geometry.Count]; waterSteps = new ushort[geometry.Count];
            centerX = new double[geometry.Count]; centerZ = new double[geometry.Count];
            cellColumns = new int[geometry.Count]; cellRows = new int[geometry.Count];
            stepLength = new double[9]; stepOffset = new int[9];
            for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
            {
                int k = (dz + 1) * 3 + dx + 1;
                stepOffset[k] = dz * geometry.Columns + dx;
                stepLength[k] = Math.Sqrt(dx * dx * geometry.CellX * geometry.CellX + dz * dz * geometry.CellZ * geometry.CellZ);
            }
            for (int i = 0; i < geometry.Count; i++)
            {
                int col = i % geometry.Columns, row = i / geometry.Columns;
                cellColumns[i] = col; cellRows[i] = row;
                centerX[i] = geometry.X(col); centerZ[i] = geometry.Z(row);
                for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (col + dx < 0 || col + dx >= geometry.Columns || row + dz < 0 || row + dz >= geometry.Rows) continue;
                    int k = (dz + 1) * 3 + dx + 1, next = i + stepOffset[k];
                    if (UncachedCanStep(i, next, false)) landSteps[i] |= (ushort)(1 << k);
                    if (UncachedCanStep(i, next, true)) waterSteps[i] |= (ushort)(1 << k);
                }
            }
            Heights = Array.AsReadOnly(this.heights); Known = Array.AsReadOnly(this.known);
            Land = Array.AsReadOnly(land); Water = Array.AsReadOnly(water);
        }

        public int CellIndex(double x, double z)
        {
            int col = ColumnIndex(x), row = RowIndex(z);
            return col < 0 || row < 0 ? -1 : row * Geometry.Columns + col;
        }
        internal int ColumnIndex(double x)
        {
            var g = Geometry;
            return !ModelSettings.Finite(x) || x < g.MinX || x >= g.MinX + g.Width ? -1 : Math.Min(g.Columns - 1, (int)((x - g.MinX) / g.CellX));
        }
        internal int RowIndex(double z)
        {
            var g = Geometry;
            return !ModelSettings.Finite(z) || z < g.MinZ || z >= g.MinZ + g.Height ? -1 : Math.Min(g.Rows - 1, (int)((z - g.MinZ) / g.CellZ));
        }

        bool Traversable(int a, int b, double distance, bool waterDomain)
        {
            var domain = waterDomain ? water : land;
            return domain[a] && domain[b] && (waterDomain || Math.Abs(heights[b] - heights[a]) <= maximumSlope * distance);
        }

        // All virtual source/target edges obey the same conservative cell transitions as graph edges.
        bool UncachedCanStep(int a, int b, bool waterDomain)
        {
            var g = Geometry;
            int col = a % g.Columns, row = a / g.Columns;
            int dx = b % g.Columns - col, dz = b / g.Columns - row;
            if (Math.Abs(dx) > 1 || Math.Abs(dz) > 1) return false;
            if (dx == 0 && dz == 0) return (waterDomain ? water : land)[a];
            double distance = Math.Sqrt(dx * dx * g.CellX * g.CellX + dz * dz * g.CellZ * g.CellZ);
            if (!Traversable(a, b, distance, waterDomain)) return false;
            if (dx == 0 || dz == 0) return true;
            int horizontal = row * g.Columns + col + dx, vertical = (row + dz) * g.Columns + col;
            return Traversable(a, horizontal, g.CellX, waterDomain) && Traversable(a, vertical, g.CellZ, waterDomain)
                && Traversable(horizontal, b, g.CellZ, waterDomain) && Traversable(vertical, b, g.CellX, waterDomain);
        }

        /// <summary>
        /// Sample a completed center-distance field. Same/adjacent source cells use a direct segment;
        /// other targets connect to reachable neighboring centers. Each segment checks domain, slope
        /// and diagonal corners. Beyond those local segments this remains an eight-neighbor upper-bound
        /// path approximation, rather than an exact continuous terrain shortest path.
        /// </summary>
        public double DistanceAt(double x, double z, double sourceX, double sourceZ, bool waterDomain, double[] distances)
        {
            if (distances == null || distances.Length != Geometry.Count) throw new ArgumentException("Distance buffer must match geometry.");
            return DistanceAt(x, z, sourceX, sourceZ, waterDomain, distances, CellIndex(x, z), CellIndex(sourceX, sourceZ));
        }

        // Caller precomputes source/target indices and validates the per-job distance buffer once.
        internal double DistanceAt(double x, double z, double sourceX, double sourceZ, bool waterDomain, double[] distances, int target, int source)
        {
            var steps = waterDomain ? waterSteps : landSteps;
            if (target < 0 || source < 0 || (steps[source] & 16) == 0 || (steps[target] & 16) == 0) return double.PositiveInfinity;
            int sx = cellColumns[target] - cellColumns[source], sz = cellRows[target] - cellRows[source];
            double dx, dz;
            if (sx >= -1 && sx <= 1 && sz >= -1 && sz <= 1 && (steps[source] & (1 << ((sz + 1) * 3 + sx + 1))) != 0)
            {
                dx = x - sourceX; dz = z - sourceZ;
                return Math.Sqrt(dx * dx + dz * dz);
            }
            double best = double.PositiveInfinity;
            int mask = steps[target];
            for (int k = 0; k < 9; k++)
            {
                if ((mask & (1 << k)) == 0) continue;
                int cell = target + stepOffset[k];
                if (double.IsPositiveInfinity(distances[cell])) continue;
                dx = x - centerX[cell]; dz = z - centerZ[cell];
                best = Math.Min(best, distances[cell] + Math.Sqrt(dx * dx + dz * dz));
            }
            return best;
        }

        /// <summary>Consume all chunks before reading the final field. Each yield bounds reset/search operations to 128.</summary>
        public IEnumerable<int> ComputeDistances(double x, double z, double radius, bool water, double[] output)
        {
            if (output == null || output.Length != Geometry.Count) throw new ArgumentException("Distance buffer must match geometry.");
            int work = 0;
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = double.PositiveInfinity;
                if (++work == 128) { yield return work; work = 0; }
            }
            int source = CellIndex(x, z);
            if (source < 0 || !ModelSettings.Finite(radius) || radius < 0 || !(water ? this.water[source] : land[source]))
            { if (work != 0) yield return work; yield break; }
            var heap = new MinHeap();
            var steps = water ? waterSteps : landSteps;
            int sourceMask = steps[source];
            // The source is a virtual vertex, not a forced visit to its containing cell center.
            for (int k = 0; k < 9; k++)
            {
                if (++work == 128) { yield return work; work = 0; }
                if ((sourceMask & (1 << k)) == 0) continue;
                int cell = source + stepOffset[k];
                double sx = centerX[cell] - x, sz = centerZ[cell] - z;
                double initial = Math.Sqrt(sx * sx + sz * sz);
                if (initial > radius) continue;
                output[cell] = initial; heap.Push(cell, initial);
            }
            while (heap.Count > 0)
            {
                Node current = heap.Pop();
                if (++work == 128) { yield return work; work = 0; }
                if (current.Distance != output[current.Cell]) continue;
                int mask = steps[current.Cell];
                for (int k = 0; k < 9; k++)
                {
                    if (++work == 128) { yield return work; work = 0; }
                    if (k == 4 || (mask & (1 << k)) == 0) continue;
                    int next = current.Cell + stepOffset[k];
                    double distance = stepLength[k];
                    double candidate = current.Distance + distance;
                    if (candidate > radius || candidate >= output[next]) continue;
                    output[next] = candidate; heap.Push(next, candidate);
                }
            }
            if (work != 0) yield return work;
        }

        struct Node
        {
            public int Cell;
            public double Distance;
            public Node(int cell, double distance) { Cell = cell; Distance = distance; }
        }
        // Small runtime-compatible heap; Unity's .NET profile lacks PriorityQueue.
        sealed class MinHeap
        {
            readonly List<Node> nodes = new List<Node>();
            public int Count => nodes.Count;
            public void Push(int cell, double distance)
            {
                var node = new Node(cell, distance);
                int index = nodes.Count; nodes.Add(node);
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (nodes[parent].Distance <= distance) break;
                    nodes[index] = nodes[parent]; index = parent;
                }
                nodes[index] = node;
            }
            public Node Pop()
            {
                Node result = nodes[0], last = nodes[nodes.Count - 1];
                nodes.RemoveAt(nodes.Count - 1);
                if (nodes.Count == 0) return result;
                int index = 0;
                while (index * 2 + 1 < nodes.Count)
                {
                    int child = index * 2 + 1;
                    if (child + 1 < nodes.Count && nodes[child + 1].Distance < nodes[child].Distance) child++;
                    if (last.Distance <= nodes[child].Distance) break;
                    nodes[index] = nodes[child]; index = child;
                }
                nodes[index] = last;
                return result;
            }
        }
    }
}
