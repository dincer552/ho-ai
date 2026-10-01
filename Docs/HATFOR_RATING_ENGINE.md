# HatFor rating engine

HatFor is the single production regional-rating engine in HattrickAI V5. Legacy engine names are accepted only as input aliases and resolve to HatFor.

## Calculation

For each occupied slot, the engine applies that formation's sector/slot weights to player skills, player order, form, experience, and loyalty. The resulting sector HAM is scaled and converted to quarter-star ratings.

The implementation includes these player transforms:

- Effective skill: `max(0, skill - 1) + loyalty`
- Form: `0.378 * sqrt(min(7, max(0, form - 1)))`
- Experience: a fourth-degree polynomial in `max(0, experience - 1)`, scaled by `0.345`
- Rating: `round(((HAM * coachModifier * sectorScale)^1.2 / 4 + 1) * 4) / 4`

For exact constants and routing, `HatForRatingEngine.cs` and `HatForCoefficientTables.cs` are authoritative.

## Formation tables

Dedicated slot/sector coefficient tables are defined for 4-4-2, 3-5-2, 4-3-3, 4-5-1, 5-4-1, 5-3-2, 3-4-3, 3-4-3-2B, 5-5-0, 2-5-3, and 2-5-3-2B. The engine reports an error for unsupported formations rather than selecting a different production engine.

## Production routing

The registry, analysis scenario, Motor 2/Motor 3 rating helpers, manual/custom endpoints, and calculation trace route to HatFor. `RegionalRatingEngineFixed`, HO, Dash, Foxtrick, and the Stage 2 display converter are not production regional-rating paths. The web selector returns HatFor only.

Legacy values `V5`, `HO`, `HattrickDash`, and `Foxtrick` are accepted by the parser for compatibility and map to HatFor.

## Calibration note

The 11 tables being present does not prove exact Excel parity. Treat the coefficients as implementation inputs until each formation is checked against a controlled Excel/Hattrick reference fixture.