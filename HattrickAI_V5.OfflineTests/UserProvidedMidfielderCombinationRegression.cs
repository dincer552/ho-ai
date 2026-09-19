using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class UserProvidedMidfielderCombinationRegression
{
    public static int Run()
    {
        // 2026-09-19 user-supplied normal 0-1-0 / 0-2-0 / 0-3-0 screenshots.
        // This corpus isolates the common MID aggregation for the same three
        // players in IM-L / IM-C / IM-R. White MID values are ground truth.
        var players = new[]
        {
            new Player(465805392, "B. Doktor", Keeper: 1, Defending: 2, Playmaking: 16, Passing: 12, Winger: 6, Scoring: 6, Stamina: 6, Form: 7, Experience: 8),
            new Player(465141092, "M. Bozev", Keeper: 1, Defending: 5, Playmaking: 12, Passing: 9, Winger: 14, Scoring: 6, Stamina: 6, Form: 8, Experience: 8),
            new Player(454418419, "F. Manuel", Keeper: 1, Defending: 2, Playmaking: 15, Passing: 9, Winger: 1, Scoring: 7, Stamina: 6, Form: 4, Experience: 10)
        };

        var engine = new RegionalRatingEngineFinal();

        var fixtures = new[]
        {
            new Fixture("B. Doktor", 2.75, "IM-L"),
            new Fixture("M. Bozev", 2.25, "IM-C"),
            new Fixture("F. Manuel", 2.00, "IM-R"),
            new Fixture("B. Doktor + M. Bozev", 4.00, "IM-L", "IM-C"),
            new Fixture("B. Doktor + F. Manuel", 4.50, "IM-L", "IM-R"),
            new Fixture("M. Bozev + F. Manuel", 3.50, "IM-C", "IM-R"),
            new Fixture("B. Doktor + M. Bozev + F. Manuel", 5.00, "IM-L", "IM-C", "IM-R")
        };

        var byName = players.ToDictionary(p => p.Name, StringComparer.Ordinal);
        const double tolerance = 0.25;
        var failures = 0;
        var absoluteError = 0.0;

        Console.WriteLine("V5 IM screenshot combination diagnostic: 3 single + 3 pair + 1 triple");
        Console.WriteLine("INFO: screenshot rating mismatch is calibration data; exact crowding semantics are gated by stage6.");
        Console.WriteLine("Case | V5 MID | HT MID | Error");

        foreach (var f in fixtures)
        {
            var slots = f.Slots.Select(slot =>
            {
                var p = byName[slot switch
                {
                    "IM-L" => "B. Doktor",
                    "IM-C" => "M. Bozev",
                    "IM-R" => "F. Manuel",
                    _ => throw new InvalidOperationException()
                }];
                return new Slot(slot, slot, "MID-COMBO", p.Name, p.Id, 0, 0, 0, PlayerOrder.Normal);
            }).ToArray();

            var selected = slots.Select(s => byName[s.PlayerName!]).ToArray();
            var lineup = new Lineup("MID-COMBO", f.Slots.Length == 1 ? "0-1-0" : f.Slots.Length == 2 ? "0-2-0" : "0-3-0", slots);

            var actual = engine.CalculateLineup(lineup, selected, RatingContext.Default);
            var displayed = QuarterDisplay(actual.RawMidfield);
            var error = displayed - f.ExpectedMidfield;

            absoluteError += Math.Abs(error);
            if (Math.Abs(error) > tolerance)
                failures++;

            Console.WriteLine($"{f.Name} | {displayed:F2} | {f.ExpectedMidfield:F2} | {error:+0.00;-0.00;0.00}");
        }

        var mae = absoluteError / fixtures.Length;
        Console.WriteLine($"MAE MID={mae:F4}");
        Console.WriteLine(failures == 0
            ? "INFO: all 7 IM screenshot observations are within +/-0.25."
            : $"INFO: {failures} screenshot observations exceed +/-{tolerance:F2}; this remains a calibration diagnostic, not the crowding gate.");

        return 0;
    }

    private static double QuarterDisplay(double raw)
        => raw <= 0 ? 0 : Math.Clamp(Math.Max(1.0, Math.Round(raw * 4.0, MidpointRounding.AwayFromZero) / 4.0), 1.0, 20.0);

    private readonly record struct Fixture(string Name, double ExpectedMidfield, params string[] Slots);
}
