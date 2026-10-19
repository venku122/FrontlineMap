# Runtime continuation independent review — 2026-10-18

Reviewer: `/root/native_contract_review`, separate agent; did not implement production changes or operate the game. Scope: final working-tree runtime continuation relative to baseline `0bc6e00`; final committed SHA must be pinned separately. This is a scoped development review, not production signoff.

## Code findings

No blocking correctness or privacy finding in the reviewed final sources. Native material admission is narrowly extended to the observed `UI/AdditiveMasked` shader with finite identity texture transform; the overlay retains its standard alpha material and inherited masking. Active map/marker hierarchy gates prevent repeated capture of frozen closed-map positions. Clear disables the RawImage before dropping its texture, avoiding Unity's white fallback; reset clears adapter references and releases controller/renderer ownership.

The captured-cohort authorization invariant is sound: every successful capture checks the previous cohort before replacing it. Without revocation, the newest cohort contains all earlier displayed/active/pending inputs. A missing/changed captured token or viewer change increments the policy revision and cancels old output/work; additions and reorder alone wait for cadence and cannot add information to an older result. Every-frame and before-publication scans remain present. Exact equality resolves hash collisions; packed unit identity includes instance and persistent identity, with persistent ID confirmed uint by coordinator. Fresh position changes intentionally wait for cadence. Cross-controller staging rejection and older-input/hash-collision regressions cover the key boundary.

StepScheduler hoists cell dimensions, row displacement squared and row index only. Stamp traversal, cell traversal, accumulation order, footprint, kernel expression and 128-cell yielding remain unchanged. No worker/thread/async solver change was introduced. Native capture/upload/allocation remain indivisible costs.

## Verification and evidence scope

Independently executed `/tmp/frontlinemap-dotnet/dotnet test tests/FrontlineMap.Core.Tests/FrontlineMap.Core.Tests.csproj --no-restore --verbosity quiet`: **88 passed, 0 failed, 0 skipped**. These tests compile core/runtime/diagnostics; they do not themselves exercise Unity adapter/plugin wiring. Target build success was reported by coordinator, not rerun by this reviewer.

Read coordinator-generated native evidence under `artifacts/runtime-2026-10-18/`: `cohort-runtime-regressions.json` contains 49 passing checks including twenty closed/reopen cycles; sampled off/on states report owned resources 0/0 and 1/1. This is ownership instrumentation, not a complete native memory-leak proof. `hoist-landmark-zoom1.json` and `hoist-landmark-zoom3.json` each report 404 admitted markers, zero missing markers, zero pixel landmark error, raycast disabled and sibling index zero. These numeric audits establish the sampled projection alignment, not universal visual acceptance.

Read `hoisted-replay/comparison.json` and `hoisted-native-replay/comparison.json`: nine synthetic and three recorded native snapshots pass with zero field error and zero classification/alpha mismatches. Replay timings are tooling measurements, not Unity frame-performance evidence. Coordinator reports stamp time approximately 130 to 100 ms and presentation age approximately 1.4 to 1.15 s in sampled native runs; these are local samples, not accepted broad performance budgets. Earlier frozen-map profiles from the faulty fixture faction-selection path are excluded as final target baselines.

Remaining gates include multiclient host/client policy transitions, stale/lost/death/ownership/type behavior under real network changes, all maps/themes/DPI/VR and hardware-input coverage, other-mod coexistence, native cleanup/fault injection, broader memory/performance baselines, production-profile deployment/rollback and final package/commit identity verification. Native fixture repair invokes the real faction callback; this review does not certify unrelated automation source. Source-only historical limitations must be read with the newer scoped evidence, without treating unexecuted gates as passed.

## Reviewed source SHA-256

- `Plugin.cs`: `9c6183fe19894d6b755a5a9c115f6907502536587c9eee7033bf83acb9acf16f`
- `src/Core/AdmissionTracker.cs`: `9ac1b157a1e9e21a44c063ecbae36359b2dc1979a2a667edb0285a315e6919ab`
- `src/Core/MapSurfaceContract.cs`: `da07b9c71e915a43de4d46c50e9555bb672ecaefb4b9a0fc00afa82d08e831bb`
- `src/Game/GameObservationAdapter.cs`: `df19092ebd75ebfb646a3cf6dbf91105b0dd8259b7e8d15a57a161a90966b230`
- `src/Game/MapBinding.cs`: `9e8b95a4081c5e979eeb578e3270482249243c0640361d0463b81a52f39f19c3`
- `src/Rendering/RasterOverlayRenderer.cs`: `e5f12ae3a63d4edd5d1acacc1489820979f12e515319d9b9cecfe16a840ce75c`
- `src/Runtime/OverlayController.cs`: `1b4255a62baca34ad0a610d9b525d0e761804ee64dd0a99816bd0886c5b611d2`
- `src/Runtime/StepScheduler.cs`: `1bafeda13e32392570ace1b3feb3b3b7eced851ecf33b2aa40ec6dfaeb79c296`
- `tests/FrontlineMap.Core.Tests/AdmissionTrackerTests.cs`: `40da572369e8d3ffe7dbf13c43b2e70a3dec1fc75c5540f6aa2f622e3d29dabd`
- `tests/FrontlineMap.Core.Tests/LifecycleTests.cs`: `d10e7b38bef76d1d1e36404f6711cca818940c48925c9ff9ff0b242978bc05e9`
