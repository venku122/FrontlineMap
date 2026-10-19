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
