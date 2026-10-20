# Layered Frontline Implementation Plan

> **For agentic workers:** Execute inline with superpowers:executing-plans; independent terrain and pure contour components may use dispatching-parallel-agents. One fresh final whole-branch review.

**Goal:** Implement the user-approved layered tactical model with uncertainty, public terrain constraints and vector contours, preserving checkpoint/knowledge safety and verify locally.
**Architecture:** Keep native admission and transactional publication. Select one semantic field; add role/confidence metadata and cached coarse terrain. Marching-squares segments render atomically with fill through native UI transforms.
**Tech Stack:** C# net472 Unity2022.3/BepInEx5, pure .NET8 xUnit, existing guarded native bridge.
**Spec:** docs/superpowers/specs/2026-10-19-layered-frontline-design.md

## Global Constraints
Checkpoint df71cd2; no normal Steam writes/push/public connections/proprietary redistribution. Preserve native-marker-only observations and immediate revocation. Hardware item5 excluded. Legacy mode must retain exact old numerical behavior. New radii/weights heuristic; do not claim radar LOS/navmesh certainty.

## Review Focus
Unknown/invalid terrain and floating origin fail conservatively; stale timestamps cannot gain confidence; air/naval selection cannot contaminate ground; vector stages cannot leak on publication failure; high resolutions and dense markers remain bounded/cancellable.

### Task 1: Terrain module and static sampler
Files: src/Core/TerrainCostGrid.cs, src/Game/TerrainSampler.cs, tests/FrontlineMap.Core.Tests/TerrainTests.cs.
Produces immutable grid Geometry, Heights, Known, Land/Water availability, CellIndex(x,z), and IEnumerable<int> ComputeDistances(x,z,radius,bool water,double[] output) yielding chunks<=128. TerrainSampler.GetOrBegin(MapGeometry,int) + Step(deadline) + Grid/Ready/Status/Clear; sample only native static map, cache, global coordinates correct. Core tests first failing, then land/water/slope/diagonal/no-data tests passing. No Plugin or project-reference edits by worker; coordinator integrates PhysicsModule.

### Task 2: Pure contour extraction
Files: src/Core/FrontContours.cs, tests/FrontlineMap.Core.Tests/ContourTests.cs.
Produces ContourPoint(U,V), ContourSegment(A,B,Confidence), FrontContours.Extract(InfluenceBuffers,double threshold,double epsilon,double[] confidence=null,double minimumConfidence=0). Deterministic marching squares, supported endpoints, zero-plateau/saddle tests, UV coordinates at cell centers. Write failing tests, run RED, implement, whole relevant tests GREEN. No renderer changes by worker.

### Task 3: Layered observation/model and scheduler
Files: ObservationSnapshot/ObservationPolicy/ModelSettings/ScalarField/StepScheduler/GameObservationAdapter plus new TacticalModel.cs and LayeredModelTests.cs. Add backward-compatible metadata/settings constructors. Test aircraft/ship exclusion from ground and inclusion in correct mode, role radii, old unknown handling, stale confidence monotonicity, contested bands; RED then implement. Preserve exact Legacy oracle/scheduler/replay. Incrementally integrate coarse path costs and confidence arrays. Compare oracle independently for new fixtures; config validation all-or-nothing.

### Task 4: Native presentation and configuration
Files: Plugin.cs, RasterOverlayRenderer.cs, new FrontContourGraphic.cs, FieldClassifier.cs, FieldResult, config/appearance tests. DefaultGroundControl; five clear layer buttons and metadata/status. Atomic segments+texture with failure rollback; screen-pixel vector width, no raycast interception. Add PhysicsModule source/build references. Test classification/uncertainty and presentation protocols before code; compile exact target. Version1.6.0, documentation/new model schema.

### Task 5: Native verification, review and delivery
Extend disposable native helper with role/terrain/segment counters and layer selection. Build/deploy only after offline tests pass; guard owned processes, checkpoint rollback verified. Check both maps/alllayers, actual vector UI, native knowledge/faults/config/UDP/lifecycle. Record stage timing/startup/memory and finite soak, screenshots. Independent fresh whole-branch review; fix important findings with RED/GREEN; clean committed source, attested exact DLL, source+patch+evidence, archive CRC+allhashes. Normal178DLL/config fingerprint unchanged. Leave useful offline game state. Report actual scopes and limitations.
