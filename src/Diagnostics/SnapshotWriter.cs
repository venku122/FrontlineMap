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
            b.Append("{\"schema\":1,\"scope\":\"sanitized native-map observations\",\"resolution\":").Append(g.Columns)
             .Append(",\"radius\":").Append(Number(settings.Radius)).Append(",\"captured_at\":").Append(Number(snapshot.CapturedAt))
             .Append(",\"simulation_time\":").Append(Number(snapshot.SimulationTime)).Append(",\"bounds\":[")
             .Append(Number(g.MinX)).Append(',').Append(Number(g.MinZ)).Append(',').Append(Number(g.Width)).Append(',').Append(Number(g.Height)).Append("],\"stamps\":[");
            bool first = true;
            foreach (var s in snapshot.Stamps)
            {
                if (!first)
                    b.Append(',');
                first = false;
                b.Append("{\"id\":").Append(Quote(s.Id)).Append(",\"side\":").Append(Quote(s.Side == Side.Friendly ? "friendly" : "enemy")).Append(",\"x\":").Append(Number(s.X)).Append(",\"z\":").Append(Number(s.Z)).Append(",\"weight\":").Append(Number(s.Weight)).Append(",\"observed_at\":").Append(Number(s.ObservedTime)).Append(",\"stale\":").Append(s.Stale ? "true" : "false").Append('}');
            }
            return b.Append("]}").ToString();
        }
    }
}
