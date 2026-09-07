# Long Shots tactic research — 2026-09-07

## Scope

Long Shots (LS) is evaluated as a tactic-specific suitability problem, not as a generic tactical score. The evaluator asks whether the current XI is actually built to use LS against this opponent.

## Sources

- Hattrick Wiki — Long shots: https://wiki.hattrick.org/wiki/Long_shots
- Hattrick Manual: https://wiki.hattrick.org/wiki/Manual
- Hattrick Rules: https://wiki.hattrick.org/wiki/Rules
- Hattrick Tactics: https://wiki.hattrick.org/wiki/Tactics
- Hattrick Scoring: https://wiki.hattrick.org/wiki/Scoring
- Hattrick Set Pieces: https://wiki.hattrick.org/wiki/Set_Pieces
- Hattrick announcement/background: https://wiki.hattrick.org/wiki/Next_season%3A_Indirect_free_kicks_and_long_shots
- Constantinou et al. (2026), *Decoding the mechanisms of the Hattrick football manager game using Bayesian network structure learning*, Entertainment Computing 57, 101131: https://doi.org/10.1016/j.entcom.2026.101131

## Verified mechanics

### 1. What LS does

LS trades a portion of normal middle/wing attacks for long-shot events. A long shot bypasses the normal attack-vs-defence sector comparison and directly pits the shooter against the opponent goalkeeper.

The official Manual says LS can convert up to around 30% of side/middle attacks and that tactical skill is based on all outfielders' Scoring and Set Pieces, with Scoring three times as important as Set Pieces.

The 2026 paper models a wider empirical conversion range of **6%–43%** and uses the exact research curve already present in `M8ChanceAllocationEngine`:

`TCR_LS(RT) = 0.00761935 * RT + 0.07520052`

where V5 tactical level 0–10 maps to paper RT 0–20. This gives 7.520052% at RT=0 and 22.758752% at RT=20; the paper's broader 6%–43% interval is the empirical/modelled range, not a claim that every team reaches 43% through the same linear curve.

### 2. Shooter quality

The public Hattrick Wiki describes the direct scoring comparison as depending on the shooter's Scoring and Set Pieces versus the goalkeeper's Goalkeeping and Set Pieces. It also publishes a community-derived probability expression:

`Shooter ratings = 0.0643 * Scoring^2.3808 * Set Pieces^2.7720`

`Goalkeeper ratings = 1977.4524 * Goalkeeping^0.9 + 31.4827 * Set Pieces^2.3262`

These formulae are treated as public/community-derived reference equations, not as a hidden proprietary live-engine formula.

The 2026 paper separately models long-shot scoring probability as a function of tactic rating minus opponent average defence, explicitly using opponent defence as a proxy for hidden goalkeeper skill. It reports scoring rates ranging from **11% to 100%** in that learned relationship. We therefore use actual opponent goalkeeper data when available and retain opponent defence as a secondary proxy, without claiming that defence is literally the goalkeeper's live skill.

### 3. Who should be good at LS

The tactic requires more than a high tactical conversion level. The whole outfield XI contributes to tactical skill. High Scoring and Set Pieces are the core requirements. Playmaking remains important because possession creates the normal attacks that LS can convert, while Defending matters because LS does not remove the opponent's remaining chances.

The public Wiki also notes that inner midfielders and wingers are more likely to be selected as shooters than defenders/forwards. The dedicated evaluator therefore weights IM/W shooter quality more heavily when estimating the XI's effective long-shot shooter pool.

Passing is **not** the documented LS tactical-skill input. The evaluator therefore uses the actual `Player.SetPiecesSkill` together with Scoring for LS suitability rather than treating Passing as a core LS requirement.

### 4. Possession

LS still needs chances. The Hattrick Wiki explicitly states that ball possession has almost the same importance as tactical level because possession creates more chances and denies them to the opponent. The evaluator therefore includes possession as a suitability component rather than assuming that a high LS level alone is sufficient.

### 5. Opportunity cost / penalties

LS gives up some normal middle/wing attack opportunities. The Manual and Rules both describe this trade-off and state that wing/middle attack and midfield become somewhat worse.

The Long Shots Wiki documents approximate rating penalties of **-2.7% to side/middle attack** and **-5% midfield**. These are used only as explicit documented penalty proxies; they are not presented as an undisclosed motor coefficient. The main opportunity-cost calculation uses the actual Normal-vs-LS M8/M9 outputs whenever available.

### 6. Opponent interaction

The strongest direct counter to LS is the opponent goalkeeper because normal sector defence is bypassed for the converted shot. The keeper's Goalkeeping + Set Pieces therefore materially changes LS suitability.

Pressing can also steer off a long-shot opportunity. This interaction belongs to the cross-tactic matchup layer; the LS evaluator itself does not invent a new Pressing suppression coefficient.

### 7. Special events / set pieces

LS does not remove the importance of special events and set pieces. The public sources emphasize that LS teams still need alternative scoring routes because only a limited number of tactical long shots occur in a match. Head specialists and general set-piece quality can therefore matter to the broader match outcome, but they are not incorrectly folded into the core LS tactical-level formula.

## Evaluator architecture

`LongShotsTacticEvaluator` calculates these layers independently:

1. **Requirements** — XI Scoring, Set Pieces, tactical input and actual shooter pool.
2. **Benefit** — actual M8 LS conversion and expected LS opportunity volume.
3. **Shooter-vs-GK** — estimated shot quality using Scoring + Set Pieces for likely shooters and the opponent goalkeeper.
4. **Possession** — ability to generate the attacks that LS can convert.
5. **Opponent interaction** — keeper quality plus the paper's tactic-rating/defence proxy relationship.
6. **Penalty** — documented attack/midfield penalty proxies.
7. **Opportunity cost** — Normal regular-attack volume and predicted win-probability loss.
8. **Squad fit** — whether the actual XI is built for LS, not just whether the tactic can be mathematically enabled.
9. **Suitability + explanation** — final bounded fit score with a human-readable reason.

## Important implementation rule

Do not change M10/M11 threshold or anti-lock behaviour for Long Shots. The tactic-specific evaluator is an input to the existing selection layer; it is not a replacement for the existing threshold logic.

## Research limitation

The 2026 paper is an empirical Bayesian-network reconstruction of a partially hidden game engine. Its learned relationships and the public Wiki's community-derived equations are not automatically identical to Hattrick's undisclosed production formulas. The implementation therefore distinguishes documented mechanics, paper-derived curves, and explicit bounded proxies.
