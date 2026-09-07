# Attack on Wings (AoW) — Deep Tactic Research

Date: 2026-09-07

## 1. Purpose

AoW is evaluated as a tactic-specific suitability decision, not as a generic tactical score. The question is:

> Does this XI gain enough from moving normal central attacks to the wings against this opponent to justify the loss of central defensive strength?

The implementation is split between:

- `M8ChanceAllocationEngine`: actual paper-derived AoW conversion and chance distribution;
- `AttackWingsTacticEvaluator`: XI/opponent suitability, benefit, cost and explanation;
- `TacticObjectiveEngine`: routing only.

M10/M11 threshold and anti-lock behaviour is intentionally untouched.

## 2. Public Hattrick mechanics

### Official/manual rules

Hattrick's public Rules and Manual describe AoW as the reverse of Attack in the Middle:

- middle attacks are converted to wing attacks;
- approximately 20–40% of middle attacks can be converted according to tactic skill;
- total Passing of all outfield players determines AoW tactical skill;
- Experience contributes to the tactical level;
- the cost is a somewhat weaker central defence.

The older Hattrick 6.5 explanation also describes the tactic as a 1:1 directional exchange: attacks are moved from the centre to the wings rather than creating a larger normal-chance pool.

### Community research / wiki

The Hattrick Wiki reports the regular distribution as approximately 35% centre, 25% left wing, 25% right wing and 15% set pieces. Its AoW page gives a 20–40% centre-to-wing conversion and a Passing-based tactical-level table. These wiki values are treated as contextual guidance, not as an authoritative hidden-engine formula.

## 3. 2026 academic paper input

Constantinou et al., *Entertainment Computing* 57 (2026) 101131, models AoW as:

- **34–52%** of central attacks exchanged to the wings;
- Normal wing share: **51.3%**;
- AoW wing share: approximately **63–70%** depending on tactic rating;
- a small penalty is applied to wing defence in the paper's AiM description, while AoW applies the corresponding **central-defence** penalty.

The paper's Appendix C, Eq. C.2 gives the AoW tactic-conversion curve:

```text
TCR_AoW(RT) = -0.00046569 * RT^2
              + 0.02894608 * RT
              + 0.10514706
```

where `RT` is the paper tactic-rating scale.

V5 does not duplicate this equation in the evaluator. `M8ChanceAllocationEngine` applies the exact curve through `TacticPaperMappingEngine` and the evaluator consumes the resulting conversion rate.

The paper also notes that its AoW model performs well in empirical validation for expected goals/variance, while tactic mechanisms that are not publicly documented should not be represented as invented exact coefficients.

## 4. Tactical requirements

### Required

1. High total outfield Passing is the primary tactical requirement.
2. Experience is a secondary live-level factor.
3. The XI needs genuinely useful wing attack routes.
4. The opponent must provide a favourable wing-vs-defence matchup.
5. The central defensive price must be tolerable.

### Not required

- There is no special AoW specialty requirement.
- A specific formation is not hard-coded as an eligibility condition.

Formation effects are already represented by the actual M7/M8 ratings and the lineup's player assignment.

## 5. Benefit calculation

The evaluator measures:

- actual left and right attack-vs-defence scoring probabilities from M8;
- weighted wing quality;
- central attack-vs-central-defence quality;
- wing-vs-centre advantage;
- actual AoW conversion rate;
- final wing share;
- expected amount of attack volume redirected from centre to wings;
- expected directional scoring gain.

The core principle is:

```text
AoW benefit ≈ redirected centre volume × (wing route quality − centre route quality)
```

This is deliberately an evaluation signal rather than a replacement for M8's chance/goal mechanics.

## 6. Opponent interaction

AoW should be less attractive when the opponent has a dangerous central attack because the tactic weakens central defence.

The evaluator therefore uses:

- opponent central attack vs our central defence probability;
- opponent expected regular chance volume;
- central chance share;
- a bounded defensive-risk proxy.

The public sources do not provide a trustworthy exact central-defence multiplier for AoW. Therefore V5 uses an explicit bounded **10% decision-risk proxy**, not a claimed engine coefficient. The actual sector ratings and scoring probabilities remain M8/M9 authority.

## 7. Opportunity cost

The evaluator explicitly charges for:

- central attack volume redirected to wings;
- the quality of the central route being abandoned;
- any loss in own normal chance volume versus Normal;
- any decline in predicted win probability;
- central defensive exposure.

This prevents a high Passing total from making AoW automatically attractive.

## 8. Possession

AoW does not create a larger normal chance pool. Possession therefore remains a soft suitability factor: with more expected possession, the team has more opportunities to exploit the chosen direction; with poor possession, the directional benefit is less valuable.

Possession is not an AoW eligibility gate.

## 9. Current code architecture

```text
TacticObjectiveEngine
    -> AttackWingsTacticEvaluator
        -> Requirements
        -> Conversion / Benefit
        -> Wing-vs-Centre matchup
        -> Possession context
        -> Opponent central-threat interaction
        -> Central-defence risk
        -> Centre opportunity cost
        -> Squad fit
        -> Matchup fit
        -> Suitability
        -> Explanation
```

`M8ChanceAllocationEngine` remains the single source of truth for:

- paper RT conversion;
- AoW conversion rate;
- chance-sector redistribution;
- normal chance volume;
- possession.

## 10. Important implementation rule

Do not turn the public 20–40% manual range or the paper's 34–52% range into an invented linear tactic formula. The exact paper curve is already implemented centrally in M8. The evaluator must consume the actual M8 result and evaluate whether that result is strategically worthwhile for the current XI and opponent.

## Sources

- Hattrick Wiki — Attack on Wings
- Hattrick Rules — Attack on Wings
- Hattrick Manual — Attack on Wings
- Hattrick 6.5 New Match Tactics — Attack on Wings
- Hattrick Wiki — Attack / Attack ratings / Defence ratings / The ABC of Tactics
- Constantinou et al. (2026), *Entertainment Computing* 57, 101131, especially §4.3, Fig. 8 and Appendix C Eq. C.2
