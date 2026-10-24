# Installation and rollback
Development validation through October22 left the normal game profile unchanged. On October23 the user separately authorized installation into their normal NOMM-managed game. The loader compatibility finding below comes from that deployment. Private game/bridge assemblies remain excluded from source/release packages.

The development DLL must be tested in an explicitly authorized disposable profile before any normal installation. Preserve existing loaders/mods. Do not place old/new FrontlineMap DLLs together. The scripts refuse detected duplicate Frontline-named DLLs and never remove arbitrary mods. Inspect unusual renamed duplicates manually.

1. Build using scripts/build.ps1 or the documented direct dotnet command. Inspect artifacts/build/binary.json (when built with the script) and artifact fingerprints.
2. Package with scripts/package.ps1 -OutputDir <empty-folder>. Package is development_only, not a production certification.
3. Preview: scripts/deploy.ps1 -ProfileDir <disposable-profile> -PackageDir <package> -ExpectAbsent -WhatIf. This must make no filesystem changes.
4. Only after that specific profile is authorized: use -AuthorizedDisposableProfile. It backs up FrontlineMap.dll and both old/current cfg states/hashes to FrontlineMap-rollback-<id>/rollback.json and copies only the candidate DLL. It does not install/replace the loader.
5. Restore: scripts/rollback.ps1 -ProfileDir <same-profile> -Manifest <rollback.json> -AuthorizedDisposableProfile. Backups are hash-checked before writes; a DLL changed since deployment is rejected. Existing configs are restored, candidate-generated configs absent before deploy are removed; unrelated files remain. Preserve the backup until restoration is verified.

The fixture harness scripts/test-tooling.py executes previews, authorization guard, package fingerprints, deploy and original-hash restoration in artifacts/tooling-tests. It does not deploy to a game or validate game persistence. Actual profile rollback remains not_run. Standard script execution policy applies; do not disable/bypass it. Tested alternate runtime: portable PowerShell7.4.13 on Linux.

Deployment requires an explicit expected state even in preview: use -ExpectAbsent for a new profile or -ExpectedInstalledSha256 <recorded SHA256> for an existing DLL. Unexpected state/hash fails before backup or overwrite. Packaging consumes build.ps1 attestation (source tree, HEAD, selected reference manifest and DLL hashes); a direct dotnet build alone is insufficient for attested packaging. The generic packager labels its actual target unverified and records the inspected baseline separately; compare selected reference hashes with the discovery manifest.

## Inspected target loader requirement (October23,2026)

On the inspected Nuclear Option Steam build24724372 with BepInEx5.4.23.5, `BepInEx/config/BepInEx.cfg` must use:

```ini
[Chainloader]
HideManagerGameObject = true
```

The existing normal installation had this set to false; the disposable acceptance copies had true. A controlled false/true comparison in the same disposable copy reproduced FrontlineMap logging its Awake startup message without subsequent frame-update evidence when false. With true, its normal LateUpdate immediately logged `Influence hidden: MapUnavailable` in the main menu. The same correction, backed up and applied to the user-authorized normal installation, restored that frame-update log after restart. Another installed mod also created its previously absent Start-bound settings. This is target-specific loader compatibility evidence, not an assertion that all BepInEx games need this setting.

Back up the existing loader config, change only this setting and restart the game; a running process cannot be certified from an initial `Loading [FrontlineMap]` line alone. Validate normal frame updates, configuration menu hotkeys and actual faction/map presentation. `MapUnavailable` is expected in the main menu. The October23 keyboard/menu and map presentation checks require the user because desktop capture was unavailable in that session; frame-update recovery was observed directly in the normal loader log.

NOMM's configured game path on this host is the ordinary Steam Nuclear Option directory, and its recorded launch path is Steam app2168680. Do not assume a different mod-manager profile without inspecting its configuration. F1 is the existing Configuration Manager shortcut; FrontlineMap uses F9 to toggle and F10 to reload. Preserve existing mod files and settings. The normal-profile installation and restart require separate user authorization; the disposable-only deployment script remains guarded.
