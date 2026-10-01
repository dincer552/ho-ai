# HatFor rating engine ? production status

## Production contract

HatFor is the only production regional-rating engine. The API registry and selector expose HatFor only. Legacy selector names (`V5`, `HO`, `HattrickDash`, `Foxtrick`) are accepted for compatibility and normalize to HatFor; they do not select independent production engines.

The production calculation path is:

- M7 scenario evaluation through `RegionalRatingScenarioEngine` and `RatingEngineRegistry` ? `HatForRatingEngine`.
- Motor 2 opponent-aware refinement and Motor 3 behaviour evaluation through `RegionalRatingEngineFinal` ? HatFor.
- Final lineup rating-calculation trace and JSON export through `HatForRatingEngine.CalculateLineupWithTrace`.
- Manual/custom lineup endpoints through `HatForRatingEngine`.

The Stage 2 nonlinear display converter and `RegionalRatingEngineFixed` remain legacy/research code and are not used by production rating paths.

## Formation coverage and calibration

`HatForCoefficientTables` defines distinct sector/slot tables for 11 formations: 4-4-2, 3-5-2, 4-3-3, 4-5-1, 5-4-1, 5-3-2, 3-4-3, 3-4-3-2B, 5-5-0, 2-5-3, and 2-5-3-2B. Each calculation uses the selected formation, position, player order, skills, form, experience, and loyalty.

Table presence is not proof of exact Excel parity. Numerical acceptance requires comparable Excel/Hattrick fixtures for each formation; do not describe unvalidated coefficients as exact official Hattrick formulas.

## Validation

`rating-contract` verifies the single-engine registry, legacy-name normalization, and registry-to-HatFor parity. `rating-validation` verifies finite HatFor results, native display scale, and neutral confidence behavior against a real CHPP fixture. The production workflow separately verifies JavaScript syntax, Docker publish, GHCR image push, VM health, and homepage response.