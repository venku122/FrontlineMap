# Follow-up to-dos — in-game feedback, October 23, 2026

Status: saved for a later follow-up; no behavior changes requested for this session. Feedback is from the user's normal NOMM-managed installation running FrontlineMap1.6.1. After the loader correction and restart, the user confirmed both the F1 mod menu and FrontlineMap were working.

## 1. Stop repetitive friendly-side flashing

- [ ] Reproduce and diagnose the blue/friendly fill repeatedly flashing during normal play.
- [ ] Trace visibility/admission changes, snapshot replacement/expiry, publication and datalink state to distinguish intended loss-of-information behavior from avoidable presentation gaps. The cause is not established by this feedback.
- [ ] Keep friendly fill and contours visually stable while valid support is being refreshed, while preserving required information revocation.
- [ ] Verify with a recorded live mission: steady friendly data, moving map, repeated updates, and datalink loss/recovery. Retain frame-time and overlay-availability evidence as well as a visual recording.

User observation: the friendly/blue area was repetitively flashing.

## 2. Represent team-wide friendly information available through datalink

- [ ] Investigate why initial friendly coverage is only a very small blue area despite the user's expectation of team-wide information through datalink.
- [ ] Audit the game's authorized friendly/team datalink sources and whether current native-marker admission unnecessarily restricts support to local or currently displayed markers.
- [ ] Use available team-authorized information to represent the full known friendly area, including distant friendly support when datalink permits it.
- [ ] Distinguish actual game-provided territory/ownership from estimated influence around known friendly assets. Do not present a heuristic estimate as authoritative controlled territory.
- [ ] Verify full/minimap views, distant friendly units, datalink connection/disconnection, mission/team changes and multiplayer viewer permissions.

User expectation: with datalink, the map should show the entire team's controlled territory rather than starting with a small local patch. Determine what the game actually exposes before choosing the implementation.

## 3. Retain and gradually fade last-known enemy territory/fronts

- [ ] Design persistent historical support from previously identified enemies: identification establishes an estimated area and border; losing datalink/contact reduces its intensity over time rather than immediately erasing the whole map estimate.
- [ ] Preserve the last authorized observed location without following hidden movement or extrapolating from live enemy positions.
- [ ] Review the existing stale-observation half-life and admission revocation separately. The current model already decays admitted stale contacts; this request also concerns retaining historical estimates when their markers or datalink are no longer available.
- [ ] Define how confidence/opacity and the precision of the border age, how reacquisition refreshes the estimate, and when old history finally expires. No half-life or final expiry has been chosen yet.
- [ ] Define separate handling for temporary datalink/contact loss versus confirmed destruction, allegiance changes, viewer/team changes and mission boundaries. Historical information must be visibly labeled and must not retain information for an unauthorized new viewer.
- [ ] Verify that aged territory remains interpretable, borders do not flicker or imply current certainty, and long-running history has bounded memory and frame cost.

User goal: still recognize previously established areas of control after losing contact, with fading intensity communicating aging information. The user saw some enemy territory and a frontline where blue and red met.

## Preserve the parts that worked

The user reported that the map looked good and performed well in this session, including opposing blue/red support and a visible frontline. Treat this as positive qualitative playtest feedback, not a replacement for the recorded uncapped/dense multiplayer performance acceptance failures. Preserve that appearance and responsiveness while addressing the items above.

The installed DLL/configuration are unchanged by saving this backlog. Existing release/performance blockers remain tracked in [current validation](RUNTIME_VALIDATION_2026-10-22_V161.md) and [readiness](benchmarks/LATEST_READINESS.json).
