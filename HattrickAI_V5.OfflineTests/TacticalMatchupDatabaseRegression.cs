using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class TacticalMatchupDatabaseRegression
{
    public static int Run()
    {
        var tactics = Enum.GetValues<TeamTactic>();
        var comparisons = new List<FormationTacticComparison>();
        for (var candidate = 0; candidate < 3; candidate++)
        {
            foreach (var tactic in tactics)
            {
                var win = 0.10 + (0.02 * (int)tactic) + (0.01 * candidate);
                var draw = 0.20;
                var loss = 1.0 - win - draw;
                comparisons.Add(new FormationTacticComparison(
                    "4-4-2", $"candidate-{candidate}", tactic, 0, 0, .5, 1, 1,
                    win, draw, loss, 1.2, 1.0, 5, AdvancedTactic.Normal)
                { TacticEligible = true, TacticFitScore = win });
            }
        }

        var db = TacticalMatchupDatabaseBuilder.Build(comparisons);
        Check(db.Count == comparisons.Count, "DB3 keeps every XI × tactic row");
        foreach (var candidate in comparisons.Select(x => x.CandidateId).Distinct(StringComparer.Ordinal))
            Check(db.ForCandidate(candidate).Count == tactics.Length, $"DB3 has seven tactics for {candidate}");

        var best = TacticalMatchupDatabaseBuilder.RankCandidateTactics(comparisons);
        Check(best.Count == 3, "DB3 produces one best tactic per XI");
        Check(best[0].ExpectedPoints >= best[1].ExpectedPoints, "DB3 best-tactic ranking is expected-points descending");
        Check(best.All(x => Math.Abs(x.ExpectedPoints - (3 * x.WinProbability + x.DrawProbability)) < 1e-12), "DB3 expected points are canonical");
        Console.WriteLine("PASS: DB3 tactical matchup database coverage + ranking");
        return 0;
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
}
