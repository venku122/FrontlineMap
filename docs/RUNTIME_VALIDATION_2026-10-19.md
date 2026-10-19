# Local acceptance continuation — 2026-10-19

Readiness: native-tested development candidate. Full production acceptance is still open. Tests use disposable copies of Nuclear Option0.34.1, Steam build24724372, Unity2022.3.62.7762112 and BepInEx5.4.23.5. The normal Steam installation is preserved. No remote publication or normal-profile upgrade is part of this work.

## Renderer defects found and fixed in1.5.2

A native exception immediately after Commit's Transform.SetParent left an enabled texture-less RawImage on the canvas and partially assigned renderer fields. Later commits could not recover. A separate native dirty-material callback threw after RawImage stored the new staged texture; the controller then released that texture, leaving a white image instead of its previously shown handle.

Both failures were observed on the exact1.5.1 delivery (`22b28aa197f311a12aafb89382a2de229e039b9d221db40bd86b4ba0d1fbce68`) before changing production code. The first callback observer incorrectly used C# null-conditional texture.name, which throws on destroyed Unity objects and skipped the broken interval. Fixing the observer alone exposed the second failure. That earlier false-green record is superseded by the corrected red test.

Version1.5.2 constructs a disabled, non-raycasting image using local references, assigns renderer fields only after layout/parenting succeeds, and disables/destroys unfinished objects on failure. Texture publication restores the previous texture and enabled state on exception; if restoration itself fails, it hides the image. Both native regressions passed after this change: no white images, cleanup/recovery after partial creation, and the shown texture preserved after the one-shot callback. Portable core tests remain88passed;9synthetic and3recorded native replay fields still match exactly. Native regression sources/run instructions are in `tests/native`.

The remaining production read/solver/knowledge/network code is unchanged from1.5.1. The delivery's final binary hash, source HEAD, reference attestation and subsequent per-case runtime results identify the exact packaged candidate; preliminary pre-commit binaries are distinguished in raw evidence.

## Executed broader local checks

- Controlled native tracking RPC/TrackingInfo/marker updates: hidden rejection, fresh admission, stale flag and held last-known position, forget removal and reacquisition. Hidden unrepresented position/neutral-owner/definition changes and hidden death left sanitized content unchanged at fixed frame/time. Known native ownership and death changed admission as expected. These synchronous adapter tests do not alone certify every displayed transition.
- A separate actual frame-loop race disabled a captured native unit at renderer Prepare without a fixture TryRead/Reset. Production BeforePublish rejected the staged result, cleared output and released staging; the next capture recovered. Pure lifecycle tests cover other active/pending/staging contexts separately.
- Real loopback UDP host/client processes connected. Same-faction390unit IDs and side assignments matched;159static buildings differed by at most0.00278m. Moving native positions differed by up to280.34m after only the host was paused, so pixel-identical network output is not claimed. Each client's current-marker transform audit passed. Opposing factions had381versus427admitted units; all7shared units reversed side correctly. A client with FrontlineMap absent connected successfully.
- Readonly native sensor polling during a live client mission observed4fresh-to-stale and3stale-to-fresh transitions,102new-marker events and72removal events. Removal reasons were not inferred, and this monitor did not compare every displayed overlay transition.
- Twenty real MissionManager restarts alternated factions: unassigned states owned0textures/0objects; displayed states1/1. A separate20-restart control with the candidate absent also completed. Both native maps loaded: Heartland and Ignus Archipelago (native naval scenario).
- Vanilla was the only loaded native theme. Window sizes1024x768,1920x1080,2560x1440 and1280x800 were checked at reported144DPI. Actual native pan offsets changed map positions; native aircraft yaw produced minimap angles0/90/180degrees. Visible native marker raycasts did not hit the overlay. Local aircraft damage/death and a new spawn recovered the minimap. Simulation pause held the game clock while monotonic publication continued.
- A copied subset of installed map/UI mods (NOX,NOXMFD,NO_Tactitools and map-trails) passed full-map and cockpit-minimap transform/raycast checks, then was removed from the disposable profile. Other installed mods are not presumed compatible.
- Native method-entry faults in Prepare/Commit recovered. The deeper1.5.2 regressions cover partial UI creation and one-shot dirty-material publication callbacks. GPU-driver failures and every possible partial upload/persistent/reentrant callback remain outside this evidence.

## What geometric audits establish

The current-marker audit projects current native marker coordinates through the overlay transform. Zero error verifies coordinate transforms and marker presence, not the committed texture's contact age/location, pixel colors, clipping at every edge or gameplay truth. Known/stale positions, replay values, lifecycle revocation and actual screenshots have separately attributed evidence. Initial direct full-map transform probes were reset by native MapControls and are superseded by real pan offsets and recorded minimap yaw. Initial raycast probes selected clipped markers and the initial runner did not assert failures; corrected visible-marker assertions and the final native UI run supersede them.

## Performance scope

Three randomized interleaved native adapter microtrials compared60reads per version within one frame, with identical sanitized native observations. Baseline means0.888–0.952ms; candidate warm-read means0.345–0.365ms. This isolates adapter CPU work; it excludes binding and full pipeline/frame scheduling.

Six native full-pipeline samples completed364publications in total: three baseline/candidate pairs, at least60publications each, paused Escalation,512grid,12km radius,2ms slice, supported1s stress cadence and15s maximum age. Inputs were stable within each trial but differed across fresh missions (387–398admitted observations), so these are repeated same-scenario stress samples, not identical-workload acceptance. Baseline read means1.371–1.430ms versus1.5.1 candidate0.762–0.797ms. Frame P99 ranged18.75–20.16ms versus16.83–16.90ms; maximum frames were not uniformly better. Each final sample had one anonymous raster texture and1/1candidate-owned resources.

These measurements used1.5.1 before the renderer hotfix. Version1.5.2 leaves those read/solver paths unchanged, but the figures are not relabeled as measurements of a later binary. Default5s live-world randomized stock/loader/legacy/candidate trials, accepted budgets and full observer-overhead accounting remain open. Final low/high-resolution, large-radius, configuration and instrumentation-calibration results are recorded separately in the delivery evidence.

## Memory and environment limits

Across cycles1→20, whole-game/bridge managed totals grew184,799,232bytes with the candidate and188,035,072bytes without it. This retention is reproducible without FrontlineMap; it does not identify the exact owner or prove zero candidate leaks. Native allocated totals largely plateaued after warm-up. Candidate-owned textures/objects stayed bounded. Mono process-working-set, per-texture-byte and graphics-driver allocation APIs returned0 and are unavailable, not zero usage. Global NVIDIA usage includes desktop/other processes and cannot certify per-mod GPU memory.

Windows Computer Use failed before initialization because sandboxCwd was not accepted as a local file URI under WSL. No physical F9/mouse result is claimed. SteamVR is installed but no active runtime/headset session was available; the normal NOVR mod is disabled. Actual VR/controller/eye-view behavior and other OS DPI settings remain untested. These hardware/environment gaps are not replaced by native fixtures.

Raw logs, controlled scripts and permitted local captures stay under `artifacts/runtime-2026-10-19`; the versioned delivery contains aggregate results, selected UI screenshots and test-source provenance, excluding proprietary assemblies/decompiled game code/raw observation captures. Consult its matrix for pass/partial/not-run distinctions.
