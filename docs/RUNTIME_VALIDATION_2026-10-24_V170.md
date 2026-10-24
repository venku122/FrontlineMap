# FrontlineMap1.7.0 exact-binary follow-up acceptance — October24,2026

The three playtest follow-ups are implemented and bounded native acceptance passes. **General release remains blocked by measured uncapped and dense multiplayer performance failures.** Normal-game FrontlineMap remains uninstalled; all candidate testing used disposable copies.

## Candidate identity

- Implementation commit: `24b816c2f61aa0b0843bf866bc2b68d7bb86c0ba`.
- Product source fingerprint: `b10f09ce7efaad187fcfe952e7e41cf4010b88907d613a69ecaf1f45a99a0166`.
- DLL SHA256: `83926cde39bb04b94ff710d5903780ac5a48bdd51f01c8c24f897c7b82f31faa`; assembly1.7.0.0.
- Loaded Mono module: `24670a61-bf17-4bfa-b8d0-a8795343df97`.
- Steam build24724372, observed Application.version0.34.1, assembly0.0.0.0. Public0.34.2 labeling remains unresolved metadata, not presumed incompatibility.
- Evidence: `artifacts/runtime-2026-10-23-v170-refresh`. The folder keeps its original start date. Later documentation commits preserve the product fingerprint; they do not rebuild this tested DLL.

## Executed gates

Core:294 portable tests,15 Python readiness fixtures,9 Legacy and11 layered replay fixtures passed. Native: all six required checks passed, including actual two-process loopback UDP opposing viewers and reconnect. Production Mono schema2 export with cached terrain replayed against the independent layered oracle, with matching fields/classifications/RGBA.

Performance: capped paired and120-second live criteria passed. Uncapped CPU-bound and moving dense multiplayer failed. Combined Readiness correctly exits2; Native exits0. Machine-readable results are in [LATEST_READINESS.json](benchmarks/LATEST_READINESS.json) and [ACCEPTANCE.json](benchmarks/ACCEPTANCE.json). Those failures are retained; thresholds were not weakened. Aggregation validates report hashes and assertions; it is not independent observation of the game.

## Friendly flashing and coverage

On1.6.1, disabling389 own-friendly icon images changed policy and blanked the display. The new adapter instead uses only the viewer’s native synchronized own roster and own airbase list. Cosmetic image filtering no longer controls authorization. Exact roster checks admitted383 full-map and384 cockpit entries with no missing or extra IDs; the pilot aircraft accounts for the additional cockpit entry. Cosmetic tests observed zero hidden frames in both views. Distant roster support is modeled without requiring an on-screen landmark.

An initial1.7.0 implementation atb655364 still measured96.484% availability and1.556s maximum blank gap during real roster revisions. That is retained as diagnosis, not final acceptance. Commit24b816c preserves the already shown same-viewer estimate during replacement, labels it “updating known estimate,” and leaves its original expiry unchanged. Publication race fixtures reject superseded work, including BeforePublish revisions, without publishing it. Hard-context changes and expired history cannot retain that frame; Legacy remains strict.

Final120-second live run: 59.37 presents/s, p99 21.77ms, **100% displayed probe availability**, zero hidden frames. Of7445 displayed probe frames,94 retained the bounded previous estimate during one contact-removal update. Availability therefore includes explicitly labeled older estimates and does not claim every frame contains the newest observations. Native read instrumentation averaged0.771ms, p991.227ms. Process working-set peak was2.268GiB for the entire game, not mod-only memory.

A sparse screenshot animation (`friendly-continuity.gif`,12 recorded native timestamps) records live cosmetic filtering/recovery. Per-game-frame availability probes provide finer evidence; the animation is not full-rate video or a performance measurement.

## Last-known enemies and timed fade

Cached contacts are copied values. The adapter reads native tracking.lastKnownPosition directly and does not call the live grace-position helper. Tracking/marker loss does not rejuvenate age; hidden movement, metadata and death do not refresh cached copies. Native contracts cover grace-window non-mutation, tracking loss, hidden metadata/movement/death after tracking removal, reacquisition and admitted ownership/death behavior. Own-roster resolution is restricted to roster IDs; global enemy enumeration occurs only in test fixtures.

Defaults:120 simulation-second stale half-life,900-second maximum history age,4096-contact FIFO capacity. Zero maximum history age disables retention. Map closing and temporary jamming preserve copies; jamming hides presentation and stops acquisition. Scene/viewer/model/config replacement clears history. Simulation pause stops aging; accelerated simulation advances it.

Actual game-clock samples of one lost contact, evaluated with its actual displayed snapshot, terrain and production Mono model/raster:

| Captured simulation age (s) | Effective confidence | Isolated maximum alpha byte |
|---|---|---|
| 0.00 | 1.000000 | 41 |
| 126.08 | 0.482749 | 20 |
| 247.36 | 0.239597 | 10 |

Screenshots contain the full multi-contact map; the numeric alpha above isolates that historical contact and is not the total screen opacity. The native expiry test set a12-second configurable lifetime and verified removal by original observation age with no retained-refresh fallback. Portable tests additionally pin the default900-second boundary, source-position stability, bounded capacity and time rollback. This is not a15-minute real-time soak.

## Movement, lifecycle and information boundaries

Rigid movement at2048 and8192 analytic contour segments caused **zero geometry rebuilds** in both full/cockpit views; zoom correctly rebuilt geometry to preserve width. Those contour-ceiling tests are separate from the real production dense workload.

Executed20 hide/reopen cycles, all five layers, invalid config preservation, sparse naval-terrain warning and explicit radial fallback, native renderer creation/commit/partial/dirty faults, GPU palette and marker alignment. Tests retain at most one displayed raster and the expected overlay objects, reject superseded publication and clear on jamming.

The game forbids faction changes once an HQ is assigned. The fixture initially mistook the UI callback’s success for a successful Player faction change; mismatched map/viewer state correctly blanked FrontlineMap. Corrected native tests stop/restart the mission and select the opposite faction before assignment. Both actual transitions pass. Same-session viewer guards are covered by portable tests, not falsely reported as an executed vanilla faction switch.

The read-only history review identified an additional unexecuted case: metadata mutation while a stale tracking record and visible native icon both remain. Current admission can read owner/role/weight in that state. Native UnitMapIcon itself subscribes to faction-change/disable events and derives its color from current HQ, so stale tracking is not equivalent to an absent native marker. The lost-tracking hidden-metadata fixture does not certify all stale-but-still-represented metadata cases. Keep this explicit security/regression review item open before general release; no complete information-security certification is claimed.

Dedicated distance-threshold friendly coverage and ordinary non-jamming datalink disconnect/reconnect remain additional acceptance cases. Complete own-roster admission and jamming/tracking loss/recovery do not independently certify those cases.

## Performance limits

Three balanced20-second capped pairs: median mean-frame time16.727ms enabled versus16.657ms disabled. Pacing near60 can conceal reduced headroom.

Three balanced20-second uncapped640x400 pairs demonstrated CPU-bound execution using observed frame rate, CPU/GPU busy ratio and top-thread CPU. Median mean-frame time9.690ms enabled versus8.766ms disabled, ratio1.1054; it exceeds1.10. P99 allowance passed. Instrumentation overhead is included.

Dense live two-player loopback UDP, at least1000 mission units and300 actually admitted contacts,512 raster, continuous map motion,20 seconds per view:

| View | Presents/s | P99 frame (ms) | Display availability |
|---|---|---|---|
| Full map | 39.14 | 36.34 | 100.00% |
| Cockpit minimap | 38.42 | 40.43 | 100.00% |

Both views miss50 presents/s and33.333ms p99. Cockpit over50ms percentage also misses0.5%. This absolute acceptance has no matched disabled baseline and does not establish how much of the whole-game slowdown comes from the mod. It does establish that the declared target was not met.

512 remains recommended.2048 memory, longer soaks, larger/remote multiplayer and other hardware remain uncertified. The2ms compute slice is not a whole-frame budget. Physical keyboard/mouse/DPI/VR item5 remains excluded by prior user instruction. No FPS-improvement claim is made.

## Reproduction and provenance

Run the independently executable Core, Native, Performance and Readiness commands in [VALIDATION_REPORT.md](VALIDATION_REPORT.md). Retained evidence includes raw PresentMon CSV/process samples, predeclared criteria, executed Python harnesses, aggregate reports, build/reference attestations, gate logs, TRX and synthetic replay results. Native collectors require a private bridge compiled with the matching tracked `tests/native` sources and an authorized SDK transport; proprietary game/reference/bridge binaries and raw production observation exports are excluded from delivery.

The executed harnesses import the retained October19 transport and launch/process helpers. These are target-specific acceptance tooling, not a redistributable game automation SDK. Canonical older `tests/native/v161` tooling was not relabeled as executed1.7 evidence. Original failed team-switch fixture logs remain with the corrected reports.

The three native tracked review chats are listed in the [implementation checklist](superpowers/plans/2026-10-23-contact-continuity.md). Their early read-only reviews may report missing final artifacts from collection time; this exact-binary report and finalized machine-readable gates provide the completion record.
