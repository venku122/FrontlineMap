# 1.7.0 limitations and release acceptance

See [current exact-binary report](RUNTIME_VALIDATION_2026-10-24_V170.md) and [machine-readable readiness](benchmarks/LATEST_READINESS.json).512 remains recommended;2048 peak memory and broad multiplayer/hardware acceptance remain uncertified. No FPS improvement or general-release readiness is claimed.

- Support weights, ranges and territory are estimates. Team roster information is not a game-owned territory map. Air threat omits weapon-specific range, altitude and LOS; terrain is a coarse public sample, not a navigation mesh.
- Last-known enemies are deliberately not updated from hidden live state, including destruction or allegiance. History fades by simulation age; defaults120-second half-life,900-second expiry,4096-contact FIFO capacity.
- During same-viewer recomputation the old estimate may remain until its original presentation expiry (default15 seconds), explicitly labeled. It can temporarily include recently removed own support. Invalid/new viewer/session contexts, jamming and expired history cannot use that fallback.
- Layer/settings reload clears contact history. Jamming hides the map; recovery can reuse unexpired copied history. Unsupported multi-faction or unresolved/spectator contexts hide it.
- Unknown terrain blocks propagation. Below50% known coverage, ground/naval terrain layers explain unavailability; radial fallback requires explicit user selection. Narrow crossings can be missed.
-8192 contour segments is the vector ceiling; excess uses the contested band. Rigid pan/rotation reuses geometry; projected scale changes rebuild it.
- The2ms scheduler target excludes full observation validation, terrain setup, native upload and UI work. Read the measured uncapped/dense acceptance results before assuming headroom.
- Stale-but-still-tracked visible-marker metadata mutations need additional native coverage; lost-tracking hidden-metadata tests are narrower. See the current report.
- Physical keyboard/mouse/DPI/VR hardware item5 remains excluded by user request. Native RPC/synthetic UI tests do not certify those hardware paths.
- Schema2 layered exports now include sanitized cached terrain and support independent replay. The Legacy runner intentionally rejects layered exports without layered mode.

Earlier limitations below are historical and are superseded by the current report and semantics where they disagree.

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
