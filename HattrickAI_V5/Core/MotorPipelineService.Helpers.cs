using System.Collections.Concurrent;
using System.Diagnostics;

namespace HattrickAI.V5.Core;

public sealed partial class MotorPipelineService
{
    private List<FormationTacticComparison> EvaluateFormationTactics(
        IReadOnlyList<CandidateEvaluationRecord> db2,
        IReadOnlyList<string> legalFormations,
        IReadOnlyList<Player> players,
        MatchDataContext context,
        string? runId,
        CancellationToken ct)
    {
        var tactics = Enum.GetValues<TeamTactic>();
        var results = new List<FormationTacticComparison>(db2.Count * tactics.Length);
        foreach (var candidate in db2)
        {
            ct.ThrowIfCancellationRequested();
            if (!legalFormations.Contains(candidate.Formation, StringComparer.Ordinal)) continue;

            var baseline = EvaluateForComparison(candidate.Lineup, players, context, TeamTactic.Normal);
            var baselineView = new ComparisonEvaluationView(baseline.Chance, baseline.Advanced, baseline.Prediction);

            foreach (var tactic in tactics)
            {
                ct.ThrowIfCancellationRequested();
                var evaluation = EvaluateForComparison(candidate.Lineup, players, context, tactic);
                var prediction = evaluation.Prediction;
                var view = new ComparisonEvaluationView(evaluation.Chance, evaluation.Advanced, evaluation.Prediction);
                var fit = TacticObjectiveEngine.Evaluate(candidate.Lineup, tactic, baselineView, view, players, context.Opponent.Players);

                results.Add(new FormationTacticComparison(
                    candidate.Formation,
                    candidate.CandidateId,
                    tactic,
                    evaluation.Tactical.TacticalScore,
                    evaluation.Chance.StructuralChanceIndex,
                    evaluation.Chance.MidfieldShare,
                    evaluation.Chance.OwnRegularChanceExpected,
                    evaluation.Chance.OpponentRegularChanceExpected,
                    prediction.WinProbability,
                    prediction.DrawProbability,
                    prediction.LossProbability,
                    prediction.ExpectedHomeGoals,
                    prediction.ExpectedAwayGoals,
                    prediction.ExpectedPoints,
                    fit.Score,
                    fit.Reason));
            }
        }
        return results;
    }

    private ComparisonEvaluation EvaluateForComparison(Lineup lineup, IReadOnlyList<Player> players, MatchDataContext context, TeamTactic tactic)
    {
        var evaluation = Evaluate(lineup, null, context.RatingContext.Attitude, false, tactic);
        var prediction = _m9.Predict(evaluation.Tactical, evaluation.Chance, context.Opponent.Rating, context.RatingContext.MatchLocation, players, context.Opponent.LastMatchLineup, context.Opponent.Players);
        return new ComparisonEvaluation(evaluation.Tactical, evaluation.Scenario, evaluation.Advanced, evaluation.Chance, prediction.Prediction);
    }

    private static IReadOnlyList<MatchEventGoals> selectedM9ResultOpponentEvents(MatchPrediction prediction)
        => prediction.OpponentEventGoals ?? [];

    private static string FormatFormationCounts(IEnumerable<CandidateEvaluationRecord> records) => string.Join(", ", records.GroupBy(x => x.Formation).OrderByDescending(g => g.Count()).Select(g => $"{g.Key}:{g.Count()}"));
    private static PositionAssignmentCandidate ToPositionCandidate(Lineup lineup, string formation, double rankingScore) => new(lineup, formation, rankingScore, rankingScore);
    private static double ComputeM9OwnChanceShare(M8ChanceResult chance) => Math.Clamp(chance.OwnRegularChanceExpected / Math.Max(0.01, chance.OwnRegularChanceExpected + chance.OpponentRegularChanceExpected), 0, 1);
    private static double Share(double own, double opponent) { var ownSafe = Math.Max(0, own); var oppSafe = Math.Max(0, opponent); return ownSafe / Math.Max(0.01, ownSafe + oppSafe); }
    private static double ComputeM9OwnLeft(RegionalRatingSnapshot own, RegionalRatingSnapshot opponent) => Share(own.LeftAttack, opponent.RightDefence);
    private static double ComputeM9OwnCentre(RegionalRatingSnapshot own, RegionalRatingSnapshot opponent) => Share(own.CentralAttack, opponent.CentralDefence);
    private static double ComputeM9OwnRight(RegionalRatingSnapshot own, RegionalRatingSnapshot opponent) => Share(own.RightAttack, opponent.LeftDefence);
    private static double ComputeM9OpponentLeft(RegionalRatingSnapshot own, RegionalRatingSnapshot opponent) => Share(opponent.LeftAttack, own.RightDefence);
    private static double ComputeM9OpponentCentre(RegionalRatingSnapshot own, RegionalRatingSnapshot opponent) => Share(opponent.CentralAttack, own.CentralDefence);
    private static double ComputeM9OpponentRight(RegionalRatingSnapshot own, RegionalRatingSnapshot opponent) => Share(opponent.RightAttack, own.LeftDefence);
    private static double WeightedAttackQuality(double left, double centre, double right, double leftW, double centreW, double rightW) => (left * leftW + centre * centreW + right * rightW) / Math.Max(0.01, leftW + centreW + rightW);
    private static double ComputeM9OwnAttackQuality(RegionalRatingSnapshot own, RegionalRatingSnapshot opponent, M8ChanceResult chance) => WeightedAttackQuality(Share(own.LeftAttack, opponent.RightDefence), Share(own.CentralAttack, opponent.CentralDefence), Share(own.RightAttack, opponent.LeftDefence), 1, 1.2, 1);
    private static double ComputeM9OpponentAttackQuality(RegionalRatingSnapshot own, RegionalRatingSnapshot opponent, M8ChanceResult chance) => WeightedAttackQuality(Share(opponent.LeftAttack, own.RightDefence), Share(opponent.CentralAttack, own.CentralDefence), Share(opponent.RightAttack, own.LeftDefence), 1, 1.2, 1);
    private static void LogStart(string? runId, string motor, string message) { if (!string.IsNullOrWhiteSpace(runId)) MotorRunLogStore.StartMotor(runId, motor, message); }
    private static void LogComplete(string? runId, string motor, string message, long durationMs = 0, int? count = null) { if (!string.IsNullOrWhiteSpace(runId)) MotorRunLogStore.CompleteMotor(runId, motor, message, durationMs, count); }
    private static void LogFail(string? runId, string motor, string message, long durationMs = 0) { if (!string.IsNullOrWhiteSpace(runId)) MotorRunLogStore.FailMotor(runId, motor, message, durationMs); }
    private static MatchupEvaluation BuildMatchup(RegionalRatingSnapshot own, RegionalRatingSnapshot opponent, M8ChanceResult chance) { double signed(double v) => (v * 2.0) - 1.0; var midfield = signed(chance.MidfieldShare); var left = signed(chance.LeftAttackVsRightDefence); var centre = signed(chance.CentreAttackVsCentreDefence); var right = signed(chance.RightAttackVsLeftDefence); var leftDef = signed(Share(own.LeftDefence, opponent.RightAttack)); var centreDef = signed(Share(own.CentralDefence, opponent.CentralAttack)); var rightDef = signed(Share(own.RightDefence, opponent.LeftAttack)); var overall = (midfield + left + centre + right + leftDef + centreDef + rightDef) / 7.0; return new MatchupEvaluation(midfield, left, centre, right, leftDef, centreDef, rightDef, overall); }
    private static double Average(RegionalRatingSnapshot r) => (r.LeftDefence + r.CentralDefence + r.RightDefence + r.Midfield + r.LeftAttack + r.CentralAttack + r.RightAttack) / 7.0;
    private static double TeamSpiritValue(TeamSpiritLevel level) => level switch { TeamSpiritLevel.Murderous => 1, TeamSpiritLevel.Furious => 2, TeamSpiritLevel.Irritated => 3, TeamSpiritLevel.Composed => 4.5, TeamSpiritLevel.Calm => 5, TeamSpiritLevel.Content => 6, TeamSpiritLevel.Satisfied => 7, TeamSpiritLevel.Delirious => 8, TeamSpiritLevel.WalkingOnClouds => 9, TeamSpiritLevel.ParadiseOnEarth => 10, _ => 4.5 };
    private static string Signature(Lineup lineup) => string.Join(";", lineup.Slots.OrderBy(s => s.Code, StringComparer.Ordinal).ThenBy(s => s.PlayerId).Select(s => $"{s.Code}:{s.PlayerId}:{(int)s.Order}"));
    private sealed record CandidateEvaluation(TacticalCandidate Tactical, RatingScenarioResult Scenario, AdvancedTacticalScenarioResult Advanced, M8ChanceResult Chance);
}

public sealed record MotorPipelineResult(PlayerAnalysisResult M3, FormationCandidateSet M4, IReadOnlyList<PositionAssignmentCandidate> M5, M6OptimizationResult M6, RatingScenarioResult M7, AdvancedTacticalScenarioResult M72, M8ChanceResult M8, M9PredictionResult M9, M10DecisionResult M10, FinalMatchPlan FinalPlan, MatchPrediction FinalPrediction)
{
    public M11DecisionResult? M11 { get; init; }
    public int CandidateDatabase1Count { get; init; }
    public int CandidateDatabase2Count { get; init; }
    public IReadOnlyList<CandidateEvaluationRecord> CandidateDatabase1 { get; init; } = [];
    public IReadOnlyList<CandidateEvaluationRecord> CandidateDatabase2 { get; init; } = [];
    public TeamAttitude SelectedMatchApproach { get; init; }
    public IReadOnlyDictionary<string, M6FormationSearchBudget> M6BFormationBudgets { get; init; } = new Dictionary<string, M6FormationSearchBudget>(StringComparer.Ordinal);
    public IReadOnlyList<FormationTacticComparison> TacticComparisons { get; init; } = [];
}
