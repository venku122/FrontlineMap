# 1.6.0 local validation

Target: Nuclear Option0.34.1 / Steam24724372 / Unity2022.3.62.7762112 / BepInEx5.4.23.5. Testing is confined to two authorized disposable copies. Normal Steam profile178 DLL/config fingerprints remain unchanged. Hardware input/DPI/VR item5 stays excluded by user instruction.

Checkpoint: annotated tag `checkpoint/frontlinemap-v1.5.3-before-layers-20261019`, commit `df71cd2c698bfcf750226dc0165fc94133a4da7e`. Verified Git bundle, previous delivery and both disposable DLL/config backups are preserved in `outputs/FrontlineMap_checkpoint_1.5.3_before_layers_2026-10-19` (workspace parent).

## Verified source and native behavior

- 164 pure .NET core checks pass in Release, including original104 invariants, role exclusion/inclusion, age/weighted confidence, unsupported/contested classifications, terrain domain/slope/diagonal/unknown cases, marching-squares saddles/UV/support, dense8192-segment fallback and extreme-origin clipping.
- Nine Legacy replay fixtures have exact field/classification/alpha parity. Unversioned original Legacy fixtures remain supported. Schema2 layered exports are explicitly rejected by the Legacy replay tool; no false replay-parity claim for terrain data that is not exported.
- Real target assembly build succeeds with0 warnings/errors. The disposable automation bridge has analyzer/nullable warnings; it is a test tool and is not part of the runtime release.
- All five selected layers render on Escalation. Actually admitted aircraft and ships contribute zero ground support and positive air/naval support in isolated production-model contract checks using unchanged admitted stamps.
- Main-map terrain128²:16384 known samples,10517 land,5867 water. Initial main-map ground view yielded108 gold contour segments. Native icon projection audit succeeds; texture and vector UI do not intercept raycasts.
- Native vector regression: original3px became4.5px after zoom. After transform/camera/viewport invalidation fix, actual CanvasRenderer mesh remains3px through native zoom1→3, rotation and resolution1280x800→1600x900. The analytic fixture is explicitly labelled and separate from gameplay data; the check does not force vertices dirty.
- Native freshness, valid/invalid settings retention, hidden movement/ownership/definition invariance, hidden death invariance, admitted ownership/death removal and visible marker click-through checks pass. Invalid layer, radius and half-life preserve the working settings/output; five close/reopen cycles release objects/textures and recover within bounds.

## Terrain coverage and interpretation

Both native map prefabs were exercised with all five views. `12. Shifting Tide` on Terrain_naval provides only4 admitted observations for the player (2 aircraft and2 ground vehicles at the offshore deployment point), no admitted ships. Its public static collision geometry covers1313/16384 samples (1076 land,237 water). Ground and confidence are empty at those offshore/unknown samples; naval is empty without admitted ships. Air and Legacy remain nonempty. An explicit radial-ground comparison produced2213 supported cells from the same permitted observations. Empty terrain-constrained output is retained rather than inventing connectivity through unknown cells. This is a practical limitation of coarse public collision sampling on that map, not evidence of full ocean navigation coverage. Users may explicitly disable terrain constraints or select Legacy. Main-map naval support is nonempty and pure tests verify disconnected water/island behavior. Narrow crossings and missing public colliders remain conservative; no claim of exact game navmesh, terrain LOS or weapon envelopes.

## Performance and acceptance scope

Only the selected full-resolution field is computed. Coarse terrain is cached per scene/extent/resolution and sampled incrementally (128 rays/~2ms per frame maximum work request); reset, search, fill and contour computation yield bounded chunks. These bounds do not make indivisible allocation, Unity upload or a single raycast obey a hard2ms limit. Layered raw/confidence arrays and terrain searches add work versus Legacy; no unconditional FPS gain is claimed. Native per-layer stage metrics, finite live soak, failure recovery, local UDP, final hash/rollback and source review evidence are saved in the delivery's `evidence` directory. Consult its final `VALIDATION.md` and manifest for exact completed runs and DLL hash; previous1.5.3 acceptance is historical and is not relabelled1.6.0 evidence.
