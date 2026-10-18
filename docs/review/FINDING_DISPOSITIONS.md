# Finding dispositions

Baseline SHA6450ab9dc75503e5fedefc0280182436e5d993ec; old locations below use pinned Plugin.cs. Current implementation commit is recorded in delivery source-manifest.json; current method anchors avoid line drift from formatting. Evidence is source/math/pure harness unless explicitly stated. No runtime claim.

|ID|Disposition|Pinned source location|Current source anchor|Evidence/test|Remaining limit|
|---|---|---|---|---|---|
|A1|partially confirmed|Plugin.cs297-342|Game/GameObservationAdapter.TryRead|ObservationPolicyTests; native contract inspection|Fresh tracked live positions match native <4s; null viewer bypass fixed in source. Actual adapter noninterference blocked.|
|A2|confirmed|Plugin.cs346-374|Core/ScalarField.Compute|EqualOpponentsRetainSupport|Separate double supports preserve opposition; target visuals unrun.|
|A3/P01|confirmed|Plugin.cs426-451|Core/FieldClassifier/Rasterizer.Sample|OneSideNeverProducesOpposingFront|Both supports required; no target screenshot.|
|A4/P03|confirmed|Plugin.cs489-511|Plugin.cs; no arrow implementation|Movement omitted from candidate|Restoration intentionally out of scope.|
|A5|confirmed|Plugin.cs169-218|Runtime/OverlayController; StepScheduler|LifecycleTests; FaultTests|Actual native capture/cleanup/unload not_run.|
|A6/P11|partially confirmed|Plugin.cs186-190,322-342|Game/MapBinding.TryCreate|ProjectionHandlesOriginAndCrop; rectangular fixtures|Airbase/global mismatch confirmed. P11 UV cropping is conditional; installed native uses Image. Calibration blocked.|
|P04|confirmed|Plugin.cs413-423|Core/Rasterizer.Sample|SteepSupportedZeroCrossingNotMagnitudeCulled|No abs(D)>5 cull.|
|P05|confirmed|Plugin.cs432-443|Core/Rasterizer.Sample|SameWorldLinearFrontKeepsScreenWidth; CoarseTexelsStillCoverBetweenSampleContour|Dashes retired. Raster crispness limited at extreme zoom; real DPI acceptance blocked.|
|P06|confirmed|Plugin.cs489-511|Core/FieldClassifier; Rasterizer.Sample|Plateau/saddle/zero fixtures|Shared explicit tie policy; no movement arrows.|
|P07|confirmed|Plugin.cs413-458|Core/Rasterizer.Sample|ByteAlphaSurvivesSingleAirbase|Uniform opacity retains configured legacy low alpha.|
|P08/P09|confirmed|Plugin.cs194-208|Runtime/StepScheduler|LargeStampChecksDeadlineWithinChunks; frame-clock fixtures|Compute chunks bounded by128cells, not total frame budget; target overshoot/cadence blocked.|
|P10|confirmed|statistical exhibit|Diagnostics/OverlayMetrics|Original unchanged probes|Target event-aligned tails and overhead blocked.|
|P12|confirmed payload only|array allocation Plugin.cs113-115|Core/InfluenceBuffers; MODEL_SEMANTICS|Checked geometry/settings; resource arithmetic|Native/GPU memory not measured; double precision increases payload explicitly.|
|C1|confirmed source assumption|Plugin.cs254-287|Game/GameObservationAdapter.TryRead|UnsupportedFactionSetsFailClosed; target DynamicMap.GetFactionMode|No invented alliance semantics; >2HQ rejected; real neutral/reordering cases blocked.|
|C2|confirmed|Plugin.cs config reads across phases|Core/ModelSettings; Plugin.Awake|ConfigurationTests|Immutable restart-only config avoids partial hot reload.|
|C3|confirmed/conditional|Plugin.cs13,64-74,121-125|Plugin; ConfigMigration; renderer ownership|ConfigMigrationTests; fixture rollback; fake lifecycle cycles|Version generated from csproj. Unity end-of-frame resource proof blocked.|
