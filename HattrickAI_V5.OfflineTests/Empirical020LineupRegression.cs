using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Empirical020LineupRegression
{
    public static int Run()
    {
        // This regression used to lock shape-specific 2-0-0 multipliers.
        // The 14-position rewrite intentionally removes those multipliers and
        // verifies the actual WB/CD slot distinction instead.
        var player = new Player(
            1001, "Defender fixture",
            Keeper: 1, Defending: 17, Playmaking: 3, Passing: 5,
            Winger: 4, Scoring: 7, Stamina: 9, Form: 7, Experience: 5);

        var engine = new RegionalRatingEngineFixed();

        var wingBack = new Lineup("WB", "2-0-0",
        [
            Slot("WB-L", player)
        ]);

        var centralDefender = new Lineup("CD", "2-0-0",
        [
            Slot("DEF-L", player)
        ]);

        var wb = engine.CalculateLineup(wingBack, [player], RatingContext.Default);
        var cd = engine.CalculateLineup(centralDefender, [player], RatingContext.Default);

        var expectedWbRatio = .268 / .083;
        var expectedCdRatio = .077 / .186;

        var wbRatio = wb.RawLeftDefence / wb.RawCentralDefence;
        var cdRatio = cd.RawLeftDefence / cd.RawCentralDefence;

        var failures = new List<string>();

        if (Math.Abs(wbRatio - expectedWbRatio) > 1e-9)
            failures.Add($"WB-L contribution ratio drift: expected {expectedWbRatio:R}, got {wbRatio:R}");

        if (Math.Abs(cdRatio - expectedCdRatio) > 1e-9)
            failures.Add($"DEF-L contribution ratio drift: expected {expectedCdRatio:R}, got {cdRatio:R}");

        if (Math.Abs(wb.RawLeftDefence - cd.RawLeftDefence) < 0.1)
            failures.Add("WB-L and DEF-L collapsed to the same defensive contribution.");

        if (failures.Count > 0)
        {
            foreach (var failure in failures)
                Console.WriteLine("FAIL: " + failure);
            return 1;
        }

        Console.WriteLine("PASS: 2-0-0 explicitly distinguishes WB-L from DEF-L.");
        Console.WriteLine($"WB-L ratio={wbRatio:R} | DEF-L ratio={cdRatio:R}");
        return 0;
    }

    private static Slot Slot(string code, Player player)
        => new(code, code, "14-position regression", player.Name, player.Id, 0, 0, 0);
}
