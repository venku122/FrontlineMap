using System.Text.Json;

namespace FrontlineMap.Replay;

// Raw replay data belongs to the tooling oracle; no production model or terrain entrypoints here.
internal sealed class ReplayGrid
{
    public int Columns, Rows;
    public double[] Bounds;
    public int Count => checked(Columns * Rows);
    public double CellX => Bounds[2] / Columns;
    public double CellZ => Bounds[3] / Rows;
    public double X(int col) => Bounds[0] + (col + .5) * CellX;
    public double Z(int row) => Bounds[1] + (row + .5) * CellZ;
    public int Locate(double x, double z)
    {
        if (!double.IsFinite(x) || !double.IsFinite(z) || x < Bounds[0] || z < Bounds[1] || x >= Bounds[0] + Bounds[2] || z >= Bounds[1] + Bounds[3]) return -1;
        return Math.Min(Rows - 1, (int)((z - Bounds[1]) / CellZ)) * Columns + Math.Min(Columns - 1, (int)((x - Bounds[0]) / CellX));
    }
}
internal sealed class ReplayTerrain
{
    public ReplayGrid Grid;
    public double SeaLevel, SlopeDegrees;
    public double[] Heights;
    public bool[] Known;
}
internal sealed class ReplayStamp
{
    public string Id, Side, Role;
    public double X, Z, Weight, Observed;
    public bool Stale;
}
internal sealed class ReplayModel
{
    public string Layer;
    public bool TerrainEnabled;
    public int TerrainResolution;
    public double GroundRadius, BaseRadius, BuildingRadius, AirRadius, AirDefenseRadius, NavalRadius;
    public double StaleHalfLife, ContestedRatio, MinimumFrontConfidence, BuildingControlFactor;
    public double Opacity, SupportThreshold, Epsilon, LineWidthPixels;
}
internal sealed class ReplayInput
{
    public ReplayGrid Grid;
    public ReplayTerrain Terrain;
    public ReplayModel Model;
    public ReplayStamp[] Stamps;
    public double Radius, CapturedAt, SimulationTime;
    public string TerrainJson;
    public bool Paths => Model.TerrainEnabled && Model.Layer != "AirThreat";

    static double Number(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out double value) || !double.IsFinite(value))
            throw new ArgumentException("Replay numbers must be finite JSON numbers.");
        return value;
    }
    static double N(JsonElement element, string key) => Number(element.GetProperty(key));
    static double Positive(JsonElement element, string key, double maximum = 1e9)
    {
        double value = N(element, key);
        if (value <= 0 || value > maximum) throw new ArgumentException("Invalid replay " + key);
        return value;
    }
    static double Ratio(JsonElement element, string key)
    {
        double value = N(element, key);
        if (value < 0 || value > 1) throw new ArgumentException("Invalid replay " + key);
        return value;
    }
    static ReplayGrid GridFrom(JsonElement element, int columns, int rows)
    {
        double[] bounds = element.GetProperty("bounds").EnumerateArray().Select(Number).ToArray();
        if (columns < 2 || columns > 2048 || rows < 2 || rows > 2048 || bounds.Length != 4 || bounds[2] <= 0 || bounds[3] <= 0
            || !double.IsFinite(bounds[0] + bounds[2]) || !double.IsFinite(bounds[1] + bounds[3])
            || bounds[0] + bounds[2] == bounds[0] || bounds[1] + bounds[3] == bounds[1])
            throw new ArgumentException("Invalid replay grid dimensions or bounds.");
        return new ReplayGrid { Columns = columns, Rows = rows, Bounds = bounds };
    }
    public static ReplayInput Parse(string json, string terrainJson)
    {
        try { return Read(json, terrainJson); }
        catch (Exception e) when (e is JsonException || e is KeyNotFoundException || e is InvalidOperationException || e is FormatException || e is OverflowException)
        { throw new ArgumentException("Malformed layered replay input: " + e.Message, e); }
    }
    static ReplayInput Read(string json, string terrainJson)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.GetProperty("schema").GetInt32() != 2) throw new ArgumentException("Layered replay requires schema2.");
        var m = root.GetProperty("model");
        string layer = m.GetProperty("layer").GetString();
        if (!new[] { "GroundControl", "AirThreat", "NavalInfluence", "Confidence" }.Contains(layer)) throw new ArgumentException("Unsupported layered replay layer.");
        var model = new ReplayModel
        {
            Layer = layer, TerrainEnabled = m.GetProperty("terrain_enabled").GetBoolean(), TerrainResolution = m.GetProperty("terrain_resolution").GetInt32(),
            GroundRadius = Positive(m, "ground_radius"), BaseRadius = Positive(m, "base_radius"), BuildingRadius = Positive(m, "building_radius"),
            AirRadius = Positive(m, "air_radius"), AirDefenseRadius = Positive(m, "air_defense_radius"), NavalRadius = Positive(m, "naval_radius"),
            StaleHalfLife = Positive(m, "stale_half_life", 86400), ContestedRatio = Ratio(m, "contested_ratio"), MinimumFrontConfidence = Ratio(m, "minimum_front_confidence"),
            BuildingControlFactor = Ratio(m, "building_control_factor"), Opacity = Ratio(m, "opacity"), SupportThreshold = Positive(m, "support_threshold", double.MaxValue),
            Epsilon = Positive(m, "epsilon", double.MaxValue), LineWidthPixels = Positive(m, "line_width_pixels", 20)
        };
        if (model.TerrainResolution < 32 || model.TerrainResolution > 256) throw new ArgumentException("Invalid terrain_resolution.");
        int n = root.GetProperty("resolution").GetInt32();
        if (n < 8 || n > 2048) throw new ArgumentException("Invalid resolution.");
        var result = new ReplayInput { Grid = GridFrom(root, n, n), Model = model, Radius = Positive(root, "radius"),
            CapturedAt = N(root, "captured_at"), SimulationTime = N(root, "simulation_time") };
        result.Stamps = root.GetProperty("stamps").EnumerateArray().Select(s => new ReplayStamp
        {
            Id = s.GetProperty("id").GetString(), Side = s.GetProperty("side").GetString(), Role = s.GetProperty("role").GetString(),
            X = N(s, "x"), Z = N(s, "z"), Weight = N(s, "weight"), Observed = N(s, "observed_at"), Stale = s.GetProperty("stale").GetBoolean()
        }).OrderBy(s => s.Id, StringComparer.Ordinal).ToArray();
        string[] roles = { "Unknown", "GroundVehicle", "Aircraft", "Ship", "Building", "Airbase", "AirDefense" };
        if (result.Stamps.Length > 100000 || result.Stamps.Any(s => string.IsNullOrEmpty(s.Id) || (s.Side != "friendly" && s.Side != "enemy") || !roles.Contains(s.Role) || s.Weight < 0 || s.Weight > 1e6)
            || result.Stamps.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != result.Stamps.Length)
            throw new ArgumentException("Invalid or duplicate replay observations.");
        bool embedded = root.TryGetProperty("terrain", out var terrain);
        if (embedded && terrainJson != null) throw new ArgumentException("Specify embedded terrain or --terrain, not both.");
        if (embedded) result.TerrainJson = terrain.GetRawText();
        else if (terrainJson != null)
        {
            using var external = JsonDocument.Parse(terrainJson);
            var e = external.RootElement;
            result.TerrainJson = e.TryGetProperty("terrain", out var nested) ? nested.GetRawText() : e.GetRawText();
        }
        if (result.TerrainJson == null)
        {
            if (result.Paths) throw new ArgumentException("Layered terrain replay requires embedded terrain or an explicit --terrain fixture; radial fallback is not permitted.");
            return result;
        }
        using var terrainDocument = JsonDocument.Parse(result.TerrainJson);
        terrain = terrainDocument.RootElement;
        var tg = GridFrom(terrain, terrain.GetProperty("columns").GetInt32(), terrain.GetProperty("rows").GetInt32());
        if (!tg.Bounds.SequenceEqual(result.Grid.Bounds)) throw new ArgumentException("Terrain bounds must match snapshot map bounds.");
        double slope = N(terrain, "max_slope_degrees");
        if (slope < 0 || slope >= 90) throw new ArgumentException("Invalid max_slope_degrees.");
        var heights = terrain.GetProperty("heights").EnumerateArray().Select(Number).ToArray();
        var known = terrain.GetProperty("known").EnumerateArray().Select(e => e.GetBoolean()).ToArray();
        if (heights.Length != tg.Count || known.Length != tg.Count) throw new ArgumentException("Terrain sample counts must match grid dimensions.");
        result.Terrain = new ReplayTerrain { Grid = tg, SeaLevel = N(terrain, "sea_level"), SlopeDegrees = slope, Heights = heights, Known = known };
        return result;
    }
}
