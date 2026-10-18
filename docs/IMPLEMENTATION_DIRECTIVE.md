# FrontlineMap Modernization — Agent Directive and Implementation Plan

> **For agentic workers:** Execute this directive task-by-task when the user gives you this handoff to implement. Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` when those skills are available, with test-driven development and verification before completion. Do not claim that an unavailable skill, reviewer, build, or game session ran. This document is an execution specification, not a request to produce another proposal.

**Goal:** Update FrontlineMap for the user's verified Nuclear Option installation and improve its correctness, reliability, legibility, and measured frame-time impact without revealing information unavailable through the native game map.

**Architecture:** A small game-observation adapter captures sanitized immutable values on the main thread. A pure numerical core computes an influence estimate; a controller owns scheduling, buffer lifetimes, and generation-safe publication; a map renderer presents only complete, currently authorized results. Preserve an improved raster path unless a measured alternative justifies replacing it.

**Tech stack:** C#; upstream currently targets .NET Framework 4.7.2 and BepInEx 5, with Unity UI integration. Retain those choices unless inspection of the actual installation establishes a concrete incompatibility. Do not rewrite this mod in TypeScript, add a web service, or introduce a new network protocol.

**Spec:** This document is the consolidated execution specification. Supporting evidence is in `inputs/review/FrontlineMap_Adversarial_Review_2026-10-18.md`, `inputs/review/Revised_Implementation_Plan.md`, and `inputs/review/evidence/`.

**Prepared:** October 18, 2026. **Document version:** 1.0. **Status:** Ready for an implementation agent to begin discovery. No updated DLL, target-runtime validation, performance improvement, or independent sign-off is implied by this handoff.

**Repository:** `https://github.com/9138noms/FrontlineMap`  
**Reviewed commit:** `6450ab9dc75503e5fedefc0280182436e5d993ec`  
**Reviewed `Plugin.cs` Git blob:** `3b571e4d9a877d2df49b4f3894bb41f0bcfcc849`  
**Reviewed project-file Git blob:** `988c2b57243ac291fd610d9b2c28b671e331176d`

## 1. Mission, authority, and completion standard

The user wants a working, maintainable client-side frontline overlay for current Nuclear Option, with better visual quality and less interference with gameplay. The previous proposal was corrected by an adversarial self-review: information correctness and lifecycle safety must precede optimization and renderer experimentation. Implement that revised direction. [R1, R2]

When executing this handoff, inspect the actual workspace and local installation, make local branch changes, build and test what the environment supports, and prepare reproducible deliverables. Do not stop after scaffolding or another plan. Complete all unblocked mandatory work and explicitly identify any remaining gate.

Use these authority boundaries:

- Local source edits, tests, isolated worktrees, and local commits are within implementation scope. Preserve the user's existing work and configuration.
- Prefer an existing user-owned fork. Do not assume upstream write access or invent a fork URL. Local development does not require an upstream permission grant.
- Prepare patch files and PR-ready descriptions locally. Push, publish releases, open upstream issues/PRs, or contact maintainers only when separately authorized by the user or an existing explicit workspace instruction.
- Build into an isolated output directory. Deploy only into an authorized disposable game/mod-manager profile, with a backup and rollback manifest. Do not silently modify the user's normal profile, replace the loader, alter Steam branches, or remove other mods.
- Use user-owned game assemblies for local inspection/building. Do not commit, redistribute, or include proprietary game assemblies, credentials, Steam identifiers, or private gameplay captures in public artifacts.
- If permissions or tools prevent an operation, complete independent work and report the precise blocker. Do not invent access, certify an untested build, or request information the workspace can reveal.

The expected environment is Windows 11, Intel i9-10850K, RTX 3090, and 64 GB RAM, as supplied in the prior handoff. Verify rather than assume it. The user's reported approximately 20 FPS on some multiplayer servers is a symptom to investigate, not evidence that this mod causes it. A non-Windows agent can implement and test the pure core; that does not establish Windows/Unity behavior. [R3]

### Global constraints

1. No hidden-enemy information in the public overlay or exported replay snapshots.
2. No change to game simulation, damage, AI, targeting, server behavior, or networking.
3. No claim of authoritative territorial ownership, weapon coverage, or actual fighting intensity from arbitrary influence weights.
4. No asynchronous job may publish into an invalid map, team, policy, or model generation.
5. No optimization claim without matched observation semantics, model parameters, visual quality, and freshness.
6. No mandatory vector/GPU rewrite, new framework dependency, or background thread without evidence that it is justified.
7. No production release while a release-blocking correctness, visibility, compatibility, or independent-review gate remains open.
8. Preserve the upstream MIT license and attribution; keep new dependency licenses and notices.

### Review focus

The five highest-risk conditions must be represented in tests, not just discussed: unresolved player/spectator context; team/map change immediately before publication; contradictory or stale knowledge about an enemy; a large stamp or texture upload exceeding a scheduling budget; and zoom/mask/origin changes misaligning an otherwise correct field. Tasks 2–7 own these cases.

## 2. Evidence to read, and what it does not prove

Read the supporting files in this order: the revised implementation plan, the full review, the probe source and outputs, and the old reviewer brief. The independently dispatched reviewer has a different source-first sequence in §12.

The package contains twelve hand-translated Python mathematical/cost exhibits. They reproduce selected legacy behaviors, not a compiled mod. Keep the original exhibits unchanged. A probe passing means its counterexample reproduced; it does not mean the production code has been fixed. A new C# regression test must assert the corrected behavior. [R1, R4]

The earlier review identified 0.34.2 as a publicly advertised patch. This is historical context, **not an instruction to assume it is still the latest, or that it matches the installed branch**. Discover the current official release and local build at execution time; record exact sources, dates, and build fingerprints. Do not use another mod's recent commit as proof of compatibility. [R1 §Scope]

| Evidence class | Examples | Permitted conclusion |
|---|---|---|
| Source-confirmed | Unsliced raster call; conditional visibility gate; metadata mismatch. | The inspected source has that structure. |
| Mathematical exhibit | Lost support information; friendly-only opposing pixels; reversed translation rule. | The specified model/input reproduces a counterexample. |
| Conditional integration risk | UV cropping; global-origin mismatch; observer permissions. | Inspect and test the actual game contract. |
| Runtime evidence | Target build/load, profiler capture, native resource counts. | Only claim the exact behavior measured on the recorded target. |

No independent reviewer ran in the prior analysis. No target DLL was built or installed there. These limitations remain until you supply new evidence. [R1 §7, R3]

### Required issue disposition ledger

Create `docs/review/FINDING_DISPOSITIONS.md`. For each item below record: confirmed/refuted/partially confirmed/unresolved; source SHA and lines; evidence class; test; implementation commit; remaining limitation. Do not assume the review is infallible.

| ID | Finding or requirement | Required treatment |
|---|---|---|
| A1 | Visibility depends on live state; null HQ bypasses tracking; airbases use a separate path. | Establish native observation rules for existence, position, ownership, type, and freshness; fail closed when unresolved. |
| A2 | `D = F - E` loses the difference between unsupported and equally opposed space. | Preserve both nonnegative faction supports, or an information-equivalent representation. |
| A3 / P01 | Friendly-only stamps can generate opposing-color boundary pixels. | Separate support edges from opposing fronts and define exact-zero color handling. |
| A4 / P03 | Legacy shift direction opposes a simple translating zero contour. | Disable arrows initially; do not restore without a validated meaning and tests. |
| A5 | Failure/transition can corrupt scheduling, history, or publication. | Generation-safe completed-result publication, cancellation, and deterministic resource ownership. |
| A6 / P11 | Coordinates, dimensions, and UI projection are unverified assumptions. | Inspect/calibrate the actual target; no guessed 100 km fallback. |
| P04 | `abs(value) > 5` is not a safe contour-distance bound. | Remove or replace with a proven bound; retain steep-gradient fixtures. |
| P05 | Dash rule changes with sampling density. | Define style in physical or screen units; do not label it combat intensity without evidence. |
| P06 | Strict sign crossing mishandles exact-zero samples. | One consistent tie/zero/plateau policy for all render paths. |
| P07 | Default fill is rounded away for a single airbase. | Separate visual opacity from arbitrary dominance thresholds; test byte quantization. |
| P08 / P09 | `SpreadFrames` does not guarantee bounded work or useful freshness. | Measured bounded scheduling, no backlog, explicit cadence and age reporting. |
| P10 | Global P99 can omit periodic hitches. | Add update-aligned, tail, threshold-count, and freshness metrics. |
| P12 | Resolution and texture payload grow quadratically. | Record buffer/native/GPU memory separately; test large-grid stress. |
| C1 | First-two-factions selection is not a hostility contract. | Resolve a supported relationship deterministically; handle neutral/allied/unsupported contexts explicitly. |
| C2 | Runtime configuration is read inconsistently across phases. | Immutable validated settings and revisioned application boundaries. |
| C3 | Cleanup, config filename, and plugin/release versions disagree. | Owned-resource accounting, unsubscribe/teardown paths, explicit migration, consistent metadata. |

The source anchors for these findings are in §15 and the full review. [R1 §§2–5]

## 3. First-session bootstrap and discovery

Begin with a concise status report: workspace, expected baseline, available runtime, and first gate. Then perform the following read-only inventory before changing source.

- [ ] Read repository instructions (`AGENTS.md` and relevant local equivalents), inspect `git status`, remotes, branches, and any existing delivery work.
- [ ] Find an existing checkout/fork. If none exists, clone the upstream into a new user-appropriate development directory; do not overwrite a directory with unrelated contents.
- [ ] Fetch refs without resetting local work. Confirm the reviewed commit and record whether upstream has advanced. Inspect intervening changes and retire findings already fixed upstream.
- [ ] Keep a reproducible reviewed baseline at the pinned SHA. Select the implementation base explicitly: current upstream if suitable, or pinned source when necessary, with the reason recorded.
- [ ] Create an isolated worktree/branch using the workspace's conventions. Suggested branch: `frontlinemap/modernization`; do not move or delete an existing branch merely to reuse that name.
- [ ] Detect the actual Steam library and matching app manifest; derive app ID, build ID, branch, and game path from the installation. Do not copy an app ID from an old README or guess that the default `C:` path is correct.
- [ ] Identify the active mod manager/profile, BepInEx version, loaded plugin GUIDs/DLL hashes, game version, Unity version, Mono/runtime details, graphics API, display settings, and target assembly hashes.
- [ ] Record both the current official public version and the installed version. If they differ, target the user's installed branch unless explicitly authorized to switch it; report the divergence.
- [ ] Try an unmodified source build and a controlled legacy load only where authorized. Classify the original as working, compile-broken, load-broken, runtime-broken, or not tested. Preserve exact errors and logs.
- [ ] Verify whether the release binary corresponds to the source/metadata being analyzed. Record its hash and any uncertainty; do not assume a release tag proves byte-for-byte source equivalence.

Useful existing-repository checks, run from the checkout:

```sh
git status --short
git remote -v
git rev-parse HEAD
git show --no-patch --format=fuller 6450ab9dc75503e5fedefc0280182436e5d993ec
git rev-parse 6450ab9dc75503e5fedefc0280182436e5d993ec:Plugin.cs
git diff --stat 6450ab9dc75503e5fedefc0280182436e5d993ec..HEAD
```

The original project hardcodes reference paths. The later `-p:GameDir=...` build command only becomes meaningful **after Task 1 implements that property**. An attempted build with a decorative unused property is not a compatibility check. [S2]

Create `artifacts/discovery/target-manifest.json`, `docs/compatibility/TARGET.md`, and `docs/compatibility/GAME_CONTRACT.md`. Use explicit values such as `not_available` with a reason for unavailable facts; never infer missing fields. Keep machine-specific paths in private artifacts, not committed example manifests.

`GAME_CONTRACT.md` must cite the inspected target assembly/hash or runtime observation for: unit and faction lookup; native friendly/enemy/neutral classification; observed/stale/lost positions; public destruction/ownership/type information; authorized spectator behavior; strategic airbase visibility; map bounds/projection/origin; full-map/minimap rendering; and lifecycle invalidation events. Comparative code is a lead, not the contract itself.

## 4. Product semantics and information boundary

### 4.1 What the overlay represents

Use the product description **“estimated influence from player-visible observations.”** A dominance color means greater modeled support under configured weights, not certified occupation or safety. Unsupported space is not proven empty. Stale knowledge must not look like current certainty.

Preserve the original radial kernel and default unit-weight semantics during the correctness baseline: airbase 10, ship 5, ground vehicle 3, aircraft 0.5, recognized building 4; exclude dismounted pilots and unknown types as in the reviewed source. These are heuristic compatibility defaults, not calibrated combat values. Confirm type hierarchy/order on the target and avoid accidentally counting one entity twice through unit and airbase paths. [R1, S1]

Changing weights, role classification, radii, terrain blocking, uncertainty smoothing, or separate naval/air layers is a distinct model change, not a hidden optimization. Document it, version it, and compare it separately. It is outside the required first delivery unless needed to satisfy a verified game contract.

### 4.2 Native-map knowledge is the authority

All fields entering the solver must be permitted by the native map for the current viewer. Cover existence, destroyed/disabled state, ownership, identity/type, position, and observation age—not only coordinates. Host access to richer replication data must not grant richer public output.

Represent viewer context explicitly: loading/unresolved, assigned faction, supported authorized spectator, unsupported context. Null HQ never means “show everything.” For unknown spectator permissions, hide and report the unsupported mode rather than synthesizing omniscience.

For enemies, use the native map's permitted known-position and freshness behavior. Do not update a stale observation from a live `Transform`, or remove it because hidden authoritative destruction occurred. Conversely, do not retain a contact after the native policy revokes it. Public strategic airbases may be included when their visibility and ownership are verified as public; do not blindly hide every enemy airbase.

Start with supported two-sided relationships. Never take the first two registry entries, automatically treat neutral/allied HQs as enemies, or merge mutually hostile factions into one side without a documented product decision. On unsupported multi-sided missions, fail clearly rather than drawing a misleading pair.

### 4.3 Knowledge noninterference acceptance

At fixed visible knowledge, viewer context, model settings, and sample time, vary hidden enemy positions, health, destruction, ownership, and type in an adapter test double. The sanitized snapshots and visible result must remain identical. Compare content, IDs exposed to the renderer, freshness labeling, and permission-driven invalidation—not diagnostic CPU timings.

Capture/replay files may contain only those sanitized observations. Do not dump the world registry for convenience. Redact player names/Steam IDs and review captures before any public release. Diagnostic counts should describe eligible observations, not undisclosed enemy forces.

### 4.4 Expiry and failure policy

A valid old result may remain visible after a recoverable compute failure only while its viewer/session identity and observation policy remain valid and it is within the declared presentation-age limit. A policy change, team switch, map replacement, or revoked observation invalidates the relevant display before the next rendered frame once the change is detected. Establish and test the detection mechanism/latency; do not rely on the five-second solver interval.

Track native observation time separately from capture time. Use the game's clock for native tracking semantics and a monotonic wall clock for computation duration and output age. Pausing simulation must not silently break expiry or scheduling assumptions; document the intended behavior and test both clocks.

## 5. Small architecture and explicit ownership

Use the following proposed layout; adapt paths to real upstream changes and explain departures. This is not authorization to build a general mod framework.

| File or directory | Responsibility |
|---|---|
| `Plugin.cs` | Loader bootstrap, configuration binding, version constant, start/stop; no numerical loops. |
| `src/Game/GameObservationAdapter.cs` | Main-thread native contracts, sanitized capture, viewer/context detection. |
| `src/Game/MapBinding.cs` | Current map identity, bounds/origin, projection, full/minimap surfaces. |
| `src/Core/ObservationSnapshot.cs` | Pure value contracts and identities; no Unity/game references. |
| `src/Core/ModelSettings.cs` | Immutable validated model/presentation settings and revisions. |
| `src/Core/InfluenceBuffers.cs` | Explicit grid layout and checked allocation/ownership. |
| `src/Core/ScalarField.cs` | Correct, full-recompute numerical oracle. |
| `src/Core/FieldClassifier.cs` | Support/dominance/contour semantics and zero policy. |
| `src/Runtime/OverlayController.cs` | Lifecycle, invalidation, result staging/publication, cleanup. |
| `src/Runtime/StepScheduler.cs` | Bounded work execution and cancellation. |
| `src/Rendering/RasterOverlayRenderer.cs` | Raster presentation, texture ownership, map masks/composition. |
| `src/Diagnostics/OverlayMetrics.cs` | Opt-in low-overhead metrics and sanitized replay. |
| `tests/FrontlineMap.Core.Tests/` | Pure C# correctness, controller fakes, scheduling and policy tests. |
| `tools/FrontlineMap.Replay/` | Deterministic sanitized-fixture replay and result comparison. |
| `scripts/` | Build, validation, package, explicit deployment/rollback helpers. |
| `docs/compatibility/`, `docs/review/`, `docs/benchmarks/` | Contracts, decisions, reviews and interpretation. |

Create a separate core project only when that provides a clean testable compilation boundary. Select its target framework from the recorded runtime compatibility, and keep the plugin compatible with the actual loader. The development test runner may use a separate installed SDK; its timing is not a Unity/Mono benchmark.

### 5.1 Internal value contracts

These names are proposed **mod-owned contracts**, not claims about Nuclear Option APIs. Define them once; keep later task interfaces consistent.

| Type | Required content and invariant |
|---|---|
| `SnapshotIdentity` | Session epoch, map identity/geometry revision, viewer-policy epoch, model revision, monotonic sample sequence. Exclude viewport-only changes from numerical identity. |
| `MapGeometry` | Validated global bounds, width/height, grid dimensions, coordinate convention, origin conversion metadata; no hardcoded square-map fallback. |
| `ObservationStamp` | Stable permitted ID, permitted side/category, known global X/Z, nonnegative weight, observation timestamp, freshness/expiry classification. No `Unit`/`Transform` references. |
| `ObservationSnapshot` | Identity, geometry, immutable owned stamps, capture monotonic time, simulation time, policy expiry metadata. No mutable registry-backed collection. |
| `ModelSettings` | Radius, type weights, resolution, semantic thresholds, version; all finite and coherently snapshotted. Presentation settings have a separate revision where possible. |
| `InfluenceBuffers` | Nonnegative friendly/enemy support or equivalent lossless representation, dimensions, explicit owner and readable/writable state. |
| `FieldResult` | Identity, completed buffers/derived presentation data, source times, counts, validity deadline, and computation diagnostics. Only complete results are publishable. |
| `PresentationHandle` | Renderer-owned staged texture/mesh resources for a completed result; not visible until committed. |

Keep coordinates in the most precise form supplied by the game until converting relative to validated map bounds. Flatten numerical storage consistently as `index = z * width + x`; iterate X contiguously for that layout. Record conversion and texture orientation explicitly.

Define these callable boundaries with ordinary classes/interfaces as appropriate; do not add abstraction layers that have no second use:

```csharp
// Mod-owned signatures; types are defined by the table above.
bool TryCapture(out ObservationSnapshot snapshot, out CaptureFailure failure);
void Compute(ObservationSnapshot snapshot, ModelSettings settings, InfluenceBuffers destination);
void Begin(ObservationSnapshot snapshot, ModelSettings settings);
StepOutcome Step(double deadlineMonotonicSeconds);
void Cancel(InvalidationReason reason);
bool TryTakeCompleted(out FieldResult result);
bool TryPrepare(FieldResult result, MapBinding binding, out PresentationHandle staged);
void Commit(PresentationHandle staged, MapBinding currentBinding);
void ClearAndRelease();
```

`TryCapture` belongs to the adapter; `Compute` to the scalar core; `Begin`/`Step`/`Cancel`/`TryTakeCompleted` to the scheduler; presentation methods to the renderer. Define `CaptureFailure`, `StepOutcome`, and `InvalidationReason` as small typed outcomes with explicit reasons. Error logs are not a substitute for state transitions.

### 5.2 Calculation and publication protocol

1. Check viewer/map validity and observe relevant invalidation signals on the main thread.
2. Capture one coherent sanitized snapshot, map geometry, and immutable model settings.
3. Reserve writable buffers. Never clear or overwrite the last complete public result or a buffer still read by a worker/renderer.
4. Compute incrementally or synchronously in the oracle. No live game/Unity API calls in pure computation.
5. On completion, check identity, expiry, and sequence against current context and last published result.
6. Prepare renderer resources without modifying the current public presentation. Recheck validity before committing the staged presentation.
7. Publish the complete result and rotate owned buffers/history only after success. Release superseded resources deterministically.
8. On failure, abort work and release work-owned resources. Keep only an authorized, unexpired old result; otherwise clear immediately.

An exception-safe `computing=false` is necessary but insufficient. Test every transition. Do not assume all Unity coroutine-stop/unload paths execute the same cleanup logic. Use an explicit step state machine if that makes guarantees easier to test. [R1 A5]

Prepare/upload failure must not partially corrupt the public texture while the controller reports the old result still displayed. A staging texture is acceptable; account for its memory. An alternative must demonstrate equivalent failure behavior on the target, not merely assume upload atomicity.

### 5.3 Scheduling and configuration rules

Default to a bounded main-thread step scheduler. Check deadlines inside large stamps and raster/clear chunks, not only between units or entire rows. Upload and native UI work may remain indivisible; measure them separately and do not describe the complete pipeline as hard real-time.

At most one active calculation and one latest pending snapshot are allowed. Replace pending work rather than enqueueing an unbounded backlog. Do not cancel every in-flight job whenever a new snapshot arrives: that can starve publication. Cancel for invalid identity or declared expiry/deadline; otherwise allow a valid completed sequence newer than the last published one, even if a newer sample is pending.

Budget, cadence, and maximum presentation age must be explicit settings selected from baseline measurements (§10). Report missed targets. Do not silently increase update intervals, lower resolution, omit observations, or change weights to make benchmark numbers look better. Explicit user-selected quality presets are allowed only with documented tradeoffs.

Validate radius/interval as finite and positive, weights as finite and nonnegative, opacity within range, and allocation dimensions with checked arithmetic. Reject invalid updates without a partial configuration. Grid/model changes invalidate buffers/history safely; viewport and purely visual changes should not require a full world-field recomputation unless the renderer genuinely needs it. Keep original configuration files backed up and document setting migrations, especially legacy `SpreadFrames`.

## 6. Numerical and rendering specification

### 6.1 Correct scalar oracle

Implement a simple full-recompute oracle before optimizing. For each eligible observation, use the legacy radial kernel in global map coordinates:

```text
K(distanceSquared, radiusSquared) = max(0, 1 - distanceSquared / radiusSquared)
F(x,z) = sum of permitted friendly weights * K
E(x,z) = sum of permitted hostile weights * K
D = F - E
S = F + E
```

Require finite nonnegative F/E and finite derived fields. Compute at explicitly documented cell centers. Handle rectangular world bounds and anisotropic cell dimensions correctly, even when the selected texture/grid is square. Clip off-map kernels safely; avoid integer overflow and division by zero.

Use a deterministic ordering policy for observations, preferably stable permitted IDs, and test order sensitivity. Record numerical tolerances in `tests/fixtures/tolerances.json` before evaluating optimized implementations. Use exact equality for empty/single-side absence and identical-policy snapshots; use justified absolute-plus-relative error bounds for floating accumulation. Also compare classifications near thresholds, not only a global numeric norm.

Define support eligibility separately from dominance. A two-sided front requires meaningful F and E. Balanced supported areas must remain distinguishable from no-support/unknown areas; a flat balanced plateau need not produce an arbitrary line. Support thresholds, numerical epsilon, tie handling, and contested shading are explicit model/presentation settings finalized and justified in Task 4, not undocumented constants scattered across renderers.

Do not generate an opposing-front color from a zero value alone. Remove the unsafe `abs(D)>5` contour shortcut unless a valid gradient/distance bound is proved. Exact-zero samples, zero plateaus, grid edges, and saddle cases must have a consistent deterministic policy. If contours are later extracted, one classifier owns these decisions for all renderers.

Movement indicators are **off by default and omitted from the required first production candidate**. Preserve configuration migration with a clear explanation. Restoring them is optional scope requiring elapsed-time normalization, regular-contour/degeneracy handling, bounded displacement, support stability, contact-change suppression, and separate reviewer approval. Correcting the sign alone is insufficient. Label any future result as observed boundary change unless physical advance is actually justified. [R1 A4]

### 6.2 Legibility contract

Use configurable opacity and readable defaults, with a low-opacity option for users who prefer subtle fill. Test conversion to actual texture alpha bytes. Do not accidentally discard all single-airbase support or make fixed support changes alter whether a line exists solely because of fill quantization.

Define line width in screen/UI pixels at a recorded DPI/canvas scale; define support/style thresholds in declared model units. Resolution refinement must not change the meaning of a dash. Remove the claim “conflict intensity” unless its meaning is justified; “opposing modeled support” is acceptable. Include a concise legend and an age/stale indication where appropriate.

Keep native map labels, icons, clicks, and masks usable. Set overlay raycast behavior appropriately. Verify canvas ordering, color/theme compatibility, contrast, full-map/minimap modes, zoom, pan, rotation, aspect ratio, and clipping. Use labeled actual target captures, not generic tactical-map imagery, as visual acceptance evidence.

A viewport-only transform change should reproject/repaint the current world field without triggering a full solver run. Determine actual UV, camera, parent-transform, material, and mask behavior before choosing an implementation. Do not blindly copy `uvRect` if the target uses a different mapping. [R1 A6]

## 7. Ordered implementation tasks and gates

Each task follows the same execution discipline: establish the relevant failing condition; run the test and capture the failure; implement the smallest coherent change; rerun affected and full tests; review the diff; make a focused local commit. A test that fails because of a syntax error or missing package is not a valid regression reproduction. Revert or disable the fix in a test branch to verify that a claimed regression test actually detects the original defect.

Use worktrees for separate baselines and experiments. Do not bundle an unrelated model change with a performance optimization. Keep a `docs/EXECUTION_LOG.md` entry per completed task, including commands, outcome, commit, evidence paths, and blocked cases.

### Task 0 — Verify the target and establish a baseline

**Files:** Create `docs/compatibility/TARGET.md`, `docs/compatibility/GAME_CONTRACT.md`, `artifacts/discovery/target-manifest.json`, `docs/review/FINDING_DISPOSITIONS.md`, and `docs/benchmarks/BASELINE_PROTOCOL.md`.

**Consumes:** Pinned upstream source, user's actual checkout/install/profile, supporting review. **Produces:** Verified target inventory, source/build relationship, baseline branches, native-contract notes, and an evidence-based scope of compatibility changes.

- [ ] Complete every discovery item in §3, including failed/unavailable operations.
- [ ] Compare stock, loader-only, and legacy mod states where a disposable profile is authorized. If the original cannot run, retain its failure evidence and use a minimal-compatibility baseline later; never present the original as measured when it was not.
- [ ] Instrument stage boundaries conservatively, preserving a pristine baseline. Measure instrumentation overhead; do not record unsanitized world data.
- [ ] Record initial capture/clear/stamp/raster/upload/UI timings where available and define the controlled test scenarios in §10.
- [ ] Dispatch a fresh reviewer for the source-first plan/contract review in §12. Resolve evidence-backed changes to this directive in a decision record.
- [ ] Commit discovery documentation with secrets and machine-local artifacts excluded.

**Gate G0:** The installed target and known/unknown native contracts are explicit; the existing mod's status is accurately classified. Missing local game access blocks integration certification, not independent core work. No “latest compatible” claim at this gate.

### Task 1 — Make building, configuration, and metadata reproducible

**Files:** Modify `FrontlineMap.csproj`, `Plugin.cs`, `README.md`, and `.gitignore`; create `src/Core/ModelSettings.cs`, `scripts/build.ps1`, and build/config tests in `tests/FrontlineMap.Core.Tests/ConfigurationTests.cs`.

**Consumes:** G0 runtime/dependency inventory. **Produces:** Parameterized references, consistent metadata, validated configuration, and build/package separation.

- [ ] Add a failing build-preflight test for a missing game/loader reference and a test showing a nondefault Steam path is honored. Return a clear missing-path error; do not silently compile against unrelated stale DLLs.
- [ ] Implement `GameDir` and, where needed, a separate loader/profile reference root. Keep proprietary references non-copying and exclude test/tool source globs from the plugin project. Ensure an extracted core is not compiled twice.
- [ ] Centralize plugin, assembly, package, startup-log, and release version metadata. Derive the candidate version from existing conventions; do not blindly call it v2.
- [ ] Test old `com.yuulf.frontlinemap.cfg` versus current `com.noms.frontlinemap.cfg` handling. Never overwrite a current config with an old file silently. Preserve backups and explain unsupported settings. Detect duplicate old/new plugin GUIDs or binaries where feasible; report rather than deleting arbitrary files.
- [ ] Test invalid NaN/infinite/negative settings, range limits, checked allocation sizes, and all-or-nothing live configuration application. Document restart-required settings rather than implying unsupported hot reload.
- [ ] Run a clean Release build against the verified target, inspect output contents, and record target/source hashes. When target assemblies are unavailable, report the plugin build as blocked; do not replace them with stubs and call it a target build.
- [ ] Commit and obtain a build/config diff review.

**Gate G1:** Build/config behavior is reproducible and metadata agrees. Proprietary DLLs are absent from distributable output. A successful pure-core build is labeled separately from a successful game plugin build.

### Task 2 — Implement sanitized observations and verified map binding

**Files:** Create `src/Game/GameObservationAdapter.cs`, `src/Game/MapBinding.cs`, `src/Core/ObservationSnapshot.cs`, `tests/FrontlineMap.Core.Tests/ObservationPolicyTests.cs`, and `MapProjectionTests.cs`; update `GAME_CONTRACT.md`.

**Consumes:** Verified native contracts and immutable settings. **Produces:** `TryCapture(...)`, coherent `ObservationSnapshot`, typed failure reasons, map/view identity and invalidation signals.

- [ ] Write adapter tests for null/unassigned context, authorized/unauthorized spectators, friendly/hostile/neutral/allied identities, and reordered/unsupported faction sets.
- [ ] Write hidden-world noninterference tests from §4.3, including hidden destruction/ownership/type changes and stale/lost positions. Test strategic airbases separately.
- [ ] Implement the adapter using only verified native-map policy. Snapshot permitted values on the main thread; no registry iterators, game objects, or mutable Unity values escape into worker/core contracts.
- [ ] Test map center/corner transforms, a rectangular geometry fixture, off-map stamps, origin shifts, and full/minimap projections. Include the P11 cropped-parent counterexample as a conditional fake, not a statement about the installed game.
- [ ] Implement explicit `MapBinding` validity. Reject absent metadata; detect map replacement and relevant geometry changes even if an old overlay GameObject still exists.
- [ ] Compare actual native map landmarks and observation behavior against captured overlay/debug positions in authorized target sessions. Record maximum alignment error and the contexts tested; do not substitute fake-engine tests for this step.
- [ ] Commit and request a dedicated information-boundary review.

**Gate G2:** Pure policy tests pass; target observations and transforms are verified where possible. No fail-open context or undocumented observation source is accepted. Unverified integration cases remain clearly blocked/unsupported.

### Task 3 — Implement controller, cancellation, and safe publication

**Files:** Create `src/Runtime/OverlayController.cs`, initial `StepScheduler.cs`, renderer staging/cleanup support, `tests/FrontlineMap.Core.Tests/LifecycleTests.cs`, and `PublicationTests.cs`.

**Consumes:** Immutable snapshots, map/view identities, settings revisions. **Produces:** Explicit state transitions and ownership described in §5, with no reliance on a happy-path coroutine tail.

- [ ] Write fault-injection tests at capture, clear, mid-stamp, raster, upload preparation, immediately before commit, and cleanup. Assert output identity, scheduler recovery, and resource ownership, not merely absence of exceptions.
- [ ] Write race tests for a team/map/config change after computation but before publication; late worker completion; repeated enable/disable; hide/show during work; and context invalidation after upload preparation.
- [ ] Implement the smallest explicit controller/scheduler state model. Separate current authorized presentation from work buffers. Invalidate policy/session output promptly and reset incompatible history.
- [ ] Test that a recoverable failure leaves a still-valid old result intact, while expired or unauthorized output is cleared. Test upload failure cannot be reported as old output while showing partially new pixels.
- [ ] Verify no buffer is reused while still being read and every owned resource has one release path. Do not block the Unity main thread waiting for cancellation or worker completion.
- [ ] Exercise repeated target mission changes and view rebindings, recording owned texture/object counts. Describe a cleanup gap as a gap until resource behavior is measured.
- [ ] Commit and obtain lifecycle/publication review before optimization.

**Gate G3:** No invalid-identity or partial result can publish in the tested state model. Failures recover, counters/state reset, and resource lifetime evidence is explicit. Unverified actual engine unload paths remain release blockers.

### Task 4 — Correct the scalar model and keep a reference raster

**Files:** Create `src/Core/InfluenceBuffers.cs`, `ScalarField.cs`, `FieldClassifier.cs`, `src/Rendering/RasterOverlayRenderer.cs`, `tests/FrontlineMap.Core.Tests/ScalarFieldTests.cs`, `FieldClassificationTests.cs`, `tests/fixtures/`, and `docs/MODEL_SEMANTICS.md`.

**Consumes:** Sanitized snapshots and owned buffers. **Produces:** Full-recompute oracle, semantic classifier, deterministic fixtures, and a corrected raster baseline.

- [ ] Preserve P01–P12 as legacy exhibits. Add C# tests asserting the desired inverse/corrected outcomes where applicable; do not count the twelve Python results as twelve fixed mod tests.
- [ ] Test empty, positive-only, negative-only, balanced colocated, separated opposing, steep-front, exact-zero, plateau, saddle, map-edge, high-density, and tiny/large-radius cases.
- [ ] Implement §6's support-preserving kernel/classifier. Specify support thresholds, zero policy, tie coloring, accumulation order, and numerical tolerance rationale in `MODEL_SEMANTICS.md` before accepting optimized parity.
- [ ] Test faction-swap symmetry, translation away from clipping, permitted-input ordering, finite outputs, and convergence under grid refinement. Separate field convergence from exact pixel equality.
- [ ] Correct false opposing boundaries, unsafe culling, physical/style scaling, and alpha quantization. Disable legacy movement arrows by default and document the intentional behavior change.
- [ ] Generate fixed-fixture output artifacts and authorized fixed-camera target screenshots. Add coordinate/pixel-class checks so approval does not depend only on someone saying the image looks better.
- [ ] Commit the corrected scalar/raster reference separately from performance changes and request semantic/numerical review.

**Gate G4:** The field and presentation have an explicit tested meaning, known counterexamples are resolved or honestly scoped, and a frozen corrected baseline exists for optimization comparisons.

### Task 5 — Optimize only demonstrated bottlenecks

**Files:** Modify `StepScheduler.cs`, `InfluenceBuffers.cs`, and measured hot paths; create `src/Diagnostics/OverlayMetrics.cs`, `tools/FrontlineMap.Replay/`, `tests/FrontlineMap.Core.Tests/SchedulerTests.cs`, and `docs/benchmarks/ACCEPTANCE.json`.

**Consumes:** G4 oracle, deterministic snapshots, measured target baseline. **Produces:** A bounded implementation that preserves approved semantics and freshness, with stage-level evidence.

- [ ] Freeze test fixtures, configurations, numeric/visual tolerances, run protocol, and measured acceptance budgets before comparing candidate optimizations (§10).
- [ ] Test variable-cost stamps/rows, zero observations, very large radius/grid, deadline overshoot, latest-pending replacement, and publication starvation at synthetic 20/30/60/120 FPS clocks.
- [ ] Implement time-budgeted small steps for work that can yield. Measure capture, grid clearing, stamping, rasterization, copy/upload, and UI integration independently. Align flat-buffer indexing and traversal if adopted.
- [ ] Reuse existing buffers intelligently; demonstrate allocation reduction rather than claiming preallocation as entirely new. Include iterator/list growth and renderer resource behavior in allocation evidence.
- [ ] Replay identical sanitized snapshots against oracle and candidate. Require parity within predeclared bounds and matching classification/knowledge semantics. Run performance trials on the target Mono/Unity runtime for runtime claims.
- [ ] Consider one worker only if measured evidence warrants it. A worker receives immutable sanitized data only, obeys bounded ownership, never calls Unity APIs, never requires a blocking main-thread join, and cannot publish invalid generations.
- [ ] Consider dirty tiles only with old/new-footprint invalidation, removal/contact-loss tests, faction/config resets, and full-recompute drift comparisons. Do not assume partial CPU changes imply partial `Texture2D.Apply` transfer.
- [ ] Keep independently measurable optimizations in separate commits. Reject apparent gains purchased by freshness, missing contacts, lower quality, or changed weights.

**Gate G5:** Improvement is demonstrated on predeclared update-aligned metrics at equivalent semantics and freshness, with uncertainty and overhead reported. If no improvement is demonstrated, retain the simpler correct path and report the outcome; do not invent a gain.

### Task 6 — Evaluate visual rendering alternatives, conditionally

**Files:** Modify the raster renderer; only if justified create `src/Rendering/ContourOverlayRenderer.cs` and/or `GpuFieldOverlayRenderer.cs`, corresponding tests/resources, and `docs/RENDERER_DECISION.md`.

**Consumes:** The same corrected fields and visual contracts. **Produces:** Measured renderer decision, not necessarily a renderer replacement.

- [ ] Improve raster readability, masking, and projection first. Establish actual target captures and total per-frame/per-update cost.
- [ ] Record an explicit decision to prototype or defer contour/GPU alternatives, citing bottleneck/quality evidence. A reasoned deferral satisfies this task.
- [ ] For contours, test ambiguous cells, exact-zero plateaus, loops, disconnected components, clipping, joins/caps, screen-space width, simplification error, and topology stability. Batch geometry; do not create a GameObject per segment.
- [ ] For GPU shading, verify supported texture formats/shaders, packaging, target graphics API, masks, blending, and fallback. Measure persistent fragment cost as well as upload cost. Do not add Burst/Jobs/engine assemblies opportunistically.
- [ ] Compare fixed fields, fixed views, uncertainty/support depiction, age, readability, and all costs. Do not call a crisper but slower high-quality preset a universal performance improvement.
- [ ] Select a renderer and retain the known-good fallback. Document why the added complexity is justified, or why improved raster remains preferable.

**Gate G6:** Legibility and integration requirements pass. A new renderer ships only with demonstrated benefit and reviewed correctness. The project can succeed with improved raster alone.

### Task 7 — Multiplayer, packaging, rollback, and final review

**Files:** Create `scripts/validate.ps1`, `scripts/package.ps1`, `scripts/deploy.ps1`, `scripts/rollback.ps1`, `docs/VALIDATION_REPORT.md`, `docs/KNOWN_LIMITATIONS.md`, `docs/RELEASE_NOTES.md`, and final `docs/review/` outputs.

**Consumes:** All preceding evidence and selected implementation. **Produces:** Reproducible release candidate or explicitly limited development delivery.

- [ ] Run the release matrix in §9, including client/host differences, unmodded-client coexistence, unsupported observer modes, team changes, respawn, mission transitions, time scaling, and other map mods when available.
- [ ] Rebuild from a clean checkout using the recorded target references; run tests and replay parity; inspect package contents and hashes. Verify no private/proprietary data enters the distributable.
- [ ] Test candidate installation only in an authorized profile, then rollback to the original DLL/config and confirm restoration. Deployment must preview exact file changes and fail rather than overwrite an unexpected file hash.
- [ ] Request the final whole-branch independent review with the tested base/head SHAs and evidence. Fix findings, then rerun affected and full verification. A changed HEAD invalidates earlier review coverage until its new diff is reviewed.
- [ ] Prepare release notes, compatibility scope, unresolved issues, benchmark interpretation, build/install/rollback instructions, and local PR-ready descriptions. Publish remotely only if authorized.
- [ ] Deliver the exact final state and remaining blockers using §13's template.

**Gate G7:** No unresolved critical/high finding, required runtime test, or independent-sign-off gate is represented as complete. A useful development artifact may be delivered with gates open, but it is not a certified production release.

## 8. Build and verification command contracts

The following are **commands the implementation must make available**, not a claim that upstream already provides these scripts/projects. Before they exist, use native commands appropriate to the inspected checkout. Scripts must return a nonzero exit code on failure and preserve logs, not swallow errors.

After Task 1 creates the build property/script, from the repository in PowerShell with `$GameDir` set to the discovered installation:

```powershell
# Preflight references, record fingerprints, and build without deploying.
.\scripts\build.ps1 -GameDir $GameDir -Configuration Release

# Equivalent direct build after parameterization; add the documented loader-root
# property when the active mod profile keeps loader assemblies outside GameDir.
dotnet build .\FrontlineMap.csproj -c Release "-p:GameDir=$GameDir"

# Pure C# tests: record the actual test SDK/runtime used.
dotnet test .\tests\FrontlineMap.Core.Tests\FrontlineMap.Core.Tests.csproj `
  --configuration Release --logger "trx;LogFileName=core-tests.trx" `
  --results-directory .\artifacts\tests
```

After Tasks 5 and 7 implement their tools:

```powershell
# Required modes: replay fixtures, compare candidate fields/classification with
# the corrected oracle, emit JSON metrics and a nonzero exit on parity failure.
dotnet run --project .\tools\FrontlineMap.Replay\FrontlineMap.Replay.csproj `
  --configuration Release -- --fixtures .\tests\fixtures `
  --compare oracle,candidate --output .\artifacts\replay

# Separate pure validation from target-runtime validation.
.\scripts\validate.ps1 -GameDir $GameDir -Mode Core
.\scripts\validate.ps1 -GameDir $GameDir -Mode Target

# Package only build artifacts/dependencies that may be distributed; no deployment.
.\scripts\package.ps1 -Configuration Release -OutputDir .\artifacts\release

# Preview only. ProfileDir must be an explicitly authorized test profile.
.\scripts\deploy.ps1 -ProfileDir $ProfileDir -PackageDir $PackageDir -WhatIf
```

`Target` mode must never report success simply because the game launched or logs contain no exceptions. It must either run/collect the declared checks or return a manifest of blocked/manual-required cases. Do not bypass PowerShell execution policy, disable security controls, or silently install global toolchains. Document genuine setup dependencies and use existing/local tools where possible.

The historical probe command is separate:

```sh
python inputs/review/evidence/adversarial_probes.py
```

Run it on a copy when preserving the original evidence, because it writes `probe_results.json` beside itself. On hosts with `python3` rather than `python`, use that executable and record its version. It is a legacy exhibit, not part of the production compatibility gate.

## 9. Mandatory acceptance matrix

Create `artifacts/validation/matrix.json` with one record per case: case ID, requirement, harness/runtime, input fixture or scenario hash, source SHA, actual result, evidence paths, and reviewer disposition. Allowed statuses are `pass`, `fail`, `not_run`, `blocked`, and `not_applicable_with_reason`. An unavailable game installation is `blocked`, not `pass` or `not_applicable`.

| Case | Required assertions | Owner |
|---|---|---|
| V01 | Null/loading/unassigned viewer emits no unauthorized observations or prior-team pixels. | Task 2/3 |
| V02 | Explicit spectator permissions control output; unsupported context is clearly hidden/unsupported. | Task 2 |
| V03 | Hidden position/death/type/ownership changes leave sanitized content and visible result unchanged at fixed knowledge/time. | Task 2 |
| V04 | Observed → stale → lost matches native known-position/existence rules; no live-position refresh on stale contacts. | Task 2/3 |
| V05 | Airbase location/ownership visibility follows verified native strategic knowledge; no accidental double-counting. | Task 2/4 |
| V06 | Reordered/neutral/allied/multiple HQ sets cannot silently choose the wrong opponents. | Task 2 |
| N01 | Empty grid remains unsupported; one-sided inputs never create an opposing-front color. | Task 4 |
| N02 | Equal colocated opponents retain nonzero support; balanced support is not confused with no support. | Task 4 |
| N03 | Steep fronts, zero samples/plateaus, and saddle/edge cases follow the documented classifier. | Task 4/6 |
| N04 | Faction swap, translation away from clipping, accumulation ordering, and resolution convergence obey predeclared tolerances. | Task 4/5 |
| N05 | Invalid/nonfinite/extreme config is rejected safely; finite outputs and checked bounds hold at high density. | Task 1/4 |
| N06 | Fill alpha survives intended simple support; dash/width semantics do not change meaning when grid resolution changes. | Task 4/6 |
| N07 | Movement arrows are disabled in the first candidate; any restoration passes translation, sensor-change, degeneracy, elapsed-time, and reset cases. | Task 4/6 |
| L01 | Fault/cancel at each pipeline phase resets work state without publishing partial data. | Task 3 |
| L02 | Team/map/model invalidation before commit rejects ready results; old public output clears when policy requires. | Task 3 |
| L03 | Late completion, pending replacement, and hide/show cannot resurrect disposed buffers or stale epochs. | Task 3/5 |
| L04 | At least 20 authorized mission/view lifecycle cycles leave owned object/texture counts at the appropriate baseline after cleanup. | Task 3/7 |
| L05 | Pause/time scaling and independent simulation/monotonic clocks preserve declared expiry/cadence behavior. | Task 3/5/7 |
| M01 | Center, corners, landmarks, rectangular geometry, origin shifts, full/minimap modes align within recorded tolerances. | Task 2/7 |
| M02 | Zoom/pan/rotation/masks/aspect/DPI/theme changes preserve alignment, contrast, native UI ordering, and click behavior. | Task 6/7 |
| P01 | Large stamp/clear/raster steps respect the measured budget plus declared overshoot; indivisible stages are reported separately. | Task 5 |
| P02 | At most one active and one latest pending calculation; no starvation, backlog growth, or unreported quality degradation. | Task 5 |
| P03 | Oracle/candidate semantics, classification, accepted observations, and output-age/cadence parity hold. | Task 5 |
| P04 | Update-aligned hitches, tail frames, stage costs, allocations, and native/GPU memory are measured on the actual target. | Task 5/7 |
| I01 | Client/host output honors identical native knowledge; stock clients coexist without a protocol requirement. | Task 7 |
| I02 | Respawn/death/team changes, load/unload, observer transitions, and available supported maps behave as documented. | Task 7 |
| I03 | Other installed map/UI mods and mod-manager profiles are tested or explicitly listed as untested, not presumed compatible. | Task 7 |
| R01 | Clean build metadata matches packaged DLL; no proprietary assemblies/private captures; license and notices preserved. | Task 1/7 |
| R02 | Authorized test-profile install/rollback restores recorded hashes/config without touching unrelated files. | Task 7 |
| R03 | Independent reviewer covers the final tested HEAD, with severity disposition and evidence of reruns. | Task 7 |

Do not collapse these cases into a single “all tests passed” sentence. Distinguish pure C#, adapter fakes, actual Unity, and actual multiplayer evidence. Resource counters must count mod-owned objects; total process memory alone is too noisy to prove deterministic cleanup.

## 10. Benchmark protocol and performance acceptance

### 10.1 Comparable baselines

Keep these identities separate:

| Baseline | Purpose |
|---|---|
| Stock game | Game without this loader/mod; only run in an authorized isolated configuration. |
| Loader-only | Isolate loader overhead and profile differences. |
| Legacy-compatible | Original behavior, with only necessary compatibility adapters if the unmodified build fails. Record every difference. |
| Corrected scalar/raster | Accepted knowledge/model/renderer semantics before performance changes. |
| Optimized corrected | Same knowledge and semantic contract with measured implementation changes. |

Compare legacy to corrected for intentional behavior differences. Compare corrected to optimized for performance at equivalent behavior. Any optional higher-quality visual preset gets its own comparison; it is not a free speedup.

### 10.2 Fixtures and controlled scenarios

Use source-controlled synthetic observations and sanitized captured snapshots. Include empty, single-sided, balanced, sparse, dense, edge-heavy, and widely moving/removed-contact inputs. Exercise 64/256/512/1024/2048 grid limits where supported, but do not require the full expensive Cartesian product of every parameter for every iteration. The initial comparative target is the current/default configuration, then targeted high-resolution/large-radius stress.

Every run records: build/assembly fingerprints, source SHA, plugin set, scenario/snapshot hash, grid and radius, weight/model revision, visible observation count, viewport/camera state, renderer, display settings, graphics API, clocks, hardware, power/thermal conditions if available, and instrumentation mode. Freeze these rather than relying on a live multiplayer match to be identical twice.

Use deterministic replay for solver/parity measurements and controlled in-game runs for engine interaction. Validate live multiplayer separately. Run the candidate and reference on the same runtime/machine; Python or a modern desktop test runner is not a substitute for Unity/Mono timings.

### 10.3 Measurement requirements

Measure capture/sanitization, allocation/clear, stamping, classification/raster, copy/upload, UI/mesh/canvas work, and end-to-end publication. Record total CPU per completed result as well as work per frame. Include persistent renderer GPU cost, not just refresh cost.

Report median/tails, maximum frame time, global P99/P99.9 where sample size supports them, frame counts over 33/50/100 ms, and frame distributions aligned to each update's start and upload/commit events. Compare excess frame time against a local or paired control. P10 shows why global P99 alone is not a sufficient hitch gate. [R1 §5, R4 P10]

Report capture-to-publish age, native observation age distribution, publication intervals, expired/rejected/completed counts, missed deadlines, pending replacements, and maximum concurrent buffer/resource ownership. Aged native contacts that the game intentionally preserves must be distinguished from computation-induced staleness.

Warm up JIT/resources before the recorded interval. Alternate or randomize reference/candidate order; start with at least three paired default-configuration trials with at least 60 completed updates per trial where feasible. If an environment prevents that coverage, report the reduced sample and avoid strong tail/causal claims. Preserve per-trial data and variance; do not publish only the best run or treat correlated frames as independent trials.

### 10.4 Set numerical gates from evidence

Before candidate performance tuning, create `docs/benchmarks/ACCEPTANCE.json` with concrete budgets and the baseline data/reviewer rationale supporting them. Required fields: benchmark scenario IDs; supported resolutions; main-thread chunk budget and overshoot allowance; publication cadence; maximum computation-induced age; update-aligned hitch objective; allowed total CPU/GPU/memory tradeoffs; numerical/classification/visual tolerances; repetition/sample requirements; instrument overhead; and regression thresholds.

These numbers are deliberately **not preclaimed in this handoff**. Choosing them is an explicit Task 5 deliverable based on Task 0/G4 data. An empty file or unexplained number does not satisfy the gate. Do not relax budgets or tolerances after seeing an unfavorable candidate without a documented decision and a full rerun of both sides.

The minimum optimization claim requires a reproducible improvement outside the observed run-to-run noise on the predeclared primary metric, no unresolved correctness issue, and no unreported freshness or quality regression. A bounded overhead reduction is a valid claim; promising a particular FPS recovery from the user's approximately 20 FPS symptom is not.

## 11. Deliverable structure and evidence preservation

Produce a local deliverable with the following contents, adapting date/version consistently:

```text
FrontlineMap_Delivery_<date>/
  START_HERE.md
  CHANGELOG.md
  source.patch                         # or source archive plus base/head identities
  release/                             # only when an actual plugin build exists
  docs/
    TARGET.md
    GAME_CONTRACT.md
    MODEL_SEMANTICS.md
    RENDERER_DECISION.md
    VALIDATION_REPORT.md
    KNOWN_LIMITATIONS.md
    INSTALL_AND_ROLLBACK.md
    FINDING_DISPOSITIONS.md
    REVIEW_DISPOSITIONS.md
  evidence/
    target-manifest.json
    source-manifest.json
    validation-matrix.json
    tests/                             # real test outputs, harness/runtime stated
    replay/                            # sanitized fixtures, hashes, parity results
    benchmarks/                        # raw per-trial data and comparison summaries
    screenshots/                       # actual target, clearly labeled baseline/candidate
    reviews/                           # independent provenance, reports and dispositions
    lifecycle/                         # fault and native resource evidence
  scripts/                             # commands needed to reproduce the delivery
  LICENSE
  THIRD_PARTY_NOTICES.md                # when applicable
  manifest.sha256
```

Never create an empty DLL or a source-only placeholder named as a release binary. Label files generated by a different runtime accordingly. The manifest must state what was built, run, reviewed, and not run, rather than infer it from which directories exist.

Carry the original mathematical exhibits unchanged in development/review evidence, with attribution. Save new reproductions separately. Keep the prior reviewer brief labeled `NOT_EXECUTED`; actual reviewer reports receive new names and provenance. Preserve source refs and raw logs supporting every summary.

This single directive is sufficient to reconstruct scope from the pinned public source. The full bundle adds the original evidence. If companion files were not transferred, do not pretend to read them: recover them from the provided bundle when available or recreate explicitly labeled source-derived tests. Do not depend on another ChatGPT conversation, an opaque Library alias, or the user's memory for required decisions.

## 12. Genuinely separate reviewer protocol

Independent review is mandatory for final sign-off. It means a separate human or agent context that did not author the implementation, not the implementer changing tone or performing a second self-review.

### 12.1 Source-first review before anchoring

Dispatch a reviewer with only the repository/base SHA, intended product meaning, this limited initial mandate, and read-only authority:

> Independently inspect FrontlineMap at the supplied SHA as a client-side influence overlay derived from native-map-permitted knowledge. Identify compatibility assumptions, information-boundary issues, numerical/visual counterexamples, scheduling/lifecycle defects, and unnecessary complexity. Read source first. Record your own findings before receiving the previous author's review or proposed fixes. Do not modify the repository, install a mod, or claim runtime results without executing and recording the relevant test.

Only after receiving the written first pass, provide this directive, the original review/probes, verified native contracts, and the implementer's evidence. Ask for a claim-by-claim confirm/refute/partial/unresolved disposition. If the reviewer has already seen the earlier review, record that anchoring limitation rather than claiming a blind pass.

The separate `REVIEWER_TASK.md` in this handoff contains the dispatch/output procedure. Do not paste the entire implementer's conversation or private reasoning into a reviewer session. Send the work product, constraints, diffs, and evidence needed to evaluate it.

### 12.2 Implementation review checkpoints

Require fresh-context review of: observation policy before its integration gate; lifecycle/publication before optimization; scalar semantics before treating the oracle as ground truth; performance comparisons before selecting a winner; and the final full branch. A reviewer may cover several checkpoints, but must remain independent of authorship and inspect the actual tested diffs.

The reviewer must challenge both the implementation and this directive, including: whether an alleged source issue is actually intended behavior; whether the native contract invalidates a proposed fix; whether performance numbers hide stale output; whether clean contours imply false certainty; and whether a smaller solution would be safer.

Every report must include reviewer identity/session/tool provenance, actual model when exposed (otherwise `not_exposed`), date, base/head SHAs, supplied evidence, independence/anchoring disclosure, commands run, raw outputs, findings with severity and source locations, falsification conditions, and an explicit verdict.

Track dispositions as accepted/fixed, refuted with evidence, deferred with impact, or unresolved. Critical/high findings block production release until resolved. A model saying “approved” without examining evidence is not sufficient. After fixes, reviewers must cover the new diff and the final tested SHA.

### 12.3 If no separate reviewer can be invoked

Complete useful implementation and verification work that does not depend on the missing decision, perform a clearly labeled self-review, and prepare the exact review packet. Mark independent review `blocked` or `not_run`. Do not fabricate a second agent, reuse the old self-review as sign-off, or claim production-ready status. Do not halt all unrelated progress merely because a reviewer is unavailable.

## 13. Required final response from the implementation agent

Deliver a concise summary with linked local artifacts and the following exact categories. Populate them from actual evidence, not confidence language.

**Implemented:** Concrete behavior changes, preserved behavior, optional work adopted or deliberately deferred.

**Source and target:** Base/head SHAs, branch/fork status, game build/branch, Unity/loader/runtime identities, assembly hashes or manifest path.

**Verification:** Pure C# test counts; adapter/fake-engine cases; actual target build/load checks; multiplayer scenarios; lifecycle cycles; replay parity. State `not_run` or `blocked` for missing categories.

**Performance and freshness:** Reference/candidate conditions, primary metric, measured difference and variance, total CPU/GPU/memory tradeoffs, publication age/cadence, and limits. No FPS estimate substitutes for a measurement.

**Independent review:** Reviewer provenance, reviewed SHA, verdict, fixes/reruns, unresolved severities; or explicit lack of independent execution.

**Delivery and rollback:** Exact package/source/evidence paths, installation scope, original-file backup and tested restoration procedure, no unexpected external writes.

**Remaining work:** Blocking gates and exact missing evidence, followed by a resumable next task. Do not end with a generic offer to finish work already authorized.

Use one readiness classification: `development_only`, `target_runtime_candidate`, or `release_ready`. Only the last classification requires and asserts all mandatory gates, including independent review and authorized target validation, have passed. Optional experiments can be deferred without preventing completion when the chosen path satisfies the product contract.

## 14. Definition of done and blocked-work behavior

The required outcome is a correctly scoped, evidence-backed implementation, not an unconditional v2 rewrite. All of the following must be true for `release_ready`:

- [ ] The actual target is inventoried and the packaged Release DLL builds/loads there.
- [ ] Native-map knowledge restrictions, unsupported contexts, and immediate policy/session invalidation are verified.
- [ ] Support/dominance semantics, one-sided/zero behavior, opacity/style rules, and default-disabled movement indicators pass the accepted tests.
- [ ] Completed-result publication, cancellation, buffer ownership, repeated lifecycle cleanup, and actual map alignment are verified.
- [ ] Performance claims use the corrected oracle and matched freshness/quality with raw evidence; any lack of improvement is stated honestly.
- [ ] The selected renderer meets visual/mask/UI requirements; a new renderer is not required merely to satisfy the original proposal.
- [ ] Multiplayer compatibility scope, untested contexts, configuration migration, package contents, and rollback are documented.
- [ ] A genuinely separate reviewer has reviewed the final tested SHA; critical/high issues and mandatory gates are resolved.

When the game is unavailable, implement/test the pure core, reproducible build tooling, fake-engine lifecycle/policy tests, and benchmark/review harnesses. Label real integration tasks blocked and deliver a source-based development package. When network/dependencies are unavailable, preserve useful local work and exact failed commands rather than substituting unverified libraries. When a discovered game contract conflicts with this directive, record the evidence and choose the least-permissive, smallest correct adaptation; seek user input only for a genuinely new product/authorization decision that cannot be resolved from the workspace.

Keep a checkpoint after each task: current SHA, gate statuses, command results, active assumptions, and next executable step. Report progress at meaningful findings and milestones. Do not promise unattended future results or invent background work.

## 15. Sources, provenance, and reference map

The handoff consolidates the supplied October 18, 2026 review; it does not claim a new upstream/runtime audit. External links below are evidence anchors for the executing agent to inspect and revalidate where time-dependent. Local companion links resolve in the full bundle.

- **[R1]** [FrontlineMap adversarial self-review](inputs/review/FrontlineMap_Adversarial_Review_2026-10-18.md), especially Scope, A1–A6, §§3–5, release gates, and unresolved items. Source-confirmed claims and mathematical examples in this directive are drawn from that review unless identified as new execution requirements.
- **[R2]** [Revised implementation plan](inputs/review/Revised_Implementation_Plan.md). Stage ordering, architecture boundaries, and conditional renderer/optimization scope.
- **[R3]** [Original independent-reviewer brief — not executed](inputs/review/Independent_Reviewer_Brief_NOT_EXECUTED.md). Prior reviewer status, evidence anchors, expected user hardware, and source-first methodology.
- **[R4]** [Original probe source](inputs/review/evidence/adversarial_probes.py), [results](inputs/review/evidence/probe_results.json), and [run output](inputs/review/evidence/probe_run.txt). Twelve mathematical/cost exhibits, not compiled production tests or runtime benchmarks.
- **[S1]** [Pinned upstream Plugin.cs](https://github.com/9138noms/FrontlineMap/blob/6450ab9dc75503e5fedefc0280182436e5d993ec/Plugin.cs). Lines 108–249: allocation/scheduling/publication/map attachment; 254–387: factions/observations/coordinates/kernel/types; 390–536: raster/opacity/shift/zero logic. Reconfirm locations if source changes.
- **[S2]** [Pinned FrontlineMap.csproj](https://github.com/9138noms/FrontlineMap/blob/6450ab9dc75503e5fedefc0280182436e5d993ec/FrontlineMap.csproj). Reviewed framework and reference paths.
- **[S3]** [NOTacMap comparison source](https://github.com/Big-LouDE/NOTacMap/blob/4d08353ce0b9b96a69a4ab860777cd50af6c2a31/SnapshotBuilder.cs). Useful native tracking/airbase leads, not official API authority or a dependency requirement.
- **[S4]** [Unity coroutine reference used by the review](https://docs.unity3d.com/2022.3/Documentation/Manual/Coroutines.html), [Texture2D.Apply](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Texture2D.Apply.html), and [RawImage.uvRect](https://docs.unity3d.com/2018.4/Documentation/ScriptReference/UI.RawImage-uvRect.html). These document general mechanisms; use documentation matching the actual target when executing. Their version numbers are not claims about the user's installed engine.
- **[S5]** [Preserved upstream license](inputs/review/UPSTREAM_LICENSE.txt) and [original evidence manifest](inputs/review/evidence_manifest.json).

The task IDs, proposed file layout/internal contracts, additional acceptance cases, command interfaces, reviewer checkpoints, and delivery structure are new implementation requirements defined by this directive. They are not assertions that those files/functions/tests already exist upstream.

**Begin with Task 0. Preserve evidence, fix correctness first, optimize against the corrected baseline, and earn every compatibility, quality, and performance claim with the appropriate test.**
