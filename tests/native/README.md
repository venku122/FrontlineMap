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
