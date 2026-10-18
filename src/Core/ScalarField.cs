using System;

namespace FrontlineMap.Core
{
    public sealed class InfluenceBuffers
    {
        public readonly MapGeometry Geometry;
        public readonly double[] Friendly, Enemy;
        public InfluenceBuffers(MapGeometry geometry)
        {
            Geometry = geometry;
            Friendly = new double[geometry.Count];
            Enemy = new double[geometry.Count];
        }
    }

    public static class ScalarField
    {
        // Deliberately simple independent full-grid oracle; candidate clips footprints.
        public static InfluenceBuffers Compute(ObservationSnapshot snapshot, ModelSettings settings)
        {
            var field = new InfluenceBuffers(snapshot.Geometry);
            var g = snapshot.Geometry;
            double radiusSquared = settings.Radius * settings.Radius;
            foreach (var stamp in snapshot.Stamps)
            {
                var support = stamp.Side == Side.Friendly ? field.Friendly : field.Enemy;
                for (int z = 0; z < g.Rows; z++)
                    for (int x = 0; x < g.Columns; x++)
                    {
                        double dx = g.X(x) - stamp.X, dz = g.Z(z) - stamp.Z;
                        double distanceSquared = dx * dx + dz * dz;
                        if (distanceSquared < radiusSquared)
                            support[z * g.Columns + x] += stamp.Weight * (1 - distanceSquared / radiusSquared);
                    }
            }
            return field;
        }
    }
}
