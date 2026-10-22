# Native renderer fault regressions

These tests require a disposable Windows Nuclear Option0.34.1/BepInEx5 copy and the existing MDK automation bridge with its0.34 compatibility adapter. They exercise the compiled production renderer in Unity/Mono. They are excluded from the shipped plugin and the portable .NET core test project.

Copy `FrontlineMapRuntimeTests.cs` and `FrontlineMapAcceptanceTests.cs` into an isolated bridge export's Adapters folder. Register authenticated, main-thread, mutating RPC commands `debug.frontlineMapRuntime`, `debug.frontlineMapTest` and `debug.frontlineMapAcceptance`, routed respectively to `FrontlineMapRuntimeTests.State()`, `.Action(params["action"].ToString())`, and `FrontlineMapAcceptanceTests.Run(params as JObject)`. Retain the bridge's disposable-profile/mutation/public-lobby guards. The native faction-selection adapter must invoke the real LeaderboardMenu.JoinFactionCallback. Build against the same game copy and install that test bridge only there.

Run from WSL with Windows PowerShell7 and the MDK's existing `invoke-automation-rpc.ps1` (no execution-policy overrides):

```bash
export FLM_RPC_SCRIPT=/absolute/path/to/invoke-automation-rpc.ps1
export FLM_GAME_ROOT=/absolute/path/to/disposable/game
export FLM_EVIDENCE_ROOT=/absolute/path/to/local/evidence
export FLM_BRIDGE_HOST=172.24.192.1 # match the bridge's configured Windows interface
export FLM_PORT=42680
python3 tests/native/renderer_faults.py --label partial-creation --case partial
python3 tests/native/renderer_faults.py --label dirty-material --case dirty
```

Each command launches that copy, joins Boscali, opens the native map, pauses simulation, injects one fault, records native state/screenshot, checks recovery and shuts down its own game process. An unsafe white image, mismatched shown texture, or failure to recover raises an assertion and nonzero exit. `active()` checks the actual enabled texture as well as controller counters. The observer uses Unity's null comparison for destroyed textures; C# `?.` would skip the broken interval by throwing during observation.

`partial` inserts a throw immediately after Commit calls Transform.SetParent, while construction is incomplete. Version1.5.1 leaves an enabled texture-less image and fails to recover. `dirty` registers a one-shot native dirty-material callback; RawImage stores the new texture before invoking that callback, so a throw must restore the previously shown texture before the controller releases staging. Both were observed failing before the renderer fix.

These cases test native UI construction/publication failures, not a GPU-driver failure or every possible persistent/reentrant third-party callback. Geometry audits in the larger fixture check transforms using current marker coordinates; they are not committed-contact position or pixel oracles. Existing synchronous knowledge fixtures and readonly native sensor monitoring have separately documented scopes.

Version1.5.3 adds native `freshnessContract`, `settingsContract`, `paletteGreen`, `gpuColors` and diagnostic `probeStart/probeStop/probeState` actions. The probe hooks the actual revision setter call inside Complete and records per-frame displayed/hidden states; its instrumentation is not a zero-overhead benchmark. Native GPU color checking uses saturated green to avoid color-space ambiguity. `FrontlineMapNetworkTests.cs` registers as authenticated, main-thread, mutating `debug.frontlineMapNetwork`; it restricts connections to literal loopback and uses the game's built-in LagUdp settings, restoring them after disconnect. Do not expose these fixtures to public lobbies.

## 1.6.0 layered helper
`FrontlineMapLayeredTests.cs` is disposable bridge source, not runtime plugin code. Register authenticated main-thread mutating `debug.frontlineMapLayered`. Actions: state, layer(string), roleContract, config(section/key/value), metrics, vectorFixtureBegin/Check/End. The analytic vector fixture uses the production renderer, is visibly labelled, pauses the production plugin and restores it in caller finally. Check uses native CanvasRenderer mesh without forcing vertices dirty. Actual admitted-stamp role contracts do not read hidden live positions. Native evidence reports sparse Terrain_naval coverage and deliberately empty unsupported layers explicitly.

## Portable 1.6.1 stage harness

`tests/native/v161/run.py` is the canonical, opt-in adaptation of the Python stages actually executed under `artifacts/runtime-2026-10-22-v161`. The original executed artifacts remain separate. Only imports, profile/tool paths, output paths and authorization wrappers were adapted; the canonical adaptation has been checked with `--help` and `py_compile`, **not itself executed in the game**. `v161/provenance.json` records the original source hashes and canonical file hashes. Existing measured results do not certify a later operator's run.

Supply your own legitimately installed Nuclear Option 0.34.1/BepInEx5 game and two independently verified disposable copies for multiplayer stages. After verifying each copy is safe to modify, manually create an empty `.frontlinemap-disposable` file at that copy's root. The harness and launcher require that existing marker and never create it. Do not mark your normal Steam installation or normal mod profile. No package stage downloads dependencies, deploys DLLs or starts a bootstrap installer.

Compile the tracked `FrontlineMapRuntimeTests.cs`, `FrontlineMapAcceptanceTests.cs`, `FrontlineMapLayeredTests.cs` and `FrontlineMapNetworkTests.cs` into your authorized MDK automation bridge, registering the authenticated main-thread RPC names described above. Retain its profile/mutation/public-lobby guards, real faction-selection adapter, normal SDK control/AI/screenshot commands, and 0.34 compatibility adapter. Install that bridge and your locally built `FrontlineMap.dll` into the verified copies yourself. The harness does not ship or install bridge binaries, authentication credentials, game assets or PresentMon. Use the MDK's authenticated RPC client; its authentication configuration remains external.

From WSL, configure explicit paths and the interface matching your bridge configuration:

```bash
export FLM_GAME_ROOT='/absolute/path/to/verified-disposable-host'
export FLM_CLIENT_ROOT='/absolute/path/to/verified-disposable-client' # udp/dense-relative only
export FLM_RPC_SCRIPT='/absolute/path/to/authenticated-sdk/invoke-automation-rpc.ps1'
export FLM_BRIDGE_HOST='<configured Windows bridge interface>'
export FLM_EVIDENCE_ROOT='/absolute/path/to/private/evidence'
export FLM_PRESENTMON='/absolute/path/to/PresentMon.exe' # performance/uncapped/udp/dense-relative
export FLM_DOTNET='/absolute/path/to/dotnet'             # export replay; defaults to installed dotnet
python3 tests/native/v161/run.py --help
```

Windows PowerShell7 must exist at its normal `C:\Program Files\PowerShell\7\pwsh.exe` location, matching the parent `acceptance.py` transport. Build the replay tool in Release before using `export`. The performance stages verify the installed profile DLL hash against `bin/Release/net472/FrontlineMap.dll`. Configure the host/client authenticated bridges on ports42680/42681 for the two-process stages; they use literal loopback game networking only.

Launch the host manually with the guarded `v161/launch.ps1` (pass `-GameRoot` and a private `-LogFile`; `-MenuOnly` is supported), or load its authorized bridge yourself. The entrypoint performs a read-only bridge executable identity check before stage mutations. UDP/dense-relative launch the marked client through that same guarded script; start with the client closed. Both profile markers are required before either multiplayer stage can start. The separate guarded `benchmark-process.ps1` also checks executable identity and the marker before inspection/capture/stop.

Run each desired stage explicitly; none is selected automatically:

```bash
python3 tests/native/v161/run.py --stage native
python3 tests/native/v161/run.py --stage export
python3 tests/native/v161/run.py --stage motion
python3 tests/native/v161/run.py --stage udp
python3 tests/native/v161/run.py --stage performance
python3 tests/native/v161/run.py --stage uncapped
python3 tests/native/v161/run.py --stage dense-relative
```

`native` covers layers, admission/revocation, lifecycle and fault recovery. `export` invokes the independent portable replay on an actual exported static-terrain snapshot. `motion` checks analytic contour motion/zoom in full and cockpit views. `udp` includes the original absolute dense live workload criteria. `dense-relative` is a separate follow-up comparison and does not erase an absolute workload failure. `performance` and `uncapped` retain the executed predeclared criteria and observer limitations. These stages mutate the disposable mission/configuration, and some last several minutes. Evidence contains local gameplay observations and belongs outside public deliveries; raw exports and native evidence remain ignored by Git.
