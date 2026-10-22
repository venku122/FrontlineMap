# 1.6.0 in-game re-verification — October 20, 2026

Verified implementation: `0546513f644d409cd602c2e9b53f9930287288fb`.
Runtime DLL SHA256: `68ff5873c284f68984d7634f0c3a418042e0b41da32f257c5cda4462f288098c`.
No product source or DLL changes were required by this verification.

## Completed checks

- All 164 Release core tests passed, and passed again before publication on October 21.
- All five views on both native maps, admitted role contracts, confidence decay, contour width under zoom/rotation/resolution changes, hidden-observation invariance, admitted ownership/death updates, click-through, invalid settings and close/reopen recovery passed.
- Native failure injection, 1024/2048 stress output and active-work cancellation passed; defaults were restored.
- Two-process loopback UDP with opposing viewers and reconnect passed.
- Actual production schema2 export passed in a fresh process. An earlier timeout was caused by the intentional 100-export limit in the old process.
- All 178 baseline normal-install DLL/config fingerprints remained unchanged. Testing used disposable copies.

## Performance

Windowed 1280×800, 512 field, 128 terrain, five-second updates, two-millisecond provisional compute slice, inherited graphics. Presentation pacing was approximately 60 FPS.

Three balanced 30-second paused on/off pairs with the same mission, camera and full map gave median 59.99 FPS on versus 60.01 off, and median p99 frame times 19.72ms on versus 18.94ms off. Whole-process CPU was approximately 5.9% higher with the overlay (about 0.084 of one CPU core). The presentation cap can hide reduced headroom; this does not demonstrate an FPS improvement.

A separate 180-second live GroundControl capture gave 59.42 FPS, 21.75ms p99 frame time, and no sampled frames above 50ms. Instrumented native observation reads averaged 0.984ms, p99 1.499ms. This evolving live phase has no equal-input off baseline. Overlay availability was 97.3%, with a longest knowledge-revocation recompute gap of 1.28 seconds. All predeclared checks for this run passed. This shorter run does not supersede the earlier 602-second soak's 87.2% availability and 3.45-second maximum gap.

## Remaining limits

Sparse public collision coverage on Terrain_naval remains conservative and can produce blank terrain-constrained output. Brief redraw gaps remain possible. Large allocations/uploads do not obey a hard two-millisecond bound. No universal or uncapped performance guarantee is made.

Additional desktop mouse-click verification could not initialize: Computer Use rejected the session workspace path with `codex/sandbox-state-meta: sandboxCwd is not a local file URI`, including after retry/reset. Native layer selection/rendering and raycast click-through passed; actual desktop button clicks remain unverified. Physical input/DPI/VR item 5 remains excluded.

The local verification archive contains raw frame captures, test outputs, screenshots, harness scripts and a SHA256 manifest. It is a verification kit, not a new runtime release. Proprietary game binaries and the automation bridge are not included in this repository.
