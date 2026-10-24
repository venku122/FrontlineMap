# FrontlineMap 1.7.0 development candidate
Estimated influence from authorized Nuclear Option team information and copied last-known enemy observations. Blue friendly support, red hostile support, purple balanced support, gold opposing-support boundaries. Unsupported space means no admitted modeled support, not safety or empty territory. Influence weights are heuristic, not weapon coverage or ownership.

This branch builds against inspected Steam build 24724372, BepInEx5/net472. It has been loaded and exercised in an authorized isolated game copy; **performance acceptance is blocked** by uncapped and dense multiplayer measurements. Read [current validation](docs/RUNTIME_VALIDATION_2026-10-24_V170.md), docs/VALIDATION_REPORT.md and docs/KNOWN_LIMITATIONS.md before testing. No FPS improvement is claimed.

## Contact continuity (1.7.0)
Friendly coverage uses the viewer’s synchronized faction roster and own airbases, including distant support and icons hidden by cosmetic filters. It estimates influence around those assets; the game does not expose authoritative territory ownership here.

Previously admitted enemy contacts retain their last authorized position. Confidence and intensity decay with original simulation age (120-second half-life,900-second maximum by default); hidden movement, allegiance and death do not refresh history. The full-map legend identifies historical contacts. While replacement work runs, an existing same-viewer estimate may remain labeled “updating known estimate” until its original expiry. Invalid contexts and viewer/session/model changes clear output; jamming hides output and stops acquisition. See [semantics](docs/MODEL_SEMANTICS.md).

## Layered model (1.6.0)
Ground control is the default. Use the five buttons along the bottom of the full map or `Model.Layer` to select GroundControl, AirThreat, NavalInfluence, Confidence or Legacy. Aircraft and ships cannot claim ground. Buildings supply limited local support. Ground and naval paths follow a cached coarse public terrain grid; air influence stays radial. Unknown terrain blocks propagation. Stale native observations lose half their influence/confidence every120 simulation seconds by default. Blank space means insufficient admitted support.

Purple marks near-balanced opposing support. Gold is a vector contour between supported ground fields with sufficient confidence; low-confidence data may show a subdued band without a precise line. Confidence colors green to amber indicate freshness, not combat strength. Native map icons remain above the overlay. Role radii, weights, slope and confidence are explicit estimates; this is not actual weapon range, radar LOS, capture ownership or a game navigation mesh.

Ground vehicles default6000m, airbases8000m, buildings2000m with0.15 control multiplier; aircraft20000m, identified SAM/SPAAG12000m, ships16000m. `Model.TerrainResolution`128 (32–256), `Confidence.StaleHalfLifeSeconds`120, contested ratio0.2, minimum front confidence0.5. Config descriptions are authoritative for exact key names. Layer changes/reloads validate before replacement. `TerrainEnabled=false` explicitly selects radial distances. Legacy preserves the1.5.3 numerical model and schema1 replay. Layered schema2 exports include roles/settings and cached public terrain samples when available. Replay them with `--layered true`; without that option the Legacy runner rejects schema2. Terrain-enabled ground/naval replay requires embedded terrain or an explicit `--terrain <file>` fixture.

See [model semantics](docs/MODEL_SEMANTICS.md) and [1.6.0 local validation](docs/RUNTIME_VALIDATION_2026-10-19_V160.md). Previous1.5.3 measurements below are historical evidence, not1.6.0 performance claims.

Follow-up playtest feedback and deferred work: [October23 to-dos](docs/FOLLOW_UP_TODOS.md).

## Changes
Version 1.6.1 repairs terrain initialization capture allocation and off-center source influence, caches terrain connectivity, reuses contour meshes through rigid map movement, and adds explicit sparse-terrain fallback plus independent layered replay and hash-bound readiness gates. 512 remains recommended; high-resolution and broader multiplayer acceptance remain open.

Version 1.5.3 separates live contact freshness from spatial authorization: still-known captured positions remain valid across fresh/stale transitions, while removal, owner/weight/type, viewer and map changes continue to revoke immediately. The full-map legend reports live last-known contact count separately from snapshot age and labels recomputation. Captures still use native marker positions; stale inputs never refresh from hidden live transforms.

Replacement jobs reuse per-job distance scratch, clip conservative circular footprints and cache raster invariants. Extreme coordinate/cell ratios fall back to full-grid predicates to preserve oracle correctness. Upload color scratch is reused until disposal. Model classifications and default RGBA are preserved by independent reference tests.

Four configurable RGB colors, fill opacity and line width support presentation tuning. Edit the config then press F10 to reload; configuration-manager changes also apply on the next game update. All settings are validated before replacement. Invalid live changes preserve the working model and display a concise warning. F10 is reserved for reload; physical-key behavior is outside the current native fixture evidence.

Version1.5.2 stages disabled UI objects until setup completes and restores the previous texture if a native UI dirty callback throws during publication. Native fault regressions and current acceptance results are described in docs/RUNTIME_VALIDATION_2026-10-19.md and tests/native/README.md.

Separate friendly/enemy supports; preserve native stale marker positions; reject unresolved/spectator/multisided/jammed contexts; invalidate old-team output; stage complete textures before publication; bounded 128-cell compute chunks; screen-pixel front coverage with repaint on viewport changes; readable opacity; movement arrows removed. Native UI icons and clicks remain above the non-raycasting overlay. Version1.5.1 supports the observed UI/AdditiveMasked map material and native rectangular clipping, clears disabled presentations, and shows a compact colored legend only on the full map. Every-frame admission checks remain; snapshot allocation is deferred to capture cadence. Newly admitted contacts do not cancel still-authorized inputs; captured contact revocations invalidate immediately.

## Build and test
Install an appropriate .NET SDK and use your own game's managed assemblies. No proprietary assemblies are included.

```powershell
.\scripts\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Nuclear Option'
dotnet test .\tests\FrontlineMap.Core.Tests -c Release
dotnet run --project .\tools\FrontlineMap.Replay -c Release -- --fixtures .\tests\fixtures --compare oracle,candidate --output .\artifacts\replay
dotnet run --project .\tools\FrontlineMap.Replay -c Release -- --fixtures .\tests\fixtures\layered --compare oracle,candidate --layered true --output .\artifacts\replay-layered
.\scripts\package.ps1 -OutputDir .\artifacts\release
```

Direct build: `dotnet build FrontlineMap.csproj -c Release -p:GameDir=<your installation>`; separate profile loader supports `-p:LoaderDir=<profile>/BepInEx/core`; copied private reference inventory supports `-p:ManagedDir=<managed directory>`. All references are non-copying. The only runtime plugin artifact is `bin/Release/net472/FrontlineMap.dll`.

Windows PowerShell on the inspected host disallowed scripts under its execution policy. Do not bypass that policy. Direct dotnet commands work in an existing SDK shell; the scripts were tested separately under portable PowerShell7.4.13 on Linux. SDK8.0.426 was installed locally under /tmp for this development session, not globally.

## Configuration and diagnostic capture
Current config: `BepInEx/config/com.noms.frontlinemap.cfg`. If absent, legacy `com.yuulf.frontlinemap.cfg` is copied with a timestamped backup; existing current config wins. F10 or configuration-manager changes reload settings atomically; invalid initial settings disable the candidate coherently. F9 toggles and clears output. Duplicate legacy GUID disables this candidate; no binaries are deleted.

| Setting | Default | Meaning |
|---|---:|---|
| GridResolution |512|64–2048, quadratic memory/cost|
| InfluenceRadius |12000|Global meters, legacy radial kernel|
| UpdateInterval |5|Monotonic seconds between capture starts|
| MaximumAgeSeconds |15|Expiry including computation; >= interval|
| TerritoryAlpha |0.16|Uniform support-fill opacity; old configured opacity retained|
| LineWidthPixels |3|Screen-space coverage target, raster sampling limits apply|
| BudgetMilliseconds |2|Provisional slice setting, not a measured total-frame guarantee|

Weights preserved: airbase10, ship5, vehicle3, aircraft0.5, recognized building4; dismounted pilots/unknown types excluded. SpreadFrames, UseFixedColors, intensity dashes and advance arrows are retired. Colors are viewer-relative and fixed. Existing airbases follow native marker availability, so only permitted own airbase icons contribute on the inspected target; this can differ between map modes.

Diagnostics.Enabled logs FLM_CAPTURE with the snapshot capture read duration, and FLM_METRIC with separately named latest_read_ms plus allocate/stamp/raster/copy/native-upload timing, age, rejection/expiry/pending and owned resource counters. Diagnostics.ExportSnapshots saves at most100 sanitized snapshots in BepInEx/FrontlineMap-replay/<run>/; these contain permitted gameplay locations and remain local. Replay schema1 Legacy exports with the same CLI; schema2 layered exports use `--layered true` and the same `--fixtures` argument (a directory or a single exported snapshot). The independent tooling oracle compares all five support/confidence arrays and exact classified RGBA against the incremental production scheduler at absolute+relative1e-9 tolerance. Reports hash snapshot, terrain and RGBA; this is tool parity evidence, not Unity runtime performance or acquisition evidence. Initial native read-cost measurements improved, but randomized full performance acceptance and instrumentation overhead remain open. Treat /tmp tools as session-local; recreate them after reboot.

## Installation and rollback
Use only an explicitly authorized disposable profile. See docs/INSTALL_AND_ROLLBACK.md. No normal profile was changed during development. The project does not modify game simulation or networking. Original MIT license retained.

Deployment requires an explicit expected state even in preview: use -ExpectAbsent for a new profile or -ExpectedInstalledSha256 <recorded SHA256> for an existing DLL. Unexpected state/hash fails before backup or overwrite. Packaging consumes build.ps1 attestation (source tree, HEAD, selected reference manifest and DLL hashes); a direct dotnet build alone is insufficient for attested packaging. The generic packager labels its actual target unverified and records the inspected baseline separately; compare selected reference hashes with the discovery manifest.
