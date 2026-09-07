namespace HattrickAI.V5.Core;

/// <summary>
/// DB2'deki XI adaylarının aynı rakibe karşı yedi taktikle ürettiği M9 sonuçlarını
/// tek bir, deterministik ve kalıcı-model olmayan matchup tablosunda tutar.
/// </summary>
public sealed class TacticalMatchupDatabase
{
    private readonly List<TacticalMatchupRecord> _records = [];

    public IReadOnlyList<TacticalMatchupRecord> Records => _records
        .OrderBy(x => x.CandidateId, StringComparer.Ordinal)
        .ThenBy(x => x.Tactic)
        .ToList();

    public int Count => _records.Count;

    public void Add(TacticalMatchupRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!double.IsFinite(record.WinProbability) || !double.IsFinite(record.DrawProbability) || !double.IsFinite(record.LossProbability)) return;
        _records.RemoveAll(x => x.CandidateId == record.CandidateId && x.Tactic == record.Tactic);
        _records.Add(record);
    }

    public void AddRange(IEnumerable<TacticalMatchupRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        foreach (var record in records) Add(record);
    }

    public IReadOnlyList<TacticalMatchupRecord> ForCandidate(string candidateId) => Records
        .Where(x => x.CandidateId.Equals(candidateId, StringComparison.Ordinal))
        .ToList();

    public IReadOnlyList<TacticalMatchupRecord> BestByExpectedPoints() => Records
        .GroupBy(x => x.CandidateId, StringComparer.Ordinal)
        .Select(g => g.OrderByDescending(x => x.ExpectedPoints).ThenByDescending(x => x.WinProbability).ThenBy(x => x.Tactic).First())
        .OrderByDescending(x => x.ExpectedPoints)
        .ThenByDescending(x => x.WinProbability)
        .ThenBy(x => x.CandidateId, StringComparer.Ordinal)
        .ToList();
}

public sealed record TacticalMatchupRecord(
    string CandidateId,
    string Formation,
    TeamTactic Tactic,
    bool Eligible,
    double TacticFitScore,
    double SquadFit,
    double MatchupFit,
    double TradeoffCost,
    double TacticalScore,
    double ExpectedHomeGoals,
    double ExpectedAwayGoals,
    double WinProbability,
    double DrawProbability,
    double LossProbability,
    double ExpectedPoints,
    double ExpectedGoalDifference,
    string Explanation)
{
    public static TacticalMatchupRecord From(string candidateId, string formation, FormationTacticComparison comparison)
        => new(candidateId, formation, comparison.Tactic, comparison.TacticEligible, comparison.TacticFitScore,
            comparison.TacticSquadFit, comparison.TacticMatchupFit, comparison.TacticTradeoffCost,
            comparison.TacticalScore, comparison.ExpectedHomeGoals, comparison.ExpectedAwayGoals,
            comparison.WinProbability, comparison.DrawProbability, comparison.LossProbability,
            comparison.ExpectedPoints, comparison.ExpectedGoalDifference, comparison.TacticExplanation);
}
