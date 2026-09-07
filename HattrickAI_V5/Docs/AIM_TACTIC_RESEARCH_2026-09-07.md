# Attack in the Middle (AiM) — Research & V5 Specification

## 1. Core mechanics

- AiM exchanges a percentage of wing attacks into central attacks.
- The 2026 Entertainment Computing study models the conversion at **20–35%** depending on tactic skill.
- The same study reports central attack share increasing from **36.15% Normal** to approximately **47–55%** under AiM.
- Tactical skill is based primarily on the Passing skill of all outfield players; the live-game calculation also includes experience contribution.
- AiM has a defensive side effect: wing defence becomes weaker. The 2026 study describes this as a small penalty but does not publish a closed-form coefficient. V5 therefore uses a bounded 10% wing-defence risk proxy inside the suitability evaluator rather than treating 10% as an exact hidden-engine constant.

## 2. Suitability model

`AttackMiddleTacticEvaluator` evaluates:

1. **Requirements** — total outfield Passing and experience fit.
2. **Benefit** — actual wing-to-centre conversion from M8 and resulting centre-share increase.
3. **Centre matchup** — own central attack versus opponent central defence.
4. **Wing opportunity cost** — quality of the attacks being removed from the wings.
5. **Defensive penalty risk** — opponent wing threat multiplied by the bounded AiM wing-defence penalty proxy.
6. **Squad fit** — Passing, Experience and Scoring support.
7. **Opponent interaction** — whether the centre is materially better than the wings against this opponent.
8. **Suitability** — benefit minus opportunity/defensive costs, with the existing M9 win probability as a secondary outcome signal.

## 3. Important implementation rule

The evaluator does not replace M8's research-derived chance distribution. M8 remains the source of truth for the tactic conversion rate and sector allocation. The evaluator answers the separate question: **is AiM actually a good choice for this XI against this opponent?**

M10/M11 threshold and anti-lock logic remain unchanged.
