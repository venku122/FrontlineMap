# FrontlineMap 1.5.2 development candidate
Estimated influence from player-visible Nuclear Option map observations. Blue friendly support, red hostile support, purple balanced support, gold opposing-support boundaries. Unsupported space means no admitted modeled support, not safety or empty territory. Influence weights are heuristic, not weapon coverage or ownership.

This branch builds against inspected Steam build 24724372, BepInEx5/net472. It has been loaded and exercised in an authorized isolated game copy; broad production gates remain open. Read docs/VALIDATION_REPORT.md and docs/KNOWN_LIMITATIONS.md before testing. No FPS improvement is claimed.

## Changes
Version1.5.2 stages disabled UI objects until setup completes and restores the previous texture if a native UI dirty callback throws during publication. Native fault regressions and current acceptance results are described in docs/RUNTIME_VALIDATION_2026-10-19.md and tests/native/README.md.

Separate friendly/enemy supports; preserve native stale marker positions; reject unresolved/spectator/multisided/jammed contexts; invalidate old-team output; stage complete textures before publication; bounded 128-cell compute chunks; screen-pixel front coverage with repaint on viewport changes; readable opacity; movement arrows removed. Native UI icons and clicks remain above the non-raycasting overlay. Version1.5.1 supports the observed UI/AdditiveMasked map material and native rectangular clipping, clears disabled presentations, and shows a compact colored legend only on the full map. Every-frame admission checks remain; snapshot allocation is deferred to capture cadence. Newly admitted contacts do not cancel still-authorized inputs; captured contact revocations invalidate immediately.

## Build and test
Install an appropriate .NET SDK and use your own game's managed assemblies. No proprietary assemblies are included.

```powershell
.\scripts\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Nuclear Option'
dotnet test .\tests\FrontlineMap.Core.Tests -c Release
dotnet run --project .\tools\FrontlineMap.Replay -c Release -- --fixtures .\tests\fixtures --compare oracle,candidate --output .\artifacts\replay
.\scripts\package.ps1 -OutputDir .\artifacts\release
```

Direct build: `dotnet build FrontlineMap.csproj -c Release -p:GameDir=<your installation>`; separate profile loader supports `-p:LoaderDir=<profile>/BepInEx/core`; copied private reference inventory supports `-p:ManagedDir=<managed directory>`. All references are non-copying. The only runtime plugin artifact is `bin/Release/net472/FrontlineMap.dll`.

Windows PowerShell on the inspected host disallowed scripts under its execution policy. Do not bypass that policy. Direct dotnet commands work in an existing SDK shell; the scripts were tested separately under portable PowerShell7.4.13 on Linux. SDK8.0.426 was installed locally under /tmp for this development session, not globally.

## Configuration and diagnostic capture
Current config: `BepInEx/config/com.noms.frontlinemap.cfg`. If absent, legacy `com.yuulf.frontlinemap.cfg` is copied with a timestamped backup; existing current config wins. All settings require restart and invalid settings disable the candidate coherently. F9 toggles and clears output. Duplicate legacy GUID disables this candidate; no binaries are deleted.

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

Diagnostics.Enabled logs FLM_CAPTURE with the snapshot capture read duration, and FLM_METRIC with separately named latest_read_ms plus allocate/stamp/raster/copy/native-upload timing, age, rejection/expiry/pending and owned resource counters. Diagnostics.ExportSnapshots saves at most100 sanitized snapshots in BepInEx/FrontlineMap-replay/<run>/; these contain permitted gameplay locations and remain local. Replay them with the same CLI. Initial native read-cost measurements improved, but randomized full performance acceptance and instrumentation overhead remain open. Treat /tmp tools as session-local; recreate them after reboot.

## Installation and rollback
Use only an explicitly authorized disposable profile. See docs/INSTALL_AND_ROLLBACK.md. No normal profile was changed during development. The project does not modify game simulation or networking. Original MIT license retained.

Deployment requires an explicit expected state even in preview: use -ExpectAbsent for a new profile or -ExpectedInstalledSha256 <recorded SHA256> for an existing DLL. Unexpected state/hash fails before backup or overwrite. Packaging consumes build.ps1 attestation (source tree, HEAD, selected reference manifest and DLL hashes); a direct dotnet build alone is insufficient for attested packaging. The generic packager labels its actual target unverified and records the inspected baseline separately; compare selected reference hashes with the discovery manifest.
