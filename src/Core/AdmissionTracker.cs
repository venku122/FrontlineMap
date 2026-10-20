using System;
using System.Collections.Generic;
namespace FrontlineMap.Core
{
    public struct AdmissionToken : IEquatable<AdmissionToken>
    {
        public readonly long Id;
        public readonly int Owner;
        public readonly double Weight;
        public readonly ObservationRole Role;
        public readonly bool Stale, Airbase;
        public AdmissionToken(long id, int owner, double weight, bool stale, bool airbase, ObservationRole role = ObservationRole.Unknown)
        {
            Id = id;
            Owner = owner;
            Weight = weight;
            Role = role;
            Stale = stale;
            Airbase = airbase;
        }
        public override int GetHashCode() => Id.GetHashCode() ^ Owner ^ Weight.GetHashCode() ^ (int)Role ^ (Stale ? 17 : 0) ^ (Airbase ? 31 : 0);
        public override bool Equals(object other) => other is AdmissionToken token && Equals(token);
        public bool Equals(AdmissionToken other) => Id == other.Id && Owner == other.Owner
            && Weight == other.Weight && Role == other.Role && Stale == other.Stale && Airbase == other.Airbase;
    }
    // Current native admissions are scanned every frame; only revocation of a captured
    // input invalidates existing work. Additions cannot reveal extra information in an
    // older snapshot and enter on the normal capture cadence. Hash collisions are resolved
    // by exact token equality; there is no hash-only authorization decision.
    public sealed class AdmissionTracker
    {
        private readonly HashSet<AdmissionToken> current = new HashSet<AdmissionToken>();
        private readonly List<AdmissionToken> captured = new List<AdmissionToken>();
        private int viewer, previousViewer;
        private bool initialized;
        public long Revision { get; private set; }
        public void Begin(int viewer)
        {
            this.viewer = viewer;
            current.Clear();
        }
        // Freshness is presentation metadata, not loss of native knowledge. Previously
        // visible positions remain known across fresh/stale transitions; actual marker
        // removal, viewer, owner, type/weight and strategic-kind changes still revoke.
        public void Add(AdmissionToken value) => current.Add(new AdmissionToken(value.Id, value.Owner, value.Weight, false, value.Airbase, value.Role));
        public void CommitCapture()
        {
            captured.Clear();
            foreach (var token in current)
                captured.Add(token);
        }
        public void Clear()
        {
            current.Clear();
            captured.Clear();
            initialized = false;
        }
        public long Complete()
        {
            bool revoked = !initialized || viewer != previousViewer;
            if (!revoked)
                foreach (var token in captured)
                    if (!current.Contains(token))
                    {
                        revoked = true;
                        break;
                    }
            if (revoked)
            {
                Revision++;
                captured.Clear();
            }
            previousViewer = viewer;
            initialized = true;
            return Revision;
        }
    }
}
