namespace HattrickAI.V5.Core;

public static class TacticalMatchupDatabaseBuilder
{
    /// <summary>
    /// Converts the canonical one-XI × seven-tactic comparison rows into DB3.
    /// No re-evaluation occurs here, so DB3 cannot silently diverge from M8/M9 output.
    /// </summary>
    public static TacticalMatchupDatabase Build(IEnumerable<FormationTacticComparison> comparisons)
    {
        ArgumentNullException.ThrowIfNull(comparisons);
        var db = new TacticalMatchupDatabase();
        foreach (var group in comparisons.GroupBy(x => x.CandidateId, StringComparer.Ordinal))
        {
            foreach (var comparison in group.OrderBy(x => x.Tactic))
                db.Add(TacticalMatchupRecord.From(comparison.CandidateId, comparison.Formation, comparison));
        }
        return db;
    }

    public static IReadOnlyList<TacticalMatchupRecord> RankCandidateTactics(
        IEnumerable<FormationTacticComparison> comparisons)
        => Build(comparisons).BestByExpectedPoints();
}
