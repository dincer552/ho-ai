using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// T8 outcome-layer regression: DB3 tactical outcomes must remain finite,
/// canonical, eligibility-aware and deterministic under edge cases.
/// This is a regression guard, not a trained calibration model.
/// </summary>
public static class C22TacticalOutcomeEdgeCaseRegression
{
    public static int Run()
    {
        var db = new TacticalMatchupDatabase();
        var tactics = Enum.GetValues<TeamTactic>();

        foreach (var tactic in tactics)
        {
            var win = tactic == TeamTactic.Creative ? 0.42 : 0.30 + (0.01 * (int)tactic);
            var draw = 0.20;
            var loss = 1.0 - win - draw;
            db.Add(new TacticalMatchupRecord(
                "edge-a", "4-4-2", tactic, true,
                0.95 - (0.03 * (int)tactic), 0.8, 0.8, 0.1, 0.7,
                1.2 + (0.05 * (int)tactic), 1.0,
                win, draw, loss,
                3.0 * win + draw,
                0.2,
                "finite tactical outcome"));
        }

        Check(db.ForCandidate("edge-a").Count == tactics.Length, "edge case keeps all seven tactics");
        Check(db.ForCandidate("edge-a").All(ValidOutcome), "all tactic outcome probabilities are finite and bounded");
        Check(db.ForCandidate("edge-a").All(x => Math.Abs(x.ExpectedPoints - (3 * x.WinProbability + x.DrawProbability)) < 1e-12), "expected points remain canonical");

        // Outcome must beat fit: deliberately give the best-fit tactic a worse outcome.
        db.Add(new TacticalMatchupRecord(
            "edge-a", "4-4-2", TeamTactic.Normal, true,
            0.999, 1, 1, 0, 1,
            0.8, 1.2, 0.20, 0.20, 0.60, 0.80, -0.40,
            "high fit but poor outcome"));
        var selected = db.ForCandidate("edge-a")
            .Where(x => x.Eligible)
            .OrderByDescending(x => x.ExpectedPoints)
            .ThenByDescending(x => x.WinProbability)
            .ThenByDescending(x => x.TacticFitScore)
            .ThenBy(x => x.Tactic)
            .First();
        Check(selected.Tactic != TeamTactic.Normal, "outcome metric overrides higher tactical fit");

        // Ineligible outcome must never win, even with an extreme Expected Points value.
        db.Add(new TacticalMatchupRecord(
            "edge-a", "4-4-2", TeamTactic.CounterAttack, false,
            1.0, 1, 1, 0, 1,
            3.0, 0.0, 0.99, 0.01, 0.0, 2.98, 3.0,
            "ineligible extreme outcome"));
        Check(db.BestEligible().Tactic != TeamTactic.CounterAttack, "ineligible tactic is excluded from final selection");

        // Duplicate candidate+tactic rows replace the old row deterministically.
        db.Add(new TacticalMatchupRecord(
            "edge-a", "4-4-2", TeamTactic.Normal, true,
            0.10, 0.5, 0.5, 0.5, 0.5,
            1.4, 0.8, 0.55, 0.20, 0.25, 1.85, 0.60,
            "replacement row"));
        Check(db.ForCandidate("edge-a").Count == tactics.Length, "duplicate tactic rows are replaced rather than duplicated");
        Check(db.ForCandidate("edge-a").Single(x => x.Tactic == TeamTactic.Normal).ExpectedPoints == 1.85, "replacement row is the canonical row");

        // Invalid probability rows are ignored at ingestion.
        db.Add(new TacticalMatchupRecord(
            "edge-invalid", "4-4-2", TeamTactic.Creative, true,
            0.5, 0.5, 0.5, 0.5, 0.5,
            1, 1, double.NaN, 0.2, 0.8, 1.0, 0,
            "invalid probability"));
        Check(db.ForCandidate("edge-invalid").Count == 0, "invalid probability rows are rejected");

        Console.WriteLine("PASS: T8 tactical outcome calibration + edge-case regression");
        return 0;
    }

    private static bool ValidOutcome(TacticalMatchupRecord x)
        => double.IsFinite(x.WinProbability)
            && double.IsFinite(x.DrawProbability)
            && double.IsFinite(x.LossProbability)
            && x.WinProbability >= 0 && x.WinProbability <= 1
            && x.DrawProbability >= 0 && x.DrawProbability <= 1
            && x.LossProbability >= 0 && x.LossProbability <= 1
            && Math.Abs((x.WinProbability + x.DrawProbability + x.LossProbability) - 1.0) < 1e-12;

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
}
