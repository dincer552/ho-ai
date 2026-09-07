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

        // T5 contract: outcome must outrank raw suitability. The higher-fit row
        // deliberately loses on ExpectedPoints and therefore must not be selected.
        var selectorDb = new TacticalMatchupDatabase();
        selectorDb.Add(new TacticalMatchupRecord(
            "winner", "4-4-2", TeamTactic.Creative, true, 0.99, 0.9, 0.9, 0.1, 0.9,
            1.1, 0.9, 0.40, 0.20, 0.40, 1.40, 0.20, "higher fit / lower outcome"));
        selectorDb.Add(new TacticalMatchupRecord(
            "outcome", "3-5-2", TeamTactic.Pressing, true, 0.40, 0.5, 0.5, 0.5, 0.5,
            1.4, 0.8, 0.55, 0.20, 0.25, 1.85, 0.60, "lower fit / higher outcome"));
        selectorDb.Add(new TacticalMatchupRecord(
            "ineligible", "5-3-2", TeamTactic.Normal, false, 1.0, 1.0, 1.0, 0, 1,
            2.0, 0.5, 0.90, 0.05, 0.05, 2.75, 1.50, "must be ignored"));
        var selected = selectorDb.BestEligible();
        Check(selected.CandidateId == "outcome" && selected.Tactic == TeamTactic.Pressing, "T5 selector chooses highest expected points, not highest fit");
        Check(selectorDb.BestByExpectedPoints().All(x => x.Eligible), "DB3 best-per-XI excludes ineligible tactics");

        Console.WriteLine("PASS: DB3 tactical matchup database + T5 outcome-driven selector");
        return 0;
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
}
