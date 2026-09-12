using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage10TacticRegression
{
    public static int Run()
    {
        var failures = 0;
        var engine = new RegionalRatingEngineFixed();
        var im = new RegionalPlayer(1, RegionalPosition.InnerMidfielder, PlayerSide.Center, PlayerOrder.Normal,
            1, 1, 10, 1, 1, 1, 7, 0, 0, 9.4);
        var defender = new RegionalPlayer(2, RegionalPosition.CentralDefender, PlayerSide.Center, PlayerOrder.Normal,
            1, 12, 1, 1, 1, 1, 7, 0, 0, 9.4);
        var forward = new RegionalPlayer(3, RegionalPosition.Forward, PlayerSide.Center, PlayerOrder.Normal,
            1, 1, 1, 1, 1, 12, 7, 0, 0, 9.4);
        var baseline = engine.Calculate(new[] { im, defender, forward }, RatingContext.Default);

        var counter = engine.Calculate(new[] { im, defender, forward }, RatingContext.Default with { Tactic = TeamTactic.CounterAttack });
        CheckRatio(counter.RawMidfield, baseline.RawMidfield, .93, "counter-attack midfield", ref failures);
        CheckSame(counter, baseline, RatingSector.LeftDefence, "counter-attack left defence", ref failures);

        var middle = engine.Calculate(new[] { im, defender, forward }, RatingContext.Default with { Tactic = TeamTactic.AttackMiddle });
        CheckRatio(middle.RawLeftDefence, baseline.RawLeftDefence, .85, "attack-middle left defence", ref failures);
        CheckRatio(middle.RawRightDefence, baseline.RawRightDefence, .85, "attack-middle right defence", ref failures);
        CheckSame(middle, baseline, RatingSector.CentralDefence, "attack-middle central defence", ref failures);

        var wings = engine.Calculate(new[] { im, defender, forward }, RatingContext.Default with { Tactic = TeamTactic.AttackWings });
        CheckRatio(wings.RawCentralDefence, baseline.RawCentralDefence, .85, "attack-wings central defence", ref failures);
        CheckSame(wings, baseline, RatingSector.LeftDefence, "attack-wings left defence", ref failures);
        CheckSame(wings, baseline, RatingSector.RightDefence, "attack-wings right defence", ref failures);

        var creative = engine.Calculate(new[] { im, defender, forward }, RatingContext.Default with { Tactic = TeamTactic.Creative });
        CheckRatio(creative.RawLeftDefence, baseline.RawLeftDefence, .93, "creative left defence", ref failures);
        CheckRatio(creative.RawCentralDefence, baseline.RawCentralDefence, .93, "creative central defence", ref failures);
        CheckRatio(creative.RawRightDefence, baseline.RawRightDefence, .93, "creative right defence", ref failures);

        var longShots = engine.Calculate(new[] { im, defender, forward }, RatingContext.Default with { Tactic = TeamTactic.LongShots });
        CheckRatio(longShots.RawLeftAttack, baseline.RawLeftAttack, .96, "long-shots left attack", ref failures);
        CheckRatio(longShots.RawCentralAttack, baseline.RawCentralAttack, .96, "long-shots central attack", ref failures);
        CheckRatio(longShots.RawRightAttack, baseline.RawRightAttack, .96, "long-shots right attack", ref failures);
        CheckSame(longShots, baseline, RatingSector.Midfield, "long-shots midfield", ref failures);

        // Pressing is a chance-model tactic; it must not silently alter regional rating.
        var pressing = engine.Calculate(new[] { im, defender, forward }, RatingContext.Default with { Tactic = TeamTactic.Pressing });
        foreach (var sector in Enum.GetValues<RatingSector>())
            CheckSame(pressing, baseline, sector, $"pressing {sector}", ref failures);

        // Normal is the neutral reference.
        var normal = engine.Calculate(new[] { im, defender, forward }, RatingContext.Default with { Tactic = TeamTactic.Normal });
        foreach (var sector in Enum.GetValues<RatingSector>())
            CheckSame(normal, baseline, sector, $"normal {sector}", ref failures);

        Console.WriteLine(failures == 0 ? "Stage 10 tactic regression: PASS" : $"Stage 10 tactic regression: FAIL ({failures})");
        return failures == 0 ? 0 : 1;
    }

    private static double Raw(RegionalRatingSnapshot s, RatingSector sector) => sector switch
    {
        RatingSector.LeftDefence => s.RawLeftDefence,
        RatingSector.CentralDefence => s.RawCentralDefence,
        RatingSector.RightDefence => s.RawRightDefence,
        RatingSector.Midfield => s.RawMidfield,
        RatingSector.LeftAttack => s.RawLeftAttack,
        RatingSector.CentralAttack => s.RawCentralAttack,
        RatingSector.RightAttack => s.RawRightAttack,
        _ => throw new ArgumentOutOfRangeException(nameof(sector), sector, null)
    };

    private static void CheckRatio(double actual, double baseline, double expected, string label, ref int failures)
    {
        var ratio = baseline == 0 ? double.NaN : actual / baseline;
        if (!double.IsFinite(ratio) || Math.Abs(ratio - expected) > 0.00001)
        {
            Console.WriteLine($"FAIL {label}: expected {expected:F5}, got {ratio:F5}");
            failures++;
        }
    }

    private static void CheckSame(RegionalRatingSnapshot actual, RegionalRatingSnapshot baseline, RatingSector sector, string label, ref int failures)
    {
        var a = Raw(actual, sector);
        var b = Raw(baseline, sector);
        if (Math.Abs(a - b) > 1e-12)
        {
            Console.WriteLine($"FAIL {label}: expected unchanged, got {a:R} vs {b:R}");
            failures++;
        }
    }
}
