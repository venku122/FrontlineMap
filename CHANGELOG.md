# 1.6.1 adversarial review remediation — 2026-10-21

- Validate admissions every frame; defer observation materialization until terrain readiness and capture cadence.
- Preserve local source influence through virtual source/target connections with domain, slope and blocked-corner checks. Cache static connectivity and grid mappings.
- Keep contour geometry through rigid pan/rotation; rebuild for projected scale changes. Add rebuild/time/allocation diagnostics.
- Explain unavailable terrain coverage below50% known samples or sampler failure; radial fallback requires explicit selection.
- Export sanitized cached terrain with schema2 diagnostics and compare incremental layered output against an independent replay oracle.
- Separate Core, Native and Performance gates, bound to current source/DLL/reference hashes. Keep512 recommended and2048 experimental; no universal FPS or general-release claim.

# 1.5.2 — 2026-10-19

- Stage disabled overlay objects and destroy unfinished UI on creation failure.
- Restore the shown texture when native UI dirty callbacks interrupt texture publication; hide the image if restoration fails.
- Add native renderer fault regression fixtures and scoped local acceptance results.

# Changes on frontlinemap/modernization

1.5.0 development candidate: separate supports and deterministic oracle; native-marker observation boundary; generation-safe staged texture publication; bounded stamp/raster chunks and expiry/latest-pending behavior; legible fill and front coverage; movement arrows disabled; all-or-nothing startup configuration and legacy backup migration; configurable game/loader references; tests, replay, diagnostic capture, packaging and guarded rollback tooling. Optional workers/dirty tiles/contours/GPU deferred. See validation report for the exact evidence and unrun gates.
