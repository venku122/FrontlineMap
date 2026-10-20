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
        private readonly TerrainSampler terrain = new TerrainSampler();
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
        private static readonly TacticalLayer[] LayerModes = { TacticalLayer.GroundControl, TacticalLayer.AirThreat, TacticalLayer.NavalInfluence, TacticalLayer.Confidence, TacticalLayer.Legacy };
        private static readonly string[] LayerNames = { "Ground", "Air threat", "Naval", "Confidence", "Legacy" };
        private OverlayAppearance appearance = OverlayAppearance.Default;
        private long modelRevision;
        private volatile bool reloadRequested;
        private string configurationError;
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
                if (!TryApplyConfiguration(true)) { enabled = false; return; }
                Config.SettingChanged += ConfigurationChanged;
                Config.ConfigReloaded += ConfigurationReloaded;
                renderer = new RasterOverlayRenderer { Appearance = appearance };
                controller = new OverlayController(renderer, () => Now);
                controller.BeforePublish = RevalidateBeforePublish;
                SceneManager.sceneLoaded += OnSceneLoaded;
                Logger.LogInfo("FrontlineMap " + PluginVersion.Value + " development candidate: estimated influence from player-visible observations. Arrows disabled. Legacy SpreadFrames/UseFixedColors retired. F10 reloads validated settings.");
            }
            catch (Exception error) { Logger.LogError(error); enabled = false; }
        }
        bool TryApplyConfiguration(bool initial)
        {
            reloadRequested = false;
            try
            {
                int n = Config.Bind("General", "GridResolution", 512, "64-2048; F10 reload. Memory grows quadratically.").Value;
                if (n < 64 || n > 2048) throw new ArgumentOutOfRangeException("GridResolution");
                string layerName = Config.Bind("Model", "Layer", "GroundControl", "GroundControl, AirThreat, NavalInfluence, Confidence or Legacy. F10 reload.").Value;
                if (!Enum.TryParse<TacticalLayer>(layerName, true, out var selectedLayer) || !Enum.IsDefined(typeof(TacticalLayer), selectedLayer)) throw new ArgumentException("Invalid tactical Layer");
                var tactical = new TacticalSettings(selectedLayer,
                    Config.Bind("Model", "TerrainEnabled", true, "Coarse public terrain path constraints for ground/naval fields; false uses radial distances.").Value,
                    Config.Bind("Model", "TerrainResolution", 128, "32-256 static terrain samples per axis; cached, incremental.").Value,
                    Config.Bind("Radii", "GroundMeters", 6000f, "Heuristic ground presence reach, not a weapon range.").Value,
                    Config.Bind("Radii", "AirbaseMeters", 8000f, "Heuristic strategic base influence.").Value,
                    Config.Bind("Radii", "BuildingMeters", 2000f, "Local strategic building influence.").Value,
                    Config.Bind("Radii", "AircraftMeters", 20000f, "Heuristic air threat reach; no radar/weapon LOS claim.").Value,
                    Config.Bind("Radii", "AirDefenseMeters", 12000f, "Known SAM/SPAAG map-name heuristic.").Value,
                    Config.Bind("Radii", "NavalMeters", 16000f, "Heuristic naval reach along sampled water.").Value,
                    Config.Bind("Confidence", "StaleHalfLifeSeconds", 120f, "Known stale position confidence halves over simulation time; no hidden motion extrapolation.").Value,
                    Config.Bind("Confidence", "ContestedRatio", .2f, "Contested band: abs(F-E)/(F+E) <= ratio, with both supports present.").Value,
                    Config.Bind("Confidence", "MinimumFrontConfidence", .5f, "Minimum local confidence to draw precise gold contour.").Value,
                    Config.Bind("Model", "BuildingControlFactor", .15f, "Multiply building weight in ground control; keeps industry from dominating by count.").Value);
                var nextSettings = new ModelSettings(n,
                    Config.Bind("General", "InfluenceRadius", 12000f, "Meters; F10 reload.").Value,
                    Config.Bind("General", "UpdateInterval", 5f, "Monotonic seconds; F10 reload.").Value,
                    Config.Bind("Visual", "TerritoryAlpha", .16f, "Uniform fill opacity 0-1; F10 reload.").Value,
                    .01, 1e-9,
                    Config.Bind("Performance", "BudgetMilliseconds", 2f, "Compute slice budget; capture/allocation/upload remain indivisible. F10 reload.").Value,
                    Config.Bind("General", "MaximumAgeSeconds", 15f, "Maximum computation/presentation age, >= UpdateInterval. F10 reload.").Value,
                    Config.Bind("Visual", "LineWidthPixels", 3f, "Opposing-front width in screen pixels; F10 reload.").Value, tactical);
                var nextAdapter = new GameObservationAdapter(
                    Config.Bind("Weights", "Airbase", 10f, "Heuristic support weight; F10 reload.").Value,
                    Config.Bind("Weights", "Ship", 5f, "Heuristic support weight; F10 reload.").Value,
                    Config.Bind("Weights", "Vehicle", 3f, "Heuristic support weight; F10 reload.").Value,
                    Config.Bind("Weights", "Aircraft", .5f, "Heuristic support weight; F10 reload.").Value,
                    Config.Bind("Weights", "Building", 4f, "Heuristic support weight; F10 reload.").Value);
                var nextAppearance = new OverlayAppearance(
                    Config.Bind("Visual", "FriendlyColor", "419BF5", "Six RGB hex digits, optional #. F10 reload.").Value,
                    Config.Bind("Visual", "EnemyColor", "F05A46", "Six RGB hex digits, optional #. F10 reload.").Value,
                    Config.Bind("Visual", "BalancedColor", "B182D7", "Six RGB hex digits, optional #. F10 reload.").Value,
                    Config.Bind("Visual", "FrontColor", "F0E19B", "Six RGB hex digits, optional #. F10 reload.").Value);
                if (nextAppearance.Friendly == OverlayAppearance.Default.Friendly && nextAppearance.Enemy == OverlayAppearance.Default.Enemy && nextAppearance.Balanced == OverlayAppearance.Default.Balanced && nextAppearance.Front == OverlayAppearance.Default.Front)
                    nextAppearance = OverlayAppearance.Default;
                var nextToggle = Config.Bind("General", "ToggleKey", KeyCode.F9, "Show/hide influence; F10 reloads settings.").Value;
                if (nextToggle == KeyCode.F10) throw new ArgumentException("F10 is reserved for reloading settings.");
                bool nextDiagnostics = Config.Bind("Diagnostics", "Enabled", false, "Opt-in per-update stage/age/resource logging; no unit locations.").Value;
                bool nextExports = Config.Bind("Diagnostics", "ExportSnapshots", false, "At most100 sanitized snapshots per process; contains permitted positions. F10 reload.").Value;
                string nextDirectory = nextExports ? exportDirectory ?? System.IO.Path.Combine(Paths.BepInExRootPath, "FrontlineMap-replay", System.Guid.NewGuid().ToString("N")) : exportDirectory;
                // All validation precedes replacement. Invalid changes retain the working model/resources.
                if (!initial) Reset();
                settings = nextSettings; adapter = nextAdapter; appearance = nextAppearance;
                toggle = nextToggle; diagnostics = nextDiagnostics; exportSnapshots = nextExports; exportDirectory = nextDirectory;
                modelRevision++;
                if (renderer != null) renderer.Appearance = appearance;
                configurationError = null;
                return true;
            }
            catch (Exception error)
            {
                configurationError = "Settings rejected; previous configuration retained: " + error.Message;
                Logger.LogWarning(configurationError);
                return false;
            }
        }
        void ConfigurationChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs args) { reloadRequested = true; }
        void ConfigurationReloaded(object sender, EventArgs args) { reloadRequested = true; }
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
            terrain.Clear();
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
        SnapshotIdentity Identity() => new SnapshotIdentity(session, mapRevision, policyRevision, modelRevision, sequence);
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
                if (Input.GetKeyDown(KeyCode.F10))
                {
                    try { Config.Reload(); reloadRequested = true; }
                    catch (Exception error) { configurationError = "Settings reload failed: " + error.Message; }
                }
                if (reloadRequested) TryApplyConfiguration(false);
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
                bool needsTerrain = settings.Tactical.Layer != TacticalLayer.Legacy && settings.Tactical.Layer != TacticalLayer.AirThreat && settings.Tactical.TerrainEnabled;
                if (needsTerrain)
                {
                    terrain.GetOrBegin(binding.Geometry, settings.Tactical.TerrainResolution);
                    terrain.Step(Math.Min(2, settings.BudgetMilliseconds));
                    if (!terrain.Ready) { controller.SetContext(Identity(), Now, false); return; }
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
                    var snapshot = new ObservationSnapshot(Identity(), binding.Geometry, stamps, now, Time.timeSinceLevelLoad, now + settings.MaximumAge, needsTerrain ? terrain.Grid : null);
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
            if (!visible || !DynamicMap.mapMaximized || renderer?.Binding?.Map == null || !renderer.Binding.Map.gameObject.activeInHierarchy)
                return;
            var result = controller?.Displayed;
            bool snapshotStale = false;
            if (result != null) foreach (var stamp in result.Snapshot.Stamps) if (stamp.Stale) { snapshotStale = true; break; }
            if (legendStyle == null)
                legendStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontSize = 12, richText = true, padding = new RectOffset(10, 10, 5, 5) };
            var layer = settings.Tactical.Layer;
            string title = layer == TacticalLayer.GroundControl ? "Ground control estimate" : layer == TacticalLayer.AirThreat ? "Air threat estimate (no LOS)"
                : layer == TacticalLayer.NavalInfluence ? "Naval influence estimate" : layer == TacticalLayer.Confidence ? "Ground information confidence" : "Legacy combined influence";
            string status = result == null ? (controller.Busy ? "Computing..." : "Waiting for permitted data...")
                : "snapshot age " + (Now - result.Snapshot.CapturedAt).ToString("F1") + "s" + (controller.Busy ? " | refreshing" : "");
            string labels = layer == TacticalLayer.Confidence ? "Green: fresher known support | amber: older | blank: insufficient support"
                : "<color=#" + appearance.Friendly + ">Friendly</color>  <color=#" + appearance.Enemy + ">Hostile</color>  <color=#" + appearance.Balanced + ">Contested</color>  <color=#" + appearance.Front + ">Higher-confidence front</color>";
            if (layer == TacticalLayer.AirThreat || layer == TacticalLayer.NavalInfluence)
                labels = "<color=#"+appearance.Friendly+">Friendly</color>  <color=#"+appearance.Enemy+">Hostile</color>  <color=#"+appearance.Balanced+">Overlapping influence</color> | no land boundary";
            if (layer == TacticalLayer.Legacy) labels = labels.Replace("Higher-confidence front", "Opposing support");
            string terrainStatus = layer == TacticalLayer.Legacy || layer == TacticalLayer.AirThreat ? "Radial heuristic; no terrain/weapon LOS model"
                : settings.Tactical.TerrainEnabled ? terrain.Status : "Terrain disabled; radial heuristic";
            string text = title + " | " + status + "\n" + labels + "\nKnown stale contacts: " + adapter.StaleContactCount
                + (snapshotStale ? " | snapshot includes last-known data" : "") + "\n" + terrainStatus + "\nF9: overlay | F10: reload";
            if (result != null && result.ContourLimited) text += " | Dense contour: contested band only";
            if (configurationError != null) text += "\nSettings rejected; previous configuration retained";
            float height = configurationError == null ? 104 : 120;
            GUI.Box(new Rect(16, Screen.height - height - 48, Math.Min(660, Screen.width - 32), height), text, legendStyle);
            for (int i = 0; i < LayerModes.Length; i++)
                if (GUI.Button(new Rect(16 + i * 105, Screen.height - 40, 101, 26), (layer == LayerModes[i] ? "• " : "") + LayerNames[i]) && layer != LayerModes[i])
                    Config[new BepInEx.Configuration.ConfigDefinition("Model", "Layer")].BoxedValue = LayerModes[i].ToString();
        }
        void OnDisable()
        {
            Reset();
        }
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Config.SettingChanged -= ConfigurationChanged;
            Config.ConfigReloaded -= ConfigurationReloaded;
            terrain.Clear();
            controller?.Dispose();
            renderer?.Dispose();
        }
    }
}
