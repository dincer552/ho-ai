namespace HattrickAI.V5.Core;

/// <summary>
/// One XI x tactic evaluation. Each tactic is scored by its own objective and squad-fit model;
/// TacticalScore remains available as a diagnostic signal and is not the tactic selector.
/// </summary>
public sealed record FormationTacticComparison(
    string Formation,
    string CandidateId,
    TeamTactic Tactic,
    double TacticalScore,
    double StructuralChanceIndex,
    double MidfieldShare,
    double OwnRegularChanceExpected,
    double OpponentRegularChanceExpected,
    double WinProbability,
    double DrawProbability,
    double LossProbability,
    double ExpectedHomeGoals,
    double ExpectedAwayGoals,
    double TacticalLevel,
    AdvancedTactic AdvancedTactic)
{
    public double TacticFitScore { get; init; }
    public double TacticPrimaryMetric { get; init; }
    public double TacticTradeoffCost { get; init; }
    public double TacticSquadFit { get; init; }
    public double TacticMatchupFit { get; init; }
    public bool TacticEligible { get; init; }
    public string TacticExplanation { get; init; } = string.Empty;
    public double ExpectedPoints => 3.0 * WinProbability + DrawProbability;
    public double ExpectedGoalDifference => ExpectedHomeGoals - ExpectedAwayGoals;
}
