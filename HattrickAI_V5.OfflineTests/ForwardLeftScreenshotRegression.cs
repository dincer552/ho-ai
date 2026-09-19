using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class ForwardLeftScreenshotRegression
{
    public static int Run()
    {
        // 2026-09-19 user-supplied 0-0-1 screenshots.
        // All players are FW-L, Normal. White values are ground truth;
        // green delta values are ignored. Sector order: LD, CD, RD, MID, LA, CA, RA.
        var fixtures = new[]
        {
            new Fixture("Abeiku Takyi", 468363070, 17, 3, 8, 4, 7, 6, 10, 6, 0, 0, 0, 1, 1.75, 2.75, 1.75),
            new Fixture("Münir Balkın", 502427630, 5, 5, 5, 5, 3, 3, 2, 6, 0, 0, 0, 1, 1.50, 1.75, 1.50),
            new Fixture("Sergen Gözay", 513885774, 6, 5, 4, 5, 5, 4, 2, 7, 0, 0, 0, 1, 1.50, 2.25, 1.50),
            new Fixture("Şaban Savlet", 495027585, 4, 5, 6, 5, 7, 6, 3, 4, 0, 0, 0, 1, 1.50, 2.00, 1.50),
            new Fixture("Utku Hakan Başak", 482291700, 4, 3, 5, 7, 8, 6, 3, 3, 0, 0, 0, 1, 1.50, 2.00, 1.50),
            new Fixture("Felix Gustavsson", 492052456, 3, 3, 9, 17, 5, 7, 4, 6, 0, 0, 0, 1, 2.25, 2.25, 2.25),
            new Fixture("Manuel Gobiet", 474962854, 9, 11, 9, 15, 8, 6, 7, 8, 0, 0, 0, 1, 2.50, 3.25, 2.50),
            new Fixture("Adrian Beța", 491743384, 4, 5, 9, 3, 15, 7, 3, 7, 0, 0, 0, 1, 2.25, 4.75, 2.25)
        };

        var engine = new RegionalRatingEngineFinal();
        var abs = new double[7];
        var failures = 0;
        const double tolerance = 0.25;

        Console.WriteLine("V5 normal FW-L 0-0-1 FULL 7-SECTOR screenshot regression");
        Console.WriteLine("Name | V5 LD CD RD MID LA CA RA | HT LD CD RD MID LA CA RA | Errors");

        foreach (var f in fixtures)
        {
            var player = new Player(
                f.Id, f.Name, Keeper: 1, Defending: f.Defending,
                Playmaking: f.Playmaking, Passing: f.Passing,
                Winger: f.Winger, Scoring: f.Scoring,
                Stamina: f.Stamina, Form: f.Form,
                Experience: f.Experience);

            var lineup = new Lineup(
                "FW-L-SCREENSHOTS", "0-0-1",
                [new Slot("FW-L", "FW-L", "FW-L-SCREENSHOTS", player.Name, player.Id, 0, 0, 0, PlayerOrder.Normal)]);

            var actual = engine.CalculateLineup(lineup, [player], RatingContext.Default);
            var v = new[]
            {
                QuarterDisplay(actual.RawLeftDefence), QuarterDisplay(actual.RawCentralDefence), QuarterDisplay(actual.RawRightDefence),
                QuarterDisplay(actual.RawMidfield), QuarterDisplay(actual.RawLeftAttack), QuarterDisplay(actual.RawCentralAttack), QuarterDisplay(actual.RawRightAttack)
            };
            var h = new[]
            {
                f.LeftDefence, f.CentralDefence, f.RightDefence,
                f.Midfield, f.LeftAttack, f.CentralAttack, f.RightAttack
            };

            var e = new double[7];
            for (var i = 0; i < 7; i++)
            {
                e[i] = v[i] - h[i];
                abs[i] += Math.Abs(e[i]);
                if (Math.Abs(e[i]) > tolerance)
                    failures++;
            }

            Console.WriteLine(
                $"{f.Name} | " +
                $"{v[0]:F2} {v[1]:F2} {v[2]:F2} {v[3]:F2} {v[4]:F2} {v[5]:F2} {v[6]:F2} | " +
                $"{h[0]:F2} {h[1]:F2} {h[2]:F2} {h[3]:F2} {h[4]:F2} {h[5]:F2} {h[6]:F2} | " +
                $"{Fmt(e[0])} {Fmt(e[1])} {Fmt(e[2])} {Fmt(e[3])} {Fmt(e[4])} {Fmt(e[5])} {Fmt(e[6])}");
        }

        Console.WriteLine(
            $"MAE LD={abs[0] / fixtures.Length:F4} CD={abs[1] / fixtures.Length:F4} RD={abs[2] / fixtures.Length:F4} " +
            $"MID={abs[3] / fixtures.Length:F4} LA={abs[4] / fixtures.Length:F4} CA={abs[5] / fixtures.Length:F4} RA={abs[6] / fixtures.Length:F4}");

        Console.WriteLine(failures == 0
            ? "PASS: all 7 V5 sectors for all 8 FW-L screenshots are within +/-0.25."
            : $"FAIL: {failures} sector observations exceed tolerance +/-{tolerance:F2}.");

        return failures == 0 ? 0 : 1;
    }

    private static double QuarterDisplay(double raw)
        => raw <= 0 ? 0 : Math.Clamp(Math.Max(1.0, Math.Round(raw * 4.0, MidpointRounding.AwayFromZero) / 4.0), 1.0, 20.0);

    private static string Fmt(double value) => $"{value:+0.00;-0.00;0.00}";

    private readonly record struct Fixture(
        string Name, int Id, int Defending, int Playmaking, int Passing,
        int Winger, int Scoring, int Stamina, int Experience, int Form,
        double LeftDefence, double CentralDefence, double RightDefence,
        double Midfield, double LeftAttack, double CentralAttack, double RightAttack);
}
