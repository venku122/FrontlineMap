# FrontlineMap 1.6.1 review remediation and local acceptance

October 22, 2026. **Development candidate; general release readiness is blocked by measured performance failures.** Core and bounded native feature acceptance passed. Capped performance passed; uncapped CPU-bound and dense moving multiplayer acceptance failed. Criteria were declared before measurement and were not relaxed after the failures.

## Exact tested product

| Identity | Value |
| --- | --- |
| Implementation commit | `47f2085b08b77556ed2dd004f37bfeb82d14a5f2` |
| Assembly version | `1.6.1.0` |
| Loaded DLL SHA256 | `1b36b26c9c09dda167c39fef5cbb96f8bb3f1ebc118c7f30181cab27aed56f6b` |
| Product source fingerprint | `0f6a8c9812f7d6884ab367f3205f292eaf2fc689427348673313b80abb01c238` |
| Loaded module ID | `87ae7bc9-5e99-4164-abfe-ff185e10cd86` |
| Reference manifest SHA256 | `b6c85136834079f50030c74c41e1822159c94a6957588f4c9fb6297ce6754ec9` |

Later documentation/harness commits do not change this product source fingerprint. The tested DLL was retained, not rebuilt with a later documentation commit embedded. Local evidence is in `artifacts/runtime-2026-10-22-v161`; build attestation is in `artifacts/build`. Native reports record the loaded module and candidate hash. Evidence files and proprietary reference/bridge binaries are not committed to Git. The companion local delivery retains machine-readable reports, raw performance samples and executed harnesses; production observation snapshot exports remain private.

The target is the inspected Windows i9-10850K / RTX 3090 host, BepInEx5/Unity Mono, Steam app2168680/build24724372. Native `Application.version` reports `0.34.1`; `Assembly-CSharp` reports `0.0.0.0`. The review's public `0.34.2` label has not been independently reconciled. Compatibility conclusions use observed build/reference identity, not an inferred public version label. Native tests used disposable copies, including a second actual UDP client; the normal Steam mod DLL/config inventory was protected.

## Review disposition

| Finding | Remediation and evidence |
| --- | --- |
| P1 capture during terrain initialization | Admission validation runs every frame; full materialization waits for terrain readiness and capture cadence. Real production `LateUpdate`, 398 admitted markers, sampler held unavailable for 300 invocations: 300 validation reads, **zero full captures/sanitizations**, then exactly one capture/sanitization on resume. These are simulated update invocations, not 300 rendered frames. |
| P1 source-distance detour | Direct within-cell and safe neighboring virtual edges remove the artificial center detour. Source-point distance is zero. The review's 64km/32-terrain/512-raster building example yields **0.6** at its source. Unknown terrain, domain/slope barriers and diagonal corner restrictions remain conservative. Longer paths retain an eight-neighbor approximation. |
| P2 moving contour rebuild | Projected metric caching reuses geometry through rigid translation/rotation. Affine stroke construction removes repeated per-segment Unity coordinate projections on zoom. Perspective with varying clip W retains conservative behavior. See motion results below. |
| Sparse terrain | Below 50% known static terrain, affected constrained output is explicitly unavailable, with an explanation and labeled radial fallback. Naval-map test observed 1,313/16,384 known cells (8.0%). Explicit configuration selection restored radial output; no silent model switch. Desktop clicking of the fallback button was not exercised. |
| Layered replay | Schema2 exports include sanitized static terrain. Independent raw-array reference propagation compares five arrays plus exact classification/RGBA with the production incremental scheduler. Eleven synthetic layered and nine Legacy fixtures passed. An actual native export replay also passed. The raw production export stays private. |
| High resolution | 512 remains recommended. 2048 remains experimental and uncertified; no repeated-capture 2048 memory/performance acceptance was performed. |
| Inconsistent validation | Core, Native, Performance and combined Readiness are executable separately, bound to source/DLL/reference hashes and fresh evidence. Current readiness records the measured failures. |

242 portable managed tests and 15 readiness verifier tests passed. The replay comparisons passed at absolute+relative1e-9 tolerance, with exact classification/RGBA. Native checks passed for all five layers, role contracts, stale/hidden/ownership/death policy, raycast behavior, invalid configuration retention, 20 lifecycle cycles, resource ownership, partial-creation/dirty-material/publication faults, and actual host/client disconnect/reconnect. These bounded checks do not certify every enemy-tracking or multiplayer policy path.

The original initialization regression was observed red before the lifecycle fix. Earlier prototype runs and setup failures remain separately labeled: slow zoom geometry motivated the affine path; an inactive cockpit fixture required a real player aircraft; the dense comparison initially connected before the client's network manager initialized and was rerun with explicit menu/network readiness. None is silently relabeled as final-binary acceptance.

## Moving-map geometry

Production renderer and native CanvasRenderer mesh, with explicitly analytic contour density, paused field, continuous rigid pan/rotation and zoom on the full map and an active cockpit minimap:

| View | Segments | Rigid rebuilds | Mean zoom mesh build |
| --- | ---: | ---: | ---: |
| Full map | 2,048 | 0 | 1.592ms |
| Full map | 8,192 | 0 | 6.423ms |
| Cockpit | 2,048 | 0 | 1.579ms |
| Cockpit | 8,192 | 0 | 6.196ms |

Each case traversed more than a full heading rotation. Width checks stayed approximately3 screen pixels; vertex counts were8,192/32,768. The instrumented rebuild allocation counter reported zero managed allocation after warmup. Earlier same-density full-map zoom measured7.78/31.11ms before the affine optimization, about a fivefold scoped improvement. This is a mesh-work measurement, not a whole-game FPS improvement. The high-density zoom path still exceeds2ms.

## Performance acceptance

Default tested model settings: raster512, terrain128, interval5s, scheduler slice2ms, maximum age15s, GroundControl, terrain enabled. Diagnostics and native observer costs are included. Timed runs were sequential, with no concurrent build/test workload.

| Scenario | Overlay enabled | Overlay disabled / criterion | Result |
| --- | --- | --- | --- |
| Paused capped, 3 balanced20s pairs,1280x800 | 60.017FPS; mean16.6618ms; median p99 19.6384ms | 60.022FPS; mean16.6605ms; p99 18.6562ms | Pass declared relative criteria |
| Live single-player,120s | 59.572FPS; p99 21.515ms; availability97.584%; longest gap1.896s | FPS≥55, p99≤33.333ms, availability≥80%, gap≤5s | Pass |
| Paused uncapped CPU-bound,3 balanced20s pairs,640x400 | 114.994FPS; mean8.6961ms; p99 12.6816ms | 134.126FPS; mean7.4557ms; p99 10.1599ms; mean overhead≤10% | **Fail:16.64% mean-frame overhead** |
| Dense moving full map, actual2-player UDP,20s | 43.968FPS; p99 32.9828ms; availability100% | FPS≥50, p99≤33.333ms | **Fail:FPS** |
| Dense moving cockpit, same workload,20s | 38.598FPS; p99 41.5577ms; availability90.65% | FPS≥50, p99≤33.333ms | **Fail:FPS and p99** |

Capped paired whole-process CPU median increased from1.326 to1.384 core equivalents (about4.36%). Live complete `TryRead` mean was0.942ms, p99 1.467ms, across7,505 reads. The60FPS cap concealed CPU headroom cost: uncapped trials had VSync0/targetFrameRate-1, one sampled process thread near one full core, FPS above90 and CPU-busy time greater than GPU time. This is real overlay overhead against F9-disabled on this target; no equivalent old-binary uncapped run establishes whether it is a new regression.

Dense scenarios had over1,000 mission units and at least300 admitted host markers, actual production fields and continuous native UI motion. A follow-up diagnosis used two counterbalanced20s on/off pairs per view in the same live UDP scene. Median mean-frame ratios were **1.168 full map** (39.113 vs45.665FPS) and **1.121 cockpit** (34.698 vs38.913FPS), both failing the separately predeclared5% overhead criterion; both p99 comparisons passed their criterion. Live AI evolves, so this is not an identical-input or statistically conclusive baseline. It neither excuses nor replaces the original absolute failures.

Native frame probes around capture-sequence changes used the maximum current/next frame delta to cover unspecified MonoBehaviour execution order. The live run had27 capture events: median16.670ms, maximum42.140ms, one window above33.333ms. Dense full/cockpit probes had five events each, maxima54.566/50.370ms. These include probe overhead and cannot assign all cost to the overlay or align every incremental computation/publication with GPU presentation. A hard2ms whole-frame claim is unsupported.

Whole-process working set in the dense absolute run peaked around2.33–2.36GB, including the game and bridge. This does not isolate plugin memory or certify2048. The new120s live sample does not supersede the earlier602s v1.6.0 soak with87.2% availability and a3.45s gap.

## Gate interpretation and remaining work

The authoritative bounded result is **Core pass, Native pass, Performance fail, combined Readiness blocked**. See [readiness summary](benchmarks/LATEST_READINESS.json) and [gate commands](VALIDATION_REPORT.md). A gate validates retained evidence and trusted-runner attestations; it does not independently launch/observe Unity. The portable native stage adaptation in `tests/native/v161` was checked with help/compilation only, not itself executed in-game; the original executed harnesses are retained locally with provenance hashes.

Before a general release, profile and reduce steady per-frame admission/binding overhead without weakening immediate revocation; investigate dense moving-map/main-thread work and repeat the failed criteria with stable workload controls; run a longer evolving soak, repeated-capture memory/resource checks at high resolution if2048 is to be promoted, and independent hardware/graphics coverage. Cached terrain transition construction and geometry publication remain indivisible work outside a hard scheduler slice guarantee. Hardware/DPI/VR expansion remains excluded by the earlier task scope. No claim of general release readiness or improved whole-game FPS is made.
