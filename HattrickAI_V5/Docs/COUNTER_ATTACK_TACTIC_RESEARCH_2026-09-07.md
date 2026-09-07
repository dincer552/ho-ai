# Counter Attack Tactic Research — 2026-09-07

## 1. Published mechanics used by V5

- Counter Attack deliberately gives up midfield to create additional counter-attacking opportunities.
- CA is useful when the team has strong defence and enough attack to finish the limited opportunities, especially against an opponent with an ineffective attack.
- The CA tactic applies a 7% midfield-capacity penalty.
- Eligibility is checked against the opponent midfield **before** that 7% penalty: if our team dominates midfield before the penalty, we do not receive the tactical CA benefit and only suffer the midfield penalty.
- CA tactical skill is based only on the defenders on the field. Passing is twice as important as Defending.
- The official manual states that an experience bonus is added for each involved defender, but the exact live-engine bonus formula is not public.

Sources:
- Hattrick Wiki — Tactics / Counter-Attack
- Hattrick Wiki — Rules / Counter-attacks
- Hattrick Manual — Match: Tactics / Counter-attacks
- Hattrick Developer Blog — New Match Engine Odyssey

## 2. Specialty interactions

### Quick

Quick Wingers, Inner Midfielders and Forwards provide an additional CA tactic boost. The published range is approximately 0–2.8 tactic levels. One extra relevant Quick player gives a 5% boost and eight give 14%; the relationship is explicitly non-linear. Opponent Quick Wing Backs, Inner Midfielders and Defenders reduce the extra boost.

V5 therefore treats Quick as a bounded specialty interaction, not as a flat additive level invented from an unpublished formula.

### Technical

Technical Defenders and Wing Backs can create non-tactical counter-attacks from an opponent's missed normal chance. Published event rates are approximately 1.7%–3.0%, depending on the number of technical back players.

### Other specialties

The evaluator does not invent a direct CA-level bonus for Powerful, Head or Unpredictable. Their normal special-event effects remain in the event layer. This avoids double-counting undocumented tactic-level effects.

## 3. 2026 academic-paper calibration

The uploaded 2026 Hattrick match-engine paper models Counter Attack with a conversion range of approximately 4%–45%. It also reports non-tactical counter-attack rates by defensive lineup size. V5 uses the paper's published range as the tactical conversion envelope and keeps the exact probability mapping in `M8ChanceAllocationEngine`.

The paper's CA mechanism is represented in M8 as:

1. determine midfield ownership;
2. require CA eligibility while the team is behind in midfield before the CA penalty;
3. apply the 7% midfield penalty;
4. identify opponent missed Normal chances;
5. convert part of those missed chances into tactical CA opportunities according to CA tactical strength.

## 4. Dedicated V5 suitability evaluator

`CounterAttackTacticEvaluator` evaluates CA independently from the generic tactic score.

Layers:

1. **Requirement** — pre-penalty midfield eligibility.
2. **CA opportunity** — opponent missed Normal chance volume.
3. **Tactical strength** — defender-only Defending + 2× Passing input, plus bounded experience quality.
4. **Defensive resistance** — whether the XI can actually suppress the opponent's attack.
5. **Attack quality** — whether generated CA opportunities can be finished by this XI.
6. **Quick interaction** — own Quick attackers vs opponent Quick defensive players.
7. **Technical support** — technical defenders / wing backs and non-tactical CA support.
8. **Trade-off** — lost normal opportunities, 7% midfield cost, and deterioration relative to the Normal baseline.
9. **Suitability** — final CA FitScore and explanation.

## 5. Deliberate non-assumptions

- No arbitrary minimum defence percentage is hard-coded.
- No arbitrary minimum CA tactic level is hard-coded.
- No invented exact experience-bonus formula is hard-coded.
- No M10/M11 threshold or anti-lock logic is changed.
- The evaluator can reject CA even when the tactic mechanically produces CA opportunities if the actual XI/matchup does not justify the opportunity cost.
