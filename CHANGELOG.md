# 1.5.2 — 2026-10-19

- Stage disabled overlay objects and destroy unfinished UI on creation failure.
- Restore the shown texture when native UI dirty callbacks interrupt texture publication; hide the image if restoration fails.
- Add native renderer fault regression fixtures and scoped local acceptance results.

# Changes on frontlinemap/modernization

1.5.0 development candidate: separate supports and deterministic oracle; native-marker observation boundary; generation-safe staged texture publication; bounded stamp/raster chunks and expiry/latest-pending behavior; legible fill and front coverage; movement arrows disabled; all-or-nothing startup configuration and legacy backup migration; configurable game/loader references; tests, replay, diagnostic capture, packaging and guarded rollback tooling. Optional workers/dirty tiles/contours/GPU deferred. See validation report for the exact evidence and unrun gates.
