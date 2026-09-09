# Hattrick AI V5 Change History

## 2026-09-09 — Tactical audit continuation

### Counter Attack audit — implementation/research layer completed

- `CounterAttackTacticEvaluator` remains a dedicated, opponent-aware evaluator rather than a generic TacticalScore multiplier.
- CA eligibility is tied to losing midfield before the documented 7% CA midfield penalty; the 7% penalty is applied by M8 to the effective midfield/possession calculation.
- The evaluator separates tactical CA conversion from non-tactical Technical-player CA contribution so the two mechanisms are not treated as the same event source.
- CA suitability considers defending + passing requirements, experience, tactical CA opportunity volume, own regular-chance opportunity cost, opponent attack quality, midfield loss, shooter/finishing quality, and opponent specialty interaction.
- The 2026 Constantinou et al. paper's CA conversion equation and published 4%-45% tactical conversion envelope are now represented in the tactic-specific paper bridge rather than silently using the generic RT=V5×2 mapping.

### Tactic-specific paper RT bridge — implemented

The paper provides tactic-specific conversion curves but does not publish a mapping from V5's compact 0-10 internal tactical scale to the paper's RT scale. V5 therefore uses explicit calibration anchors at the published conversion envelopes and labels these as V5 bridges, not official hidden-engine formulas:

- AiM: 20%-35%
- AoW: 34%-52%
- Counter Attack: 4%-45%
- Pressing: 5%-41%
- Long Shots: existing RT=0..20 bridge retained because its documented paper equation over that RT range produces the current 7.52%-22.76% curve; the paper's wider 6%-43% interval is treated as an empirical range rather than an endpoint claim for this compact V5 scale.

`TacticPaperMappingRegression` now checks the tactic-specific conversion envelopes, monotonicity, and AiM/AoW sector-share conservation envelopes.

### Current next target

`AIM-01` is now the active tactic audit. The next pass compares `AttackMiddleTacticEvaluator` against the verified AiM source chain, M8 redistribution, opponent central defence, wing-defence risk, and Normal opportunity cost before moving to `AOW-01`.

---

## 2026-09-06 — Stage 9: Motor Output JSON Database

### Stage 9.9 — Documentation / Publication Snapshot

The Stage 9 documentation snapshot was completed without falsely replacing the existing A8 publication.

- The 05.09.2026 A8 technical manual remains the 208-page base PDF.
- A dated Stage 9 publication supplement was generated: `HattrickAI_V5_Teknik_Manuel_A9_SNAPSHOT_2026-09-06.pdf`.
- `TECHNICAL_MANUAL_INDEX.md` now records the A8 base snapshot and the 06.09.2026 A9 supplement separately.
- `README.md` records the exact publication filenames and snapshot dates.
- The Stage 9 supplement documents JSON schema, archive persistence, API access, Motor Panel behavior, C19 regression and C20 deterministic JSON validation.

The A9 supplement is intentionally a dated supplement rather than a claim that the older A8 PDF contains post-05.09.2026 changes.

### C20 — Deterministic Motor DB JSON Regression

C20 was added and passed in the V5 acceptance workflow.

The regression writes the same stable `Analysis` + `MotorPipelineResult` payload twice through the real `MotorResultArchive` writer and compares the normalized JSON payload while excluding only the volatile `savedAt` and `runId` metadata fields.

Verified by C20:

- deterministic motor-database payload across repeated archive writes;
- `savedAt` remains per-write metadata;
- `runId` remains the supplied run identifier;
- `build` remains stable;
- schema version remains `hattrickai-v5-motor-database-v1`.

This verifies determinism of the JSON archive representation, not a new motor calculation or tactic-selection feature.

### C19 — Motor DB JSON Regression

C19 passed and verifies the archive writer, schema, latest snapshot, run lookup and archive list behavior. The successful C19 workflow also completed Docker build and Azure deployment.

### Stage 9 documentation rule

The Stage 9 JSON layer stores existing production outputs. It does not invent missing motor results, add a tactic selector, or relabel `TeamTactic.Normal` as an optimized tactic.

### 9.6 live web validation boundary

The planned "real web JSON validation" was not represented as completed without a direct live endpoint test record. It remains a separately identifiable follow-up validation item; Stage 9 publication documentation does not claim that test was performed.

---

## 2026-09-05

### Project Memory System Added

Added permanent documentation files:

- PROJECT_MEMORY.md
- ENGINE_MAP.md
- CHANGE_HISTORY.md

Purpose:

Keep development decisions, engine mappings, and investigation results inside the repository.

---

## 2026-09-05 — Stage 1: System Architecture

Created:

- `HattrickAI_V5/Docs/SYSTEM_ARCHITECTURE.md`

The document records the verified production flow from `AnalysisService` and CHPP input through `MatchDataContext`, M3-M11, Candidate DB #1/#2, `FinalPlan`, `FinalPrediction` and frontend response.

Verified architectural boundaries include:

- M3 = player suitability profiles.
- M4 = legal/feasible formation candidates.
- M5 = player-slot optimization.
- M6 = formation-aware behaviour search and downstream evaluation.
- M7 = regional rating scenario.
- M7.2 = advanced tactical scenario based on supplied tactic.
- M8 = chance/matchup calculation based on supplied tactical state.
- M9 = match prediction.
- M10 = formation competition/final decision and TeamAttitude handling.
- M6-B = M10-rank-driven refinement.
- M11 = final selection from DB2 finalists.

The architecture document deliberately leaves unverified calculation details for later source inspection.

---

## 2026-09-05 — Tactical Display Investigation

### Verified finding

The current web production analysis path does not contain a team-tactic selector.

`HattrickAI_V5/Core/AnalysisService.cs` creates the `RatingContext` with `TeamTactic.Normal`.

`HattrickAI_V5/Core/MotorPipelineService.cs` carries that tactic into `MatchState` and downstream motor calculations.

`HattrickAI_V5/Core/AdvancedTacticalScenarioEngine.cs` consumes the supplied tactic, maps it to `AdvancedTactic`, calculates tactic skill and tactical effects, and returns a scenario. It does not choose the tactic.

`HattrickAI_V5/Core/M8ChanceAllocationEngine.cs` consumes the tactic to calculate conversion and chance-distribution effects. It does not choose the tactic.

`HattrickAI_V5/Core/M10FinalDecisionEngine.cs` selects a final formation/plan and can select `TeamAttitude`; this is separate from `TeamTactic`.

### UI rule

Do not display `ORTADAN ATAK`, `KANATTAN ATAK`, `KONTRA ATAK`, etc. as a calculated engine decision unless an actual selector is added to the production pipeline.

For the current web path, the truthful UI semantic is `TAKTİK YOK`; the underlying supplied value is `TeamTactic.Normal`.

---

## 2026-09-05 — Stage 5: Real Match Example Analysis

Source fixture:

`TestJSON/HattrickAI_V5_CHPP_FullOffline_2026-09-01.json`

Fixture SHA:

`540a63d381defbf49d4d89553370b90114bf4815`

The fixture was used to create `HattrickAI_V5/Docs/REAL_MATCH_ANALYSIS.md`.

Documented facts include:

- future match `769648177`: Zeytinburnu Sahil Spor vs S4MSUNFC, 2026-09-06 15:00 UTC;
- opponent historical reference match `769648173`: bombacı mülayim spor 3-2 Zeytinburnu Sahil Spor;
- questionnaire values Coach=0, TeamSpirit=3, MatchImportance=0;
- M3 player-analysis examples and eligibility;
- M4/M5 generated `3-5-2` S4MSUNFC XI;
- M7 own/opponent regional ratings;
- opponent threat/opportunity values;
- tactical-selector limitation.

The fixture's stored `v5Analysis` does not provide complete standalone M8/M9/M10/M6-B/DB2/M11 final-result objects. These missing values were intentionally not reconstructed or invented.

---

## Known Acceptance / Regression Documentation Risks

- C13 previously compared exposed DB2 count against production SecondPass count incorrectly. The production pipeline exposes a formation-diversified DB2 subset, so exposed count and production DB2 count are not required to be equal.
- C17 has pipeline/telemetry continuity checks; successful completion must be verified from an actual acceptance run before being documented as fully passing.
- C18 deterministic rerun regression exists and is invoked after C17; its current pass status must be verified from an actual run before being documented as green.

---

## Documentation Policy

- Record only behavior verified from repository code, tests, configuration or reference documents.
- Separate implemented behavior from reference formulas and planned behavior.
- Record missing selectors, incomplete telemetry, acceptance mismatches and other risks explicitly rather than filling the gap with assumptions.
