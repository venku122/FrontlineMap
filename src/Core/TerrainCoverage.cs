namespace FrontlineMap.Core
{
    public static class TerrainCoverage
    {
        // Availability heuristic: at least half the static sample cells must be
        // known. This guards incomplete map coverage, not support strength or
        // navigability: a fully sampled map with no ships remains a valid blank.
        public static bool IsUnavailable(TacticalLayer layer, bool terrainEnabled, bool ready, bool failed, int known, int total)
        {
            if (!terrainEnabled || layer == TacticalLayer.Legacy || layer == TacticalLayer.AirThreat) return false;
            if (failed) return true;
            if (!ready) return false;
            return total <= 0 || known < 0 || known > total || (long)known * 2 < total;
        }
    }
}
