# 1.6.0 limitations

- Role radii, support weights and confidence are heuristics, not actual combat or ownership probabilities.
- Coarse public terrain sampling can omit narrow crossings; unknown cells block influence. Surface ships ignore seabed slope; draft, currents and game navmesh are not modeled.
- Air threat has no weapon-specific range, altitude or LOS. Air-defense identification uses native public unit names(SAM/SPAAG).
- Selected-layer computation saves memory relative to simultaneous four-layer fields but layered jobs add raw/confidence buffers and terrain path work versus Legacy. No unconditional FPS gain is claimed.
- Native knowledge revocation remains immediate and can briefly hide output during recomputation. Stale confidence updates at capture cadence.
- More than8192 contour segments uses the contested band only. Schema2 exports omit terrain and cannot be replayed by the Legacy tool.
- Physical keyboard/mouse/DPI/VR hardware item5 remains excluded from testing by user request.

## Historical limitations

# Runtime continuation scope

Version1.5.1 fixes the native map material rejection and has actual isolated-game evidence. See RUNTIME_VALIDATION_2026-10-18.md for exact executed cases, measured samples and unexecuted gates. Earlier source-only limitations below are historical unless superseded there. Multiclient networking, all maps/themes/DPI/hardware input and complete randomized stock/loader/legacy/candidate performance acceptance remain unverified.

# Known limitations and release blockers
Readiness is development_only. Successful compilation against real references is not a game load or visibility certification.

- Native adapter and Plugin are compiled against actual game assemblies but not exercised by the .NET pure-core tests. Policy-value tests do not prove actual hidden-world noninterference. The earlier duplicated-native-value test is only a boundary-contract check. Host/client stale/lost/death/ownership/type and marker filtering need real target evidence.
- Spectator/editor/unresolved/null HQ, jamming and more than two non-null HQs intentionally hide output. This candidate supports assigned viewers in exactly two-sided missions only. Native strategic airbase contribution follows actual permitted own icons, not the global registry.
- Source-derived map binding remains uncalibrated on actual map surfaces. Packed/cropped/custom-material/aspect/fill mappings reject. Fixed colors, alpha and line sampling need real visual acceptance. Existing native marker positions may lag their tracker due to the game's batched icon refresh.
- Every-frame admission scanning/copying, allocation/zeroing, native texture construction/upload and UI work are not bounded by the128-cell compute chunks. Diagnostics show them separately, but there are no actual target measurements or accepted budgets. No20FPS recovery claim.
- All settings require restart. Legacy SpreadFrames/UseFixedColors, intensity dashes and movement indicators are retired. Memory can be substantial at2048; allocations are not optimized away.
- Fake publication faults/cycles cover owned handle requests, not Unity end-of-frame destruction. Capture/native cleanup faults, actual mission lifecycle cycles and native memory counts remain unrun.
- Windows PowerShell scripts were blocked by local policy and were not bypassed. Portable Linux PowerShell tooling tests passed; that is not Windows script certification.
- No target game launch, screenshots, multiplayer, other-map-mod coexistence, or real game profile deploy/rollback occurred. Disposable filesystem rollback is tested separately.

Resume with an explicitly authorized disposable game/mod profile, target scenario control, native policy/alignment tests and measured baseline. Production release requires final independent re-review plus all runtime gates, even if source issues are fixed.

Terrain connectivity cache construction is currently indivisible at sampling completion. The approximate2ms ray/scheduler slice does not bound this allocation/setup step, Unity uploads, or individual physics queries.
