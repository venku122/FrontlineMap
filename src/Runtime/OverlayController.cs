using System;
using FrontlineMap.Core;
namespace FrontlineMap.Runtime
{
    // Prepare must be invisible; Commit must bind prepared resources atomically or leave old output intact.
    public interface IPresentation
    {
        IDisposable Prepare(FieldResult result);
        void Commit(IDisposable staged);
        void Clear();
        void Release(IDisposable handle);
    }
    public sealed class OverlayController : IDisposable
    {
        private readonly IPresentation view;
        private readonly Func<double> clock;
        private readonly StepScheduler scheduler;
        private SnapshotIdentity context;
        private bool valid;
        private long lastSequence;
        private IDisposable shown;
        public FieldResult Displayed
        {
            get; private set;
        }
        public bool RetainingPreviousKnowledge { get; private set; }
        public long PublishedCount
        {
            get; private set;
        }
        public long RejectedCount
        {
            get; private set;
        }
        public long ExpiredCount => scheduler.ExpiredCount;
        public long PendingReplacementCount => scheduler.PendingReplacementCount;
        public Exception LastError
        {
            get; private set;
        }
        public Action BeforePublish; // Optional host revalidation callback, also used for race injection.
        public bool Busy => scheduler.Busy;
        public FrontlineMap.Diagnostics.OverlayMetrics Metrics => scheduler.Metrics;
        public OverlayController(IPresentation presentation, Func<double> clock)
        {
            view = presentation;
            this.clock = clock;
            scheduler = new StepScheduler(clock);
        }
        public void SetContext(SnapshotIdentity identity, double now, bool isValid)
        {
            if (!isValid || (valid && !context.SameContext(identity)))
                Cancel(InvalidationReason.Map);
            context = identity;
            valid = isValid;
            if (Displayed != null && now >= Displayed.Snapshot.ExpiresAt)
                ClearShown();
        }
        // A same-viewer knowledge revision may keep the last complete estimate until
        // its original expiry. It cannot authorize old-policy computation or publication.
        public void ReviseKnowledge(SnapshotIdentity identity, double now, bool sameViewer)
        {
            bool retain = sameViewer && valid && Displayed != null && shown != null
                && context.Session == identity.Session && context.Map == identity.Map
                && context.Model == identity.Model && ModelSettings.Finite(now)
                && now >= 0 && now < Displayed.Snapshot.ExpiresAt;
            if (!retain)
            {
                // Force cancellation even when all identity values happen to match:
                // the caller's viewer guard is an independent authorization boundary.
                Cancel(InvalidationReason.Map);
                SetContext(identity, now, true);
                return;
            }
            scheduler.Cancel();
            context = identity;
            RetainingPreviousKnowledge = true;
        }
        public void Submit(ObservationSnapshot snapshot, ModelSettings settings, double width, double height)
        {
            if (!valid || !context.SameContext(snapshot.Identity))
                return;
            scheduler.Begin(snapshot, settings, width, height);
        }
        public void Repaint(ModelSettings settings, double width, double height)
        {
            if (valid && Displayed != null && !RetainingPreviousKnowledge)
                scheduler.BeginRepaint(Displayed, settings, width, height);
        }
        public void Tick(double now, double deadline)
        {
            if (!valid)
                return;
            if (Displayed != null && now >= Displayed.Snapshot.ExpiresAt)
                ClearShown();
            scheduler.DiscardExpired(now);
            IDisposable staged = null;
            try
            {
                scheduler.Step(deadline);
                if (!scheduler.TryTakeCompleted(out var result))
                    return;
                if (!CanPublish(result, now))
                {
                    RejectedCount++;
                    return;
                }
                staged = view.Prepare(result);
                BeforePublish?.Invoke();
                if (!CanPublish(result, Math.Max(now, clock())))
                {
                    RejectedCount++;
                    return;
                }
                view.Commit(staged);
                var old = shown;
                shown = staged;
                staged = null;
                Displayed = result;
                RetainingPreviousKnowledge = false;
                lastSequence = result.Snapshot.Identity.Sequence;
                PublishedCount++;
                if (old != null)
                    view.Release(old);
                LastError = null;
            }
            catch (Exception error) { LastError = error; scheduler.Cancel(); }
            finally { if (staged != null) view.Release(staged); }
        }
        private bool CanPublish(FieldResult result, double now) => valid && context.SameContext(result.Snapshot.Identity) && (result.Snapshot.Identity.Sequence > lastSequence || (Displayed != null && result.Snapshot.Identity.Equals(Displayed.Snapshot.Identity) && ReferenceEquals(result.Field, Displayed.Field))) && now < result.Snapshot.ExpiresAt;
        public void Cancel(InvalidationReason reason)
        {
            valid = false;
            lastSequence = 0;
            scheduler.Cancel();
            ClearShown();
        }
        private void ClearShown()
        {
            RetainingPreviousKnowledge = false;
            view.Clear();
            var old = shown;
            shown = null;
            Displayed = null;
            if (old != null)
                view.Release(old);
        }
        public void Dispose() => Cancel(InvalidationReason.Shutdown);
    }
}
