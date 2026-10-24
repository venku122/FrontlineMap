# Contact continuity and team coverage

Execution scope: the user's October23 request authorizes implementing all three saved follow-up items. No normal-install deployment is included. Existing C#/BepInEx architecture, Legacy numerics, terrain source-distance fix and renderer safety remain.

## Intent

Friendly blue support should not disappear repeatedly because an icon blinks, is filtered, or a known enemy is temporarily lost. Team-wide friendly information exposed by the game should contribute even when its icons are hidden. Previously identified enemies should leave an explicitly historical estimate at their last authorized position, fading with information age.

## Approach and tradeoffs

Retaining the previous combined texture unconditionally would retain revoked context and obscure freshness. Expanding or smoothing arbitrary radial territory would invent control the game does not supply. Instead separate authorized knowledge from cosmetic map presentation, use the native own-team roster, and retain copied enemy observations with bounded memory and aging. This is the selected implementation approach under the user's execution request. There is no authoritative territory polygon in the inspected contract: display estimated support, not guaranteed ownership.

## Data boundary

Use only viewer.factionUnits and viewer.GetAirbases for own-side expansion; resolve only those IDs and require current own HQ. Own positions use the same source as the native friendly map. Enemies enter history only through a permitted visible native marker plus that viewer's tracking record. Read the record's cached lastKnownPosition directly; do not invoke TrackingInfo.GetPosition, which can refresh from live world state. Store copied role, weight, owner and timestamp; never resolve a hidden historical unit for new facts.

Tracking/marker disappearance is soft loss and makes an existing copy stale. It does not itself certify death or allegiance change. Without a separate authorized confirmation, hidden disappearance/destruction ages identically. A currently admitted conflicting owner/role replaces or revokes the old record; own roster removal revokes current friendly support. Mission/session, viewer, invalid faction context and model/config changes are hard boundaries. A closed map or temporary jamming hides presentation and stops acquisition but preserves same-viewer history and original age. History must clear before a changed viewer can access it.

## Defaults chosen for implementation

Reuse StaleHalfLifeSeconds=120. Add HistoryMaximumAgeSeconds=900 (0 disables historical retention), cap retained enemy records at4096 and expire by original observed simulation time. Repeated stale reads must not refresh time or position. A backward simulation clock clears history. Reacquisition replaces one record without duplication. These are implementation defaults, not previously agreed user settings.

History is stale input to existing layered confidence/fill/front logic, with a visible historical-contact count and age explanation. Old precision fades as confidence falls. Normal monotonic computation/presentation expiry stays intact. Legacy keeps its original marker-only admission and no historical retention, preserving its numerical and policy baseline.

## Verification

Pin source/role/age semantics in portable tests, then reproduce cosmetic revocation on the old native binary and verify the new binary. Prove own roster/airbases survive cosmetic hiding and work on both map views; enemy cached data do not follow hidden movement or hidden death; reacquisition is unique; history survives temporary map closure/jamming but not viewer/session boundaries. Keep immediate cancellation on hard revocation and publication races. Update native fixtures that formerly equated icon disappearance with lost historical knowledge.

Retain source/DLL/reference identities, real host/client cases, frame/availability measurements, memory/resource bounds and screenshots. Report measured failures honestly; previous performance blockers are not silently waived. Native keyboard/hardware/DPI/VR expansion remains outside the earlier authorized test scope. The native checklist API is not exposed in this session: the active native goal and repository checklist track execution.

## Live-test refinement: retain the known estimate during same-viewer refresh

The first attested120-second live run still had three real own-roster revocations and gaps up to1.556s, despite cosmetic regressions passing. Address this remaining flicker with an explicit controller policy-revision path: cancel outdated work and reject old-policy publication, but retain the already shown snapshot/handle while a new same-viewer estimate computes. Preserve its original15-second monotonic expiry; never refresh that deadline. Label it “updating known estimate”. The shown image conveys previously authorized information and cannot acquire hidden facts.

The plugin independently compares the current viewer identity with the viewer associated with the presentation. Viewer, Session, Map or Model differences, invalid/jammed/hidden presentation and expired historical records still clear immediately. Legacy uses the existing strict context setter. Retained old-policy snapshots cannot drive repaint. Independent source/design review accepted these guards; add publication-race, original-expiry and exactly-once resource regressions before implementation. The first b655 runtime evidence remains separately identified and will not certify the revised binary.
