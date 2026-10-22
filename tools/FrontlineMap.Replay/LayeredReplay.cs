using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FrontlineMap.Core;
using FrontlineMap.Runtime;

namespace FrontlineMap.Replay;

public sealed class LayeredReplayReport
{
    public bool Pass { get; set; }
    public int ComparedBuffers { get; set; } = 5;
    public int PixelMismatches { get; set; }
    public int BufferMismatches { get; set; }
    public int Cells { get; set; }
    public int Observations { get; set; }
    public int SchedulerSteps { get; set; }
    public double MaxAbsoluteError { get; set; }
    public double OracleToolMilliseconds { get; set; }
    public double CandidateToolMilliseconds { get; set; }
    public string SnapshotSha256 { get; set; }
    public string TerrainSha256 { get; set; }
    public string RgbaSha256 { get; set; }
    [JsonIgnore] public byte[] Rgba { get; set; }
    [JsonIgnore] public double[][] ReferenceBuffers { get; set; }
    [JsonIgnore] public double[][] CandidateBuffers { get; set; }
}

public static class LayeredReplay
{
    public const double AbsoluteTolerance = 1e-9, RelativeTolerance = 1e-9;
    static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    public static LayeredReplayReport Compare(string snapshot, string terrain = null)
    {
        var input = ReplayInput.Parse(snapshot, terrain);
        var timer = Stopwatch.StartNew();
        var oracle = LayeredReplayReference.Compute(input);
        double oracleMs = timer.Elapsed.TotalMilliseconds;
        var m = input.Model; var g = input.Grid;
        var geometry = new MapGeometry(g.Bounds[0], g.Bounds[1], g.Bounds[2], g.Bounds[3], g.Columns, g.Rows);
        TerrainCostGrid productionTerrain = null;
        if (input.Terrain != null)
        {
            var t = input.Terrain; var tg = t.Grid;
            productionTerrain = new TerrainCostGrid(new MapGeometry(tg.Bounds[0], tg.Bounds[1], tg.Bounds[2], tg.Bounds[3], tg.Columns, tg.Rows), t.Heights, t.Known, t.SeaLevel, t.SlopeDegrees);
        }
        var tactical = new TacticalSettings(Enum.Parse<TacticalLayer>(m.Layer), m.TerrainEnabled, m.TerrainResolution,
            m.GroundRadius, m.BaseRadius, m.BuildingRadius, m.AirRadius, m.AirDefenseRadius, m.NavalRadius,
            m.StaleHalfLife, m.ContestedRatio, m.MinimumFrontConfidence, m.BuildingControlFactor);
        var settings = new ModelSettings(g.Columns, input.Radius, 5, m.Opacity, m.SupportThreshold, m.Epsilon, 2, 3600, m.LineWidthPixels, tactical);
        var stamps = input.Stamps.Select(s => new ObservationStamp(s.Id, s.Side == "friendly" ? Side.Friendly : Side.Enemy, s.X, s.Z, s.Weight, s.Observed, s.Stale, Enum.Parse<ObservationRole>(s.Role)));
        // Export capture timestamps use another process's monotonic epoch. Rebase only the scheduler
        // lease; preserve observation/simulation timestamps used for the field's stale confidence.
        var production = new ObservationSnapshot(new SnapshotIdentity(1, 1, 1, 1, 1), geometry, stamps, 0, input.SimulationTime, 3600, productionTerrain);
        var clock = Stopwatch.StartNew();
        using var scheduler = new StepScheduler(() => clock.Elapsed.TotalSeconds);
        scheduler.ChunkCompleted = (_, cells) => { if (cells < 0 || cells > 128) throw new InvalidOperationException("Production scheduler exceeded bounded chunk contract."); };
        timer.Restart(); scheduler.Begin(production, settings, 800, 800);
        int steps = 0; FieldResult candidate;
        do
        {
            scheduler.Step(clock.Elapsed.TotalSeconds + .002); steps++;
            if (clock.Elapsed.TotalSeconds > 120) throw new TimeoutException("Layered tooling replay exceeded 120 seconds.");
        } while (!scheduler.TryTakeCompleted(out candidate));
        double candidateMs = timer.Elapsed.TotalMilliseconds;
        double[][] actual = { candidate.Field.Friendly, candidate.Field.Enemy, candidate.Field.RawFriendly, candidate.Field.RawEnemy, candidate.Field.Confidence };
        var report = new LayeredReplayReport { Pass = true, Cells = g.Count, Observations = input.Stamps.Length, SchedulerSteps = steps,
            OracleToolMilliseconds = oracleMs, CandidateToolMilliseconds = candidateMs, SnapshotSha256 = Hash(snapshot),
            TerrainSha256 = input.TerrainJson == null ? null : Hash(input.TerrainJson), ReferenceBuffers = oracle.Buffers, CandidateBuffers = actual, Rgba = new byte[g.Count * 4] };
        for (int buffer = 0; buffer < 5; buffer++) for (int i = 0; i < g.Count; i++)
        {
            double expected = oracle.Buffers[buffer][i], value = actual[buffer][i], error = Math.Abs(expected - value);
            report.MaxAbsoluteError = Math.Max(report.MaxAbsoluteError, error);
            if (!double.IsFinite(expected) || !double.IsFinite(value) || error > AbsoluteTolerance + RelativeTolerance * Math.Abs(expected))
            { report.Pass = false; report.BufferMismatches++; }
        }
        for (int i = 0; i < g.Count; i++)
        {
            var expected = oracle.Pixels[i]; var pixel = candidate.Pixels[i];
            if (pixel.Kind.ToString() != expected.Kind || pixel.R != expected.R || pixel.G != expected.G || pixel.B != expected.B || pixel.A != expected.A)
            { report.Pass = false; report.PixelMismatches++; }
            report.Rgba[i * 4] = pixel.R; report.Rgba[i * 4 + 1] = pixel.G; report.Rgba[i * 4 + 2] = pixel.B; report.Rgba[i * 4 + 3] = pixel.A;
        }
        report.RgbaSha256 = Convert.ToHexString(SHA256.HashData(report.Rgba));
        return report;
    }

    public static int Run(IReadOnlyDictionary<string, string> options)
    {
        if (!options.TryGetValue("--compare", out string compare) || compare != "oracle,candidate") throw new ArgumentException("Supported comparison: oracle,candidate");
        string fixtures = options["--fixtures"], output = options["--output"];
        string terrain = options.TryGetValue("--terrain", out string terrainPath) ? File.ReadAllText(terrainPath) : null;
        Directory.CreateDirectory(output);
        string[] files = File.Exists(fixtures) ? new[] { fixtures } : Directory.GetFiles(fixtures, "*.snapshot.json").OrderBy(v => v, StringComparer.Ordinal).ToArray();
        if (files.Length == 0) throw new ArgumentException("No layered snapshot fixtures found.");
        var reports = new List<object>(); bool allPass = true;
        foreach (string file in files)
        {
            var result = Compare(File.ReadAllText(file), terrain);
            string name = Path.GetFileName(file);
            File.WriteAllBytes(Path.Combine(output, name + ".rgba"), result.Rgba);
            reports.Add(new { fixture = name, result }); allPass &= result.Pass;
            Console.WriteLine($"{name}: {(result.Pass ? "PASS" : "FAIL")} all5_max_error={result.MaxAbsoluteError:R}, buffer_mismatches={result.BufferMismatches}, rgba_or_kind_mismatches={result.PixelMismatches}");
        }
        File.WriteAllText(Path.Combine(output, "comparison.json"), JsonSerializer.Serialize(new
        {
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            platform = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            scope = "independent layered static-terrain tooling parity only; not Unity performance, acquisition, or freshness evidence; oracle and candidate timings are not comparable",
            absolute_tolerance = AbsoluteTolerance, relative_tolerance = RelativeTolerance,
            status = allPass ? "pass" : "fail", fixtures = reports
        }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }));
        return allPass ? 0 : 1;
    }
}
