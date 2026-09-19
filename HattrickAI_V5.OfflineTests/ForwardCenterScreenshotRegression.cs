using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class ForwardCenterScreenshotRegression
{
    public static int Run()
    {
        // 2026-09-19 user-supplied 0-0-1 screenshots.
        // All players are FW-C, Normal. White values are ground truth;
        // green delta values are ignored. Sector order: LD, CD, RD, MID, LA, CA, RA.
        var fixtures = new[]
        {
            new Fixture("Oğuz Can Çitel", 514089104, 3, 5, 6, 5, 4, 4, 1, 3, 0, 0, 0, 1, 1.25, 1.50, 1.25),
            new Fixture("Cristian Pesalovo", 476114406, 16, 6, 9, 3, 6, 7, 6, 7, 0, 0, 0, 1, 1.25, 1.75, 1.25),
            new Fixture("Dawid Nocoń", 458524225, 17, 3, 5, 4, 7, 5, 12, 7, 0, 0, 0, 1, 1.75, 2.75, 1.75),
            new Fixture("Daim Evliya", 513040102, 4, 5, 7, 4, 4, 5, 2, 4, 0, 0, 0, 1, 1.25, 1.75, 1.25),
            new Fixture("Felix Gustavsson", 492052456, 3, 3, 9, 17, 5, 7, 4, 6, 0, 0, 0, 1, 2.25, 2.25, 2.25),
            new Fixture("Bertalan Doktor", 465805392, 2, 16, 12, 6, 6, 6, 8, 7, 0, 0, 0, 1, 2.00, 3.00, 2.00),
            new Fixture("Nelson Ferrante", 501442578, 4, 6, 9, 3, 11, 6, 2, 7, 0, 0, 0, 1, 2.00, 3.75, 2.00),
            new Fixture("Andres Nahasepp", 495041177, 3, 6, 8, 5, 13, 7, 3, 6, 0, 0, 0, 1, 2.00, 4.00, 2.00),
            new Fixture("Adrian Beța", 491743384, 4, 5, 9, 3, 15, 7, 3, 7, 0, 0, 0, 1, 2.25, 4.75, 2.25)
        };

        var engine = new RegionalRatingEngineFinal();
        var abs = new double[7];
        var failures = 0;
        const double tolerance = 0.25;

        Console.WriteLine("V5 normal FW-C 0-0-1 FULL 7-SECTOR screenshot regression");
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
                "FW-C-SCREENSHOTS", "0-0-1",
                [new Slot("FW-C", "FW-C", "FW-C-SCREENSHOTS", player.Name, player.Id, 0, 0, 0, PlayerOrder.Normal)]);

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
            ? "PASS: all 7 V5 sectors for all 9 FW-C screenshots are within +/-0.25."
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
