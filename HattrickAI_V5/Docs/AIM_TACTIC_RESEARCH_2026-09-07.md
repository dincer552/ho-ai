# Attack in the Middle (AiM) — Research & V5 Specification

## 1. Sources

- Hattrick Rules / Manual: AiM trades wing attacks for centre attacks; manual guidance gives roughly 15–30%; tactical skill is the total Passing of outfield players and experience contributes to the live tactic level.
- Hattrick Wiki — Attack in the Middle / Tactics: AiM increases central attack share, weakens wing defence, and is strongest when the redirected central attacks are more valuable than the sacrificed wing attacks.
- Constantinou et al., *Entertainment Computing 57 (2026) 101131*, §4.3 and Appendix C: empirical/KB-probabilistic AiM conversion model.

## 2. Core mechanics

AiM is a **1:1 redistribution**, not a chance-generation tactic. It moves part of the wing attack volume into the centre. The total chance pool therefore does not increase merely because AiM is selected.

The 2026 paper models:

- **20–35%** of wing attacks converted to the centre depending on tactic skill.
- Normal central share: **36.15%**.
- AiM central share: approximately **47–55%** depending on tactic level.
- The paper describes a small defensive penalty for the tactic. Public Hattrick rules describe the practical downside as weaker **wing defence**.
- Tactical skill is driven by total Passing of all outfield players, with an experience contribution in the live-game calculation.

The official manual reports a slightly different public range (**15–30%**). V5 uses the newer 2026 empirical/KB-probabilistic model in M8, while keeping the manual range documented as a source discrepancy rather than silently mixing the two.

## 3. Exact paper conversion curve used by M8

Appendix C, Eq. C.2 gives the AiM tactic conversion curve as a function of the paper tactic rating `RT`:

```text
TCR_AiM(RT) = -0.00036765 * RT² + 0.02180462 * RT + 0.0705084
```

The result is bounded to a probability and mapped into the V5 tactical-strength scale by `TacticPaperMappingEngine` before M8 evaluates the curve.

This is important: **the evaluator does not invent a linear 20–35% conversion formula.** M8 calculates the research-derived conversion and the AiM evaluator consumes that actual value.

## 4. Sector redistribution

The research model starts from the Normal sector distribution used by V5:

- Left: 25.65%
- Centre: 36.15%
- Right: 25.65%
- Set pieces: 12.55%

For AiM, the engine moves a percentage of the two wing shares into the centre while preserving the total sector probability mass. This produces the observed/target central-share range of approximately 47–55%.

## 5. Suitability calculation

`AttackMiddleTacticEvaluator` is intentionally separate from the generic tactic objective. It evaluates the decision to use AiM in layers:

### A. Requirements

- Total Passing of the actual XI's outfield players.
- Soft Experience fit because experience contributes to live tactical level.
- No invented hard threshold: the exact live experience bonus is not publicly specified.

### B. Benefit

- Actual M8 AiM conversion rate.
- Actual resulting central chance share.
- Central attack vs opponent central defence scoring probability.
- Difference between central route quality and the quality of the wing attacks being sacrificed.

### C. Possession context

AiM does not create additional total chances. Hattrick tactical guidance therefore favours attack-direction tactics when the team can generate enough possession/chances to exploit the redistribution. V5 treats possession as a **soft suitability factor**, not an eligibility gate.

### D. Opportunity cost

The evaluator measures:

- Wing volume redirected to the centre.
- Quality of the baseline wing route being sacrificed.
- Any own regular-chance volume loss versus Normal.
- Any M9 win-probability loss versus Normal.

### E. Opponent interaction

The centre is not automatically good just because it is the centre. AiM receives value when:

```text
our central attack > our sacrificed wing route
AND
our central attack has a favourable matchup against opponent central defence
```

Conversely, if the opponent has very strong central defence, AiM loses value even when the team's Passing is high.

### F. Defensive risk

The public game rules state that AiM weakens wing defence. The 2026 paper calls this a small penalty but does not publish a defensible closed-form coefficient. V5 therefore uses:

```text
wing_defence_risk = bounded_proxy * opponent_wing_threat
```

with a **10% bounded risk proxy** used only for decision scoring. It is explicitly **not** claimed to be the hidden match-engine coefficient.

The evaluator additionally estimates relative expected defensive damage from the opponent's wing threat and the wing chance share.

## 6. Final suitability structure

The dedicated evaluator combines:

1. Primary AiM benefit
2. Central-vs-wing directional advantage
3. Centre matchup
4. Passing/Experience squad fit
5. Possession context
6. Wing opportunity cost
7. Own chance-volume loss
8. Opponent wing threat / defensive risk
9. M9 match-level win probability as a secondary guard

The objective is not `"AiM has high tactical skill => choose AiM"`. The objective is:

> **Choose AiM only when the central route gained by redistributing wing attacks is worth more than the wing attacking value and defensive strength sacrificed against this opponent.**

## 7. 2026 paper limitation / calibration rule

The paper is a probabilistic reconstruction of the Hattrick engine, not a disclosure of every proprietary formula. Where the paper provides an explicit equation (such as the AiM TCR curve), V5 uses it. Where it only gives a range or qualitative penalty, V5 does not manufacture an exact hidden coefficient.

## 8. Regression requirements

The AiM implementation must preserve:

- M8 chance-pool conservation.
- Centre/wing share conservation.
- 20–35% paper-derived AiM conversion bounds after curve evaluation.
- Approximately 47–55% central share under AiM across the supported tactic-strength range.
- Normal remains the comparison baseline.
- M10/M11 threshold and anti-lock logic remain untouched.
