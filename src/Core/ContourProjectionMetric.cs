using System;
namespace FrontlineMap.Core
{
    // For an affine local-to-screen map A, A^T A determines every projected
    // length and hence the local offsets of a constant-pixel-width stroke.
    // Screen translation and left multiplication by a rotation do not change it.
    public readonly struct ContourProjectionMetric
    {
        readonly double xLengthSquared, cross, yLengthSquared, minimumStretchSquared;
        readonly bool reflected;
        ContourProjectionMetric(double x, double xy, double y, double minimumStretchSquared, bool reflected)
        { xLengthSquared = x; cross = xy; yLengthSquared = y; this.minimumStretchSquared = minimumStretchSquared; this.reflected = reflected; }
        public static bool TryCreate(double xx, double xy, double yx, double yy,
            double wx, double wy, double w, out ContourProjectionMetric metric)
        {
            metric = default;
            // A tilted perspective plane has position-dependent scale. Do not
            // approximate it from a few sampled points; use full invalidation.
            if (wx != 0 || wy != 0 || !Finite(w) || w == 0) return false;
            xx /= w; xy /= w; yx /= w; yy /= w;
            double x = xx*xx + yx*yx, y = xy*xy + yy*yy, dot = xx*xy + yx*yy;
            double determinant = xx*yy - xy*yx;
            if (!Finite(x) || !Finite(y) || !Finite(dot) || !Finite(determinant)
                || x <= 0 || y <= 0 || Math.Abs(determinant) <= 1e-10 * Math.Sqrt(x*y)) return false;
            double maximumStretchSquared = .5 * (x+y + Math.Sqrt((x-y)*(x-y) + 4*dot*dot));
            double minimumStretchSquared = determinant * determinant / maximumStretchSquared;
            if (!Finite(minimumStretchSquared) || minimumStretchSquared <= 0) return false;
            metric = new ContourProjectionMetric(x, dot, y, minimumStretchSquared, determinant < 0);
            return true;
        }
        public bool SameWidths(ContourProjectionMetric other)
        {
            // Relative to the last invalidation, so sub-tolerance zoom changes
            // eventually accumulate into a rebuild rather than being forgotten.
            // Bound error even in the least stretched direction; a large axis
            // must never hide zoom or shear on a much smaller axis.
            double tolerance = Math.Min(minimumStretchSquared, other.minimumStretchSquared) * 1e-5;
            return reflected == other.reflected
                && Math.Abs(xLengthSquared-other.xLengthSquared) <= tolerance
                && Math.Abs(yLengthSquared-other.yLengthSquared) <= tolerance
                && Math.Abs(cross-other.cross) <= tolerance;
        }
        static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
