using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class CustomV5LineupRegression
{
    public static int Run()
    {
        var players = Enumerable.Range(1, 11)
            .Select(id => new Player(id, $"P{id}", 1, 10 + id % 5, 5 + id % 6, 5, 8, 10, 7, 7, 3))
            .ToArray();

        var slots = new[]
        {
            "GK", "DEF-L", "DEF-C", "DEF-R",
            "W-L", "W-R",
            "FW-L", "FW-C", "FW-R",
            "IM-L", "IM-R"
        }.Select((code, i) => new Slot(code, code, "", players[i].Name, players[i].Id, 0, 0, 0)).ToArray();

        var custom = new Lineup("Custom XI", "3-2-3", slots);
        var resolvedLeft = RatingPositionResolver.Resolve(custom, "DEF-L");
        var resolvedRight = RatingPositionResolver.Resolve(custom, "DEF-R");
        if (resolvedLeft != RegionalPosition.CentralDefender || resolvedRight != RegionalPosition.CentralDefender)
            return Fail("3-2-3 DEF-L/DEF-R must resolve as central defenders.");

        var rating = new V5RatingEngine().Calculate(new RatingEngineRequest(custom, players, RatingContext.Default)).Rating;
        var values = new[]
        {
            rating.LeftDefence, rating.CentralDefence, rating.RightDefence, rating.Midfield,
            rating.LeftAttack, rating.CentralAttack, rating.RightAttack
        };
        if (values.Any(x => !double.IsFinite(x)))
            return Fail("Custom XI produced a non-finite V5 rating.");

        Console.WriteLine("PASS: V5 accepts arbitrary XI slot assignments (3-2-3)");
        return 0;
    }

    private static int Fail(string message)
    {
        Console.WriteLine("FAIL: " + message);
        return 1;
    }
}
