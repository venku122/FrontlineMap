# Follow-up to-dos — in-game feedback, October 23, 2026

Status: all three implementation items completed with bounded native verification; general release remains blocked by measured performance failures. See [October24 acceptance](RUNTIME_VALIDATION_2026-10-24_V170.md). See [implementation checklist](superpowers/plans/2026-10-23-contact-continuity.md). Feedback is from the user's normal NOMM-managed installation running FrontlineMap1.6.1. After the loader correction and restart, the user confirmed both the F1 mod menu and FrontlineMap were working.

## 1. Stop repetitive friendly-side flashing

- [x] Reproduce and diagnose the blue/friendly fill repeatedly flashing during normal play.
- [x] Trace visibility/admission changes, snapshot replacement/expiry, publication and datalink state to distinguish intended loss-of-information behavior from avoidable presentation gaps. Diagnosis: cosmetic marker state revoked own support; real same-viewer roster revisions also caused replacement gaps.
- [x] Keep friendly fill and contours visually stable while valid support is being refreshed, while preserving required information revocation.
- [x] Verify with a recorded live mission: steady friendly data, moving map, repeated updates, and native tracking/jamming loss/recovery. Retain frame-time and overlay-availability evidence as well as a visual recording.

User observation: the friendly/blue area was repetitively flashing.

## 2. Represent team-wide friendly information available through datalink

- [x] Investigate why initial friendly coverage is only a very small blue area despite the user's expectation of team-wide information through datalink.
- [x] Audit the game's authorized friendly/team datalink sources and whether current native-marker admission unnecessarily restricts support to local or currently displayed markers.
- [x] Use available team-authorized information to represent the full known friendly area, including distant friendly support when datalink permits it.
- [x] Distinguish actual game-provided territory/ownership from estimated influence around known friendly assets. Do not present a heuristic estimate as authoritative controlled territory.
- [x] Verify full/minimap roster coverage (including distant roster IDs), native tracking/jamming loss/recovery, mission/team boundaries and opposing multiplayer viewers.

User expectation: with datalink, the map should show the entire team's controlled territory rather than starting with a small local patch. Determine what the game actually exposes before choosing the implementation.

## 3. Retain and gradually fade last-known enemy territory/fronts

- [x] Design persistent historical support from previously identified enemies: identification establishes an estimated area and border; losing datalink/contact reduces its intensity over time rather than immediately erasing the whole map estimate.
- [x] Preserve the last authorized observed location without following hidden movement or extrapolating from live enemy positions.
- [x] Review the existing stale-observation half-life and admission revocation separately. The current model already decays admitted stale contacts; this request also concerns retaining historical estimates when their markers or datalink are no longer available.
- [x] Define how confidence/opacity and the precision of the border age, how reacquisition refreshes the estimate, and when old history finally expires. Implemented defaults:120 simulation-second half-life,900-second maximum history age.
- [x] Define separate handling for temporary datalink/contact loss versus confirmed destruction, allegiance changes, viewer/team changes and mission boundaries. Historical information must be visibly labeled and must not retain information for an unauthorized new viewer.
- [x] Verify that aged territory remains interpretable, borders do not flicker or imply current certainty, and long-running history has bounded memory and frame cost.

User goal: still recognize previously established areas of control after losing contact, with fading intensity communicating aging information. The user saw some enemy territory and a frontline where blue and red met.

## Preserve the parts that worked

The user reported that the map looked good and performed well in this session, including opposing blue/red support and a visible frontline. Treat this as positive qualitative playtest feedback, not a replacement for the recorded uncapped/dense multiplayer performance acceptance failures. Preserve that appearance and responsiveness while addressing the items above.

The normal installation remains uninstalled; verification used disposable copies. Release/performance blockers remain tracked in [current validation](RUNTIME_VALIDATION_2026-10-24_V170.md) and [readiness](benchmarks/LATEST_READINESS.json).

Completion details: the visual recording is a sparse screenshot animation backed by per-frame probes. Native faction transitions use mission restarts; same-session guards are portable-tested. Timed expiry uses a12-second configured lifetime, with the900-second default boundary additionally unit-tested. These scopes are explicit in the acceptance report.

Additional release verification still open: a dedicated distance-threshold coverage assertion, ordinary non-jamming datalink disconnect/reconnect, and stale-but-still-tracked visible-marker metadata mutation. Existing roster completeness, jamming/tracking and lost-tracking hidden-metadata tests have narrower scopes; see the exact-binary report.
