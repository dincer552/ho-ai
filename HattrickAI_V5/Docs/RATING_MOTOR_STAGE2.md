# Stage 2 display conversion ? legacy/research only

The Stage 2 nonlinear raw-to-display converter is retained for historical regression and research. It is not part of the current production rating path.

Production ratings use `HatForRatingEngine` / `RegionalRatingEngineFinal`, whose snapshots already contain HatFor-native quarter-star display values. Applying `HattrickRatingDisplayConverter` to those results would corrupt the displayed ratings.

The M7 scenario path uses the HatFor-only registry. Motor 2 opponent-aware refinement, Motor 3 behaviour/order evaluation, and the final rating trace also use HatFor. Do not wire `Stage2RegionalRatingEngineFixed` or `RegionalRatingEngineFixed` back into production.