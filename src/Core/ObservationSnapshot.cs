using System;
using System.Collections.Generic;
using System.Linq;
namespace FrontlineMap.Core
{
    public enum Side
    {
        Friendly, Enemy
    }
    public enum CaptureFailure
    {
        None, UnresolvedViewer, UnsupportedSpectator, UnsupportedFactions, InvalidMap, Jammed, InvalidObservation
    }
    public struct SnapshotIdentity : IEquatable<SnapshotIdentity>
    {
        public readonly long Session, Map, Policy, Model, Sequence;
        public SnapshotIdentity(long session, long map, long policy, long model, long sequence)
        {
            Session = session;
            Map = map;
            Policy = policy;
            Model = model;
            Sequence = sequence;
        }
        public bool SameContext(SnapshotIdentity o) => Session == o.Session && Map == o.Map && Policy == o.Policy && Model == o.Model;
        public bool Equals(SnapshotIdentity o) => SameContext(o) && Sequence == o.Sequence;
        public override bool Equals(object o) => o is SnapshotIdentity s && Equals(s);
        public override int GetHashCode() => Session.GetHashCode() ^ Map.GetHashCode() ^ Policy.GetHashCode() ^ Model.GetHashCode() ^ Sequence.GetHashCode();
    }
    public sealed class MapGeometry
    {
        public readonly double MinX, MinZ, Width, Height;
        public readonly int Columns, Rows, Count;
        public double CellX => Width / Columns; public double CellZ => Height / Rows;
        public MapGeometry(double minX, double minZ, double width, double height, int columns, int rows)
        {
            if (!ModelSettings.Finite(minX) || !ModelSettings.Finite(minZ) || !ModelSettings.Positive(width) || !ModelSettings.Positive(height) || !ModelSettings.Finite(minX + width) || !ModelSettings.Finite(minZ + height) || !ModelSettings.Positive(width / columns) || !ModelSettings.Positive(height / rows) || minX + width == minX || minZ + height == minZ || columns < 2 || rows < 2 || columns > 2048 || rows > 2048)
                throw new ArgumentOutOfRangeException("geometry");
            MinX = minX;
            MinZ = minZ;
            Width = width;
            Height = height;
            Columns = columns;
            Rows = rows;
            Count = checked(columns * rows);
        }
        public double X(int x) => MinX + (x + .5) * CellX; public double Z(int z) => MinZ + (z + .5) * CellZ;
        public double U(double x) => (x - MinX) / Width; public double V(double z) => (z - MinZ) / Height;
        public static double CroppedCoordinate(double uv, double start, double size)
        {
            if (!ModelSettings.Positive(size))
                throw new ArgumentOutOfRangeException("size");
            return (uv - start) / size;
        }
    }
    public sealed class ObservationStamp
    {
        public readonly string Id; public readonly Side Side; public readonly double X, Z, Weight, ObservedTime; public readonly bool Stale;
        public ObservationStamp(string id, Side side, double x, double z, double weight, double observedTime, bool stale)
        {
            if (string.IsNullOrEmpty(id) || !ModelSettings.Finite(x) || !ModelSettings.Finite(z) || !ModelSettings.Finite(weight) || weight < 0 || weight > 1e6 || !ModelSettings.Finite(observedTime) || (side != Side.Friendly && side != Side.Enemy))
                throw new ArgumentOutOfRangeException("observation");
            Id = id;
            Side = side;
            X = x;
            Z = z;
            Weight = weight;
            ObservedTime = observedTime;
            Stale = stale;
        }
    }
    public sealed class ObservationSnapshot
    {
        public readonly SnapshotIdentity Identity; public readonly MapGeometry Geometry; public readonly double CapturedAt, SimulationTime, ExpiresAt; public IReadOnlyList<ObservationStamp> Stamps
        {
            get;
        }
        public ObservationSnapshot(SnapshotIdentity identity, MapGeometry geometry, IEnumerable<ObservationStamp> stamps, double capturedAt, double simulationTime, double expiresAt)
        {
            if (geometry == null || stamps == null || !ModelSettings.Finite(capturedAt) || !ModelSettings.Finite(simulationTime) || !ModelSettings.Finite(expiresAt) || expiresAt < capturedAt)
                throw new ArgumentOutOfRangeException("snapshot");
            var a = stamps.ToArray();
            if (a.Length > 100000 || a.Any(v => v == null) || a.Select(v => v.Id).Distinct(StringComparer.Ordinal).Count() != a.Length)
                throw new ArgumentException("Invalid or duplicate permitted IDs");
            Array.Sort(a, (a1, a2) => StringComparer.Ordinal.Compare(a1.Id, a2.Id));
            Stamps = Array.AsReadOnly(a);
            Identity = identity;
            Geometry = geometry;
            CapturedAt = capturedAt;
            SimulationTime = simulationTime;
            ExpiresAt = expiresAt;
        }
    }
}
