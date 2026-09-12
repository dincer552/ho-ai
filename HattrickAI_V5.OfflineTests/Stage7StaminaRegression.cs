using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage7StaminaRegression
{
    public static int Run()
    {
        var failures = new List<string>();
        var engine = new RegionalRatingEngineFixed();
        var basePlayer = new RegionalPlayer(1, RegionalPosition.InnerMidfielder, PlayerSide.Center, PlayerOrder.Normal, 0, 1, 10, 1, 1, 1, 5, 0, 0, 7);

        // Stage 7 isolates stamina/minute: form, loyalty and experience are neutral.
        var start = engine.Calculate(new[] { basePlayer }, RatingContext.Default);
        var at45 = engine.Calculate(new[] { basePlayer }, RatingContext.Default with { MatchMinute = 45 });
        var at90 = engine.Calculate(new[] { basePlayer }, RatingContext.Default with { MatchMinute = 90 });
        var at120 = engine.Calculate(new[] { basePlayer }, RatingContext.Default with { MatchMinute = 120 });

        CheckRatio(at45.RawMidfield, start.RawMidfield, .926, "stamina 7 at 45'", failures);
        CheckRatio(at90.RawMidfield, start.RawMidfield, .787, "stamina 7 at 90'", failures);
        CheckRatio(at120.RawMidfield, start.RawMidfield, .595, "stamina 7 at 120'", failures);

        // High stamina should avoid the researched 90' penalty.
        var excellent = basePlayer with { Stamina = 9 };
        var excellent90 = engine.Calculate(new[] { excellent }, RatingContext.Default with { MatchMinute = 90 });
        CheckRatio(excellent90.RawMidfield, start.RawMidfield, 1.0, "stamina 9 at 90'", failures);

        // Low stamina should degrade materially by 90'.
        var disastrous = basePlayer with { Stamina = 2 };
        var disastrous90 = engine.Calculate(new[] { disastrous }, RatingContext.Default with { MatchMinute = 90 });
        CheckRatio(disastrous90.RawMidfield, start.RawMidfield, .294, "stamina 2 at 90'", failures);

        // No match minute means pre-match/start-of-match rating: stamina must not alter it.
        var noMinute = engine.Calculate(new[] { disastrous }, RatingContext.Default);
        CheckRatio(noMinute.RawMidfield, start.RawMidfield, 1.0, "minute 0 preserves full performance", failures);

        // The minute curve must be monotonic for a fixed stamina level.
        if (!(at45.RawMidfield > at90.RawMidfield && at90.RawMidfield > at120.RawMidfield))
            failures.Add("stamina 7 minute curve must decline monotonically");

        // Boundary behaviour: values above 120' are clamped to the 120' endpoint.
        var at121 = engine.Calculate(new[] { basePlayer }, RatingContext.Default with { MatchMinute = 121 });
        CheckRatio(at121.RawMidfield, at120.RawMidfield, 1.0, "minute 121 clamps to 120'", failures);

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
        if (Math.Abs(baseline) < 1e-12)
        {
            failures.Add($"{name}: baseline contribution is zero");
            return;
        }

        var ratio = actual / baseline;
        if (Math.Abs(ratio - expected) > 1e-9)
            failures.Add($"{name}: expected ratio {expected:F6}, got {ratio:F6}");
    }
}
