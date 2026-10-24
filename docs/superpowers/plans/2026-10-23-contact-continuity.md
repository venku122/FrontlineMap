# Contact continuity implementation plan

> For agentic workers: use superpowers:executing-plans to execute each tracked item with red/green verification.

Goal: execute all three October23 playtest to-dos with native and portable evidence.
Architecture: copied bounded enemy history, own-roster admission, presentation cancellation separated from knowledge lifetime; existing incremental model and confidence rendering remain.
Tech stack: C#, net472/BepInEx5 production; .NET8 xUnit/replay; disposable Unity/Mono bridge tests.
Spec: ../specs/2026-10-23-contact-continuity-design.md

## Global constraints

Normal installed FrontlineMap stays uninstalled. No hidden-unit live reads for history. No global unit enumeration for production observation admission. Preserve Legacy numerical compatibility, hash-bound build evidence, exactly two assigned-viewer factions, jamming rejection and hard-context cancellation. 512 remains recommended; previous performance failures remain recorded until a measured rerun accepts them.

## Review focus

- Cosmetic friendly icon state must not revoke own roster support.
- Cached enemy data must not refresh on hidden movement/death or stale recapture.
- Viewer/session and backward time must clear knowledge; map hiding must not.
- Capacity, expiry, duplicate IDs and reacquisition must be deterministic and bounded.
- Corrected availability must not disguise expired output or publication failures.

## Tracked deliverables

- [x] Read backlog, create active execution goal and verify clean242-test baseline.
- [x] Audit native own-roster and tracking contracts independently.
- [x] Implement and test bounded pure ContactMemory with original timestamps, context clearing, capacity and reacquisition.
- [x] Add new native regression cases and reproduce friendly cosmetic flashing on1.6.1.
- [x] Expand layered adapter to own-team roster/airbases; retain enemy copies; preserve Legacy source.
- [x] Separate presentation reset from historical cache destruction and expose validated history configuration/legend.
- [x] Run core/replay, review source boundaries and regression cases; fix failures before candidate build.
- [x] Build/hash candidate and bridge, execute native cosmetic, hidden-motion/death, history, reacquisition and lifecycle tests.
- [x] Execute both-map host/client and bounded availability/performance/resource comparisons; record exact workload and outcomes.
- [x] Update original to-do checklist and evidence/readiness docs with actual outcomes, commit and push to fork.

Files: src/Core/ContactMemory.cs; tests/FrontlineMap.Core.Tests/ContactMemoryTests.cs; src/Game/GameObservationAdapter.cs; Plugin.cs; tests/native/FrontlineMapAcceptanceTests.cs; tests/native/FrontlineMapLayeredTests.cs; tests/native/FrontlineMapRuntimeTests.cs; native follow-up harness; version/docs as needed. Core tests precede memory implementation; native fixtures precede adapter changes. Root owns game launches and shared adapter/plugin integration. Independent agents may audit source or implement only explicitly assigned pure files.

## Evidence so far (prototype, not final acceptance)

Baseline242 core tests passed. Pure history now has36 additional tests, total278. Native1.6.1 red: disabling389 friendly icon images changed policy1→2 and cleared displayed output. Prototype1.7.0 green: same cosmetic change preserved policy/display; frame probe observed514 displayed frames, zero hidden frames. Native cached-position grace, tracking loss, hidden metadata/movement/death, reacquisition, ownership and friendly death contracts passed. Final-source attestation, lifecycle/roster/both-view/multiplayer/performance evidence remain pending.

## Remaining live refresh correction

- [x] Red/green tests for explicit same-viewer policy revision retaining original displayed knowledge without extending expiry.
- [x] Independently track viewer identity, keep expired-history and hard-context cancellation, and label retained output.
- [x] Rerun native publication race, view/team boundaries and live availability on the revised exact binary.

The b655120s run measured96.484% availability, three real cohort revocations and1.556s longest gap. Those results motivated this correction; do not present them as acceptance of the next binary.

## Native tracked items

The active execution goal owns implementation and delivery. Separate native Codex chats hold read-only acceptance review for each requested item:

- Friendly flashing: `01a16c4f-dca7-7c10-85cd-6273c5c19a4f`.
- Team-wide friendly coverage: `01a16c4f-ee15-7f13-be63-725b9fecd9d6`.
- Fading enemy history: `01a16c4f-fa3b-72f0-98ba-f3f52e904fca`.

They do not deploy or duplicate implementation. Final status and measured release limits are recorded in the October24 validation report.

Final execution:294 core tests,15 readiness fixtures,9/11 replay fixtures and bounded Native passed. Capped/live performance passed; uncapped/dense failed declared thresholds and remain release blockers. See docs/RUNTIME_VALIDATION_2026-10-24_V170.md. Delivery/push verification is recorded with the package manifest.
