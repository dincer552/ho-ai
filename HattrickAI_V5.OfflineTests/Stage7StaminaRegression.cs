using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage7StaminaRegression
{
    public static int Run()
    {
        var failures = new List<string>();
        var engine = new RegionalRatingEngineFixed();
        var basePlayer = new RegionalPlayer(1, RegionalPosition.InnerMidfielder, PlayerSide.Center, PlayerOrder.Normal, 0, 1, 10, 1, 1, 1, 5, 0, 0, 7);

        var start = engine.Calculate(new[] { basePlayer }, RatingContext.Default);
        var at45 = engine.Calculate(new[] { basePlayer }, RatingContext.Default with { MatchMinute = 45 });
        var at90 = engine.Calculate(new[] { basePlayer }, RatingContext.Default with { MatchMinute = 90 });
        var at120 = engine.Calculate(new[] { basePlayer }, RatingContext.Default with { MatchMinute = 120 });

        CheckRatio(at45.RawMidfield, start.RawMidfield, .926, "stamina 7 at 45'", failures);
        CheckRatio(at90.RawMidfield, start.RawMidfield, .787, "stamina 7 at 90'", failures);
        CheckRatio(at120.RawMidfield, start.RawMidfield, .595, "stamina 7 at 120'", failures);

        var excellent = basePlayer with { Stamina = 9 };
        var excellent90 = engine.Calculate(new[] { excellent }, RatingContext.Default with { MatchMinute = 90 });
        CheckRatio(excellent90.RawMidfield, start.RawMidfield, 1.0, "stamina 9 at 90'", failures);

        var disastrous = basePlayer with { Stamina = 2 };
        var disastrous90 = engine.Calculate(new[] { disastrous }, RatingContext.Default with { MatchMinute = 90 });
        CheckRatio(disastrous90.RawMidfield, start.RawMidfield, .294, "stamina 2 at 90'", failures);

        var noMinute = engine.Calculate(new[] { disastrous }, RatingContext.Default);
        var noMinuteRatio = noMinute.RawMidfield / start.RawMidfield;
        if (Math.Abs(noMinuteRatio - 1.0) > 1e-9)
            failures.Add($"minute 0 must preserve full stamina performance: got {noMinuteRatio:F6}");

        if (failures.Count == 0)
        {
            Console.WriteLine("PASS: Stage 7 stamina/match-minute regression");
            return 0;
        }

        foreach (var failure in failures) Console.WriteLine("FAIL: " + failure);
        Console.WriteLine($"FAIL: Stage 7 ({failures.Count} assertion(s))");
        return 1;
    }

    private static void CheckRatio(double actual, double baseline, double expected, string name, List<string> failures)
    {
        var ratio = actual / baseline;
        if (Math.Abs(ratio - expected) > 1e-9)
            failures.Add($"{name}: expected ratio {expected:F6}, got {ratio:F6}");
    }
}
