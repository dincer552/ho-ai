using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class UserProvidedMidfielderScreenshotRegression
{
    public static int Run()
    {
        // Six 0-1-0 normal IM-C Hattrick screenshots supplied on 2026-09-19.
        // Player skills are taken from the Hattrick export fixture:
        // HattrickAI_V5.OfflineTests/fixtures/s4msunfc-m7-m8.json
        // Ground truth sector order is LD, CD, RD, MID, LA, CA, RA.
        var fixtures = new[]
        {
            new Fixture("Şaban Savlet", 495027585, 4, 5, 5, 5, 7, 6, 3, 6, 1.25, 1, 0, 1, 1.75, 1, 0),
            new Fixture("Bumin Pehlivanlar", 493851210, 4, 4, 7, 5, 7, 5, 2, 8, 1.25, 1, 0, 1, 2.25, 1, 0),
            new Fixture("Adrian Beța", 491743384, 4, 5, 8, 3, 15, 7, 3, 7, 1.25, 1, 0, 1, 2, 1, 0),
            new Fixture("Mikel Thiebault", 498487535, 4, 5, 8, 4, 12, 6, 2, 7, 1.25, 1, 0, 1, 2.25, 1, 0),
            new Fixture("Sergen Gözay", 513885774, 6, 5, 4, 5, 5, 4, 2, 3, 1.5, 1, 0, 1, 2.25, 1, 0),
            new Fixture("Dawid Nocoń", 458524225, 17, 3, 5, 4, 7, 5, 12, 6, 2.75, 1.5, 0, 1, 2, 1, 0)
        };

        var engine = new RegionalRatingEngineFinal();
        var abs = new double[7];
        var failures = 0;
        const double tolerance = 0.25;

        Console.WriteLine("V5 user-supplied Hattrick normal IM-C 0-1-0 screenshot regression");
        Console.WriteLine("Name | V5 LD CD RD MID LA CA RA | HT LD CD RD MID LA CA RA | Errors LD CD RD MID LA CA RA");

        foreach (var f in fixtures)
        {
            var player = new Player(
                f.Id, f.Name, Keeper: 1, Defending: f.Defending,
                Playmaking: f.Playmaking, Passing: f.Passing,
                Winger: f.Winger, Scoring: f.Scoring,
                Stamina: f.Stamina, Form: f.Form,
                Experience: f.Experience);

            var lineup = new Lineup(
                "USER-IM", "0-1-0",
                [new Slot("IM-C", "IM-C", "USER-IM", player.Name, player.Id, 0, 0, 0, PlayerOrder.Normal)]);

            var actual = engine.CalculateLineup(lineup, [player], RatingContext.Default);
            var v = new[]
            {
                actual.LeftDefence, actual.CentralDefence, actual.RightDefence,
                actual.Midfield, actual.LeftAttack, actual.CentralAttack, actual.RightAttack
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
                $"{actual.LeftDefence:F2} {actual.CentralDefence:F2} {actual.RightDefence:F2} {actual.Midfield:F2} {actual.LeftAttack:F2} {actual.CentralAttack:F2} {actual.RightAttack:F2} | " +
                $"{f.LeftDefence:F2} {f.CentralDefence:F2} {f.RightDefence:F2} {f.Midfield:F2} {f.LeftAttack:F2} {f.CentralAttack:F2} {f.RightAttack:F2} | " +
                $"{Fmt(e[0])} {Fmt(e[1])} {Fmt(e[2])} {Fmt(e[3])} {Fmt(e[4])} {Fmt(e[5])} {Fmt(e[6])}");
        }

        Console.WriteLine(
            $"MAE LD={abs[0] / fixtures.Length:F4} CD={abs[1] / fixtures.Length:F4} RD={abs[2] / fixtures.Length:F4} " +
            $"MID={abs[3] / fixtures.Length:F4} LA={abs[4] / fixtures.Length:F4} CA={abs[5] / fixtures.Length:F4} RA={abs[6] / fixtures.Length:F4}");

        Console.WriteLine(failures == 0
            ? "PASS: all 7 sectors are within +/-0.25 of the 6 supplied Hattrick screenshots."
            : $"FAIL: {failures} sector observations exceed tolerance +/-{tolerance:F2}.");
        return failures == 0 ? 0 : 1;
    }

    private static string Fmt(double value) => $"{value:+0.00;-0.00;0.00}";

    private readonly record struct Fixture(
        string Name, int Id, int Defending, int Playmaking, int Passing,
        int Winger, int Scoring, int Stamina, int Experience, int Form,
        double LeftDefence, double CentralDefence, double RightDefence,
        double Midfield, double LeftAttack, double CentralAttack, double RightAttack);
}
