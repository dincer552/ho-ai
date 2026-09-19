using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class MidfielderSingletonDiagnosticRegression
{
    public static int Run()
    {
        // Controlled live Hattrick 0-1-0 screenshots:
        // one normal central midfielder, all other field positions empty.
        var fixtures = new[]
        {
            new Fixture("Utku Hakan Başak", 482291700, 4, 3, 5, 7, 8, 6, 3, 1.25, 3),
            new Fixture("Andres Nahasepp", 495041177, 3, 6, 8, 5, 13, 7, 3, 1.75, 6),
            new Fixture("Mikel Thiebault", 498487535, 4, 5, 9, 4, 12, 6, 2, 2.00, 8),
            new Fixture("Sergen Gözay", 513885774, 6, 5, 4, 5, 5, 4, 2, 1.50, 7),
            new Fixture("Adrian Beța", 491743384, 4, 5, 9, 3, 15, 7, 3, 1.50, 7),
            new Fixture("Abeiku Takyi", 468363070, 17, 3, 8, 4, 7, 6, 10, 2.00, 6),
            new Fixture("Cristian Pesalovo", 476114406, 16, 6, 9, 3, 6, 7, 6, 1.50, 7),
            new Fixture("Milen Bozev", 465141092, 5, 12, 9, 14, 6, 6, 8, 2.25, 8),
            new Fixture("Bertalan Doktor", 465805392, 2, 16, 12, 6, 6, 6, 8, 2.75, 7)
        };

        var engine = new RegionalRatingEngineFinal();
        var totalAbsoluteError = 0.0;
        var failures = 0;
        const double tolerance = 0.13;

        Console.WriteLine("V5 MID 0-1-0 controlled diagnostic");
        Console.WriteLine("Name | PM | Form | Exp | V5 raw MID | V5 display MID | Hattrick MID | Error");

        foreach (var f in fixtures)
        {
            var player = new Player(
                f.Id, f.Name, Keeper: 1, Defending: f.Defending,
                Playmaking: f.Playmaking, Passing: f.Passing,
                Winger: f.Winger, Scoring: f.Scoring,
                Stamina: f.Stamina, Form: f.Form,
                Experience: f.Experience);

            var lineup = new Lineup(
                "MID-DIAG", "0-1-0",
                [new Slot("IM-C", "IM-C", "MID-DIAG", player.Name, player.Id, 0, 0, 0, PlayerOrder.Normal)]);

            var actual = engine.CalculateLineup(lineup, [player], RatingContext.Default);
            var error = actual.RawMidfield - f.ExpectedMidfield;
            totalAbsoluteError += Math.Abs(error);
            if (Math.Abs(error) > tolerance)
                failures++;

            Console.WriteLine(
                $"{f.Name} | {f.Playmaking} | {f.Form} | {f.Experience} | " +
                $"{actual.RawMidfield:F3} | {actual.Midfield:F2} | {f.ExpectedMidfield:F2} | {error:+0.000;-0.000;0.000}");
        }

        var mae = totalAbsoluteError / fixtures.Length;
        Console.WriteLine($"MID MAE={mae:F4}");
        Console.WriteLine(failures == 0
            ? "PASS: V5 normal IM MID matches the 9 controlled Hattrick observations."
            : $"FAIL: {failures}/9 MID fixtures exceed tolerance {tolerance:F2}.");
        return failures == 0 ? 0 : 1;
    }

    private readonly record struct Fixture(
        string Name, int Id, int Defending, int Playmaking, int Passing,
        int Winger, int Scoring, int Stamina, int Experience,
        double ExpectedMidfield, int Form);
}
