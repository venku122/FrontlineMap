using System;
using System.Collections.Generic;

namespace FrontlineMap.Core
{
    public struct ContourPoint
    {
        public readonly double U, V;
        public ContourPoint(double u, double v) { U = u; V = v; }
    }

    public struct ContourSegment
    {
        public readonly ContourPoint A, B;
        public readonly double Confidence;
        public ContourSegment(ContourPoint a, ContourPoint b, double confidence)
        { A = a; B = b; Confidence = confidence; }
    }

    /// <summary>Zero contours of cell-center friendly minus enemy support, in map UV.</summary>
    public static class FrontContours
    {
        public static List<ContourSegment> Extract(InfluenceBuffers field, double supportThreshold,
            double epsilon, double[] confidence = null, double minimumConfidence = 0)
        {
            var output = new List<ContourSegment>();
            foreach (int work in ExtractSteps(field, supportThreshold, epsilon, output, confidence, minimumConfidence)) { }
            return output;
        }

        /// <summary>Appends contours incrementally, yielding the number of cells processed (at most 128).</summary>
        public static IEnumerable<int> ExtractSteps(InfluenceBuffers field, double supportThreshold,
            double epsilon, List<ContourSegment> output, double[] confidence = null, double minimumConfidence = 0)
        {
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (!Finite(supportThreshold) || supportThreshold < 0) throw new ArgumentOutOfRangeException(nameof(supportThreshold));
            if (!Finite(epsilon) || epsilon < 0) throw new ArgumentOutOfRangeException(nameof(epsilon));
            if (!Finite(minimumConfidence) || minimumConfidence < 0 || minimumConfidence > 1)
                throw new ArgumentOutOfRangeException(nameof(minimumConfidence));
            if (confidence != null && confidence.Length != field.Geometry.Count)
                throw new ArgumentException("Confidence must match field dimensions.", nameof(confidence));

            int work = 0;
            var g = field.Geometry;
            // Reuse fixed scratch for the full extraction; no per-cell heap allocations.
            var indices = new int[4];
            var values = new double[4];
            var points = new ContourPoint[4];
            var edgeConfidence = new double[4];
            var eligible = new bool[4];
            var crossings = new int[4];
            for (int z = 0; z < g.Rows - 1; z++)
                for (int x = 0; x < g.Columns - 1; x++)
                {
                    if (work == 128) { yield return work; work = 0; }
                    work++;
                    // Counterclockwise: bottom-left, bottom-right, top-right, top-left.
                    indices[0] = z * g.Columns + x;
                    indices[1] = indices[0] + 1;
                    indices[2] = indices[1] + g.Columns;
                    indices[3] = indices[0] + g.Columns;
                    bool valid = true;
                    for (int i = 0; i < 4; i++)
                    {
                        int index = indices[i];
                        double f = field.Friendly[index], e = field.Enemy[index];
                        double c = confidence == null ? 1 : confidence[index];
                        if (!Finite(f) || !Finite(e) || f < 0 || e < 0 || !Finite(c) || c < 0 || c > 1)
                        { valid = false; break; }
                        double value = f - e;
                        values[i] = Math.Abs(value) <= epsilon ? 0 : value;
                    }
                    if (!valid) continue;
                    int count = 0;
                    for (int edge = 0; edge < 4; edge++)
                    {
                        int next = (edge + 1) % 4;
                        // Zero consistently belongs to the positive side; a zero plateau
                        // has no crossings. Coincident zero-corner touches are removed below.
                        if ((values[edge] < 0) == (values[next] < 0)) continue;
                        double a = Math.Abs(values[edge]), b = Math.Abs(values[next]);
                        double scale = Math.Max(a, b);
                        double t = (a / scale) / (a / scale + b / scale);
                        int ia = indices[edge], ib = indices[next];
                        double friendly = Lerp(field.Friendly[ia], field.Friendly[ib], t);
                        double enemy = Lerp(field.Enemy[ia], field.Enemy[ib], t);
                        double c = confidence == null ? 1 : Lerp(confidence[ia], confidence[ib], t);
                        points[edge] = new ContourPoint(
                            Lerp((ia % g.Columns + .5) / g.Columns, (ib % g.Columns + .5) / g.Columns, t),
                            Lerp((ia / g.Columns + .5) / g.Rows, (ib / g.Columns + .5) / g.Rows, t));
                        edgeConfidence[edge] = c;
                        eligible[edge] = friendly >= supportThreshold && enemy >= supportThreshold && c >= minimumConfidence;
                        crossings[count++] = edge;
                    }
                    if (count == 2)
                        Add(output, points, edgeConfidence, eligible, crossings[0], crossings[1]);
                    else if (count == 4)
                    {
                        // Asymptotic decider for the bilinear saddle. Scale first so
                        // very large finite support cannot overflow the determinant.
                        double scale = 0;
                        for (int i = 0; i < 4; i++) scale = Math.Max(scale, Math.Abs(values[i]));
                        double determinant = (values[0] / scale) * (values[2] / scale)
                            - (values[1] / scale) * (values[3] / scale);
                        // Exact ties consistently pair bottom/right and top/left.
                        if (determinant >= 0)
                        { Add(output, points, edgeConfidence, eligible, 0, 1); Add(output, points, edgeConfidence, eligible, 2, 3); }
                        else
                        { Add(output, points, edgeConfidence, eligible, 0, 3); Add(output, points, edgeConfidence, eligible, 1, 2); }
                    }
                }
            if (work > 0) yield return work;
        }

        private static void Add(List<ContourSegment> output, ContourPoint[] points,
            double[] confidence, bool[] eligible, int a, int b)
        {
            if (!eligible[a] || !eligible[b]) return;
            var start = points[a]; var end = points[b];
            if (!Finite(start.U) || !Finite(start.V) || !Finite(end.U) || !Finite(end.V)
                || (start.U == end.U && start.V == end.V)) return;
            output.Add(new ContourSegment(start, end, Math.Min(confidence[a], confidence[b])));
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
