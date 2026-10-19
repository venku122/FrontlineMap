using System;
using System.Collections.Generic;
using FrontlineMap.Core;
using UnityEngine;

namespace FrontlineMap.Game
{
    public sealed class GameObservationAdapter
    {
        public readonly double AirbaseWeight, ShipWeight, VehicleWeight, AircraftWeight, BuildingWeight;
        private readonly HashSet<FactionHQ> hqs = new HashSet<FactionHQ>();
        private readonly List<NativeObservation> native = new List<NativeObservation>();
        private readonly List<AirbaseMapIcon> airbaseIcons = new List<AirbaseMapIcon>();
        private readonly AdmissionTracker admissions = new AdmissionTracker();
        public GameObservationAdapter(double airbase, double ship, double vehicle, double aircraft, double building)
        {
            foreach (double w in new[] { airbase, ship, vehicle, aircraft, building })
                if (!ModelSettings.Finite(w) || w < 0 || w > 1e6)
                    throw new ArgumentOutOfRangeException("weights");
            AirbaseWeight = airbase;
            ShipWeight = ship;
            VehicleWeight = vehicle;
            AircraftWeight = aircraft;
            BuildingWeight = building;
        }
        public bool TryRead(MapBinding binding, bool capture, out ObservationStamp[] observations, out long admissionRevision, out CaptureFailure failure)
        {
            observations = null;
            admissionRevision = 0;
            failure = CaptureFailure.UnresolvedViewer;
            if (binding == null || binding.Map == null || !MissionManager.IsRunning || !GameManager.GetLocalHQ(out var viewer) || viewer == null || binding.Map.HQ != viewer)
                return false;
            var hud = SceneSingleton<CombatHUD>.i;
            if (hud == null)
                return false;
            if (hud.jamAccumulation > 0)
            {
                failure = CaptureFailure.Jammed;
                return false;
            }
            var factions = FactionRegistry.factions;
            hqs.Clear();
            if (factions != null)
                foreach (var f in factions)
                {
                    var hq = FactionRegistry.HQFromFaction(f);
                    if (hq != null)
                        hqs.Add(hq);
                }
            if (hqs.Count != 2 || !hqs.Contains(viewer))
            {
                failure = CaptureFailure.UnsupportedFactions;
                return false;
            }
            if (capture) native.Clear();
            admissions.Begin(viewer.GetInstanceID());
            int admittedCount = 0;
            // Native markers are the admission source. Never consult allUnits, unitState, health, or true position.
            foreach (var marker in binding.Map.mapIcons)
            {
                var icon = marker as UnitMapIcon;
                if (icon == null || icon.unit == null || !Visible(icon))
                    continue;
                var unit = icon.unit;
                var mode = DynamicMap.GetFactionMode(unit.NetworkHQ, true);
                if (mode != FactionMode.Friendly && mode != FactionMode.Enemy)
                    continue;
                if (!hqs.Contains(unit.NetworkHQ))
                    continue;
                double weight = Weight(unit);
                if (weight <= 0)
                    continue;
                var tracking = viewer.GetTrackingData(unit.persistentID);
                // An enemy without native tracking data is unresolved; don't fall back to its live position.
                if (mode == FactionMode.Enemy && tracking == null)
                    continue;
                bool stale = mode == FactionMode.Enemy && !tracking.Observed();
                double observed = tracking == null ? Time.timeSinceLevelLoad : tracking.lastSpottedTime;
                Vector3 position = icon.iconImage.transform.localPosition;
                var side = mode == FactionMode.Friendly ? Side.Friendly : Side.Enemy;
                double x = position.x / binding.Map.mapDisplayFactor, z = position.y / binding.Map.mapDisplayFactor;
                if (!ModelSettings.Finite(x) || !ModelSettings.Finite(z) || !ModelSettings.Finite(observed) || ++admittedCount > 100000)
                {
                    failure = CaptureFailure.InvalidObservation;
                    return false;
                }
                admissions.Add(new AdmissionToken(((long)unit.GetInstanceID() << 32) | unit.persistentID.Id, unit.NetworkHQ.GetInstanceID(), weight, stale, false));
                if (capture)
                    native.Add(new NativeObservation("unit:" + unit.persistentID, side, x, z, weight, observed, stale, true));
            }
            // Native airbase markers are generated only from HQ.GetAirbases on this target.
            binding.Map.GetComponentsInChildren(true, airbaseIcons);
            foreach (var icon in airbaseIcons)
            {
                if (AirbaseWeight <= 0 || icon == null || icon.airbase == null || !Visible(icon) || icon.airbase.CurrentHQ != viewer)
                    continue;
                var position = icon.iconImage.transform.localPosition;
                double x = position.x / binding.Map.mapDisplayFactor, z = position.y / binding.Map.mapDisplayFactor;
                if (!ModelSettings.Finite(x) || !ModelSettings.Finite(z) || ++admittedCount > 100000)
                {
                    failure = CaptureFailure.InvalidObservation;
                    return false;
                }
                admissions.Add(new AdmissionToken(icon.airbase.GetInstanceID(), viewer.GetInstanceID(), AirbaseWeight, false, true));
                if (capture)
                    native.Add(new NativeObservation("airbase:" + icon.airbase.GetInstanceID(), Side.Friendly, x, z, AirbaseWeight, Time.timeSinceLevelLoad, false, true));
            }
            admissionRevision = admissions.Complete();
            failure = CaptureFailure.None;
            if (!capture)
                return true;
            if (!ObservationPolicy.TrySanitize(ViewerMode.Assigned, hqs.Count, native, out observations, out failure))
                return false;
            admissions.CommitCapture();
            return true;
        }

        public void Clear()
        {
            hqs.Clear();
            native.Clear();
            airbaseIcons.Clear();
            admissions.Clear();
        }
        private static bool Visible(MapIcon icon) => icon.gameObject.activeInHierarchy && icon.iconImage != null && icon.iconImage.gameObject.activeInHierarchy && icon.iconImage.enabled && icon.iconImage.color.a > 0;
        private double Weight(Unit unit)
        {
            if (unit is PilotDismounted)
                return 0;
            if (unit is Aircraft)
                return AircraftWeight;
            if (unit is Ship)
                return ShipWeight;
            if (unit is GroundVehicle)
                return VehicleWeight;
            if (unit is Building)
                return BuildingWeight;
            return 0;
        }
    }
}
