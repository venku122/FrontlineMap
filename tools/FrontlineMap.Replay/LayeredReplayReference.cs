namespace FrontlineMap.Replay;

internal readonly record struct ReplayPixel(byte R, byte G, byte B, byte A, string Kind);
internal sealed class ReplayReferenceResult
{
    // Friendly, Enemy, RawFriendly, RawEnemy, Confidence, in that order.
    public double[][] Buffers;
    public ReplayPixel[] Pixels;
}

// Full-raster oracle. It uses raw parsed fixtures, its own explicit terrain graph and .NET's
// PriorityQueue; it never invokes production terrain/model/profile/confidence/classifier code.
internal static class LayeredReplayReference
{
    sealed class Graph
    {
        readonly ReplayTerrain terrain;
        readonly bool[] allowed;
        readonly List<(int Cell, double Length)>[] edges;
        public ReplayGrid Grid => terrain.Grid;
        public Graph(ReplayTerrain terrain, bool naval)
        {
            this.terrain = terrain;
            int count = Grid.Count;
            allowed = new bool[count]; edges = new List<(int, double)>[count];
            for (int i = 0; i < count; i++)
            {
                allowed[i] = terrain.Known[i] && (naval ? terrain.Heights[i] <= terrain.SeaLevel : terrain.Heights[i] > terrain.SeaLevel);
                edges[i] = new List<(int, double)>();
            }
            double slope = Math.Tan(terrain.SlopeDegrees * Math.PI / 180);
            bool Cross(int a, int b, double length) => allowed[a] && allowed[b]
                && (naval || Math.Abs(terrain.Heights[a] - terrain.Heights[b]) <= slope * length);
            for (int a = 0; a < count; a++)
            {
                if (!allowed[a]) continue;
                int ax = a % Grid.Columns, az = a / Grid.Columns;
                for (int bz = Math.Max(0, az - 1); bz <= Math.Min(Grid.Rows - 1, az + 1); bz++)
                for (int bx = Math.Max(0, ax - 1); bx <= Math.Min(Grid.Columns - 1, ax + 1); bx++)
                {
                    int b = bz * Grid.Columns + bx;
                    if (a == b) continue;
                    double dx = (bx - ax) * Grid.CellX, dz = (bz - az) * Grid.CellZ;
                    double length = Math.Sqrt(dx * dx + dz * dz);
                    if (!Cross(a, b, length)) continue;
                    if (bx != ax && bz != az)
                    {
                        int h = az * Grid.Columns + bx, v = bz * Grid.Columns + ax;
                        if (!Cross(a, h, Grid.CellX) || !Cross(a, v, Grid.CellZ) || !Cross(h, b, Grid.CellZ) || !Cross(v, b, Grid.CellX)) continue;
                    }
                    edges[a].Add((b, length));
                }
            }
        }
        double Length(double x, double z, int center)
        {
            double dx = x - Grid.X(center % Grid.Columns), dz = z - Grid.Z(center / Grid.Columns);
            return Math.Sqrt(dx * dx + dz * dz);
        }
        public double[] Search(ReplayStamp source, double radius)
        {
            var distance = Enumerable.Repeat(double.PositiveInfinity, Grid.Count).ToArray();
            int start = Grid.Locate(source.X, source.Z);
            if (start < 0 || !allowed[start]) return distance;
            var queue = new PriorityQueue<int, double>();
            void Seed(int cell)
            {
                double length = Length(source.X, source.Z, cell);
                if (length <= radius) { distance[cell] = length; queue.Enqueue(cell, length); }
            }
            Seed(start);
            foreach (var edge in edges[start]) Seed(edge.Cell);
            while (queue.TryDequeue(out int a, out double cost))
            {
                if (cost > distance[a]) continue;
                foreach (var edge in edges[a])
                {
                    double candidate = cost + edge.Length;
                    if (candidate <= radius && candidate < distance[edge.Cell])
                    { distance[edge.Cell] = candidate; queue.Enqueue(edge.Cell, candidate); }
                }
            }
            return distance;
        }
        public double Sample(ReplayStamp source, double x, double z, double[] distance)
        {
            int start = Grid.Locate(source.X, source.Z), end = Grid.Locate(x, z);
            if (start < 0 || end < 0 || !allowed[start] || !allowed[end]) return double.PositiveInfinity;
            if (start == end || edges[start].Any(e => e.Cell == end))
            {
                double dx = x - source.X, dz = z - source.Z;
                return Math.Sqrt(dx * dx + dz * dz);
            }
            double best = distance[end] + Length(x, z, end);
            foreach (var edge in edges[end]) best = Math.Min(best, distance[edge.Cell] + Length(x, z, edge.Cell));
            return best;
        }
    }
    static (double Radius, double Weight) Profile(ReplayStamp s, ReplayModel m)
    {
        double radius = 0, weight = s.Weight;
        if (m.Layer == "GroundControl" || m.Layer == "Confidence")
        {
            if (s.Role == "GroundVehicle" || s.Role == "AirDefense") radius = m.GroundRadius;
            if (s.Role == "Airbase") radius = m.BaseRadius;
            if (s.Role == "Building") { radius = m.BuildingRadius; weight *= m.BuildingControlFactor; }
        }
        else if (m.Layer == "AirThreat")
        {
            if (s.Role == "Aircraft") radius = m.AirRadius;
            if (s.Role == "AirDefense") radius = m.AirDefenseRadius;
        }
        else if (m.Layer == "NavalInfluence" && s.Role == "Ship") radius = m.NavalRadius;
        return (radius, weight);
    }
    public static ReplayReferenceResult Compute(ReplayInput input)
    {
        var result = new ReplayReferenceResult { Buffers = Enumerable.Range(0, 5).Select(_ => new double[input.Grid.Count]).ToArray(), Pixels = new ReplayPixel[input.Grid.Count] };
        var b = result.Buffers; var m = input.Model; var g = input.Grid;
        var graph = input.Paths ? new Graph(input.Terrain, m.Layer == "NavalInfluence") : null;
        foreach (var stamp in input.Stamps)
        {
            var (radius, weight) = Profile(stamp, m);
            if (radius <= 0 || weight <= 0) continue;
            double freshness = stamp.Stale ? Math.Pow(.5, Math.Max(0, input.SimulationTime - stamp.Observed) / m.StaleHalfLife) : 1;
            double[] distance = graph?.Search(stamp, radius);
            int side = stamp.Side == "friendly" ? 0 : 1;
            for (int row = 0; row < g.Rows; row++) for (int col = 0; col < g.Columns; col++)
            {
                double dx = g.X(col) - stamp.X, dz = g.Z(row) - stamp.Z;
                double squared = dx * dx + dz * dz;
                if (graph != null) { double path = graph.Sample(stamp, g.X(col), g.Z(row), distance); squared = path * path; }
                if (squared >= radius * radius) continue;
                int index = row * g.Columns + col;
                double contribution = weight * (1 - squared / (radius * radius));
                b[side + 2][index] += contribution; b[side][index] += contribution * freshness;
            }
        }
        for (int i = 0; i < g.Count; i++)
        {
            double raw = b[2][i] + b[3][i];
            b[4][i] = raw > 0 ? Math.Clamp((b[0][i] + b[1][i]) / raw, 0, 1) : 0;
            double f = b[0][i], e = b[1][i], confidence = b[4][i];
            if (Math.Max(f, e) < m.SupportThreshold) { result.Pixels[i] = new ReplayPixel(0, 0, 0, 0, "Unsupported"); continue; }
            byte alpha = (byte)Math.Round(255 * m.Opacity * Math.Max(.15, confidence));
            if (m.Layer == "Confidence") result.Pixels[i] = new ReplayPixel((byte)Math.Round(240 * (1 - confidence)), (byte)Math.Round(160 + 80 * confidence), 70, (byte)Math.Round(255 * m.Opacity), "Confidence");
            else if (f >= m.SupportThreshold && e >= m.SupportThreshold && Math.Abs(f - e) / (f + e) <= m.ContestedRatio) result.Pixels[i] = new ReplayPixel(177, 130, 215, alpha, "Balanced");
            else result.Pixels[i] = f > e ? new ReplayPixel(65, 155, 245, alpha, "Friendly") : new ReplayPixel(240, 90, 70, alpha, "Enemy");
        }
        return result;
    }
}
