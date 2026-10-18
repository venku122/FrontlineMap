using FrontlineMap.Core;
using FrontlineMap.Runtime;
using Xunit;
public class AdditionalInvariantTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    public void SameWorldLinearFrontKeepsScreenWidth(int n)
    {
        var g = new MapGeometry(-4, -4, 8, 8, n, n);
        var f = new InfluenceBuffers(g);
        for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                f.Friendly[z * n + x] = 100 + g.X(x);
                f.Enemy[z * n + x] = 100;
            }
        var p = Rasterizer.Render(f, ScalarFieldTests.Settings(n), 8, 8);
        int hits = p.Take(n).Count(v => v.Kind == PixelKind.Front);
        double width = hits * 8.0 / n;
        Assert.InRange(width, 2.9, 4.1);
    }
    [Fact]
    public void RepaintReusesWorldFieldAndPreservesCaptureTime()
    {
        var s = ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp());
        using var q = new StepScheduler(() => 0);
        q.Begin(s, ScalarFieldTests.Settings(), 800, 800);
        q.Step(1);
        Assert.True(q.TryTakeCompleted(out var old));
        q.BeginRepaint(old, ScalarFieldTests.Settings(), 400, 400);
        q.Step(1);
        Assert.True(q.TryTakeCompleted(out var next));
        Assert.Same(old.Field, next.Field);
        Assert.Equal(old.Snapshot.CapturedAt, next.Snapshot.CapturedAt);
        Assert.Equal(400, next.ScreenWidth);
    }
    [Fact]
    public void SaddlesAndZeroSamplesRemainFiniteAndDeterministic()
    {
        var g = new MapGeometry(-4, -4, 8, 8, 8, 8);
        var f = new InfluenceBuffers(g);
        for (int z = 0; z < 8; z++)
            for (int x = 0; x < 8; x++)
            {
                f.Friendly[z * 8 + x] = 100 + (x - 4) * (z - 4);
                f.Enemy[z * 8 + x] = 100;
            }
        var p = Rasterizer.Render(f, ScalarFieldTests.Settings(), 8, 8);
        Assert.Equal(p.Select(v => v.Kind), Rasterizer.Render(f, ScalarFieldTests.Settings(), 8, 8).Select(v => v.Kind));
        Assert.Equal(PixelKind.Balanced, p[36].Kind);
    }
    [Theory]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void SchedulerCompletesWithoutFrameCountDependency(int fps)
    {
        double now = 0;
        using var q = new StepScheduler(() => now);
        q.Begin(ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp()), ScalarFieldTests.Settings(), 800, 800);
        for (int frame = 0; frame < fps; frame++)
        {
            q.Step(now + .002);
            now += 1.0 / fps;
            if (q.TryTakeCompleted(out var result))
            {
                Assert.Equal(10, result.Field.Friendly[36]);
                return;
            }
        }
        Assert.Fail("Work starved");
    }

    [Fact]
    public void FieldConvergesAtSharedCellCentersUnderThreefoldRefinement()
    {
        var a = ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp(), ScalarFieldTests.Stamp("b", Side.Enemy, -.75, -1.25, 5));
        var b = new ObservationSnapshot(a.Identity, new MapGeometry(-4, -4, 8, 8, 24, 24), a.Stamps, 0, 0, 15);
        var coarse = ScalarField.Compute(a, ScalarFieldTests.Settings());
        var fine = ScalarField.Compute(b, ScalarFieldTests.Settings(24));
        for (int z = 0; z < 8; z++)
            for (int x = 0; x < 8; x++)
            {
                Assert.Equal(coarse.Friendly[z * 8 + x], fine.Friendly[(z * 3 + 1) * 24 + x * 3 + 1], 9);
                Assert.Equal(coarse.Enemy[z * 8 + x], fine.Enemy[(z * 3 + 1) * 24 + x * 3 + 1], 9);
            }
    }
    [Fact]
    public void SnapshotExportContainsOnlySanitizedValuesAndValidEscaping()
    {
        var s = ScalarFieldTests.Snapshot(ScalarFieldTests.Stamp("quote\"\n"));
        using var json = System.Text.Json.JsonDocument.Parse(FrontlineMap.Diagnostics.SnapshotWriter.ToJson(s, ScalarFieldTests.Settings()));
        Assert.Equal("quote\"\n", json.RootElement.GetProperty("stamps")[0].GetProperty("id").GetString());
        Assert.Equal(.5, json.RootElement.GetProperty("stamps")[0].GetProperty("x").GetDouble());
        Assert.False(json.RootElement.TryGetProperty("unit", out _));
    }
}
