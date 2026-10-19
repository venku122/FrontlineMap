using Xunit;
using FrontlineMap.Core;
using FrontlineMap.Runtime;

public class NumericalEquivalenceTests
{
    [Fact]
    public void CachedSamplerMatchesRetainedReferenceAcrossEdgesAndRandomValues()
    {
        var random = new Random(4153);
        foreach (var dimensions in new[] { (2, 2), (3, 17), (31, 7), (64, 64) })
        foreach (double opacity in new[] { 0, .16, .5, 1 })
        {
            var g = new MapGeometry(-123, 456, 12000, 791, dimensions.Item1, dimensions.Item2);
            var field = new InfluenceBuffers(g);
            var settings = new ModelSettings(64, 3000, 5, opacity, .01, 1e-9, 2, 15);
            for (int i = 0; i < g.Count; i++)
            {
                // Zero, threshold neighbours, flat balanced cells, dense support and contours.
                double value = i % 7 == 0 ? 0 : i % 7 == 1 ? .01 : random.NextDouble() * 10000;
                field.Friendly[i] = value;
                field.Enemy[i] = i % 5 == 0 ? value : random.NextDouble() * 10000;
            }
            foreach (var viewport in new[] { (800.0, 800.0), (3600.0, 741.0), (.001, 1e7) })
            {
                var sampler = new Rasterizer.Sampler(field, settings, viewport.Item1, viewport.Item2);
                var rendered = Rasterizer.Render(field, settings, viewport.Item1, viewport.Item2);
                for (int i = 0; i < g.Count; i++)
                {
                    var expected = RetainedRasterReference.Sample(field, settings, i, viewport.Item1, viewport.Item2);
                    Assert.Equal(expected, sampler.Sample(i));
                    Assert.Equal(expected, rendered[i]);
                    Assert.Equal(expected, Rasterizer.Sample(field, settings, i, viewport.Item1, viewport.Item2));
                }
            }
        }
    }

    [Fact]
    public void CachedSamplerRejectsInvalidViewportBeforeSampling()
    {
        var field = new InfluenceBuffers(new MapGeometry(0, 0, 1, 1, 2, 2));
        foreach (double invalid in new[] { 0, -1, double.NaN, double.PositiveInfinity })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rasterizer.Sampler(field, ScalarFieldTests.Settings(), invalid, 800));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Rasterizer.Sampler(field, ScalarFieldTests.Settings(), 800, invalid));
        }
    }

    [Fact]
    public void SchedulerMatchesFullGridOracleForTinyRectangularClippedAndDenseFields()
    {
        var random = new Random(15153);
        foreach (var g in new[] {
            new MapGeometry(-4, -4, 8, 8, 2, 2),
            new MapGeometry(100, -31, 101, 7, 31, 7),
            new MapGeometry(-6000, -6000, 12000, 12000, 64, 64),
            new MapGeometry(1e15, -1e15, 1000, 2000, 31, 17),
            new MapGeometry(1e15, -1e15, 8, 16, 64, 31) })
        foreach (double radius in new[] { .001, g.CellX * .5, g.CellX, g.Width / 3, g.Width * 2 })
        {
            var settings = ScalarFieldTests.Settings(64, radius);
            var stamps = new List<ObservationStamp>();
            for (int i = 0; i < 97; i++)
                stamps.Add(ScalarFieldTests.Stamp(i.ToString("D3"), i % 2 == 0 ? Side.Friendly : Side.Enemy,
                    g.MinX + (random.NextDouble() * 1.4 - .2) * g.Width,
                    g.MinZ + (random.NextDouble() * 1.4 - .2) * g.Height, random.NextDouble() * 10000));
            // Strict radius boundaries, adjacent representable coordinates and rectangle corners.
            foreach (double x in new[] { g.MinX - radius, g.X(0) + radius, Math.BitIncrement(g.X(0) + radius), Math.BitDecrement(g.X(0) + radius), g.MinX + g.Width + radius })
                stamps.Add(ScalarFieldTests.Stamp("edge" + stamps.Count, Side.Enemy, x, g.Z(0)));
            var snapshot = new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, 1), g, stamps, 0, 0, 15);
            using var scheduler = new StepScheduler(() => 0);
            scheduler.ChunkCompleted = (_, cells) => Assert.InRange(cells, 0, 128);
            scheduler.Begin(snapshot, settings, 3600, 791);
            Assert.Equal(StepOutcome.Complete, scheduler.Step(1));
            Assert.True(scheduler.TryTakeCompleted(out var result));
            var expected = ScalarField.Compute(snapshot, settings);
            Assert.Equal(expected.Friendly, result.Field.Friendly);
            Assert.Equal(expected.Enemy, result.Field.Enemy);
            for (int i = 0; i < g.Count; i++)
                Assert.Equal(RetainedRasterReference.Sample(expected, settings, i, 3600, 791), result.Pixels[i]);
        }
    }
}

// Retained pre-optimization algorithm, intentionally independent of the cached sampler.
internal static class RetainedRasterReference
{
    private static CellClass Classify(double f, double e, ModelSettings s)
    {
        if (!double.IsFinite(f) || !double.IsFinite(e) || f < 0 || e < 0) throw new ArgumentOutOfRangeException("support");
        if (Math.Max(f, e) < s.SupportThreshold) return CellClass.Unsupported;
        if (f >= s.SupportThreshold && e >= s.SupportThreshold && Math.Abs(f - e) <= s.Epsilon) return CellClass.Balanced;
        return f > e ? CellClass.Friendly : CellClass.Enemy;
    }
        public static Pixel Sample(InfluenceBuffers field, ModelSettings settings, int index, double screenWidth, double screenHeight)
        {
            var g = field.Geometry;
            if (!ModelSettings.Positive(screenWidth) || !ModelSettings.Positive(screenHeight))
                throw new ArgumentOutOfRangeException("viewport");
            double f = field.Friendly[index], e = field.Enemy[index], d = f - e;
            var kind = Classify(f, e, settings);
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
