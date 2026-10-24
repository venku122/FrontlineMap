# 1.7.0 contact continuity — 2026-10-24

- Admit friendly support from the assigned viewer’s synchronized faction roster and own airbases, independently of cosmetic map-icon filtering.
- Retain bounded copies of previously admitted enemy observations. Preserve the original last-known position and simulation timestamp; never follow hidden movement, ownership or destruction.
- Fade stale support/confidence with the configured half-life (120 simulation seconds by default), then expire history at900 seconds. Capacity is4096 contacts; zero history lifetime disables retention.
- Keep an already displayed same-viewer estimate while its replacement computes, label it “updating known estimate,” and preserve its original expiry. Viewer/session/map/model changes, invalid contexts, jamming and expired history still clear output.
- Preserve Legacy admission/numerics. Add portable history/publication tests and native roster, cosmetic filtering, hidden-world invariance, lifecycle and timed-history evidence.

This is a development candidate. See [exact-binary validation](RUNTIME_VALIDATION_2026-10-24_V170.md) for measured acceptance and remaining limits. Normal-game installation remains unmodified.

# 1.6.1 adversarial review remediation — 2026-10-21

- Validate admissions every frame; defer observation materialization until terrain readiness and capture cadence.
- Preserve local source influence through virtual source/target connections with domain, slope and blocked-corner checks. Cache static connectivity and grid mappings.
- Keep contour geometry through rigid pan/rotation; rebuild for projected scale changes. Add rebuild/time/allocation diagnostics.
- Explain unavailable terrain coverage below50% known samples or sampler failure; radial fallback requires explicit selection.
- Export sanitized cached terrain with schema2 diagnostics and compare incremental layered output against an independent replay oracle.
- Separate Core, Native and Performance gates, bound to current source/DLL/reference hashes. Keep512 recommended and2048 experimental; no universal FPS or general-release claim.

# 1.6.0

- Separate ground, air threat, naval and confidence views; retain Legacy compatibility.
- Public-marker role metadata, per-role radii and reduced building ground support.
- Stale confidence decay and contested band; precise gold requires enough confidence.
- Incremental cached public terrain sampling and bounded land/water path costs.
- Supported marching-squares vector front with masked3px geometry and bounded segment count.
- Layer controls, explicit terrain/heuristic legend, schema2 diagnostic exports and Legacy replay guard.
- Checkpoint1.5.3 preserved before implementation. See RUNTIME_VALIDATION_2026-10-19_V160.md for actual candidate validation and limitations.

# 1.5.3 local continuation

Fresh/stale transitions update live uncertainty metadata without revoking still-known captured positions. Actual revocations still cancel immediately. Native tests pin stale known-position holding, freshness continuity and lost-contact removal. The legend distinguishes live freshness, captured age and recomputation.

Replacement CPU work uses cached column distances, conservative circle clipping and cached raster invariants, preserving operation order. A reproduced extreme-coordinate clipping error uses an exact full-grid fallback. Renderer upload scratch is reused; transactional texture publication remains unchanged.

Validated RGB/opacity/line configuration reloads through F10 or configuration-manager value changes. Invalid reloads retain the working model; full-map labels use the selected palette. Hardware testing is explicitly excluded by the user's request. Final native/performance evidence and source/binary identities are provided in the versioned delivery; portable timings are not an FPS claim.

# 1.5.2 renderer recovery — 2026-10-19

Native fault testing reproduced two failures: an exception after parenting an unfinished UI object left a permanent white rectangle, and a dirty-material callback could throw after RawImage stored a staged texture that the controller then released. UI creation now uses disabled local staging and publishes renderer references only after setup succeeds. Failed construction destroys the pending object. Texture binding restores the previously shown texture on failure and disables the image if restoration also fails.

Both native automated regressions failed on1.5.1 and passed after this fix. The existing88compiled tests and9synthetic plus3recorded-native replay comparisons still pass. Read/solver/knowledge/network behavior is unchanged. Broader executed checks and remaining hardware/performance limits are in RUNTIME_VALIDATION_2026-10-19.md. This remains a development candidate.

# 1.5.1 runtime continuation

Fixes the observed0.34.1 UI/AdditiveMasked material rejection while retaining full-UV, identity-transform and native-mask checks. Hidden maps and inactive marker hierarchies fail closed. Cleared RawImage components are disabled before texture release, preventing white fallback rectangles. Overlay objects inherit the native UI layer. The full-map legend uses a compact, backed panel and color-matched labels; cockpit minimaps do not carry the full legend.

Exact reusable admission tracking preserves every-frame and before-publication revocation checks. Expensive observation objects and sanitization are materialized at capture time. Captured inputs remain the authorization cohort: additions and reordering wait for the normal cadence without canceling valid work, while captured removals/owner/weight/freshness/viewer changes invalidate immediately. This fixes publication starvation observed during live map lifecycle tests. Reusable native references are cleared on reset. Diagnostics now distinguish snapshot capture cost from the latest revalidation read.

Tested using a fresh copy of build24724372 and the existing MDK bridge, with guarded expected-hash deployment. Full matrix and measured scope are in RUNTIME_VALIDATION_2026-10-18.md. Remains a development package until all broader runtime/performance gates pass.

# 1.5.0 development candidate
Preserves BepInEx5/net472 and legacy radial weights; fixes source-level support loss, false opposing edges, alpha quantization and coroutine publication structure. Captures existing native map markers instead of global unit/airbase registries; rejects unresolved and jammed contexts. Complete, context/sequence/expiry-checked staged publication; coherent restart configuration; viewport-only raster repaint; opt-in sanitized replay capture and diagnostics. Version generated from project Version property.

This is a development build, not a release recommendation. Target load, multiplayer, information-boundary integration, map calibration, performance/freshness and native resource lifetime acceptance remain blocked. No remote publication occurred.
