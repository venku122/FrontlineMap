# Validation and readiness

The current patched source is **development_only / pending fresh native evidence**. Core, Native, Performance and combined Readiness are separate executable gates. A successful build or Core run does not establish a Unity load, native policy correctness, or performance acceptance.

The October 20, 2026 v1.6.0 run in `artifacts/runtime-2026-10-20-v160-reverify` is historical evidence for DLL SHA256 `68ff5873c284f68984d7634f0c3a418042e0b41da32f257c5cda4462f288098c`. Its `performance-result.json` reports capped paused mean ratio 1.00039, capped live present rate 59.42/s, live p99 21.75 ms, native-read mean 0.984 ms and observed availability 97.30%. These are measurements of that run with diagnostic overhead; they do not certify changed source. [The October 18 report](RUNTIME_VALIDATION_2026-10-18.md) describes the earlier v1.5.1 run, not the current source.

512 remains the recommended grid resolution. 2048 is a stress option and is not performance certified. Uncapped CPU-bound and moving dense multiplayer acceptance remain unverified. Single-host capped samples do not establish support across hardware, graphics settings, resolutions or mission compositions. See [performance acceptance](benchmarks/ACCEPTANCE.json) and [target identity](compatibility/TARGET.md).

## Executable gates

Run from repository root in PowerShell; pass installed Python and dotnet executable paths if they are not on PATH. These commands validate existing evidence and run portable tests; they do not start a game or deploy a DLL.

```powershell
./scripts/validate.ps1 -Mode Core -Python python -Dotnet dotnet
./scripts/validate.ps1 -Mode Native -EvidenceDir C:/private/fresh-run -Python python
./scripts/validate.ps1 -Mode Performance -EvidenceDir C:/private/fresh-run -Python python
./scripts/validate.ps1 -Mode Readiness -EvidenceDir C:/private/fresh-run -Python python -Dotnet dotnet
```

`Target` aliases `Readiness`: it executes Core now and then evaluates both runtime gates. Core runs the Python readiness fixtures, portable .NET tests and synthetic replay comparison. It rejects a source change during execution. Runtime modes require an explicit directory and return 2 when blocked; malformed, missing, stale or incomplete evidence cannot pass. Results go to `artifacts/validation/core.json`, `native-runtime.json`, `performance-runtime.json`, `target-runtime.json` and `readiness.json` as applicable. These files describe their recorded run; invoke the gate again after edits rather than assuming an old result remains current.

The runtime verifier can run independently in WSL:

```bash
python3 scripts/readiness.py --gate native --evidence-dir /private/fresh-run
python3 scripts/readiness.py --gate performance --evidence-dir /private/fresh-run
python3 scripts/readiness.py --gate target --evidence-dir /private/fresh-run
python3 scripts/test-readiness.py
```

The helper's `--gate target` evaluates runtime evidence only; use PowerShell Readiness/Target for the combined Core gate. `--binary` overrides the default `bin/Release/net472/FrontlineMap.dll`; it must match the Release build attestation. `--max-age-hours` defaults to 48 and may only tighten that window. PowerShell exposes it as `-MaxEvidenceAgeHours`.

## Runtime evidence contract

Build the final stable source with `scripts/build.ps1`, run the compiled DLL in the authorized disposable target, and collect scenario reports. The trusted runner must record the actually loaded DLL hash, retain the supporting observations locally, and only set a named check to boolean true after executing its assertions. The gate verifies that attestation and its integrity; it does not independently observe Unity or prove that an operator's handwritten claims are true. Do not manufacture a new run timestamp, relabel old reports or copy old success flags to satisfy this contract.

Place `evidence.json` in the explicitly supplied evidence directory:

```json
{
  "schema": 1,
  "captured_at_utc": "<UTC timestamp from the completed fresh run>",
  "source_tree_sha256": "<artifacts/build/binary.json source_tree_sha256>",
  "binary_sha256": "<SHA256 of the DLL actually loaded>",
  "reference_manifest_sha256": "<SHA256 of artifacts/build/references.json>",
  "gates": {
    "native": [{"path": "native-acceptance.json", "sha256": "<report SHA256>"}],
    "performance": [{"path": "performance-acceptance.json", "sha256": "<report SHA256>"}]
  }
}
```

Each report is a JSON object with `binarySha256`, `success: true`, and a `checks` object. A lone `success` flag is insufficient. Multiple reports may cover the required checks; every supplied report must succeed and every required check present must be exactly boolean true. Report paths must remain inside the evidence directory. Duplicate JSON keys are rejected. Reports can additionally retain test identifiers, target/hardware/settings, measurements and local evidence paths; the verifier emits only hashes and named statuses, never raw positions.

| Gate | Required named check | Evidence obligation before declaring true |
| --- | --- | --- |
| Native | `policy` | Actual admitted/native visibility, stale/lost revocation, airbase and host/client policy assertions |
| Native | `renderer_faults` | Native partial creation and dirty-material failure injection and recovery assertions |
| Native | `map_geometry` | Native full/minimap landmarks, zoom, masks and DPI checks; include corrected contour projection |
| Native | `lifecycle` | Twenty mission/team/map lifecycle cycles with owned-resource and recovery checks |
| Native | `layered_contract` | Patched terrain fallback, roles/layers, admitted snapshot/export and scalar/vector contracts |
| Native | `multiplayer_coexistence` | Authorized host/client connection and other installed-plugin coexistence checks |
| Performance | `paired_capped` | Repeated paired on/off capped trials, retained samples and declared comparison criteria |
| Performance | `live_capped` | Capped live frame-time, native-read, freshness/availability and memory measurements |
| Performance | `uncapped_cpu_bound` | Actual uncapped CPU-bound paired measurements and declared acceptance criteria |
| Performance | `moving_dense_multiplayer` | Actual moving dense multiplayer measurements, freshness and declared acceptance criteria |

Reports must document the criteria used; the verifier checks coverage and recorded boolean outcomes, not numerical threshold arithmetic. Preserve unsuccessful or unrun scenarios as false/missing; a partial report may be recorded but cannot make the full gate pass. The existing capped v1.6.0 sample does not fulfill the final two performance obligations.

The helper recomputes the source fingerprint using the same file set and algorithm as `source-state.ps1`, hashes the candidate DLL and reference manifest, checks all three against the build attestation and run manifest, verifies each report hash and loaded DLL identity, and rejects timestamps outside the freshness window or in the future. A same-version replacement DLL or modified source therefore requires a fresh matching build and runtime run.
