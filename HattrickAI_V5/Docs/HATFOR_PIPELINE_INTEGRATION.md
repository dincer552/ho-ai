# HatFor Pipeline Integration

## Production rating path

- `RegionalRatingScenarioEngine` (M7) → `RatingEngineRegistry` → **HatFor only**
- `RegionalRatingEngineFinal` → HatFor
- `RatingEngineSelectionContext` default: `RatingEngineKind.HatFor`

## Per-formation formulas

`HatForCoefficientTables.ForFormation` dispatches to independent tables:
4-4-2, 3-5-2, 4-3-3, 4-5-1, 5-4-1, 5-3-2, 3-4-3, 3-4-3-2B, 5-5-0, 2-5-3, 2-5-3-2B.

## Order (emir) search

After M11 formation/XI selection, `OrderNeighborhoodSearch` runs a cheap
single-slot + second-pass neighborhood over legal `PlayerOrder` values.
Candidates are scored with HatFor sector ratings (and optional opponent matchup).
The winning order set is applied to the final lineup before M9 re-evaluation.

## Cache

`HatForRatingEngine` caches by `(formation, coachMod, slot:playerId:order)`.
Clear via `HatForRatingEngine.ClearCache()` in tests.

## Decision metric (unchanged)

`ExpectedPoints = 3W + D` remains the canonical M10/M11/DB3 objective.
