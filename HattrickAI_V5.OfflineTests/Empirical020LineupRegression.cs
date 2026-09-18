using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Empirical020LineupRegression
{
    public static int Run()
    {
        var players = new[]
        {
            new Player(1001, "Enzo Bultot", 17, 4, 1, 2, 1, 4, 7, 5, 6),
            new Player(1002, "Milen Bozev", 1, 5, 12, 9, 14, 6, 6, 8, 8, 0, -1, PlayerSpecialty.Powerful, 2),
            new Player(1003, "Nandor Dobovari", 2, 3, 5, 8, 17, 4, 7, 7, 3, 0, -1, PlayerSpecialty.Quick, 1)
        };

        var engine = new RegionalRatingEngineFinal();

        var central = new Lineup("2-0-0 IM", "2-0-0", new[]
        {
            Slot("GK", players[0]),
            Slot("IM-L", players[1]),
            Slot("IM-R", players[2])
        });
        var wide = new Lineup("2-0-0 W", "2-0-0", new[]
        {
            Slot("GK", players[0]),
            Slot("W-L", players[1]),
            Slot("W-R", players[2])
        });

        var centralActual = Values(engine.CalculateLineup(central, players, RatingContext.Default));
        var wideActual = Values(engine.CalculateLineup(wide, players, RatingContext.Default));

        var centralExpected = new[] { 5.25, 5.75, 5.00, 1.25, 0.00, 0.00, 0.00 };
        var wideExpected = new[] { 6.25, 4.50, 5.50, 1.25, 3.00, 0.00, 3.25 };

        var failures = new List<string>();
        Check("IM-L/IM-R", centralActual, centralExpected, failures);
        Check("W-L/W-R", wideActual, wideExpected, failures);

        if (failures.Count > 0)
        {
            foreach (var failure in failures) Console.WriteLine("FAIL: " + failure);
            return 1;
        }

        Console.WriteLine("PASS: V5 reproduces controlled 2-0-0 Hattrick sector pair.");
        return 0;
    }

    private static Slot Slot(string code, Player player)
        => new(code, code, "empirical 2-0-0 fixture", player.Name, player.Id, 0, 0, 0);

    private static double[] Values(RegionalRatingSnapshot r)
        => new[] { r.LeftDefence, r.CentralDefence, r.RightDefence, r.Midfield,
            r.LeftAttack, r.CentralAttack, r.RightAttack };

    private static void Check(string name, double[] actual, double[] expected, List<string> failures)
    {
        for (var i = 0; i < actual.Length; i++)
        {
            if (Math.Abs(actual[i] - expected[i]) > 0.01)
                failures.Add($"{name} sector {i}: expected {expected[i]:0.00}, got {actual[i]:0.00}");
        }
    }
}
