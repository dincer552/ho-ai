using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class MidfielderSingletonDiagnosticRegression
{
    public static int Run()
    {
        // Controlled live Hattrick 0-1-0 screenshots:
        // one normal central midfielder, all other field positions empty.
        // The screenshots provide the complete 7-sector ground truth.
        var fixtures = new[]
        {
            new Fixture("Utku Hakan Başak", 482291700, 4, 3, 5, 7, 8, 6, 3, 3, 1, 1, 1.25, 1, 1, 1.25, 1),
            new Fixture("Andres Nahasepp", 495041177, 3, 6, 8, 5, 13, 7, 3, 6, 1, 1, 1.25, 1, 1.75, 1, 1),
            new Fixture("Mikel Thiebault", 498487535, 4, 5, 9, 4, 12, 6, 2, 8, 1, 1.25, 1.25, 1, 2, 1, 1),
            new Fixture("Sergen Gözay", 513885774, 6, 5, 4, 5, 5, 4, 2, 7, 1.25, 2, 1.25, 1, 1.5, 1, 1),
            new Fixture("Adrian Beța", 491743384, 4, 5, 9, 3, 15, 7, 3, 7, 1, 1, 1, 1, 1.5, 1, 1),
            new Fixture("Abeiku Takyi", 468363070, 17, 3, 8, 4, 7, 6, 10, 6, 1.25, 2, 1.25, 1, 1.5, 1, 1),
            new Fixture("Cristian Pesalovo", 476114406, 16, 6, 9, 3, 6, 7, 6, 7, 1.25, 2.25, 1.25, 1.25, 1.75, 1.25, 1.25),
            new Fixture("Milen Bozev", 465141092, 5, 12, 9, 14, 6, 6, 8, 8, 1.25, 2, 1.25, 1.25, 1.75, 1.25, 2.25),
            new Fixture("Bertalan Doktor", 465805392, 2, 16, 12, 6, 6, 6, 8, 7, 1.25, 2.75, 1.25, 1.25, 1.75, 1.25, 1.75)
        };

        var engine = new RegionalRatingEngineFinal();
        var abs = new double[7];

        Console.WriteLine("V5 normal IM-C 0-1-0 FULL 7-SECTOR diagnostic");
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
                "MID-DIAG", "0-1-0",
                [new Slot("IM-C", "IM-C", "MID-DIAG", player.Name, player.Id, 0, 0, 0, PlayerOrder.Normal)]);

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

        Console.WriteLine("DIAGNOSTIC ONLY: no sector is failed/changed by this run.");
        return 0;
    }

    private static string Fmt(double value) => $"{value:+0.00;-0.00;0.00}";

    private readonly record struct Fixture(
        string Name, int Id, int Defending, int Playmaking, int Passing,
        int Winger, int Scoring, int Stamina, int Experience, int Form,
        double LeftDefence, double CentralDefence, double RightDefence,
        double Midfield, double LeftAttack, double CentralAttack, double RightAttack);
}
