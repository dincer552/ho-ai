using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class RatingEngineValidationRegression
{
    private const string FixturePath = "HattrickAI_V5.OfflineTests/Fixtures/HO_Real_CHPP_Fixture_2026-09-01.json";

    private static readonly double[] ExpectedV5 = { 8.814876331125825, 15.131275059602647, 8.755167139072846, 5.3073582240775785, 9.3881423692354, 10.733139483443706, 8.34554898438368 };
    private static readonly double[] ExpectedHO = { 8.9278407632371284, 14.241616934311249, 8.8845667124997263, 5.437522215250981, 8.1350636338080307, 9.5241231738830496, 7.647351119659394 };
    private static readonly double[] ExpectedDash = { 13.033333333333333, 13.033333333333333, 13.033333333333333, 12.64, 11.85, 11.85, 11.85 };

    public static int Run()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(FixturePath));
        var root = document.RootElement;
        var fixturePlayers = root.GetProperty("players").EnumerateArray().Select(ToPlayer).ToList();
        var fixtureSlots = root.GetProperty("players").EnumerateArray().Select((p, i) =>
        {
            var id = p.GetProperty("id").GetInt32();
            var name = p.GetProperty("name").GetString() ?? id.ToString();
            var code = p.GetProperty("slot").GetString() ?? throw new InvalidOperationException("Missing slot code.");
            var order = (PlayerOrder)p.GetProperty("order").GetInt32();
            return new Slot(code, code, "validation fixture", name, id, 0, i, 0, order);
        }).ToList();
        var lineup = new Lineup(root.GetProperty("teamName").GetString() ?? "RealFixture", root.GetProperty("formation").GetString() ?? "3-5-2", fixtureSlots);
        var request = new RatingEngineRequest(lineup, fixturePlayers, RatingContext.Default);
        var registry = new RatingEngineRegistry();
        var results = registry.All.Select(engine => engine.Calculate(request)).ToDictionary(x => x.Engine);

        if (results.Count != 4)
            throw new InvalidOperationException($"Expected 4 rating engine results, got {results.Count}.");

        CheckSectors(results[RatingEngineKind.V5].Rating, ExpectedV5, "V5");
        CheckSectors(results[RatingEngineKind.HO].Rating, ExpectedHO, "HO");
        CheckSectors(results[RatingEngineKind.HattrickDash].Rating, ExpectedDash, "HattrickDash");
        CheckSectors(results[RatingEngineKind.Foxtrick].Rating, ExpectedV5, "Foxtrick sector source");

        var fox = results[RatingEngineKind.Foxtrick];
        CheckNear(fox.HatStats!.Value, 317.36089615638735, 1e-9, "Fox HatStats");
        CheckNear(fox.LoddarStats!.Value, 23.78, 1e-9, "Fox LoddarStats");

        var comparison = new RatingEngineComparisonService(registry).Compare(request, RatingEngineKind.V5);
        if (comparison.Baseline != RatingEngineKind.V5 || comparison.Selected != RatingEngineKind.V5 || comparison.Rows.Count != 4)
            throw new InvalidOperationException("Rating engine comparison contract drift.");

        Console.WriteLine("RatingEngineValidationRegression PASS");
        Console.WriteLine("Real CHPP fixture validated across V5 / HO / HattrickDash / Foxtrick.");
        return 0;
    }

    private static void CheckSectors(RegionalRatingSnapshot snapshot, double[] expected, string label)
    {
        var actual = Values(snapshot).ToArray();
        for (var i = 0; i < expected.Length; i++)
            CheckNear(actual[i], expected[i], 1e-9, $"{label} sector {i}");
    }

    private static void CheckNear(double actual, double expected, double tolerance, string label)
    {
        if (Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{label}: expected {expected:R}, got {actual:R}");
    }

    private static IEnumerable<double> Values(RegionalRatingSnapshot s)
    {
        yield return s.LeftDefence; yield return s.CentralDefence; yield return s.RightDefence;
        yield return s.Midfield; yield return s.LeftAttack; yield return s.CentralAttack; yield return s.RightAttack;
    }

    private static Player ToPlayer(JsonElement p) => new(
        p.GetProperty("id").GetInt32(),
        p.GetProperty("name").GetString() ?? "",
        p.GetProperty("keeper").GetInt32(),
        p.GetProperty("defending").GetInt32(),
        p.GetProperty("playmaking").GetInt32(),
        p.GetProperty("passing").GetInt32(),
        p.GetProperty("winger").GetInt32(),
        p.GetProperty("scoring").GetInt32(),
        p.GetProperty("stamina").GetInt32(),
        p.GetProperty("form").GetInt32(),
        p.GetProperty("experience").GetInt32(),
        p.GetProperty("loyalty").GetInt32(),
        -1,
        (PlayerSpecialty)p.GetProperty("specialty").GetInt32(),
        0);
}
