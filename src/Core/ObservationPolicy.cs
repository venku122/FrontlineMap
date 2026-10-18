using System;
using System.Collections.Generic;
namespace FrontlineMap.Core
{
    public enum ViewerMode
    {
        Unresolved, Assigned, Spectator, Unsupported
    }
    // Values admitted by the native-map adapter, never authoritative hidden-world values.
    public sealed class NativeObservation
    {
        public readonly string Id; public readonly Side Side; public readonly double X, Z, Weight, ObservedAt; public readonly bool Stale, Visible;
        public NativeObservation(string id, Side side, double x, double z, double weight, double observedAt, bool stale, bool visible)
        {
            Id = id;
            Side = side;
            X = x;
            Z = z;
            Weight = weight;
            ObservedAt = observedAt;
            Stale = stale;
            Visible = visible;
        }
    }
    public static class ObservationPolicy
    {
        public static bool TrySanitize(ViewerMode viewer, int factionCount, IEnumerable<NativeObservation> native, out ObservationStamp[] stamps, out CaptureFailure failure)
        {
            stamps = Array.Empty<ObservationStamp>();
            failure = CaptureFailure.None;
            if (viewer != ViewerMode.Assigned)
            {
                failure = viewer == ViewerMode.Spectator ? CaptureFailure.UnsupportedSpectator : CaptureFailure.UnresolvedViewer;
                return false;
            }
            if (factionCount != 2)
            {
                failure = CaptureFailure.UnsupportedFactions;
                return false;
            }
            var list = new List<ObservationStamp>();
            try
            {
                foreach (var v in native)
                    if (v.Visible && v.Weight > 0)
                        list.Add(new ObservationStamp(v.Id, v.Side, v.X, v.Z, v.Weight, v.ObservedAt, v.Stale));
            }
            catch (ArgumentException) { failure = CaptureFailure.InvalidObservation; return false; }
            stamps = list.ToArray();
            return true;
        }
    }
}
