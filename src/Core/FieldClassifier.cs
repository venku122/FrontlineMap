using System;
namespace FrontlineMap.Core
{
    public enum CellClass
    {
        Unsupported, Friendly, Enemy, Balanced
    }
    public static class FieldClassifier
    {
        public static CellClass Classify(double friendly, double enemy, ModelSettings settings)
        {
            if (!ModelSettings.Finite(friendly) || !ModelSettings.Finite(enemy) || friendly < 0 || enemy < 0)
                throw new ArgumentOutOfRangeException("support");
            if (Math.Max(friendly, enemy) < settings.SupportThreshold)
                return CellClass.Unsupported;
            if (friendly >= settings.SupportThreshold && enemy >= settings.SupportThreshold &&
                Math.Abs(friendly - enemy) <= settings.Epsilon)
                return CellClass.Balanced;
            return friendly > enemy ? CellClass.Friendly : CellClass.Enemy;
        }
    }
    public enum PixelKind
    {
        Unsupported, Friendly, Enemy, Balanced, Front
    }
    public struct Pixel
    {
        public readonly byte R, G, B, A;
        public readonly PixelKind Kind;
        public Pixel(byte r, byte g, byte b, byte a, PixelKind kind)
        {
            R = r;
            G = g;
            B = b;
            A = a;
            Kind = kind;
        }
    }
    public static class Rasterizer
    {
        public static Pixel[] Render(InfluenceBuffers field, ModelSettings settings, double screenWidth, double screenHeight)
        {
            var pixels = new Pixel[field.Geometry.Count];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Sample(field, settings, i, screenWidth, screenHeight);
            return pixels;
        }
        public static Pixel Sample(InfluenceBuffers field, ModelSettings settings, int index, double screenWidth, double screenHeight)
        {
            var g = field.Geometry;
            if (!ModelSettings.Positive(screenWidth) || !ModelSettings.Positive(screenHeight))
                throw new ArgumentOutOfRangeException("viewport");
            double f = field.Friendly[index], e = field.Enemy[index], d = f - e;
            var kind = FieldClassifier.Classify(f, e, settings);
            if (kind == CellClass.Unsupported)
                return default(Pixel);
            int x = index % g.Columns, z = index / g.Columns;
            int left = index - (x > 0 ? 1 : 0), right = index + (x < g.Columns - 1 ? 1 : 0);
            int down = index - (z > 0 ? g.Columns : 0), up = index + (z < g.Rows - 1 ? g.Columns : 0);
            double dx = (field.Friendly[right] - field.Enemy[right] - field.Friendly[left] + field.Enemy[left]) /
                ((right - left) * g.CellX);
            double dz = (field.Friendly[up] - field.Enemy[up] - field.Friendly[down] + field.Enemy[down]) /
                (((up - down) / g.Columns) * g.CellZ);
            // Gradient in screen pixels, no magnitude cull and no density-dependent dash rule.
            double gradient = Math.Sqrt(Math.Pow(dx * g.Width / screenWidth, 2) + Math.Pow(dz * g.Height / screenHeight, 2));
            // Both supports required at this sample: support edges can never become fronts.
            if (f >= settings.SupportThreshold && e >= settings.SupportThreshold && gradient > settings.Epsilon)
            {
                // Conservative subpixel coverage prevents a narrow front falling between coarse texel centers.
                // Bilinear raster has a finite sampling limit; this preserves coverage, not crispness at arbitrary zoom.
                double halfTexel = .5 * (Math.Abs(dx) * g.CellX + Math.Abs(dz) * g.CellZ) / gradient;
                double coverage = Math.Max(0, Math.Min(1, (settings.LineWidthPixels / 2 + halfTexel - Math.Abs(d) / gradient) / (2 * halfTexel)));
                byte lineAlpha = (byte)Math.Round(230 * coverage);
                if (lineAlpha > 0)
                    return new Pixel(240, 225, 155, lineAlpha, PixelKind.Front);
            }
            byte alpha = (byte)Math.Round(255 * settings.Opacity);
            if (kind == CellClass.Balanced)
                return new Pixel(177, 130, 215, alpha, PixelKind.Balanced);
            return kind == CellClass.Friendly ? new Pixel(65, 155, 245, alpha, PixelKind.Friendly) : new Pixel(240, 90, 70, alpha, PixelKind.Enemy);
        }
    }
}
