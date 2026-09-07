# Creative / Play Creatively — Research Specification

**Research date:** 07.09.2026  
**Status:** Research complete; implementation specification ready.

## Verified mechanics

- Play Creatively has a tactical level. Hattrick's official developer material states that Passing is weighted **4× Experience** for this tactic and that **Unpredictable players contribute 2×** to the tactic.
- The exact current tactical-level formula is not officially published. V5 must not invent an exact formula and label it authoritative.
- Creative increases the chance that the match engine creates a special event instead of another event.
- Maximum special events increase by **+1** when one team uses Creative and **+2** when both use it.
- For individual special events, Creative also improves the chance that the Creative team receives the event; if both teams use Creative, this ownership bonus is void.
- Creative slightly increases weather-event activity.
- Creative reduces the team's defence ratings by **7.5%**.

## Why suitability is matchup-dependent

Hattrick's developer description says individual events are selected using the actual players, specialties and positions of both teams. Therefore Creative must compare the XI with the opponent instead of treating the tactic as a generic multiplier.

Hattrick explicitly warns that Creative can backfire when the opponent has the more specialized lineup, because increasing event volume also increases the opponent's opportunities.

## Event requirements to model

The evaluator must inspect the actual XI slots and player skills for at least:

- Quick: Winger / Inner Midfielder / Forward.
- Technical-vs-Head: Technical Winger / IM / Forward versus Head Defender / WB / IM.
- Unpredictable long pass: GK / WB / Defender; Passing versus midfield defending and receiver Scoring versus GK.
- Unpredictable score on his own: Winger / IM / Forward; Scoring versus GK.
- Unpredictable special action: Passing + Experience plus a Winger/Forward receiver.
- Unpredictable mistake: WB / Defender / IM; Defending + Experience versus opponent Scoring + Experience.
- Unpredictable own goal: Winger / Forward; low Passing increases risk.
- Powerful Forward / Sitting Midfielder: Playmaking/Defending/Scoring/GK or Defending/Scoring/Stamina interactions.
- Winger events: Winger skill, receiver Scoring, same-side defence and GK.
- Corner events: Set Pieces, defensive set pieces and Head-specialist balance.
- Experienced Forward, Inexperienced Defender and Tired Defender team events.

## Creative failure / damage conditions

The evaluator must explicitly model:

1. **Defensive penalty:** the 7.5% defence reduction.
2. **Opponent specialty advantage:** more relevant opponent specialists can make increased event volume harmful.
3. **Unpredictable negative exposure:** mistakes from vulnerable defensive/IM players and own-goal exposure from low-Passing Wingers/Forwards.
4. **Inexperience risk:** low-experience defenders/IMs can give the opponent additional events.
5. **Tired-defender risk:** low stamina defenders can create second-half opportunities for the opponent.
6. **Technical-vs-Head risk:** opponent Technical players can exploit our Head defenders/IMs.
7. **Normal opportunity cost:** Creative must be compared against Normal using the exact same XI.

## Proposed V5 calculation

For every **DB2 XI × Creative**:

### 1. Tactical-level strength

Use the M7.2 Creative tactical level as an input. Keep the documented relationship explicit:

`CreativeInput = 4 × total outfield Passing + total outfield Experience`

and incorporate the documented Unpredictable 2× contribution at player level when calculating the tactic input. Do not replace the game's undisclosed exact formula with a guessed formula.

### 2. Specialty portfolio

Build a position-aware portfolio from the XI:

- Quick offensive / defensive
- Head offensive / defensive
- Technical offensive / defensive
- Powerful Forward / Powerful IM
- Unpredictable offensive
- Unpredictable long-pass sources
- Unpredictable mistake exposure
- Unpredictable own-goal exposure
- Experienced Forward potential
- Inexperienced Defender exposure
- Tired Defender exposure
- Corner / Head event potential

### 3. Own-vs-opponent event edge

For every event family, compare the actual eligible players on both sides and use the event's documented skills where available. The model should distinguish **event creation**, **team ownership**, and **goal conversion**.

The code may use a normalized event-edge model, but it must be documented as a V5 model input rather than claimed as an official Hattrick formula.

### 4. Net Creative value

Creative suitability should increase when:

- tactical level is strong,
- useful specialties are in suitable positions,
- our relevant event skill matchups are favorable,
- our opponent specialty exposure is low,
- expected positive event value exceeds expected negative event value,
- and the special-event benefit compensates for the 7.5% defence penalty and Normal opportunity cost.

Creative suitability should decrease when the opposite is true.

## Current V5 gap found

`TacticObjectiveEngine` currently uses `CreativeEventMultiplier`, a generic Creative input and M9 special-event goals. That is not enough: it does not yet model the opponent's specialty portfolio and does not expose the full negative-event risk. The `CreativeProfile` already contains `4 × Passing + Experience`, but the upstream `CalculateTacticSkill` currently averages Passing and Experience instead of preserving the documented 4× relationship. This must be audited before final Creative calibration.

## Validation plan

A Creative regression must verify:

1. High Creative multiplier alone cannot force a high suitability score.
2. Removing useful specialties reduces suitability.
3. Adding correct-position specialties improves suitability.
4. A highly specialized opponent can reduce or reverse suitability.
5. Low Passing on Unpredictable Wingers/Forwards increases negative exposure.
6. Low Defending/Experience on Unpredictable defensive players increases negative exposure.
7. The 7.5% defence penalty is visible in trade-off.
8. Normal and Creative use the exact same XI.
9. M10/M11 thresholds and anti-lock rules remain unchanged.

## Implementation order

`Research → position-aware event profile → Creative evaluator → M9 integration → regression → Motor DB inspection → historical/real-match validation → calibration`

## Sources

- Hattrick Developer Blog — *Specialties and Special Events* (09.05.2017).
- Hattrick Developer Blog — *A new Match Engine Odyssey* (18.12.2017).
- Hattrick Wiki — *Play creatively*.
- Hattrick Wiki — *Special Event*.
- Hattrick Wiki — *Specialty*.

Official developer material is treated as the primary mechanics source; current Hattrick Wiki pages are used as a cross-check. Where the exact formula is undisclosed, V5 will not manufacture a false exact formula.