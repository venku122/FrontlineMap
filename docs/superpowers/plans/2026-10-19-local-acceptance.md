# Remaining Local Acceptance Plan

> For agentic workers: execute inline with superpowers:executing-plans. Use one independent final review if production changes are required.

**Goal:** Execute the previously listed remaining checks on the available local target and fix reproduced defects.
**Architecture:** Keep production code unchanged unless a failing test identifies a defect. Extend the isolated MDK bridge fixture, not the user's automation checkout, to invoke native game lifecycle/network/knowledge APIs and gather aggregate evidence. Separate controlled native transitions from end-to-end sensor/network observations.
**Tech stack:** Nuclear Option0.34.1, Unity2022.3, BepInEx5, C# net472 production, isolated netstandard2.1 bridge, Python orchestration, Windows PowerShell7 RPC.
**Spec:** docs/RUNTIME_VALIDATION_2026-10-18.md remaining gates and the user's2026-10-19 execution request.

## Global constraints

Use only disposable game copies. Preserve normal profile and unrelated automation edits. Do not publish or redistribute game code/assemblies. Hardware/remote-client tests require actual available devices/clients; simulated fixtures cannot replace those claims. No execution-policy bypass. All reports identify source and binary hashes.

## Review focus

- Stale contacts must follow native last-known markers rather than live positions.
- Death/forget/owner transitions must invalidate captured admissions before publication.
- Real scene unloads must release candidate textures and references.
- Multiplayer clients must use their own native knowledge; matching hashes are expected only for equal knowledge/time.
- UI scaling, clipping, faults and long runs must not retain disabled/white overlays or unbounded resources.

### Task1: Native acceptance fixture and baseline
- [ ] Record current long-session state/logs and normal-profile fingerprints.
- [ ] Add isolated bridge commands for native restart/start, network state, memory and knowledge transition checks.
- [ ] Build fixture, deploy only to stopped disposable copy and verify RPC responses.

### Task2: Gameplay transitions and faults
- [ ] Exercise native tracking fresh/stale/forget/reacquire with native marker updates and compare adapter outputs; include hidden-position invariance.
- [ ] Exercise native unit death/ownership callbacks, aircraft spawn/respawn and supported team/observer paths. Distinguish controlled knowledge from real detector coverage.
- [ ] Inject native renderer preparation fault and verify cleanup/recovery; reproduce/fix production defects through failing tests first.

### Task3: Scene/UI stability
- [ ] Execute20real mission restarts, alternating factions, validating unassigned rejection, publication, unload and resource baselines.
- [ ] Audit available maps, native themes, zoom/pan/rotation and supported window sizes; verify raycast order via Unity event-system raycasts.
- [ ] Attempt supported Computer Use hardware-key path and inventory VR availability. If unavailable, record exact blocker without inventing hardware proof.

### Task4: Local multiplayer and coexistence
- [ ] Create separate disposable client copy and loopback UDP host/client, distinct bridge ports; verify actual roles and connection.
- [ ] Test assigned knowledge, observer transitions and a client without FrontlineMap; run parity only under equal recorded observations.
- [ ] Test copied installed UI/map mods in disposable profile, restoring isolated baseline afterwards; record incompatible dependencies rather than changing normal installation.

### Task5: Performance and memory
- [ ] Run randomized repeated instrumented baseline/candidate samples with60completed updates per trial where feasible, collecting read/frame/stage/resource and memory data.
- [ ] Measure instrumentation overhead separately, and run low/default/high resolution and radius stress trials. Use explicit measurement limits, no unavailable allocation/GPU claims.
- [ ] Investigate regressions; change production only after root cause and reproducing regression test.

### Task6: Verify and deliver
- [ ] Rerun compiled suite/replay for any production changes; independent exact-source review if changed.
- [ ] Update validation matrix with executed/pass/partial/unavailable, produce versioned local evidence package and verify its manifest.

## Execution update

Tasks1–4 executed within the recorded native scopes; hardware input/VR and exhaustive network/other-mod coverage remain unavailable or partial. Twenty candidate-present and twenty candidate-absent mission restarts completed. Three paired adapter microtrials and six full-pipeline stress samples completed. New deeper renderer fault tests exposed two defects, observed RED on1.5.1 and GREEN on preliminary1.5.2. The bounded hotfix is transactional UI construction and restoration of prior texture on publication callback failure. Final package verification and configuration stress results are recorded in the versioned delivery; no blanket production signoff.
