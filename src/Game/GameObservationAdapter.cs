using System;
using System.Collections.Generic;
using FrontlineMap.Core;
using UnityEngine;

namespace FrontlineMap.Game
{
    public sealed class GameObservationAdapter
    {
        public readonly double AirbaseWeight, ShipWeight, VehicleWeight, AircraftWeight, BuildingWeight;
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
        public bool TryRead(MapBinding binding, out ObservationStamp[] observations, out string admissionKey, out CaptureFailure failure)
        {
            observations = Array.Empty<ObservationStamp>();
            admissionKey = null;
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
            var hqs = new HashSet<FactionHQ>();
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
            var native = new List<NativeObservation>();
            var admission = new List<string>();
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
                string id = "unit:" + unit.persistentID;
                var side = mode == FactionMode.Friendly ? Side.Friendly : Side.Enemy;
                native.Add(new NativeObservation(id, side, position.x / binding.Map.mapDisplayFactor, position.y / binding.Map.mapDisplayFactor, weight, observed, stale, true));
                admission.Add(id + ":" + unit.NetworkHQ.GetInstanceID() + ":" + weight.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + stale);
            }
            // Native airbase markers are generated only from HQ.GetAirbases on this target.
            foreach (var icon in binding.Map.GetComponentsInChildren<AirbaseMapIcon>(true))
            {
                if (icon == null || icon.airbase == null || !Visible(icon) || icon.airbase.CurrentHQ != viewer)
                    continue;
                var position = icon.iconImage.transform.localPosition;
                string id = "airbase:" + icon.airbase.GetInstanceID();
                native.Add(new NativeObservation(id, Side.Friendly, position.x / binding.Map.mapDisplayFactor, position.y / binding.Map.mapDisplayFactor, AirbaseWeight, Time.timeSinceLevelLoad, false, true));
                admission.Add(id + ":" + viewer.GetInstanceID());
            }
            admission.Sort(StringComparer.Ordinal);
            admissionKey = viewer.GetInstanceID() + "|" + string.Join("|", admission);
            return ObservationPolicy.TrySanitize(ViewerMode.Assigned, hqs.Count, native, out observations, out failure);
        }
        private static bool Visible(MapIcon icon) => icon.gameObject.activeSelf && icon.iconImage != null && icon.iconImage.enabled && icon.iconImage.color.a > 0;
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
