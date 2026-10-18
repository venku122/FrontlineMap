# Validation report
Readiness: **development_only**. Source and target-reference builds are usable development artifacts. Production runtime gates are open.

The compiled .NET8 core suite passes64 cases. It exercises model/config/migration, immutable snapshots, policy-value fail-closed cases, scalar invariants, classification/opacity, projection arithmetic, deadline chunks, pending replacement/expiry, fault recovery, context publication races, repeated fake owned-handle cycles, repaint reuse, resolution refinement and sanitized serialization. It excludes Unity/native GameObservationAdapter/Plugin execution. These64 are not64 successful game scenarios.

Nine synthetic replay fixtures match the independent full-grid oracle with zero field/classification/alpha differences. Five production-code mutations were detected by regression assertions. Nine Linux PowerShell filesystem checks passed, including hash-restoring fixture rollback. The unchanged12 Python exhibits reproduce legacy counterexamples and remain separate mathematical evidence.

The pristine legacy source with path-only adaptation and candidate net472 plugin compile against the real installed assemblies. See artifacts/baseline/path-only-build.log, artifacts/tests/candidate-build-final.log and artifacts/discovery/target-manifest.json. Neither build was loaded in Unity. Windows PowerShell script execution was policy-blocked; no security setting was altered or bypassed.

Actual target load, native visibility/noninterference/stale/lost/airbase/host-client policy, all map landmarks/transforms/masks/theme/DPI,20 mission cycles, multiplayer coexistence, profile deployment/rollback, paired performance/freshness trials and instrumentation/native/GPU memory measurements: **blocked/not_run**. `validate.ps1 -Mode Target` deliberately writes required gates and exits2.

No performance comparison or FPS recovery is claimed. Replay timings are .NET tooling-only, not comparable field-only oracle versus field+raster candidate costs. No accepted target budgets exist; docs/benchmarks/ACCEPTANCE.json explains missing baseline and provisional settings. Preserve this distinction when resuming.

Independent reviewer reports source-derived and actual-diff findings with provenance; final report/review dispositions are packaged from artifacts/reviews after examining the final tested source SHA. A development-conditional verdict does not waive runtime gates. Full per-case statuses and evidence are in artifacts/validation/matrix.json (delivery evidence/validation-matrix.json).
