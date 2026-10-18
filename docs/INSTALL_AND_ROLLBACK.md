# Installation and rollback
No installed game files, loader, Steam branch or normal mod profile were modified. Private copies of owned assemblies were used only for inspection/building and are excluded from source/release packages.

The development DLL must be tested in an explicitly authorized disposable profile before any normal installation. Preserve existing loaders/mods. Do not place old/new FrontlineMap DLLs together. The scripts refuse detected duplicate Frontline-named DLLs and never remove arbitrary mods. Inspect unusual renamed duplicates manually.

1. Build using scripts/build.ps1 or the documented direct dotnet command. Inspect artifacts/build/binary.json (when built with the script) and artifact fingerprints.
2. Package with scripts/package.ps1 -OutputDir <empty-folder>. Package is development_only, not a production certification.
3. Preview: scripts/deploy.ps1 -ProfileDir <disposable-profile> -PackageDir <package> -ExpectAbsent -WhatIf. This must make no filesystem changes.
4. Only after that specific profile is authorized: use -AuthorizedDisposableProfile. It backs up FrontlineMap.dll and both old/current cfg states/hashes to FrontlineMap-rollback-<id>/rollback.json and copies only the candidate DLL. It does not install/replace the loader.
5. Restore: scripts/rollback.ps1 -ProfileDir <same-profile> -Manifest <rollback.json> -AuthorizedDisposableProfile. Backups are hash-checked before writes; a DLL changed since deployment is rejected. Existing configs are restored, candidate-generated configs absent before deploy are removed; unrelated files remain. Preserve the backup until restoration is verified.

The fixture harness scripts/test-tooling.py executes previews, authorization guard, package fingerprints, deploy and original-hash restoration in artifacts/tooling-tests. It does not deploy to a game or validate game persistence. Actual profile rollback remains not_run. Standard script execution policy applies; do not disable/bypass it. Tested alternate runtime: portable PowerShell7.4.13 on Linux.

Deployment requires an explicit expected state even in preview: use -ExpectAbsent for a new profile or -ExpectedInstalledSha256 <recorded SHA256> for an existing DLL. Unexpected state/hash fails before backup or overwrite. Packaging consumes build.ps1 attestation (source tree, HEAD, selected reference manifest and DLL hashes); a direct dotnet build alone is insufficient for attested packaging. The generic packager labels its actual target unverified and records the inspected baseline separately; compare selected reference hashes with the discovery manifest.
