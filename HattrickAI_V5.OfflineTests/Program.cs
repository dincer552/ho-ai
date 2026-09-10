using HattrickAI.V5.Core;
using HattrickAI.V5.OfflineTests;

if (args.Length > 0 && string.Equals(args[0], "write3", StringComparison.OrdinalIgnoreCase))
    return ChppWrite03ValidationRegression.Run();
if (args.Length > 0 && string.Equals(args[0], "write4", StringComparison.OrdinalIgnoreCase))
    return ChppWrite04MockWriteRegression.Run();

var startFrom = args.Length > 0 && args[0].StartsWith("c", StringComparison.OrdinalIgnoreCase) ? args[0].ToLowerInvariant() : "c1";
var path = args.Length > 1
    ? args[1]
    : args.Length > 0 && !args[0].StartsWith("c", StringComparison.OrdinalIgnoreCase)
        ? args[0]
        : startFrom == "c24"
            ? "TestJSON/TacticalOutcomeCalibrationCorpus_2026-09-07.json"
            : "TestJSON/HattrickAI_V5_CHPP_FullOffline_2026-09-01.json";
var startNumber = startFrom.Length > 1 && int.TryParse(startFrom[1..], out var parsed) ? parsed : 1;
if (startNumber < 1 || startNumber > 24) throw new ArgumentException($"Geçersiz acceptance başlangıcı: {startFrom}. c1-c24 kullanın.");

bool From(int c) => startNumber == c;

if (From(1)) { var r = WebInputIntegrityRegression.Run(); if (r != 0) return r; r = CoreWebParityRegression.Run(); if (r != 0) return r; r = await M3M11EndToEndRegression.RunAsync(path); if (r != 0) return r; }
if (From(2)) { var r = M4LegalFormationRegression.Run(); if (r != 0) return r; }
if (From(3)) { var r = HistoricalCalibrationRegression.Run(); if (r != 0) return r; r = SetPieceTakerCalibrationRegression.Run(); if (r != 0) return r; r = SpecialtyInteractionRegression.Run(); if (r != 0) return r; r = TacticPaperMappingRegression.Run(); if (r != 0) return r; r = PressingWeightRegression.Run(); if (r != 0) return r; r = LongShotOpportunityRegression.Run(); if (r != 0) return r; r = M9EventGoalRegression.Run(); if (r != 0) return r; r = await M5XICandidatesRegression.RunAsync(path); if (r != 0) return r; }
if (From(4)) { var r = await M6ACandidateEvaluationRegression.RunAsync(path); if (r != 0) return r; }
if (From(5)) { var r = await M7RegionalRatingRegression.RunAsync(path); if (r != 0) return r; }
if (From(6)) { var r = await M7_2TacticalScenarioRegression.RunAsync(path); if (r != 0) return r; }
if (From(7)) { var r = await M8ChanceModelRegression.RunAsync(path); if (r != 0) return r; }
if (From(8)) { var r = await M9PredictionRegression.RunAsync(path); if (r != 0) return r; }
if (From(9)) { var r = await DB1FormationCoverageRegression.RunAsync(path); if (r != 0) return r; }
if (From(10)) { var r = await M10FormationCompetitionRegression.RunAsync(path); if (r != 0) return r; }
if (From(11)) { var r = await M10ToM6BRankDrivenHandoffRegression.RunAsync(path); if (r != 0) return r; }
if (From(12)) { var r = await M6BRefinementRegression.RunAsync(path); if (r != 0) return r; }
if (From(13)) { var r = await DB2FormationCoverageRegression.RunAsync(path); if (r != 0) return r; }
if (From(14)) { var r = await M11FinalistPoolRegression.RunAsync(path); if (r != 0) return r; }
if (From(15)) { var r = await M11FinalSelectionRegression.RunAsync(path); if (r != 0) return r; }
if (From(16)) { var r = await FinalPlanContinuityRegression.RunAsync(path); if (r != 0) return r; }
if (From(17)) { var r = await FinalPredictionContinuityRegression.RunAsync(path); if (r != 0) return r; }
if (From(18)) { var r = await DeterministicRerunRegression.RunAsync(path); if (r != 0) return r; r = HistoricalMultiMatchProductionAcceptance.Run(path); if (r != 0) return r; r = await FullPipelineRegressionRunner.RunAsync(path); if (r != 0) return r; }
if (From(19)) { var r = C19MotorDatabaseJsonRegression.Run(); if (r != 0) return r; r = TacticalMatchupDatabaseRegression.Run(); if (r != 0) return r; }
if (From(20)) { var r = C20MotorDatabaseDeterministicJsonRegression.Run(); if (r != 0) return r; }
if (From(21)) return await C21TacticalMatchupMatrixRegression.RunAsync(path);
if (From(22)) { var r = C22TacticalOutcomeEdgeCaseRegression.Run(); if (r != 0) return r; }
if (From(23)) { var r = C23TacticalOutcomeCalibrationRegression.Run(); if (r != 0) return r; }
if (From(24)) return C24TacticalOutcomeCalibrationCorpusRegression.Run(path);
return 0;
