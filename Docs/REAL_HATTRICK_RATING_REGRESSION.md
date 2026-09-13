# Real Hattrick Rating Regression

## Fixture

`TestJSON/RealHattrickMatch_769648184_343_2026-09-13.json`

Source:
- Hattrick match-order screenshot supplied by the user
- CHPP player export dated 2026-09-12

Formation: `3-4-3`
Tactic: `Normal`

### Hattrick UI baseline

`Left Defence / Central Defence / Right Defence / Midfield / Left Attack / Central Attack / Right Attack`

`13 / 12.75 / 13.25 / 7 / 15.75 / 13.75 / 13.5`

This is treated as an external calibration observation. It is not presented as a hidden Hattrick formula.

## Four-engine offline result

| Engine | Left D | Centre D | Right D | Midfield | Left A | Centre A | Right A | MAE vs Hattrick |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| V5 | 3.27 | 3.90 | 3.70 | 1.79 | 3.14 | 3.24 | 3.19 | 9.5386 |
| HO | 8.68839942 | 14.2271002 | 9.69051866 | 5.44673275 | 9.1645407 | 12.05888469 | 9.09550333 | 3.3689 |
| HattrickDash | 13.46666667 | 13.46666667 | 13.46666667 | 12.525 | 11.63333333 | 11.63333333 | 11.63333333 | 2.1464 |
| Foxtrick | 7.55 | 15.39 | 8.72 | 8.36 | 9.74 | 12.14 | 9.93 | 3.5957 |

## Acceptance rules

1. All four engines must accept the same canonical 11-player lineup.
2. All seven sector values must be finite.
3. Each engine's output is deterministic on repeated execution.
4. Each engine has a locked offline baseline in the fixture.
5. Hattrick UI values remain a separate external calibration baseline.
6. No engine is silently forced to equal another engine or the Hattrick UI observation.

## Important finding

This real lineup demonstrates a large gap between the current V5 regional rating implementation and the observed Hattrick UI rating. The test intentionally **does not hide or normalize that gap**. It records it so future calibration work can improve the model against real observations without changing the existing production V5 pipeline until a separate, tested calibration stage is approved.

The HO adapter also now accepts canonical side-labelled defender slots (`DEF-L/DEF-C/DEF-R`) for a `3-4-3` legacy HO central-defender role set. This is an adapter compatibility fix only; V5 coefficients and the M3→M11 pipeline are untouched.
