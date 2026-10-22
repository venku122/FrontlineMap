using System;
using System.Diagnostics;
using FrontlineMap.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrontlineMap.Game
{
    /// <summary>Main-thread static map sampler. No dynamic entities or hidden observations are consulted.</summary>
    public sealed class TerrainSampler
    {
        MapGeometry geometry;
        double[] heights;
        bool[] known;
        int scene, next, knownCount, landCount, waterCount;
        bool failed;
        public TerrainCostGrid Grid { get; private set; }
        public bool Ready => Grid != null;
        public bool Failed => failed;
        public string Status { get; private set; } = "Terrain unavailable";
        public int KnownCount => knownCount;
        public int LandCount => landCount;
        public int WaterCount => waterCount;

        public void GetOrBegin(MapGeometry map, int resolution)
        {
            if (map == null) { Clear(); return; }
            if (resolution < 2 || resolution > 2048) throw new ArgumentOutOfRangeException(nameof(resolution));
            int activeScene = SceneManager.GetActiveScene().handle;
            if (geometry != null && scene == activeScene && geometry.Columns == resolution && geometry.Rows == resolution
                && geometry.MinX == map.MinX && geometry.MinZ == map.MinZ && geometry.Width == map.Width && geometry.Height == map.Height) return;
            Clear();
            scene = activeScene;
            geometry = new MapGeometry(map.MinX, map.MinZ, map.Width, map.Height, resolution, resolution);
            heights = new double[geometry.Count]; known = new bool[geometry.Count];
            UpdateStatus();
        }

        /// <summary>At most 128 rays and approximately the requested milliseconds, measured on a monotonic clock.</summary>
        public void Step(double budgetMilliseconds = 2)
        {
            if (geometry == null || failed) return;
            if (scene != SceneManager.GetActiveScene().handle) { Clear(); return; }
            if (Ready || !ModelSettings.Positive(budgetMilliseconds)) return;
            long start = Stopwatch.GetTimestamp();
            double budgetTicks = Math.Min(budgetMilliseconds, 2) * Stopwatch.Frequency / 1000d;
            int rays = 0;
            try
            {
                while (next < geometry.Count && rays < 128 && Stopwatch.GetTimestamp() - start < budgetTicks)
                {
                    int index = next++;
                    double x = geometry.MinX + (index % geometry.Columns + .5) * geometry.CellX;
                    double z = geometry.MinZ + (index / geometry.Columns + .5) * geometry.CellZ;
                    // Global positions must be converted for every ray because the game shifts its floating origin.
                    Vector3 origin = new GlobalPosition((float)x, 10000f, (float)z).ToLocalPosition();
                    rays++;
                    // Some mission units use the static layer too. Read through their colliders to
                    // public map geometry; hidden buildings/wrecks must not alter the sampled terrain.
                    var hits = Physics.RaycastAll(origin, Vector3.down, 20000f, PhysicsLayers.StaticsMask, QueryTriggerInteraction.Ignore);
                    float nearest = float.PositiveInfinity; Vector3 point = Vector3.zero;
                    foreach (var hit in hits)
                    {
                        if (hit.collider == null || hit.rigidbody != null || hit.collider.GetComponentInParent<Unit>() != null) continue;
                        if (hit.distance < nearest) { nearest = hit.distance; point = hit.point; }
                    }
                    if (float.IsPositiveInfinity(nearest)) continue;
                    double y = point.ToGlobalPosition().y;
                    if (!ModelSettings.Finite(y)) continue;
                    heights[index] = y; known[index] = true; knownCount++;
                    if (y > 0) landCount++; else waterCount++;
                }
                if (next == geometry.Count) Grid = new TerrainCostGrid(geometry, heights, known);
                UpdateStatus();
            }
            catch (Exception exception)
            {
                failed = true; Grid = null;
                Status = "Terrain unavailable: " + exception.GetType().Name;
            }
        }

        void UpdateStatus()
        {
            string state = Ready ? (knownCount == 0 ? "unavailable (no static hits)" : "ready") : "sampling " + next + "/" + geometry.Count;
            Status = "Terrain " + state + "; " + geometry.Columns + "x" + geometry.Rows + "; known " + knownCount + "; land " + landCount + "; water " + waterCount;
        }

        public void Clear()
        {
            Grid = null; geometry = null; heights = null; known = null;
            next = knownCount = landCount = waterCount = 0; scene = 0; failed = false;
            Status = "Terrain unavailable";
        }
    }
}
