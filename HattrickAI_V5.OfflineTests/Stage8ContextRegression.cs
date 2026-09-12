using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage8ContextRegression
{
    public static int Run()
    {
        var engine = new RegionalRatingEngineFixed();
        var im = new RegionalPlayer(1, RegionalPosition.InnerMidfielder, PlayerSide.Center, PlayerOrder.Normal,
            1, 1, 10, 1, 1, 1, 7, 0, 0, 9.4);
        var baseContext = RatingContext.Default;
        var baseline = engine.Calculate(new[] { im }, baseContext);
        var failures = 0;

        CheckRatio(engine.Calculate(new[] { im }, baseContext with { MatchLocation = MatchLocation.Home }).RawMidfield,
            baseline.RawMidfield, 1.19892, "home midfield", ref failures);
        CheckRatio(engine.Calculate(new[] { im }, baseContext with { MatchLocation = MatchLocation.DerbyAway }).RawMidfield,
            baseline.RawMidfield, 1.11493, "derby-away midfield", ref failures);
        CheckRatio(engine.Calculate(new[] { im }, baseContext with { Attitude = TeamAttitude.PlayItCool }).RawMidfield,
            baseline.RawMidfield, .83945, "PIC midfield", ref failures);
        CheckRatio(engine.Calculate(new[] { im }, baseContext with { Attitude = TeamAttitude.MatchOfTheSeason }).RawMidfield,
            baseline.RawMidfield, 1.1149, "MOTS midfield", ref failures);
        CheckRatio(engine.Calculate(new[] { im }, baseContext with { Tactic = TeamTactic.CounterAttack }).RawMidfield,
            baseline.RawMidfield, .93, "counter-attack midfield", ref failures);

        var defender = new RegionalPlayer(2, RegionalPosition.CentralDefender, PlayerSide.Center, PlayerOrder.Normal,
            1, 12, 1, 1, 1, 1, 7, 0, 0, 9.4);
        var forward = new RegionalPlayer(3, RegionalPosition.Forward, PlayerSide.Center, PlayerOrder.Normal,
            1, 1, 1, 1, 1, 12, 7, 0, 0, 9.4);
        var defBase = engine.Calculate(new[] { defender }, baseContext);
        var attBase = engine.Calculate(new[] { forward }, baseContext);
        var defLead = engine.Calculate(new[] { defender }, baseContext with { GoalDifference = 2 });
        var attLead = engine.Calculate(new[] { forward }, baseContext with { GoalDifference = 2 });
        CheckRatio(defLead.RawCentralDefence, defBase.RawCentralDefence, 1.075, "2-goal lead defence", ref failures);
        CheckRatio(attLead.RawCentralAttack, attBase.RawCentralAttack, .91, "2-goal lead attack", ref failures);

        var ignored = engine.Calculate(new[] { forward }, baseContext with { GoalDifference = 2, IgnoreLeadRetreat = true });
        CheckRatio(ignored.RawCentralAttack, attBase.RawCentralAttack, 1.0, "ignored lead-retreat attack", ref failures);

        Console.WriteLine(failures == 0 ? "Stage 8 context regression: PASS" : $"Stage 8 context regression: FAIL ({failures})");
        return failures == 0 ? 0 : 1;
    }

    private static void CheckRatio(double actual, double baseline, double expected, string label, ref int failures)
    {
        var ratio = baseline == 0 ? double.NaN : actual / baseline;
        if (Math.Abs(ratio - expected) > 0.00001)
        {
            Console.WriteLine($"FAIL {label}: expected {expected:F5}, got {ratio:F5}");
            failures++;
        }
        else Console.WriteLine($"PASS {label}: {ratio:F5}");
    }
}
