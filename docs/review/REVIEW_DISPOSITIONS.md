# Independent review dispositions
Source-first review provenance: /root/independent_review, separate context, model not_exposed. First pass read only pinned source before prior review. Focused implementation report hashes the reviewed interim files. Final report covers actual final tested SHA separately in artifacts/reviews; no final verdict is invented here.

|Finding|Disposition|Evidence|
|---|---|---|
|FI1 high routine native refresh reset|accepted/fixed|Removed DynamicMap.onMapChanged subscription; source UpdateMap invokes it at0.1s cadence. True identity/admission checks remain every frame; RoutineRefreshKeepsAuthorizedCompleteOutput verifies core context refresh. Actual plugin loop still requires runtime.|
|FI2 medium expired work monopolizes scheduler|accepted/fixed|StepScheduler.DiscardExpired evicts active/pending/completed expired work; ExpiredActiveReleasesBudgetForFreshPending passes.|
|FI3 medium contour falls between coarse samples|accepted/fixed with visual limit|Conservative texel coverage, 512-grid/3600px counterexample test passes. Arbitrary-zoom crispness not claimed; target quality blocked.|
|FI4 medium tests exclude actual native adapter/wiring|unresolved release blocker|Pure DTO policy checks explicitly scoped; installed source contract read and actual game references compile, but no fake is represented as native adapter noninterference. Requires authorized target tests.|

Legacy report claim refinements: fresh tracking true positions are native-permitted <4s; generic hidden death/type assumptions do not override actual native marker policy; native airbases use own HQ.GetAirbases; jamming distortion requires conservative hiding; RawImage UV counterexample is conditional because this target map is Image. See full claim-by-claim report.

Final review tooling findings accepted/fixed: explicit expected-absent/existing-hash deployment contract; exact build attestation consumed by package; package target label now unverified unless actual evidence is consulted.14 filesystem cases exercise unexpected existing DLL, wrong prior hash, stale same-version binary and changed source. Final independent report and exact reviewed SHA are preserved outside tracked source under artifacts/reviews so reports cannot make their own reviewed SHA circular.
