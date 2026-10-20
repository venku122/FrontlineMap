using System;
namespace FrontlineMap.Core
{
    public enum TacticalLayer { Legacy, GroundControl, AirThreat, NavalInfluence, Confidence }
    public enum ObservationRole { Unknown, GroundVehicle, Aircraft, Ship, Building, Airbase, AirDefense }
    public sealed class TacticalSettings
    {
        public readonly TacticalLayer Layer;
        public readonly bool TerrainEnabled;
        public readonly int TerrainResolution;
        public readonly double GroundRadius, BaseRadius, BuildingRadius, AirRadius, AirDefenseRadius, NavalRadius;
        public readonly double StaleHalfLife, ContestedRatio, MinimumFrontConfidence, BuildingControlFactor;
        public TacticalSettings(TacticalLayer layer = TacticalLayer.Legacy, bool terrainEnabled = true, int terrainResolution = 128,
            double groundRadius = 6000, double baseRadius = 8000, double buildingRadius = 2000,
            double airRadius = 20000, double airDefenseRadius = 12000, double navalRadius = 16000,
            double staleHalfLife = 120, double contestedRatio = .2, double minimumFrontConfidence = .5, double buildingControlFactor = .15)
        {
            if (!Enum.IsDefined(typeof(TacticalLayer), layer) || terrainResolution < 32 || terrainResolution > 256)
                throw new ArgumentOutOfRangeException("tactical settings");
            foreach (double radius in new[] { groundRadius, baseRadius, buildingRadius, airRadius, airDefenseRadius, navalRadius })
                if (!ModelSettings.Positive(radius) || radius > 1e9) throw new ArgumentOutOfRangeException("role radius");
            if (!ModelSettings.Positive(staleHalfLife) || staleHalfLife > 86400 || !ModelSettings.Finite(contestedRatio) || contestedRatio < 0 || contestedRatio > 1
                || !ModelSettings.Finite(minimumFrontConfidence) || minimumFrontConfidence < 0 || minimumFrontConfidence > 1
                || !ModelSettings.Finite(buildingControlFactor) || buildingControlFactor < 0 || buildingControlFactor > 1)
                throw new ArgumentOutOfRangeException("confidence settings");
            Layer = layer; TerrainEnabled = terrainEnabled; TerrainResolution = terrainResolution;
            GroundRadius = groundRadius; BaseRadius = baseRadius; BuildingRadius = buildingRadius;
            AirRadius = airRadius; AirDefenseRadius = airDefenseRadius; NavalRadius = navalRadius;
            StaleHalfLife = staleHalfLife; ContestedRatio = contestedRatio; MinimumFrontConfidence = minimumFrontConfidence; BuildingControlFactor = buildingControlFactor;
        }
    }
}
