using System;
using System.Diagnostics;
using BepInEx;
using BepInEx.Bootstrap;
using FrontlineMap.Core;
using FrontlineMap.Game;
using FrontlineMap.Rendering;
using FrontlineMap.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FrontlineMap
{
    [BepInPlugin(Guid, "FrontlineMap", PluginVersion.Value)]
    public sealed class FrontlineMapPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.noms.frontlinemap";
        private ModelSettings settings;
        private GameObservationAdapter adapter;
        private RasterOverlayRenderer renderer;
        private OverlayController controller;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private string geometryKey, lastFailure;
        private long admissionKey = -1;
        private long session = 1, mapRevision, policyRevision, sequence;
        private bool visible = true, diagnostics, exportSnapshots;
        private string exportDirectory;
        private int exported;
        private double nextCapture, lastMetric;
        private KeyCode toggle;
        private GUIStyle legendStyle;
        private Exception reportedError;
        private double Now => clock.Elapsed.TotalSeconds;
        void Awake()
        {
            try
            {
                if (ConfigMigration.CopyIfMissing(Paths.ConfigPath))
                {
                    Config.Reload();
                    Logger.LogInfo("Copied legacy config with backup; current config was absent.");
                }
                int n = Config.Bind("General", "GridResolution", 512, "64-2048; restart required. Memory grows quadratically.").Value;
                if (n < 64 || n > 2048)
                    throw new ArgumentOutOfRangeException("GridResolution");
                settings = new ModelSettings(n,
                    Config.Bind("General", "InfluenceRadius", 12000f, "Meters; restart required.").Value,
                    Config.Bind("General", "UpdateInterval", 5f, "Monotonic seconds; restart required.").Value,
                    Config.Bind("Visual", "TerritoryAlpha", .16f, "Uniform fill opacity 0-1; restart required. Legacy subtle values are preserved.").Value,
                    .01, 1e-9,
                    Config.Bind("Performance", "BudgetMilliseconds", 2f, "Provisional compute slice budget; allocations/capture/upload remain indivisible and unmeasured. Restart required.").Value,
                    Config.Bind("General", "MaximumAgeSeconds", 15f, "Maximum computation/presentation age, >= UpdateInterval. Restart required.").Value,
                    Config.Bind("Visual", "LineWidthPixels", 3f, "Opposing-front width in screen pixels; restart required.").Value);
                adapter = new GameObservationAdapter(
                    Config.Bind("Weights", "Airbase", 10f, "Heuristic support weight; restart required.").Value,
                    Config.Bind("Weights", "Ship", 5f, "Heuristic support weight; restart required.").Value,
                    Config.Bind("Weights", "Vehicle", 3f, "Heuristic support weight; restart required.").Value,
                    Config.Bind("Weights", "Aircraft", .5f, "Heuristic support weight; restart required.").Value,
                    Config.Bind("Weights", "Building", 4f, "Heuristic support weight; restart required.").Value);
                toggle = Config.Bind("General", "ToggleKey", KeyCode.F9, "Show/hide influence; restart required.").Value;
                diagnostics = Config.Bind("Diagnostics", "Enabled", false, "Opt-in per-update stage/age/resource logging; no unit locations.").Value;
                exportSnapshots = Config.Bind("Diagnostics", "ExportSnapshots", false, "Capture at most 100 sanitized native-map snapshots locally for replay; contains permitted gameplay positions. Restart required.").Value;
                if (exportSnapshots)
                    exportDirectory = System.IO.Path.Combine(Paths.BepInExRootPath, "FrontlineMap-replay", System.Guid.NewGuid().ToString("N"));
                renderer = new RasterOverlayRenderer();
                controller = new OverlayController(renderer, () => Now);
                controller.BeforePublish = RevalidateBeforePublish;
                SceneManager.sceneLoaded += OnSceneLoaded;
                Logger.LogInfo("FrontlineMap " + PluginVersion.Value + " development candidate: estimated influence from player-visible observations. Arrows disabled. Legacy SpreadFrames/UseFixedColors retired. All settings require restart.");
            }
            catch (Exception error) { Logger.LogError(error); enabled = false; }
        }
        void Start()
        {
            if (Chainloader.PluginInfos.ContainsKey("com.yuulf.frontlinemap"))
            {
                Logger.LogError("Legacy FrontlineMap is also loaded. Disable the duplicate manually; this candidate will stay disabled.");
                Reset();
                enabled = false;
            }
        }
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            session++;
            Reset();
        }
        void Reset()
        {
            controller?.Cancel(InvalidationReason.Map);
            adapter?.Clear();
            renderer?.Dispose();
            geometryKey = null;
            admissionKey = -1;
            nextCapture = 0;
        }
        bool TryRead(out MapBinding binding, out ObservationStamp[] stamps, bool capture = false)
        {
            binding = null;
            stamps = null;
            double captureStart = Now;
            if (!visible)
                return false;
            if (!MapBinding.TryCreate(settings.Resolution, out binding, out var bindingFailure))
            {
                if (lastFailure != bindingFailure)
                {
                    Logger.LogInfo("Influence hidden: " + bindingFailure);
                    lastFailure = bindingFailure;
                }
                return false;
            }
            if (!adapter.TryRead(binding, capture, out stamps, out var key, out var failure))
            {
                string reason = failure.ToString();
                if (lastFailure != reason)
                {
                    Logger.LogInfo("Influence hidden: " + reason);
                    lastFailure = reason;
                }
                return false;
            }
            if (geometryKey != binding.GeometryKey)
            {
                controller.Cancel(InvalidationReason.Map);
                renderer.Dispose();
                geometryKey = binding.GeometryKey;
                mapRevision++;
                nextCapture = 0;
            }
            if (admissionKey != key)
            {
                controller.Cancel(InvalidationReason.Team);
                admissionKey = key;
                policyRevision++;
                nextCapture = 0;
            }
            renderer.Binding = binding;
            lastFailure = null;
            if (diagnostics)
                captureMilliseconds = (Now - captureStart) * 1000;
            return true;
        }
        double captureMilliseconds;
        SnapshotIdentity Identity() => new SnapshotIdentity(session, mapRevision, policyRevision, 1, sequence);
        void RevalidateBeforePublish()
        {
            if (!TryRead(out _, out _))
            {
                Reset();
                return;
            }
            controller.SetContext(Identity(), Now, true);
        }
        void LateUpdate()
        {
            if (controller == null)
                return;
            try
            {
                if (Input.GetKeyDown(toggle))
                {
                    visible = !visible;
                    Reset();
                }
                if (!TryRead(out var binding, out var stamps, Now >= nextCapture))
                {
                    Reset();
                    return;
                }
                double now = Now;
                controller.SetContext(Identity(), now, true);
                if (now >= nextCapture)
                {
                    if (stamps == null)
                    {
                        if (!TryRead(out binding, out stamps, true))
                        {
                            Reset();
                            return;
                        }
                        controller.SetContext(Identity(), now, true);
                    }
                    sequence++;
                    if (diagnostics)
                        Logger.LogInfo(string.Format(System.Globalization.CultureInfo.InvariantCulture, "FLM_CAPTURE seq={0} read_ms={1:F3} admitted={2}", sequence, captureMilliseconds, stamps.Length));
                    var snapshot = new ObservationSnapshot(Identity(), binding.Geometry, stamps, now, Time.timeSinceLevelLoad, now + settings.MaximumAge);
                    if (exportSnapshots && exported < 100)
                    {
                        System.IO.Directory.CreateDirectory(exportDirectory);
                        System.IO.File.WriteAllText(System.IO.Path.Combine(exportDirectory, sequence.ToString("D8") + ".snapshot.json"), FrontlineMap.Diagnostics.SnapshotWriter.ToJson(snapshot, settings));
                        exported++;
                    }
                    controller.Submit(snapshot, settings, binding.ScreenWidth, binding.ScreenHeight);
                    nextCapture = now + settings.Interval;
                }
                if (controller.Displayed != null && !controller.Busy &&
                    (Math.Abs(controller.Displayed.ScreenWidth - binding.ScreenWidth) > .5 || Math.Abs(controller.Displayed.ScreenHeight - binding.ScreenHeight) > .5))
                    controller.Repaint(settings, binding.ScreenWidth, binding.ScreenHeight);
                double stepStart = Now;
                controller.Tick(now, now + settings.BudgetMilliseconds / 1000);
                if (controller.LastError != null && controller.LastError != reportedError)
                {
                    reportedError = controller.LastError;
                    Logger.LogError(reportedError);
                }
                if (diagnostics && controller.Displayed != null && controller.Displayed.Snapshot.CapturedAt != lastMetric)
                {
                    lastMetric = controller.Displayed.Snapshot.CapturedAt;
                    Logger.LogInfo(string.Format(System.Globalization.CultureInfo.InvariantCulture, "FLM_METRIC seq={0} latest_read_ms={1:F3} step_with_prepare_upload_ms={2:F3} age_s={3:F3} owned_textures={4} owned_objects={5} allocate_ms={6:F3} stamp_ms={7:F3} raster_ms={8:F3} max_chunk_ms={9:F3} copy_ms={10:F3} native_prepare_upload_ms={11:F3} published={12} rejected={13} expired={14} pending_replaced={15}", controller.Displayed.Snapshot.Identity.Sequence, captureMilliseconds, (Now - stepStart) * 1000, Now - lastMetric, renderer.OwnedTextures, renderer.OwnedObjects, controller.Metrics.Milliseconds(FrontlineMap.Diagnostics.WorkStage.Allocation), controller.Metrics.Milliseconds(FrontlineMap.Diagnostics.WorkStage.Stamp), controller.Metrics.Milliseconds(FrontlineMap.Diagnostics.WorkStage.Raster), controller.Metrics.MaximumChunkMilliseconds, renderer.LastCopyMilliseconds, renderer.LastUploadMilliseconds, controller.PublishedCount, controller.RejectedCount, controller.ExpiredCount, controller.PendingReplacementCount));
                }
            }
            catch (Exception error) { Logger.LogError(error); Reset(); }
        }
        void OnGUI()
        {
            if (!DynamicMap.mapMaximized || controller?.Displayed == null || renderer.Binding?.Map == null || !renderer.Binding.Map.gameObject.activeInHierarchy)
                return;
            bool stale = false;
            foreach (var s in controller.Displayed.Snapshot.Stamps)
                if (s.Stale)
                {
                    stale = true;
                    break;
                }
            if (legendStyle == null)
                legendStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontSize = 12, richText = true, padding = new RectOffset(10, 10, 5, 5) };
            GUI.Box(new Rect(16, Screen.height - 80, Math.Min(420, Screen.width - 32), 64),
                "Estimated influence  |  age " + (Now - controller.Displayed.Snapshot.CapturedAt).ToString("F1") + "s\n"
                + "<color=#419BF5>Friendly</color>  <color=#F05A46>Hostile</color>  <color=#B182D7>Balanced</color>  <color=#F0E19B>Opposing support</color>\n"
                + "Player-visible observations" + (stale ? " | last-known contacts" : ""), legendStyle);
        }
        void OnDisable()
        {
            Reset();
        }
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            controller?.Dispose();
            renderer?.Dispose();
        }
    }
}
