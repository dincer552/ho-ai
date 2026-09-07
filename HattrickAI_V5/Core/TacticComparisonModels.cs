namespace HattrickAI.V5.Core;

/// <summary>
/// All supported team tactics evaluated against one representative XI of a legal formation.
/// This is an inspection/diagnostic result; it does not override M10/M11 selection.
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
    AdvancedTactic AdvancedTactic);
