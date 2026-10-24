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
        public int StaleContactCount { get; private set; }
        private readonly AdmissionTracker admissions = new AdmissionTracker();
        private readonly ContactMemory history;
        private readonly bool teamKnowledge;
        private readonly HashSet<long> currentUnits = new HashSet<long>();
        private int knowledgeViewer;
        public int HistoricalContactCount { get; private set; }
        public int RetainedContactCount => history.Count;
        public int ViewerIdentity => knowledgeViewer;
        public bool HistoryExpiredThisRead { get; private set; }
        public GameObservationAdapter(double airbase, double ship, double vehicle, double aircraft, double building)
            : this(airbase, ship, vehicle, aircraft, building, false, 0) { }
        public GameObservationAdapter(double airbase, double ship, double vehicle, double aircraft, double building, bool teamKnowledge, double maximumHistoryAge)
        {
            this.teamKnowledge = teamKnowledge;
            history = new ContactMemory(4096, maximumHistoryAge);
            foreach (double w in new[] { airbase, ship, vehicle, aircraft, building })
                if (!ModelSettings.Finite(w) || w < 0 || w > 1e6)
                    throw new ArgumentOutOfRangeException("weights");
            AirbaseWeight = airbase;
            ShipWeight = ship;
            VehicleWeight = vehicle;
            AircraftWeight = aircraft;
            BuildingWeight = building;
        }
        // Check even while the map is closed: a changed/unassigned viewer cannot inherit history.
        public void ValidateContext()
        {
            if (!MissionManager.IsRunning || !GameManager.GetLocalHQ(out var viewer) || viewer == null)
            { Clear(); return; }
            int identity = viewer.GetInstanceID();
            if (knowledgeViewer != identity) { Clear(); knowledgeViewer = identity; history.BeginContext(identity); }
        }
        public bool TryRead(MapBinding binding, bool capture, out ObservationStamp[] observations, out long admissionRevision, out CaptureFailure failure)
        {
            if (!teamKnowledge) return ReadLegacy(binding, capture, out observations, out admissionRevision, out failure);
            observations = null; admissionRevision = 0; failure = CaptureFailure.UnresolvedViewer;
            ValidateContext();
            if (binding == null || binding.Map == null || !MissionManager.IsRunning || !GameManager.GetLocalHQ(out var viewer) || viewer == null || binding.Map.HQ != viewer)
                return false;
            var hud = SceneSingleton<CombatHUD>.i;
            if (hud == null) return false;
            if (hud.jamAccumulation > 0) { failure = CaptureFailure.Jammed; return false; }
            hqs.Clear();
            if (FactionRegistry.factions != null) foreach (var faction in FactionRegistry.factions)
            { var hq = FactionRegistry.HQFromFaction(faction); if (hq != null) hqs.Add(hq); }
            if (hqs.Count != 2 || !hqs.Contains(viewer)) { Clear(); failure = CaptureFailure.UnsupportedFactions; return false; }
            double now = Time.timeSinceLevelLoad;
            int retainedBeforePrune = history.Count;
            history.Prune(now);
            HistoryExpiredThisRead = history.Count < retainedBeforePrune;
            if (capture) native.Clear();
            currentUnits.Clear(); admissions.Begin(viewer.GetInstanceID());
            StaleContactCount = HistoricalContactCount = 0;
            int admitted = 0;
            // The native GenerateMap admits the replicated own-team roster independently
            // of icon filtering, blinking and minimap-only airbase hiding.
            foreach (var id in viewer.factionUnits)
            {
                if (!UnitRegistry.TryGetUnit(id, out var unit) || unit == null || unit.NetworkHQ != viewer) continue;
                long key = UnitKey(unit); currentUnits.Add(key);
                if (capture) history.Remove("unit:" + unit.persistentID);
                if (unit.disabled) continue;
                double weight = Weight(unit); if (weight <= 0) continue;
                var token = new AdmissionToken(key, viewer.GetInstanceID(), weight, false, false, Role(unit));
                admissions.Add(token);
                if (++admitted > 100000) { failure = CaptureFailure.InvalidObservation; return false; }
                if (capture)
                {
                    string identity = "unit:" + unit.persistentID;
                    history.Remove(identity);
                    var position = unit.GlobalPosition().AsVector3();
                    if (!Finite(position.x, position.z, now)) { failure = CaptureFailure.InvalidObservation; return false; }
                    native.Add(new NativeObservation(identity, Side.Friendly, position.x, position.z, weight, now, false, true, token.Role));
                }
            }
            foreach (var airbase in viewer.GetAirbases())
            {
                if (AirbaseWeight <= 0 || airbase == null || airbase.CurrentHQ != viewer || airbase.center == null) continue;
                admissions.Add(new AdmissionToken(airbase.GetInstanceID(), viewer.GetInstanceID(), AirbaseWeight, false, true, ObservationRole.Airbase));
                if (++admitted > 100000) { failure = CaptureFailure.InvalidObservation; return false; }
                if (capture)
                {
                    var position = airbase.center.GlobalPosition().AsVector3();
                    if (!Finite(position.x, position.z, now)) { failure = CaptureFailure.InvalidObservation; return false; }
                    native.Add(new NativeObservation("airbase:" + airbase.GetInstanceID(), Side.Friendly, position.x, position.z, AirbaseWeight, now, false, true, ObservationRole.Airbase));
                }
            }
            foreach (var marker in binding.Map.mapIcons)
            {
                var icon = marker as UnitMapIcon;
                if (icon == null || !Visible(icon) || icon.unit == null) continue;
                var unit = icon.unit; long key = UnitKey(unit);
                if (currentUnits.Contains(key)) continue; // Own roster already admitted it.
                // A dangling native image after tracking loss does not authorize fresh
                // metadata reads from its old Unit reference. Age the copied record instead.
                var tracking = viewer.GetTrackingData(unit.persistentID);
                if (tracking == null) continue;
                currentUnits.Add(key);
                var mode = DynamicMap.GetFactionMode(unit.NetworkHQ, true);
                double weight = Weight(unit);
                if (mode != FactionMode.Enemy || !hqs.Contains(unit.NetworkHQ) || weight <= 0)
                { if (capture) history.Remove("unit:" + unit.persistentID); continue; }
                bool stale = !tracking.Observed(); if (stale) StaleContactCount++;
                var token = new AdmissionToken(key, unit.NetworkHQ.GetInstanceID(), weight, stale, false, Role(unit));
                admissions.Add(token);
                if (++admitted > 100000) { failure = CaptureFailure.InvalidObservation; return false; }
                if (capture)
                {
                    // GetPosition() can refresh from the live unit during its grace period.
                    // Reading the already cached native record never performs that lookup.
                    var position = tracking.lastKnownPosition.AsVector3(); double observed = tracking.lastSpottedTime;
                    if (!Finite(position.x, position.z, observed) || observed > now) { failure = CaptureFailure.InvalidObservation; return false; }
                    var observation = new NativeObservation("unit:" + unit.persistentID, Side.Enemy, position.x, position.z, weight, observed, stale, true, token.Role);
                    history.Remember(observation, token, now);
                    if (history.TryGet(observation.Id, out var retained))
                    { var remembered = retained.Observation; observation = new NativeObservation(remembered.Id, Side.Enemy, remembered.X, remembered.Z, remembered.Weight, remembered.ObservedAt, stale, true, remembered.Role); }
                    native.Add(observation);
                }
            }
            foreach (var retained in history.Contacts)
            {
                if (currentUnits.Contains(retained.Token.Id)) continue;
                admissions.Add(retained.Token); HistoricalContactCount++; StaleContactCount++;
                if (++admitted > 100000) { failure = CaptureFailure.InvalidObservation; return false; }
                if (capture)
                {
                    var known = retained.Observation;
                    native.Add(new NativeObservation(known.Id, Side.Enemy, known.X, known.Z, known.Weight, known.ObservedAt, true, true, known.Role));
                }
            }
            admissionRevision = admissions.Complete(); failure = CaptureFailure.None;
            if (!capture) return true;
            if (!ObservationPolicy.TrySanitize(ViewerMode.Assigned, hqs.Count, native, out observations, out failure)) return false;
            admissions.CommitCapture(); return true;
        }
        private static long UnitKey(Unit unit) => ((long)unit.GetInstanceID() << 32) | unit.persistentID.Id;
        private static bool Finite(double x, double z, double time) => ModelSettings.Finite(x) && ModelSettings.Finite(z) && ModelSettings.Finite(time);
        private bool ReadLegacy(MapBinding binding, bool capture, out ObservationStamp[] observations, out long admissionRevision, out CaptureFailure failure)
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
            StaleContactCount = 0;
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
                var role = Role(unit);
                if (weight <= 0)
                    continue;
                var tracking = viewer.GetTrackingData(unit.persistentID);
                // An enemy without native tracking data is unresolved; don't fall back to its live position.
                if (mode == FactionMode.Enemy && tracking == null)
                    continue;
                bool stale = mode == FactionMode.Enemy && !tracking.Observed();
                if (stale) StaleContactCount++;
                double observed = tracking == null ? Time.timeSinceLevelLoad : tracking.lastSpottedTime;
                Vector3 position = icon.iconImage.transform.localPosition;
                var side = mode == FactionMode.Friendly ? Side.Friendly : Side.Enemy;
                double x = position.x / binding.Map.mapDisplayFactor, z = position.y / binding.Map.mapDisplayFactor;
                if (!ModelSettings.Finite(x) || !ModelSettings.Finite(z) || !ModelSettings.Finite(observed) || ++admittedCount > 100000)
                {
                    failure = CaptureFailure.InvalidObservation;
                    return false;
                }
                admissions.Add(new AdmissionToken(((long)unit.GetInstanceID() << 32) | unit.persistentID.Id, unit.NetworkHQ.GetInstanceID(), weight, stale, false, role));
                if (capture)
                    native.Add(new NativeObservation("unit:" + unit.persistentID, side, x, z, weight, observed, stale, true, role));
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
                admissions.Add(new AdmissionToken(icon.airbase.GetInstanceID(), viewer.GetInstanceID(), AirbaseWeight, false, true, ObservationRole.Airbase));
                if (capture)
                    native.Add(new NativeObservation("airbase:" + icon.airbase.GetInstanceID(), Side.Friendly, x, z, AirbaseWeight, Time.timeSinceLevelLoad, false, true, ObservationRole.Airbase));
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
            StaleContactCount = HistoricalContactCount = 0;
            HistoryExpiredThisRead = false;
            knowledgeViewer = 0;
            history.Clear();
            currentUnits.Clear();
            hqs.Clear();
            native.Clear();
            airbaseIcons.Clear();
            admissions.Clear();
        }
        public void ResetAdmissions() { admissions.Clear(); native.Clear(); currentUnits.Clear(); }
        private static bool Visible(MapIcon icon) => icon.gameObject.activeInHierarchy && icon.iconImage != null && icon.iconImage.gameObject.activeInHierarchy && icon.iconImage.enabled && icon.iconImage.color.a > 0;
        private static ObservationRole Role(Unit unit)
        {
            if (unit is Aircraft) return ObservationRole.Aircraft;
            if (unit is Ship) return ObservationRole.Ship;
            if (unit is GroundVehicle)
            {
                // Native UnitMapIcon.GetInfoText exposes this same name. No private loadout/state is read.
                string name = unit.unitName ?? "";
                if (name.IndexOf("SAM", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("SPAAG", StringComparison.OrdinalIgnoreCase) >= 0)
                    return ObservationRole.AirDefense;
                return ObservationRole.GroundVehicle;
            }
            return unit is Building ? ObservationRole.Building : ObservationRole.Unknown;
        }
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
