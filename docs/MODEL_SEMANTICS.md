# Model semantics — 1.6.0

Selected-layer field: each admitted native-marker observation has an observable role. Ground uses vehicles/air defense, airbases and reduced building support. AirThreat uses aircraft and publicly named SAM/SPAAG vehicles. NavalInfluence uses ships. Unknown roles contribute only to Legacy. Configured radii are heuristic. Capture admission, viewer-relative allegiance, native stale positions and immediate removal/context revocation retain1.5.3 rules.

Each contribution is `weight * max(0, 1 - distance² / radius²) * confidence`. Fresh confidence is1. Last-known confidence is `2^(-max(0, simulationTime-observedTime)/halfLife)`; default halfLife120s. Position is never extrapolated. Local confidence is scaled total support divided by raw total support; it expresses age of contributing observations, not a calibrated probability of ownership. Map snapshot expiry still uses a monotonic real clock.

Ground/naval distance is a bounded8-neighbor Dijkstra on public static terrain samples, default128². Ground requires positive sea-relative elevation and slope<=35degrees. Surface naval paths require water and ignore seabed slope. Unknown cells and diagonal corner cutting are blocked. The fine-grid sample adds its distance from the reached coarse cell center. Coarse sampling can miss narrow roads/channels; it is deliberately conservative and is not the game's navmesh. AirThreat bypasses terrain and does not model LOS.

Both sides must exceed support threshold for a contested purple band; near balance means `abs(F-E)/(F+E)<=0.2`. Marching squares extracts supported ground equal-influence contours with deterministic saddle handling. Every interpolated endpoint must meet both support and minimum confidence(default0.5). No line is drawn at unsupported edges or on flatzero plateaus. Gold is separate masked, non-raycasting Unity UI geometry. Dense geometry above8192 segments shows the band only, with a visible legend notice. Fill alpha scales with confidence (minimum15% of configured opacity); Confidence layer colors green/amber at configured opacity.

Legacy retains the original combined radial field and raster front, exact scalar/replay oracle behavior. Schema1 exports are unchanged. Schema2 exports contain permitted observations, roles and settings, excluding terrain arrays; Legacy replay rejects schema2. Raw local snapshots may contain gameplay positions and are not included in public deliveries.

## Historical model (1.5.3)

# Correctness baseline model
Product: estimated influence from player-visible observations. Not authoritative control, safety, weapon range or combat intensity.

At each cell center x=minX+(column+0.5)*width/columns, z=minZ+(row+0.5)*height/rows, use K=max(0,1-distanceSquared/radiusSquared). Sum nonnegative friendly and enemy support independently in double arrays, index=z*columns+x. Stable ordinal admitted ID order ensures deterministic accumulation. Default heuristic weights remain10/5/3/0.5/4. Model revision1; no terrain blocking or new naval/air models.

Bounds derive from inspected native Image rect/pivot divided by mapDisplayFactor; no100km fallback. Double computations retain the game's supplied float precision but cannot restore precision already lost there. Off-map footprints clip; rectangular cell dimensions remain separate. Maximum100000 admitted stamps, weights<=1e6, radius<=1e9, finite validated coordinates/settings. Allocation dimensions use checked multiplication.

Support threshold0.01 is compatibility/presentation scale (legacy negligible influence cutoff), separate from opacity. Epsilon1e-9 is the explicit numerical tie/gradient guard in modeled support units. Below threshold on both sides means unsupported. Meaningful equal support is balanced purple, not empty. Flat balanced plateaus have no arbitrary boundary. Both sides must meet threshold before gold front coverage can exist; one-sided support edges cannot make gold/red opposing boundaries. At exact zero with nonzero gradient, the front is eligible; zero plateau gradient produces balanced fill. Saddles follow this same local gradient policy; no contour topology claims.

The raster estimates distance to local D=F-E zero using physical gradients converted into screen pixels. No abs(D)>5 shortcut. Width defaults3 screen pixels. Conservative texel coverage avoids dropping an inter-texel contour at high zoom; bilinear raster cannot preserve crisp3px geometry at arbitrarily coarse sampling. There are no density-dependent dashes or conflict-intensity claims. Fill opacity is uniform above support eligibility and rounded directly to an alpha byte, independent of total arbitrary weight. Default .16 gives byte41 for ordinary isolated airbase support. Legacy user alpha .012 remains byte3 rather than being silently overridden.

Movement arrows are omitted. Native last-known contact timestamps/stale flags are preserved and the legend labels stale input plus computation age. Unsupported observations remain unsupported; unknown does not mean empty. Snapshot captured_at uses a monotonic clock; observed_at and simulation_time use the native simulation clock. Pausing does not freeze expiration/cadence.

Reference: independent full-grid summation. Candidate: same sums/order with clipped footprints and128-cell stamp/raster chunks. Repaint reuses the published field and original capture/expiry. Predeclared tolerances tests/fixtures/tolerances.json: absolute+relative1e-9; classifications/alpha exact in replay. No worker, dirty tiles, vector framework or GPU solver introduced.

Memory is deliberately explicit: two double fields16*N² bytes plus Pixel struct8*N². At2048, one field/result payload is96MiB; simultaneous shown+active results can be192MiB, plus staged Color32, native texture/GPU copies, object overhead and snapshots. Native allocation/upload and managed allocation/zeroing are indivisible. These solver/result allocations remain in1.5.1; admission-read allocation/cost reductions are measured separately in the runtime report. Full target acceptance still requires measured memory/frame budgets.

## Freshness and authorization in1.5.3
A fresh/stale flag describes uncertainty, not revocation of an already known position. Captured fields remain time-bounded snapshots; reacquisition does not make their older positions current. The live last-known count updates separately from captured stale flags. Native marker removal, ownership/weight/type or viewer/map changes still revoke captured input immediately. No position is read from a hidden unit's Transform.
