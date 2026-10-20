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
        public MapGeometry Geometry { get; }
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
            Heights = Array.AsReadOnly(this.heights); Known = Array.AsReadOnly(this.known);
            Land = Array.AsReadOnly(land); Water = Array.AsReadOnly(water);
        }

        public int CellIndex(double x, double z)
        {
            var g = Geometry;
            if (!ModelSettings.Finite(x) || !ModelSettings.Finite(z) || x < g.MinX || z < g.MinZ || x >= g.MinX + g.Width || z >= g.MinZ + g.Height) return -1;
            return Math.Min(g.Rows - 1, (int)((z - g.MinZ) / g.CellZ)) * g.Columns + Math.Min(g.Columns - 1, (int)((x - g.MinX) / g.CellX));
        }

        bool Traversable(int a, int b, double distance, bool waterDomain)
        {
            var domain = waterDomain ? water : land;
            return domain[a] && domain[b] && (waterDomain || Math.Abs(heights[b] - heights[a]) <= maximumSlope * distance);
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
            var g = Geometry;
            double dx0 = g.MinX + (source % g.Columns + .5) * g.CellX - x;
            double dz0 = g.MinZ + (source / g.Columns + .5) * g.CellZ - z;
            double initial = Math.Sqrt(dx0 * dx0 + dz0 * dz0);
            if (initial > radius) { if (work != 0) yield return work; yield break; }
            var heap = new MinHeap();
            output[source] = initial; heap.Push(source, initial);
            while (heap.Count > 0)
            {
                Node current = heap.Pop();
                if (++work == 128) { yield return work; work = 0; }
                if (current.Distance != output[current.Cell]) continue;
                int col = current.Cell % g.Columns, row = current.Cell / g.Columns;
                for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (++work == 128) { yield return work; work = 0; }
                    if ((dx == 0 && dz == 0) || col + dx < 0 || col + dx >= g.Columns || row + dz < 0 || row + dz >= g.Rows) continue;
                    int next = (row + dz) * g.Columns + col + dx;
                    double distance = Math.Sqrt(dx * dx * g.CellX * g.CellX + dz * dz * g.CellZ * g.CellZ);
                    if (!Traversable(current.Cell, next, distance, water)) continue;
                    if (dx != 0 && dz != 0)
                    {
                        int horizontal = row * g.Columns + col + dx, vertical = (row + dz) * g.Columns + col;
                        if (!Traversable(current.Cell, horizontal, g.CellX, water) || !Traversable(current.Cell, vertical, g.CellZ, water)
                            || !Traversable(horizontal, next, g.CellZ, water) || !Traversable(vertical, next, g.CellX, water)) continue;
                    }
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
