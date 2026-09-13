# Player Contribution Deep Calculation — 2026-09-13

## 1. Scope

This note is the first deep-calculation pass for the M7 regional rating model. It uses the current production V5 fixed engine, the CHPP player snapshot, and the empirical single-player / 3-defender observations already collected from Hattrick screenshots.

This is calibration/reverse-engineering work. The coefficients below are not claimed to be official Hattrick source code.

## 2. Ground-truth player inputs

From the CHPP snapshot used for the current calibration:

- Cristian Pesalovo: Def 16, PM 6, Pass 9, Winger 3, Scoring 6, Form 7, XP 6, Stamina 7.
- Dawid Nocoń: Def 17, PM 3, Pass 5, Winger 4, Scoring 7, Form 6, XP 12, Stamina 5.
- Abeiku Takyi: Def 17, PM 3, Pass 8, Winger 4, Scoring 7, Form 5, XP 10, Stamina 6.

Source snapshot: `hattrickai-team-players-2026-09-12T04-49-44-576Z.json`.

## 3. Empirical singleton observations

Observed displayed regional ratings for the three defender calibration players are quarter-step values. Important examples:

### Pesalovo
- R:  DEF-L 0 / DEF-C 2 / DEF-R 6, MID 1, ATT-R 1.25
- CR: 0 / 4 / 3.5, MID 1
- C: 2 / 4 / 2, MID 1
- CL: 3.5 / 4 / 0, MID 1
- L: 6 / 2 / 0, MID 1, ATT-L 1.25

### Nocoń
- R: 0 / 1.75 / 5.75, MID 1, ATT-R 1.25
- CR: 0 / 3.75 / 3.5, MID 1
- C: 2 / 3.75 / 2, MID 1
- CL: 3.5 / 3.75 / 0, MID 1
- L: 5.75 / 1.75 / 0, MID 1, ATT-L 1.5

### Takyi
- R: 0 / 1.75 / 5.75, MID 1, ATT-R 1.5
- CR: 0 / 3.75 / 3.25, MID 1
- C: 2 / 3.75 / 2, MID 1
- CL: 3.25 / 3.75 / 0, MID 1
- L: 5.75 / 1.75 / 0, MID 1, ATT-L 1.5

## 4. Additivity check

The cleanest three-defender observations are approximately additive:

- P-L + N-C + T-R => 7.75 / 7.00 / 7.50
- P-L + N-CL + T-R => 9.50 / 7.00 / 5.75
- P-L + N-CR + T-R => 6.00 / 7.00 / 9.25
- T-L + N-C + P-R => 7.50 / 7.00 / 7.75

The first three imply the Nocoń position shift C -> CL is roughly +1.75 left / -1.75 right, and C -> CR is roughly -1.75 left / +1.75 right. Residuals in other combinations are small (usually 0.25–0.50), so a mostly additive player-contribution ledger remains justified.

## 5. Important current-engine mismatch

`RegionalRatingEngineFixed` currently performs all of the following before/around the position coefficients:

1. Converts each skill with `max(0, skill - 1)`.
2. Applies loyalty as an additive skill term.
3. Applies a strong fixed-engine form multiplier (`FormFactor(form) / .756`).
4. Adds stamina-at-minute multiplier.
5. Adds experience as a separate sector contribution after positional contribution.
6. Applies position crowding.
7. Applies team/context calibration.

The current engine therefore has a stronger player-state effect than the older `RegionalRatingEngine`, which instead folds `experienceDelta = ExperienceBonus(XP) - 1.13` into the skill vector and uses the smaller researched form table.

## 6. First numerical test — normal central defender

Using the current `RegionalRatingEngineFixed` logic for a single normal central defender (no crowding, default match context, minute 0):

| Player | Current raw DEF-C | Observed DEF-C | Delta |
|---|---:|---:|---:|
| Pesalovo | ~3.933 | 4.00 | -0.067 |
| Nocoń | ~3.935 | 3.75 | +0.185 |
| Takyi | ~3.531 | 3.75 | -0.219 |

For the lateral defence component of a normal central defender:

| Player | Current raw side DEF | Observed side DEF | Delta |
|---|---:|---:|---:|
| Pesalovo | ~1.716 | 2.00 | -0.284 |
| Nocoń | ~1.751 | 2.00 | -0.249 |
| Takyi | ~1.574 | 2.00 | -0.426 |

These numbers show that the published positional coefficients alone are close enough to explain the shape, but the current state layer does not reproduce all three players consistently. In particular, Takyi's lower-form player state is under-predicted despite high defending, while Nocoń's high XP compensates much of the lower form.

## 7. Critical finding: display conversion is not the source of these screenshot values

The repository contains a separate nonlinear `HattrickRatingDisplayConverter` using a researched `pow(x, 1.2) / 4 + 1` transformation. However, the production `RegionalRatingEngineFixed` currently publishes `RegionalRatingEngine.Display(raw)`, which is only 2-decimal rounding/clamping. The empirical values collected here are also naturally quarter-step rating values rather than the output of the unused nonlinear converter.

Therefore the next calibration step should **not** blindly wire the nonlinear converter into M7. It must first be validated against a controlled screenshot series.

## 8. Deep-calculation conclusions so far

### High confidence
- Player contribution is approximately additive across players before contextual/team layers.
- Position and lateral behaviour are a first-order determinant of which sector receives the player's skills.
- The empirical sector values are quantized in quarter steps.
- The current `max(skill-1)` + separate XP layer is a model choice, not something yet proven by the empirical set.

### Medium confidence
- The existing Contribution research coefficients are directionally correct and produce the right positional pattern.
- Form and experience both affect the observed contribution, but their exact placement in the pipeline is still unresolved.

### Not yet solved
- Exact skill normalization (`skill`, `skill-1`, or another latent level mapping).
- Exact form multiplier curve.
- Exact experience interaction (separate sector bonus vs skill-vector uplift).
- Exact quarter-step rounding/quantization point.
- Whether the old researched nonlinear display converter is relevant to any layer of the live Hattrick display.

## 9. Next calibration target

Do **not** change the production coefficient table yet. The next step is to fit the player-state layer against the controlled singleton observations first, then re-run all 3-defender combinations.

Most valuable next evidence is a controlled screenshot set where the same player is tested in the same slot while only the player state differs (form/experience/skill profile). That will separate the state multiplier from the positional coefficients. Existing data is already sufficient to continue the numerical fitting pass without waiting for a new screenshot.
