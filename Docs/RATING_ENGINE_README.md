# HattrickAI V5 ? HatFor rating engine

## Current production behavior

HatFor is the only production regional-rating engine. Analysis scenarios, opponent-aware lineup refinement, behaviour/order evaluation, manual/custom lineup APIs, and rating-calculation JSON all use the HatFor implementation. The API/UI selector returns HatFor only.

Legacy engine labels (`V5`, `HO`, `HattrickDash`, `Foxtrick`) are compatibility inputs and are normalized to HatFor. They are not separate production calculations. Legacy formula implementations and Stage 2 converters may remain for historical regression/research, but production code must not call them for regional ratings.

## Implementation

- `HatForRatingEngine` calculates seven sectors with the selected formation-specific slot tables.
- `HatForCoefficientTables` contains dedicated tables for 11 supported formations: 4-4-2, 3-5-2, 4-3-3, 4-5-1, 5-4-1, 5-3-2, 3-4-3, 3-4-3-2B, 5-5-0, 2-5-3, and 2-5-3-2B.
- Player order, skills, form, experience, and loyalty feed the HatFor calculation and contribution trace.
- The HatFor snapshot is already in its native display scale; do not pass it through `HattrickRatingDisplayConverter`.

## Production entry points

`RatingEngineRegistry` registers HatFor only. `RegionalRatingEngineFinal` adapts both canonical lineups and regional-player lists to HatFor. The M7 scenario engine uses the registry; Motor 2 and Motor 3 use the final HatFor adapter. `AnalysisService` uses `HatForRatingEngine.CalculateLineupWithTrace` for the downloadable trace.

Relevant APIs include `/api/v5/rating-engines`, `/api/v5/rating-engine/selection`, `/api/v5/rating-engine/selected`, `/api/v5/rating-engines/compare`, `/api/v5/rating-engine/manual`, and `/api/v5/rating-engine/custom`. The selector and comparison surface expose only HatFor.

## Calibration status

All 11 formation tables are present in code. Their presence alone does not establish exact Excel parity. Validate coefficient values against controlled Excel/Hattrick fixtures before claiming exactness; retain each fixture and expected output as a regression.

## Validation commands

- `dotnet run --project HattrickAI_V5.OfflineTests/HattrickAI_V5.OfflineTests.csproj -- rating-contract`
- `dotnet run --project HattrickAI_V5.OfflineTests/HattrickAI_V5.OfflineTests.csproj -- rating-validation`
- `dotnet publish HattrickAI_V5/HattrickAI.V5.csproj -c Release`