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
- [ ] Run core/replay, review source boundaries and regression cases; fix failures before candidate build.
- [ ] Build/hash candidate and bridge, execute native cosmetic, hidden-motion/death, history, reacquisition and lifecycle tests.
- [ ] Execute both-map host/client and bounded availability/performance/resource comparisons; record exact workload and outcomes.
- [ ] Update original to-do checklist and evidence/readiness docs with actual outcomes, commit and push to fork.

Files: src/Core/ContactMemory.cs; tests/FrontlineMap.Core.Tests/ContactMemoryTests.cs; src/Game/GameObservationAdapter.cs; Plugin.cs; tests/native/FrontlineMapAcceptanceTests.cs; tests/native/FrontlineMapLayeredTests.cs; tests/native/FrontlineMapRuntimeTests.cs; native follow-up harness; version/docs as needed. Core tests precede memory implementation; native fixtures precede adapter changes. Root owns game launches and shared adapter/plugin integration. Independent agents may audit source or implement only explicitly assigned pure files.

## Evidence so far (prototype, not final acceptance)

Baseline242 core tests passed. Pure history now has36 additional tests, total278. Native1.6.1 red: disabling389 friendly icon images changed policy1→2 and cleared displayed output. Prototype1.7.0 green: same cosmetic change preserved policy/display; frame probe observed514 displayed frames, zero hidden frames. Native cached-position grace, tracking loss, hidden metadata/movement/death, reacquisition, ownership and friendly death contracts passed. Final-source attestation, lifecycle/roster/both-view/multiplayer/performance evidence remain pending.
