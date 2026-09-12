using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class CAL001CurrentMotorRegression
{
    public static int Run()
    {
        var players = new[]
        {
            new Player(479235895, "Enzo Bultot", 17, 4, 1, 2, 1, 4, 7, 6, 6),
            new Player(468363070, "Abeiku Takyi", 1, 17, 3, 8, 4, 7, 6, 5, 10),
            new Player(458524225, "Dawid Nocoć", 0, 17, 3, 5, 4, 7, 5, 6, 12),
            new Player(476114406, "Cristian Pesalovo", 1, 16, 6, 9, 3, 6, 7, 7, 6),
            new Player(492052456, "Felix Gustavsson", 1, 3, 3, 8, 17, 5, 7, 7, 4),
            new Player(465805392, "Bertalan Doktor", 1, 2, 16, 11, 6, 6, 6, 6, 8),
            new Player(465141092, "Milen Bozev", 1, 5, 12, 9, 14, 6, 6, 7, 8),
            new Player(474962854, "Manuel Gobiet", 1, 9, 11, 9, 15, 8, 6, 8, 7),
            new Player(497641568, "Ersin Akşin", 1, 2, 5, 9, 4, 13, 6, 7, 3),
            new Player(491743384, "Adrian Beţa", 1, 4, 5, 8, 3, 15, 7, 7, 3),
            new Player(495041177, "Andres Nahasepp", 1, 3, 6, 7, 5, 13, 7, 7, 3)
        };

        var slots = new[]
        {
            Slot("GK", players[0]), Slot("DEF-L", players[1]), Slot("DEF-C", players[2]), Slot("DEF-R", players[3]),
            Slot("W-L", players[4]), Slot("IM-L", players[5]), Slot("IM-R", players[6]), Slot("W-R", players[7]),
            Slot("FW-L", players[8]), Slot("FW-C", players[9]), Slot("FW-R", players[10])
        };

        var lineup = new Lineup("CAL-001", "3-4-3", slots);
        var actual = new RegionalRatingEngineFixed().CalculateLineup(lineup, players, RatingContext.Default);

        var expected = new[] { 7.547173492520718, 15.38792826270137, 8.722175800147264, 8.356595269523073, 9.736919924603303, 12.138912750812356, 9.931162425931916 };
        var displayed = new[] { actual.LeftDefence, actual.CentralDefence, actual.RightDefence, actual.Midfield, actual.LeftAttack, actual.CentralAttack, actual.RightAttack };
        var raw = new[] { actual.RawLeftDefence, actual.RawCentralDefence, actual.RawRightDefence, actual.RawMidfield, actual.RawLeftAttack, actual.RawCentralAttack, actual.RawRightAttack };
        var groundTruth = new[] { 13.0, 12.75, 13.25, 7.0, 15.75, 13.75, 13.5 };
        var labels = new[] { "LD", "CD", "RD", "MID", "LA", "CA", "RA" };

        var failures = 0;
        for (var i = 0; i < labels.Length; i++)
        {
            if (Math.Abs(raw[i] - expected[i]) > 1e-12) failures++;
            Console.WriteLine($"CAL-001 {labels[i]}: raw={raw[i]:F12} display={displayed[i]:F2} gt={groundTruth[i]:F2} error={displayed[i] - groundTruth[i]:+0.00;-0.00;0.00}");
        }

        var errors = displayed.Zip(groundTruth, (p, g) => p - g).ToArray();
        Console.WriteLine($"CAL-001 MAE={errors.Select(Math.Abs).Average():F4} Bias={errors.Average():+0.0000;-0.0000;0.0000} RMSE={Math.Sqrt(errors.Select(x => x * x).Average()):F4}");
        Console.WriteLine(failures == 0 ? "PASS: CAL-001 current V5 motor baseline" : $"FAIL: CAL-001 current V5 motor baseline ({failures} raw mismatches)");
        return failures == 0 ? 0 : 1;
    }

    private static Slot Slot(string code, Player player) => new(code, code, "CAL-001", player.Name, player.Id, 0, 0, 0, PlayerOrder.Normal);
}
