# 1.5.1 runtime continuation

Fixes the observed0.34.1 UI/AdditiveMasked material rejection while retaining full-UV, identity-transform and native-mask checks. Hidden maps and inactive marker hierarchies fail closed. Cleared RawImage components are disabled before texture release, preventing white fallback rectangles. Overlay objects inherit the native UI layer. The full-map legend uses a compact, backed panel and color-matched labels; cockpit minimaps do not carry the full legend.

Exact reusable admission tracking preserves every-frame and before-publication revocation checks. Expensive observation objects and sanitization are materialized at capture time. Captured inputs remain the authorization cohort: additions and reordering wait for the normal cadence without canceling valid work, while captured removals/owner/weight/freshness/viewer changes invalidate immediately. This fixes publication starvation observed during live map lifecycle tests. Reusable native references are cleared on reset. Diagnostics now distinguish snapshot capture cost from the latest revalidation read.

Tested using a fresh copy of build24724372 and the existing MDK bridge, with guarded expected-hash deployment. Full matrix and measured scope are in RUNTIME_VALIDATION_2026-10-18.md. Remains a development package until all broader runtime/performance gates pass.

# 1.5.0 development candidate
Preserves BepInEx5/net472 and legacy radial weights; fixes source-level support loss, false opposing edges, alpha quantization and coroutine publication structure. Captures existing native map markers instead of global unit/airbase registries; rejects unresolved and jammed contexts. Complete, context/sequence/expiry-checked staged publication; coherent restart configuration; viewport-only raster repaint; opt-in sanitized replay capture and diagnostics. Version generated from project Version property.

This is a development build, not a release recommendation. Target load, multiplayer, information-boundary integration, map calibration, performance/freshness and native resource lifetime acceptance remain blocked. No remote publication occurred.
