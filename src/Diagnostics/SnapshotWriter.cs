using System;
using System.Globalization;
using System.Text;
using FrontlineMap.Core;
namespace FrontlineMap.Diagnostics
{
    public static class SnapshotWriter
    {
        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Quote(string value)
        {
            var b = new StringBuilder("\"");
            foreach (char c in value)
            {
                if (c == '"' || c == '\\')
                    b.Append('\\').Append(c);
                else if (c < 32)
                    b.Append("\\u").Append(((int)c).ToString("x4"));
                else
                    b.Append(c);
            }
            return b.Append('"').ToString();
        }
        public static string ToJson(ObservationSnapshot snapshot, ModelSettings settings)
        {
            var g = snapshot.Geometry;
            var b = new StringBuilder();
            bool layered = settings.Tactical.Layer != TacticalLayer.Legacy;
            b.Append("{\"schema\":").Append(layered ? 2 : 1).Append(",\"scope\":\"sanitized native-map observations\",\"resolution\":").Append(g.Columns)
             .Append(",\"radius\":").Append(Number(settings.Radius)).Append(",\"captured_at\":").Append(Number(snapshot.CapturedAt))
             .Append(",\"simulation_time\":").Append(Number(snapshot.SimulationTime)).Append(",\"bounds\":[")
             .Append(Number(g.MinX)).Append(',').Append(Number(g.MinZ)).Append(',').Append(Number(g.Width)).Append(',').Append(Number(g.Height)).Append(']');
            if (layered)
            {
                var t = settings.Tactical;
                b.Append(",\"replay_scope\":\"observations_only; local terrain required for layered replay\",\"model\":{\"layer\":").Append(Quote(t.Layer.ToString()))
                 .Append(",\"terrain_enabled\":").Append(t.TerrainEnabled ? "true" : "false").Append(",\"terrain_resolution\":").Append(t.TerrainResolution)
                 .Append(",\"ground_radius\":").Append(Number(t.GroundRadius)).Append(",\"base_radius\":").Append(Number(t.BaseRadius))
                 .Append(",\"building_radius\":").Append(Number(t.BuildingRadius)).Append(",\"air_radius\":").Append(Number(t.AirRadius))
                 .Append(",\"air_defense_radius\":").Append(Number(t.AirDefenseRadius)).Append(",\"naval_radius\":").Append(Number(t.NavalRadius))
                 .Append(",\"stale_half_life\":").Append(Number(t.StaleHalfLife)).Append(",\"contested_ratio\":").Append(Number(t.ContestedRatio))
                 .Append(",\"minimum_front_confidence\":").Append(Number(t.MinimumFrontConfidence)).Append(",\"building_control_factor\":").Append(Number(t.BuildingControlFactor))
                 .Append(",\"opacity\":").Append(Number(settings.Opacity)).Append(",\"support_threshold\":").Append(Number(settings.SupportThreshold))
                 .Append(",\"epsilon\":").Append(Number(settings.Epsilon)).Append(",\"line_width_pixels\":").Append(Number(settings.LineWidthPixels)).Append('}');
            }
            b.Append(",\"stamps\":[");
            bool first = true;
            foreach (var s in snapshot.Stamps)
            {
                if (!first)
                    b.Append(',');
                first = false;
                b.Append("{\"id\":").Append(Quote(s.Id)).Append(",\"side\":").Append(Quote(s.Side == Side.Friendly ? "friendly" : "enemy")).Append(",\"x\":").Append(Number(s.X)).Append(",\"z\":").Append(Number(s.Z)).Append(",\"weight\":").Append(Number(s.Weight)).Append(",\"observed_at\":").Append(Number(s.ObservedTime)).Append(",\"stale\":").Append(s.Stale ? "true" : "false");
                if (layered) b.Append(",\"role\":").Append(Quote(s.Role.ToString()));
                b.Append('}');
            }
            return b.Append("]}").ToString();
        }
    }
}
