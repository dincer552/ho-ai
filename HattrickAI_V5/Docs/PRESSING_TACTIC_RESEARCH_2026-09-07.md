# Pressing Tactic Research — 07.09.2026

## 1. Verified Hattrick mechanics

- Pressing reduces the total number of potential **normal chances** in the match, affecting both teams. It does not suppress special events.
- The tactic is built from the whole outfield XI: defending and stamina are the primary inputs; experience contributes to the tactical level.
- A **Powerful** player's defending skill counts double for the Pressing tactical calculation.
- Pressing consumes stamina faster than Normal. Fatigue therefore matters both to the tactic itself and to late-match player performance.
- If both teams use Pressing, the chance-reduction effect is cumulative.
- Pressing is structurally suited to matches where reducing total chance volume is valuable, especially when the opponent is stronger or when the objective is to keep the score low.
- A strong opponent midfield and attack can overcome a pressing setup; therefore Pressing must not be scored from the own XI alone.
- A team optimized for Creative can retain an advantage because Pressing removes normal chances, not special events.

Sources: Hattrick Rules / Manual, Hattrick Wiki Pressing/Tactics, and the official Developer Blog framework.

## 2. What the evaluator must answer

For the exact DB2 XI × Pressing matchup, the evaluator must answer:

1. **Requirements** — Is the whole outfield XI strong enough in defending, stamina and experience?
2. **Powerful leverage** — How much additional Pressing input comes from Powerful defenders?
3. **Benefit** — How many opponent normal chances does the Pressing scenario remove relative to the same XI under Normal?
4. **Self-damage** — How many of our own normal chances are also removed?
5. **Stamina risk** — Is the XI resilient enough to sustain pressure without a major late-match degradation risk?
6. **Opponent interaction** — Does the opponent actually have enough normal attacking volume for suppression to be valuable? Is its midfield/attack strong enough to make weak Pressing unattractive?
7. **Opportunity cost** — Does Pressing reduce our win probability/chance volume more than the opponent suppression is worth?
8. **Suitability** — Is the net defensive benefit large enough to justify the sacrifice for this specific XI and opponent?

## 3. Implementation in V5

`PressingTacticEvaluator` is a dedicated evaluator and is now called directly by `TacticObjectiveEngine` for `TeamTactic.Pressing`.

The evaluator uses these layers:

- DEF support: average defending of the XI.
- STAM support: average stamina plus a weakest-link component because every outfield player contributes.
- EXP support: average experience.
- Powerful defence boost: explicit double-defending contribution for Powerful players.
- Opponent suppression: Normal opponent regular-chance expectation versus Pressing expectation.
- Own chance loss: Normal own regular-chance expectation versus Pressing expectation.
- Net suppression: opponent benefit after accounting for own chance sacrifice.
- Opponent attack value: prevents Pressing from being rewarded merely for suppressing an opponent that was already harmless.
- Stamina risk: penalizes weak stamina resilience.
- Midfield risk: penalizes situations where the defensive tactic is likely to leave the team exposed through loss of possession/late fatigue.
- Opportunity cost: combines own chance loss, stamina risk and Normal-vs-Pressing win-probability loss.

No hard rule such as “minimum N Powerful players” is used. Powerful players are a continuous benefit, not an eligibility gate.

## 4. Formula policy

The official sources document the inputs and effects, but they do not expose a complete modern closed-form Pressing tactical-level formula. V5 therefore keeps the existing M7.2/M8 calibration structure and uses the dedicated evaluator to add the tactic-specific suitability logic around it.

We must not fabricate an exact official formula. Historical match calibration can later tune the evaluator weights.

## 5. Important non-goals

- Do not change M10/M11 thresholds.
- Do not change the M10/M11 anti-lock mechanism.
- Do not turn Pressing into a generic `TacticalScore` multiplier.
- Do not reward Pressing only because its suppression percentage is high.
- Do not ignore the loss of our own chances.
- Do not treat special events as suppressed by Pressing.

## 6. Next validation loop

`Pressing research → dedicated evaluator → offline regression → real Motor DB comparison → real match observations → calibration`

The next tactic after Pressing is Counter Attack, following the dated README tactic plan.
