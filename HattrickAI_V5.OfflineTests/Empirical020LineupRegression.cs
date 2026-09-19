using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Empirical020LineupRegression
{
    public static int Run()
    {
        // Controlled live Hattrick observations: one normal DEF-CL in 1-0-0.
        // These are the 10 screenshot fixtures used to calibrate the canonical
        // DEF-CL normal-position contribution matrix.
        var fixtures = new[]
        {
            new Fixture("Milen Bozev", 5, 12, 9, 14, 6, 8, 8, 1.75, 1.75),
            new Fixture("Abeiku Takyi", 17, 3, 8, 4, 6, 6, 10, 3.50, 4.00),
            new Fixture("Nelson Ferrante", 4, 6, 9, 3, 6, 7, 2, 1.50, 1.50),
            new Fixture("Dawid Nocoń", 17, 3, 5, 4, 5, 7, 12, 3.75, 4.25),
            new Fixture("Manuel Gobiet", 9, 11, 9, 15, 6, 8, 7, 2.25, 2.50),
            new Fixture("Nándor Dobóvári", 3, 5, 8, 17, 7, 7, 3, 1.25, 1.25),
            new Fixture("Francisco Manuel", 2, 15, 9, 1, 4, 4, 10, 1.00, 1.25),
            new Fixture("Zübeyir Balaban", 6, 6, 4, 5, 5, 5, 2, 1.75, 1.75),
            new Fixture("Sergen Gözay", 6, 5, 4, 5, 4, 7, 2, 1.50, 1.75),
            new Fixture("Mikel Thiebault", 4, 5, 9, 4, 6, 8, 2, 1.50, 1.75)
        };

        var engine = new RegionalRatingEngineFixed();
        var failures = new List<string>();

        foreach (var fixture in fixtures)
        {
            var player = new Player(
                1000 + Array.IndexOf(fixtures, fixture),
                fixture.Name,
                Keeper: 0,
                Defending: fixture.Defending,
                Playmaking: fixture.Playmaking,
                Passing: fixture.Passing,
                Winger: fixture.Winger,
                Scoring: 0,
                Stamina: fixture.Stamina,
                Form: fixture.Form,
                Experience: fixture.Experience);

            var lineup = new Lineup("DEF-CL", "1-0-0",
            [
                new Slot("DEF-CL", "DEF-CL", "14-position DEF-CL regression", player.Name, player.Id, 0, 0, 0)
            ]);

            var actual = engine.CalculateLineup(lineup, [player], RatingContext.Default);

            if (Math.Abs(actual.RawLeftDefence - fixture.ExpectedLeft) > .26)
                failures.Add($"{fixture.Name} DEF-L: expected {fixture.ExpectedLeft:F2}, got {actual.RawLeftDefence:F2}");

            if (Math.Abs(actual.RawCentralDefence - fixture.ExpectedCentral) > .26)
                failures.Add($"{fixture.Name} DEF-C: expected {fixture.ExpectedCentral:F2}, got {actual.RawCentralDefence:F2}");

            if (Math.Abs(actual.RawRightDefence) > .001)
                failures.Add($"{fixture.Name} DEF-R must remain 0, got {actual.RawRightDefence:F3}");
        }

        if (failures.Count > 0)
        {
            foreach (var failure in failures)
                Console.WriteLine("FAIL: " + failure);
            return 1;
        }

        Console.WriteLine("PASS: 10 controlled DEF-CL fixtures match the empirical calibration.");
        return 0;
    }

    private readonly record struct Fixture(
        string Name,
        int Defending,
        int Playmaking,
        int Passing,
        int Winger,
        int Stamina,
        int Form,
        int Experience,
        double ExpectedLeft,
        double ExpectedCentral);
}
