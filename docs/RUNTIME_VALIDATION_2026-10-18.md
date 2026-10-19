# 1.5.1 runtime validation — 2026-10-18

Readiness: development candidate with executed native tests. Full production acceptance remains open. This report supersedes the historical source-only load statements in the1.5.0 report.

## Target and execution

Nuclear Option0.34.1, Steam build24724372, Unity2022.3.62.7762112 and BepInEx5.4.23.5. Assembly-CSharp SHA256 `eb3b93bdaec37dd7b3bab72f801a2c84e5be2ae3c559f39251e2320ae6b11ccc`. A fresh authorized game copy lives at `work/FrontlineMap-runtime-test`. Guarded deployment used expected hashes, WhatIf preview and rollback manifests. Normal Steam installation DLL/config fingerprints are checked separately. No remote publication occurred.

The existing MDK bridge was built in an isolated export with its existing0.34 compatibility patch. Scoped fixture repairs invoke the real native faction callback and close path. The automation checkout's pre-existing dirty work was preserved. Windows PowerShell7's existing RPC client controlled the game without execution-policy overrides. Earlier faulty fixture states that left mapHQ null/AllowedToOpen false are not accepted profiling baselines.

## Fixed native failures

- Normal map material `UI/AdditiveMasked` was rejected. Its full UVs, identity texture transform and native RectMask2D were verified; the narrow material contract now accepts it. Unknown shaders/geometry still fail closed.
- Closed maps stopped native marker updates while the adapter continued scanning frozen children. Active map/root/image hierarchy gates now cancel output and prevent recapture.
- Clearing a RawImage texture could draw Unity's white fallback. Clear disables the component; commit enables it after binding the prepared texture.
- Every admission addition/reorder canceled full computation, causing an observed12s publication starvation in a lifecycle trial. Current tracking checks the successfully captured cohort: additions/reorders do not revoke older authorized input; captured removal/owner/weight/freshness/viewer changes still revoke immediately. Every-frame and before-publication checks remain.
- Per-frame observation allocation/sorting was unnecessary. Reusable exact token sets scan every frame; snapshots/sanitization materialize only when a capture is needed. Reset clears retained native references.
- Capture timing excluded binding and could be overwritten by publication revalidation. FLM_CAPTURE records capture read cost; FLM_METRIC separately names latest_read_ms.
- A compact backed, color-matched full-map legend replaces the cockpit-wide plain label. Overlay objects use the native UI layer and stay below marker siblings with raycastTarget=false.
- Solver cell-size/row invariants are hoisted without changing arithmetic order, stamp order, chunk size, expiry or publication rules.

## Executed evidence

| Scope | Result |
|---|---|
| Pure compiled suite |88passed, including material projection contract, exact admission revocation/hash collision, allocation reuse, and displayed/active/pending staging revocation |
| Replay |9synthetic +3recorded native snapshots: zero field error and zero classification/alpha mismatch against independent full-grid field calculation; shared rasterizer is not an independent visual oracle |
| Mutation checks |5deliberate production mutations rejected by assertions |
| Build/reference/tooling |Actual target-reference build;10references from intended roots, non-copying;14PowerShell filesystem/rollback guards |
| Native recovery suite |49passed assertions: assigned map; toggle state off/on; map close/reopen; controlled jam at LateUpdate and recovery; observer-map rejection/recovery;20map close/reopen lifecycle cycles |
| Native resource ownership |Closed states0textures/0objects; published states1/1; scene unload returned0/0 |
| Native geometric audits |Boscali full map at zoom1 and3:404admitted markers, missing0, projection error0px; Primeva full map:432markers, missing0,error0px; cockpit minimap:433markers, missing0,error0px |
| Hidden movement fixture |Moved an untracked, unrepresented enemy in one native frame and restored it; freshly sanitized432-observation content remained identical at fixed knowledge/time |
| Native factions |Boscali and Primeva tested in separate fresh missions. Native game refuses changing/nulling an already assigned faction; deliberately mismatched mapHQ/localHQ correctly clears output |

Toggle fixture exercises the same visibility/reset state transition; physical F9 hardware injection was not executed. Controlled jam injection verifies the runtime gate and recovery, not a complete enemy jammer flight. Geometric audits check projection against native marker centers, clipping presentation and ordering; they do not certify every map, DPI, theme or native mouse interaction.

## Preliminary performance

Windows/Unity samples use the same Escalation scenario, default512grid/12km radius/2ms slice/5s cadence and60FPS cap. Local Harmony instrumentation surrounds complete Plugin.TryRead calls. These are short same-scenario samples, not randomized/interleaved accepted performance trials; instrumentation overhead was not subtracted.

| Read path | Reads | Mean ms | P99 ms | Whole-game generation0 collections during sample |
|---|---:|---:|---:|---:|
| Material-only1.5.0 fix |1245|1.4830|5.0072|20|
|1.5.1 cohort + loop hoist |1280|0.8165|1.4078|4|

This measured about45% less average read time and72% less P99 read time in these samples. Typical native stamp-stage CPU time fell from approximately130to100ms, with sampled publication age around1.4to1.15s. The .NET admission warm-loop regression reduced6,565,600allocated bytes per100scans to0after capacity warmup; that is a pure managed test, not a claim of zero Unity allocations. Mono's allocated-byte API returned a constant-zero stub during a deliberate allocation probe, so native allocation bytes are unavailable. Field/result/texture allocations remain; main-thread allocation and upload can still produce indivisible hitches. Whole-game max frame time was not consistently improved, and no broad FPS gain is claimed.

## Remaining acceptance gates

Multiclient host/client parity and other installed mods; all maps/themes/DPI/VR; physical F9/mouse input; full real sensor fresh/stale/lost/death/ownership/type transitions; complete native fault injection;20scene/mission-loading cycles beyond the20view cycles; native/GPU memory accounting; and randomized stock/loader/legacy/candidate performance trials with accepted budgets. These remain unexecuted or partial, not passed. The normal installation has not been upgraded. Use the versioned delivery's build attestation, package hash, final review provenance and per-case matrix to identify the exact candidate.

Raw logs, local permitted captures and fixture scripts are under `artifacts/runtime-2026-10-18`; private game code/assemblies and raw captures are excluded from the redistributable source/plugin package. The delivery evidence contains aggregate results and selected UI screenshots.
