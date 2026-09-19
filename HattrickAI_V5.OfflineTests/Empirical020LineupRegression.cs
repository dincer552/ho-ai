using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Empirical020LineupRegression
{
    public static int Run()
    {
        // Controlled live Hattrick observations: one normal DEF-CL in 1-0-0.
        // The values below are the sector ratings visible in the Hattrick
        // lineup screen and are used as the empirical DEF-CL ground truth.
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
                new Slot("DEF-CL", "DEF-CL", "14-position regression", player.Name, player.Id, 0, 0, 0)
            ]);

            var actual = engine.CalculateLineup(lineup, [player], RatingContext.Default);

            if (Math.Abs(actual.RawLeftDefence - fixture.ExpectedLeft) > .20)
                failures.Add($"{fixture.Name} DEF-L: expected {fixture.ExpectedLeft:F2}, got {actual.RawLeftDefence:F2}");

            if (Math.Abs(actual.RawCentralDefence - fixture.ExpectedCentral) > .20)
                failures.Add($"{fixture.Name} DEF-C: expected {fixture.ExpectedCentral:F2}, got {actual.RawCentralDefence:F2}");

            if (Math.Abs(actual.RawRightDefence) > .001)
                failures.Add($"{fixture.Name} DEF-R must remain 0, got {actual.RawRightDefence:F3}");
        }

        // Controlled live Hattrick observations: one normal DEF-C in 1-0-0.
        var defCFixtures = new[]
        {
            new DefCFixture("Patrik Zarins", 4, 6, 8, 2, 6, 7, 2, 1.25, 1.50),
            new DefCFixture("Nurtaç Armağan", 2, 2, 2, 2, 6, 4, 3, 1.00, 1.25),
            new DefCFixture("Andres Nahasepp", 3, 6, 8, 5, 7, 6, 3, 1.00, 1.25),
            new DefCFixture("Milen Bozev", 5, 12, 9, 14, 6, 8, 8, 1.25, 1.75),
            new DefCFixture("Zübeyir Balaban", 6, 6, 4, 5, 5, 5, 2, 1.25, 1.75),
            new DefCFixture("Bertalan Doktor", 2, 16, 12, 6, 6, 7, 8, 1.25, 1.75),
            new DefCFixture("Manuel Gobiet", 9, 11, 9, 15, 6, 8, 7, 1.50, 2.50),
            new DefCFixture("Cristian Pesalovo", 16, 6, 9, 3, 7, 7, 6, 2.00, 4.00)
        };

        foreach (var fixture in defCFixtures)
        {
            var player = new Player(
                2000 + Array.IndexOf(defCFixtures, fixture),
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

            var lineup = new Lineup("DEF-C", "1-0-0",
            [
                new Slot("DEF-C", "DEF-C", "14-position DEF-C regression", player.Name, player.Id, 0, 0, 0)
            ]);

            var actual = engine.CalculateLineup(lineup, [player], RatingContext.Default);

            if (Math.Abs(actual.RawLeftDefence - fixture.ExpectedSide) > .20 ||
                Math.Abs(actual.RawRightDefence - fixture.ExpectedSide) > .20)
                failures.Add($"{fixture.Name} DEF-L/R: expected {fixture.ExpectedSide:F2}, got {actual.RawLeftDefence:F2}/{actual.RawRightDefence:F2}");

            if (Math.Abs(actual.RawCentralDefence - fixture.ExpectedCentral) > .20)
                failures.Add($"{fixture.Name} DEF-C: expected {fixture.ExpectedCentral:F2}, got {actual.RawCentralDefence:F2}");
        }

        // Controlled live Hattrick observations: one normal W-L in 0-1-0.
        var wingerFixtures = new[]
        {
            new WingerFixture("Şaban Savlet", 4, 5, 6, 5, 6, 4, 3, 1.25, 1.00, 1.00, 1.75, 1.00),
            new WingerFixture("Bumin Pehlivanlar", 4, 4, 7, 5, 5, 7, 2, 1.25, 1.00, 1.00, 2.25, 1.00),
            new WingerFixture("Mikel Thiebault", 4, 5, 9, 4, 6, 8, 2, 1.50, 1.00, 1.00, 2.25, 1.00),
            new WingerFixture("Sergen Gözay", 6, 5, 4, 5, 4, 7, 2, 1.50, 1.00, 1.00, 2.25, 1.00),
            new WingerFixture("Adrian Beța", 4, 5, 9, 3, 7, 7, 3, 1.25, 1.00, 1.00, 2.00, 1.00),
            new WingerFixture("Dawid Nocoń", 17, 3, 5, 4, 5, 7, 12, 2.75, 1.50, 1.00, 2.00, 1.00),
            new WingerFixture("Nándor Dobóvári", 3, 5, 8, 17, 7, 7, 3, 1.25, 1.00, 1.00, 5.00, 1.00),
            new WingerFixture("Milen Bozev", 5, 12, 9, 14, 6, 8, 8, 1.50, 1.00, 1.50, 5.00, 1.00),
            new WingerFixture("Manuel Gobiet", 9, 11, 9, 15, 6, 8, 7, 1.75, 1.25, 1.50, 5.00, 1.00)
        };

        foreach (var fixture in wingerFixtures)
        {
            var player = new Player(
                3000 + Array.IndexOf(wingerFixtures, fixture),
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

            var lineup = new Lineup("W-L", "0-1-0",
            [
                new Slot("W-L", "W-L", "14-position W-L regression", player.Name, player.Id, 0, 0, 0)
            ]);

            var actual = engine.CalculateLineup(lineup, [player], RatingContext.Default);

            if (Math.Abs(actual.RawLeftDefence - fixture.ExpectedLeftDefence) > .20)
                failures.Add($"{fixture.Name} W-L DEF-L: expected {fixture.ExpectedLeftDefence:F2}, got {actual.RawLeftDefence:F2}");
            if (Math.Abs(actual.RawCentralDefence - fixture.ExpectedCentralDefence) > .20)
                failures.Add($"{fixture.Name} W-L DEF-C: expected {fixture.ExpectedCentralDefence:F2}, got {actual.RawCentralDefence:F2}");
            if (Math.Abs(actual.RawMidfield - fixture.ExpectedMidfield) > .20)
                failures.Add($"{fixture.Name} W-L MID: expected {fixture.ExpectedMidfield:F2}, got {actual.RawMidfield:F2}");
            if (Math.Abs(actual.RawLeftAttack - fixture.ExpectedLeftAttack) > .20)
                failures.Add($"{fixture.Name} W-L ATT-L: expected {fixture.ExpectedLeftAttack:F2}, got {actual.RawLeftAttack:F2}");

            // Hattrick sector ratings have a 1.00 visible floor in these
            // single-player screenshots; central/right attack remain absent.
            if (Math.Abs(Math.Max(1.0, actual.RawCentralAttack) - fixture.ExpectedCentralAttack) > .20)
                failures.Add($"{fixture.Name} W-L ATT-C: expected {fixture.ExpectedCentralAttack:F2}, got {Math.Max(1.0, actual.RawCentralAttack):F2}");
        }

        if (failures.Count > 0)
        {
            foreach (var failure in failures)
                Console.WriteLine("FAIL: " + failure);
            return 1;
        }

        Console.WriteLine("PASS: 10 DEF-CL + 8 DEF-C + 9 W-L controlled fixtures match the empirical calibration.");
        return 0;
    }

    private readonly record struct WingerFixture(
        string Name,
        double Defending,
        double Playmaking,
        double Passing,
        double Winger,
        double Stamina,
        double Form,
        double Experience,
        double ExpectedLeftDefence,
        double ExpectedCentralDefence,
        double ExpectedMidfield,
        double ExpectedLeftAttack,
        double ExpectedCentralAttack);

    private readonly record struct DefCFixture(
        string Name,
        double Defending,
        double Playmaking,
        double Passing,
        double Winger,
        double Stamina,
        double Form,
        double Experience,
        double ExpectedSide,
        double ExpectedCentral);

    private readonly record struct Fixture(
        string Name,
        double Defending,
        double Playmaking,
        double Passing,
        double Winger,
        double Stamina,
        double Form,
        double Experience,
        double ExpectedLeft,
        double ExpectedCentral);
}
