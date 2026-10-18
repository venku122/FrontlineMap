# Independent source and directive review — 2026-10-18

Verdict: **approve the implementation direction with conditions; request changes before production release**. This is a source/contract review of the legacy baseline and directive, not approval of an implementation or packaged binary. The baseline has high-severity lifecycle and unresolved-context defects. No target load, multiplayer, calibration, or performance trial ran in this review.

Reviewer: separate agent `/root/independent_review`; model identity `not_exposed`. The reviewer did not author the implementation. The initial task provided the product, pinned source and review scope only; the reviewer returned written source-derived findings before receiving the earlier review, directive, probes or implementation reasoning. This session subsequently examined those materials and private decompilations. It is independent of authorship, but its later conclusions are informed by the supplied evidence.

Source base and reviewed HEAD: `6450ab9dc75503e5fedefc0280182436e5d993ec`. Initial worktree was clean. The coordinator later changed build files and added documentation/tests; those changes are not covered by this report. Reviewed target manifest: `artifacts/discovery/target-manifest.json`, Steam build `24724372`, Assembly-CSharp SHA-256 `eb3b93bdaec37dd7b3bab72f801a2c84e5be2ae3c559f39251e2320ae6b11ccc`. Existing-log engine/loader identities are historical observations, not a new launch.

Evidence: **S** direct source/control flow; **D** mathematical reproduction; **T** privately inspected target decompilation; **R** actual game/runtime observation. This report contains no new R evidence. Private target source is referenced by class/method and local decompiler line number, not reproduced or redistributed.

## Independently discovered findings

### IR1 — High: unresolved viewer context fails open

Legacy `Plugin.cs:297-317` catches local-HQ resolution failures, then applies the tracking gate only if the HQ is non-null. Lines 322-323 stamp all otherwise eligible units of the chosen pair. Invariant: unavailable viewer context must not authorize hidden observations. Counterexample: two HQs exist during a transition, local-HQ lookup fails, and both sides' live positions are admitted. Minimum correction: explicit resolved assigned/spectator/unsupported context, fail closed on unresolved context, immediate invalidation on transitions. Evidence S. Target `DynamicMap.GenerateMap:461-470` intentionally permits all units for an explicitly null map HQ, so null can represent native spectator display; this does **not** authorize treating any failed local-HQ lookup as that display. Evidence that could overturn the defect: proof every reachable null lookup has an explicitly initialized and authorized spectator map, including all transitions. That evidence is absent.

### IR2 — High: refresh failure can permanently strand scheduling

`Plugin.cs:171` sets `computing`; only line 218 clears it; `Update:141` refuses subsequent work. The outer handler at 145-166 does not cover coroutine resumption after yields. Counterexample: texture is destroyed between yield 208 and upload 211; the exception leaves the persistent helper unable to refresh. Grids/history are also swapped before collection succeeds at 173-180. Invariant: failure must release work ownership and preserve only valid complete output. Minimum correction: explicit abort/recovery and identity-checked publication, not just a happy-path flag reset. Evidence S; engine transition reproduction remains missing. Evidence that could overturn it: an enclosing coroutine failure/recovery mechanism not present in the pinned file.

### IR3 — Medium: one-sided input generates opposing-colored contours

`Plugin.cs:426-451` gives an exact-zero sample with a positive neighbor nonzero gradient, zero estimated distance, full line alpha, and faction-1 color. Counterexample: only one friendly radial stamp, then a zero sample just beyond its footprint. Invariant: unsupported space must not imply an opposing front. Minimum correction: retain opposing support, require two-sided support for fronts, define exact-zero coloring. Evidence S+D. Native screenshots are absent; the arithmetic is sufficient to establish the counterexample. A deliberately specified unrelated meaning for the color could change severity, but conflicts with the requested influence/front semantics.

### IR4 — Medium: movement indicators oppose translating contour motion

`Plugin.cs:489-511` draws along `sign(delta field) * gradient`. For `F(x,t)=x+t`, the contour moves left as time grows and the implementation points right. Invariant: a boundary-shift indicator must agree with its defined displacement. Minimum correction: disable indicators initially; a sign change alone does not address contacts, degeneracy, elapsed time or history. Evidence S+D. The author could overturn the displacement interpretation by specifying a different intended indicator meaning; no such definition was supplied.

### IR5 — Medium: ordinary sources cannot produce default territory fill

Default alpha is .012 (`Plugin.cs:44`); byte conversion and cutoff occur at 413-423 and 453-458. Fill requires absolute influence approximately 32.68. The default strongest isolated source has weight 10. Independent Python arithmetic produced bytes 0 for 10, 1 for 20/32 (discarded), 2 for 33 and 3 for 50. Invariant: readable fill defaults must survive byte quantization for intended ordinary support. Minimum correction: separate opacity from arbitrary dominance magnitude and test real byte conversion. Evidence S+D. If the desired default is effectively contour-only this is a design limitation, rather than a runtime failure; the directive explicitly requires readable simple-source fill.

### IR6 — Medium: projection assumptions and airbase coordinates disagree

`Plugin.cs:186-190,352-365` assumes one scalar dimension and centered square bounds. Units use global positions (322), but bases use their root transform's local position (341-342). Target `GlobalPositionExtensions:8-11` subtracts `Datum.originPosition`; native `AirbaseMapIcon.UpdateIcon:71` uses the base center in global coordinates. Counterexample: nonzero datum offset; equivalent base/unit positions stamp different cells, and root-versus-center offsets remain possible at zero datum. Target `DynamicMap.LoadMapImage:219-225` sizes the map-image rectangle from the two-dimensional MapSize, while unit icons use the scalar display factor. Minimum correction: verified rectangular geometry and the same base center/global conversion as native. Evidence S+T; actual origin shifts/calibration remain unobserved. Native dimensions and assets proving square geometry in every supported map could narrow the square-map risk, but cannot cure the mixed coordinate access.

### IR7 — Medium: registry order is not a supported faction relationship

`Plugin.cs:266-273` chooses the first two HQs, and 310-312 discards others. Counterexample: local HQ is third; local influence disappears and fallback colors can label another faction friendly. Minimum correction: derive the local/hostile relationship deterministically and reject unsupported multi-sided contexts. Evidence S. Target `DynamicMap.GetFactionMode:1081-1114` treats a nonlocal non-null HQ as enemy for its map; it does not establish a game alliance system. Do not invent alliance handling unsupported by this target. Missing: runtime supported mission configurations.

### IR8 — Medium: persistent history crosses sessions; texture ownership is incomplete

The helper persists (`Plugin.cs:110`), history is never invalidated (`hasPrevGrid:88,214,473`), and replacement textures are assigned at 239 without releasing prior ones except final helper destruction at 124. Counterexample: new map begins with previous mission's comparison grid, producing fictitious shift indicators; repeated overlay-parent destruction overwrites texture references. Minimum correction: session/binding identity, history reset, one explicit release path per texture. Evidence S. Actual retained native resource growth has not been measured, so this is an ownership gap, not a measured memory leak.

### IR9 — High contract gate added after native inspection: marker eligibility and jamming

Target `DynamicMap.SpawnQueuedIcons:748-755` excludes disabled units and definitions with nonpositive map-icon size. `DynamicMap.UpdateIcons:796-819` deliberately distorts and fades unit icons while cockpit jamming is active; `UnitMapIcon.JammingDistortion:348-352` implements that presentation degradation. An overlay of undistorted true coordinates could restore spatial certainty intentionally removed by native presentation. Invariant: native-map permission includes deliberate degradation and marker eligibility, not only tracking membership. Minimum correction for initial scope: respect verified marker eligibility and hide the influence output while map jamming is active, until a better policy is justified. Evidence T for native behavior, S for absence of equivalent legacy checks. Counterexample to test: fresh contacts during active jamming remain crisp in the overlay while their map symbols degrade. Missing runtime test and explicit product/native permission interpretation. Evidence that could overturn it: authoritative contract showing this degradation is cosmetic and exact undistorted positions remain deliberately available to map users through another native mechanism.

## Claim-by-claim reconciliation

| Prior item | Disposition | Reason / remaining evidence |
| --- | --- | --- |
| A1 live-state visibility | Partially confirmed | Null-context bypass and separate base path are real. Broad fresh-position leak claim is refuted on this target: `TrackingInfo.GetPosition:82-88` intentionally exposes live coordinates while observation age is <4 seconds; legacy `IsTargetBeingTracked` admits ages <=4. Exact age 4 is an edge discrepancy. Stale markers are omitted by legacy rather than followed indefinitely. Native map exposes unit map-icon definition, live HQ updates and disable events; do not assert those are hidden without a narrower example. Jamming/eligibility remains a concrete missing contract. |
| A2 signed field loses support | Confirmed | Equal F/E and absent F/E produce the same D. Separate nonnegative supports or lossless equivalent are appropriate to required semantics. |
| A3 / P01 false opposing pixels | Confirmed | Independent source/math finding IR3; source-derived float32 probe reproduces it. |
| A4 / P03 arrow direction | Confirmed for boundary motion | IR4; disable initially rather than pretend sign correction solves movement inference. |
| A5 lifecycle/publication | Confirmed | IR2/IR8; multiple invalidation points and history mutation precede success. |
| A6 / P11 projection | Partially confirmed | Airbase global conversion discrepancy is established from target source. Rectangular map scaling requires handling. P11 UV cropping remains a conditional fake: this target's mapImage is a GameObject containing an Image, not proof of a RawImage UV crop. Native child transforms plausibly handle zoom/pan/rotation; don't duplicate them without evidence. |
| P04 magnitude shortcut | Confirmed for generic fields; kernel realizability needs fixture | `abs(D)>5` is not a distance bound. Probe supplies a local linear stencil, not a complete sum-of-permitted-radial-stamps construction. Build a kernel-generated steep-front case for production regression. |
| P05 dash sampling dependence | Confirmed | Unnormalized two-sample differences change with grid spacing. Test at matching physical locations; probe establishes rule dependence, not a full resampled world fixture. |
| P06 exact zero | Confirmed | Strict ZeroCross disagrees with contour treatment. Arrow removal resolves its immediate path; keep one deterministic classifier policy. |
| P07 fill quantization | Confirmed | IR5. |
| P08/P09 scheduling | Confirmed arithmetic | Units-per-frame division is not a deadline; one stamp and whole raster remain indivisible. Yield count is not measured Unity time. |
| P10 P99 blind spot | Confirmed statistical example | Periodic costs can fall beyond P99. Requires target event-aligned metrics, not a claimed real hitch distribution. |
| P12 quadratic scaling | Confirmed payload estimate | Managed arrays and texture payload scale quadratically. Actual native/GPU/driver/transient allocations need measurement. |
| C1 factions | Confirmed source assumption | IR7. No evidence yet of an actual native allied-HQ relationship. |
| C2 config | Confirmed | Different settings are read in different phases, resolution only at helper creation; finite/positive validation absent for most entries. |
| C3 cleanup/metadata | Confirmed source mismatch; migration provenance incomplete | Plugin attribute 1.3.1 versus startup log 1.0.0 is direct. Scene listener lacks unsubscribe. Historical `com.yuulf` configuration is supplied by handoff, not proved by this pinned source. Preserve files; don't infer current ownership from a historical filename. |

## Directive challenges and minimum scope

The architecture is appropriate only as small concrete components. Its file/type table is explicitly proposed; do not implement an interface framework, a worker, dirty tiles or a renderer alternative merely to satisfy a list of names. A simple explicit main-thread state machine, two support grids, incremental raster work and staged texture publication suffice as the first candidate. Preserve the direct scalar oracle independently enough for parity tests to detect mistakes; sharing the complete solver between oracle and candidate weakens that check.

The native map permits live coordinates during its observation grace window, uses known coordinates afterward, displays actual icon/type, updates actual faction and reacts to disable events. Applying a stricter speculative policy may unnecessarily hide valid native information. Conversely, omitting jamming and icon eligibility would be less restrictive than native presentation. Record these narrower rules and add a directive decision rather than label all true-state reads leaks. Airbase registry membership alone is not evidence all hostile base ownership is public: native generation uses `HQ.GetAirbases`, and the underlying ownership list population requires inspection.

Task 4 says both disable arrows by default and omit them from the first production candidate (§6.1). Follow the stronger omission rule; don't retain an enabled configuration path without its separate evidence. Screen-space line width and existing map transforms need target verification before claiming visual acceptance. A finite-difference contour test can pass while native UI ordering/masks still fail.

Runtime-dependent gates remain open. Target assemblies enable a real compile, not an observed load. Main-thread budgets, opacity defaults and freshness limits may be provisional development values until measured; they must not be reported as tuned or passing G5. No runtime profile has been created or launched by this reviewer. A useful local development delivery with explicit blocked gates is authorized by the directive; production certification is not.

Minimum sequence: (1) explicit native contract including jamming/marker eligibility and viewer resolution; (2) validated settings and reproducible target build; (3) coherent sanitized capture, identity invalidation and staged publication; (4) two-support scalar semantics, arrow omission, deterministic raster tests; (5) bounded intrastamp/raster scheduling with a separate oracle and honest target-measurement blockers; (6) final tested-HEAD/package review. Do not claim a performance win without a corrected comparable baseline.

## Executed and not executed

Read pinned Plugin.cs and project with numbered lines; checked HEAD/status; independently evaluated alpha and zero-cell examples with Python 3; read the directive, original review/plan/reviewer brief and probe source; inspected the supplied target manifest and private decompiled native classes listed in the provenance file. The Python mathematical exhibits were rerun with their output redirected into this report directory; their original package was left unchanged. They remain 12 legacy mathematical/cost exhibits, not compiled production tests or target gameplay checks.

Not executed: builds, package inspection, installation, launch, native map calibration, host/client multiplayer, actual lifecycle cycles, actual fault injection, profiler/timing benchmarks, or implementation review. The missing integration and final-HEAD evidence prevents production approval. No source or game files were modified by this reviewer; only independent reports under `docs/review/` were written.
