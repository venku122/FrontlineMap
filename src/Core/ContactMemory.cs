using System;
using System.Collections.Generic;
namespace FrontlineMap.Core
{
    // Copied native knowledge only: no game objects or hidden-world lookup handles.
    public sealed class RetainedContact
    {
        public readonly NativeObservation Observation;
        public readonly AdmissionToken Token;
        public RetainedContact(NativeObservation observation, AdmissionToken token)
        {
            Observation = new NativeObservation(observation.Id, observation.Side, observation.X, observation.Z,
                observation.Weight, observation.ObservedAt, observation.Stale, observation.Visible, observation.Role);
            Token = token;
        }
    }
    public sealed class ContactMemory
    {
        private readonly Dictionary<string, LinkedListNode<RetainedContact>> entries;
        private readonly LinkedList<RetainedContact> insertionOrder = new LinkedList<RetainedContact>();
        private bool hasContext, hasTime;
        private long currentContext;
        private double previousTime;
        public int Count => entries.Count;
        public int Capacity { get; }
        public double MaximumAge { get; }
        public IEnumerable<RetainedContact> Contacts => insertionOrder;

        public ContactMemory(int capacity = 4096, double maximumAge = 900)
        {
            if (capacity <= 0 || !ModelSettings.Finite(maximumAge) || maximumAge < 0)
                throw new ArgumentOutOfRangeException("history bounds");
            Capacity = capacity;
            MaximumAge = maximumAge;
            entries = new Dictionary<string, LinkedListNode<RetainedContact>>(StringComparer.Ordinal);
        }
        public void BeginContext(long context)
        {
            if (!hasContext || currentContext != context) Clear();
            currentContext = context;
            hasContext = true;
        }
        public void Remember(NativeObservation observation, AdmissionToken token, double now)
        {
            Prune(now);
            if (!hasTime || MaximumAge == 0 || observation == null || string.IsNullOrEmpty(observation.Id)) return;
            entries.TryGetValue(observation.Id, out var existing);
            bool conflict = existing != null && !SameAuthorization(existing.Value.Token, token);
            double oldTime = existing == null ? double.NegativeInfinity : existing.Value.Observation.ObservedAt;
            if (conflict) Remove(observation.Id);
            if (!Valid(observation, token, now) || now - observation.ObservedAt >= MaximumAge) return;
            // Repeated reads of the same authorization cannot advance position or age.
            // Changed authorization revokes the old copy; its valid replacement may
            // carry a cached timestamp equal to or older than the revoked record.
            if (!conflict && observation.ObservedAt <= oldTime) return;
            var retained = new RetainedContact(observation, token);
            if (existing != null && !conflict)
            {
                existing.Value = retained;
                return;
            }
            if (entries.Count == Capacity) Remove(insertionOrder.First.Value.Observation.Id);
            entries.Add(observation.Id, insertionOrder.AddLast(retained));
        }
        public bool TryGet(string id, out RetainedContact contact)
        {
            contact = null;
            if (id == null || !entries.TryGetValue(id, out var node)) return false;
            contact = node.Value;
            return true;
        }
        public bool Remove(string id)
        {
            if (id == null || !entries.TryGetValue(id, out var node)) return false;
            entries.Remove(id);
            insertionOrder.Remove(node);
            return true;
        }
        public void Prune(double now)
        {
            if (!ModelSettings.Finite(now) || now < 0) { Clear(); return; }
            if (hasTime && now < previousTime) Clear();
            // Many admissions share one simulation instant. Newly inserted entries are
            // independently age-checked, so there is no need to rescan them at that instant.
            if (hasTime && now == previousTime) return;
            previousTime = now;
            hasTime = true;
            var node = insertionOrder.First;
            while (node != null)
            {
                var next = node.Next;
                if (now - node.Value.Observation.ObservedAt >= MaximumAge) Remove(node.Value.Observation.Id);
                node = next;
            }
        }
        public void Clear()
        {
            entries.Clear();
            insertionOrder.Clear();
            hasTime = false;
        }
        private static bool SameAuthorization(AdmissionToken a, AdmissionToken b) =>
            a.Id == b.Id && a.Owner == b.Owner && a.Weight == b.Weight && a.Role == b.Role && a.Airbase == b.Airbase;
        private static bool Valid(NativeObservation observation, AdmissionToken token, double now) =>
            observation.Side == Side.Enemy && observation.Visible &&
            ModelSettings.Finite(observation.ObservedAt) && observation.ObservedAt >= 0 && observation.ObservedAt <= now &&
            ModelSettings.Finite(observation.X) && ModelSettings.Finite(observation.Z) &&
            ModelSettings.Finite(observation.Weight) && observation.Weight > 0 && observation.Weight <= 1e6 &&
            observation.Role >= ObservationRole.Unknown && observation.Role <= ObservationRole.AirDefense &&
            observation.Weight == token.Weight && observation.Role == token.Role;
    }
}
