using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class FoxtrickEngineStage4Regression
{
    private const string FixturePath = "HattrickAI_V5.OfflineTests/Fixtures/HO_Real_CHPP_Fixture_2026-09-01.json";
    private static readonly double[] ExpectedV5RawSectors = { 8.814876331125825, 15.131275059602647, 8.755167139072846, 5.3073582240775785, 9.3881423692354, 10.733139483443706, 8.34554898438368 };
    private static readonly double[] ExpectedDashSectors = { 13.033333333333333, 13.033333333333333, 13.033333333333333, 12.64, 11.85, 11.85, 11.85 };
    private const double ExpectedDashHatStats = 112.57;
    private const double ExpectedDashLoddarStats = 25.081666666666666;
    private const double ExpectedFoxHatStats = 317.36089615638735;
    private const double ExpectedFoxLoddarStats = 23.78;
    private const double ExpectedFoxPeasoStats = 33.04;
    private const double ExpectedFoxVnukStats = 8.97;
    private const double ExpectedFoxHTitaVal = 301.7;
    private const double ExpectedFoxGardierStats = 335;

    public static int Run()
    {
        var failures = new List<string>();
        var synthetic = new RegionalRatingSnapshot(10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10);
        var normal = FoxtrickRatingStatistics.Calculate(synthetic, TeamTactic.Normal);
        CheckNear(normal.HatStats, 369.0, .0001, "synthetic HatStats", failures);
        CheckNear(normal.LoddarStats, 36.74065979099788, .0001, "synthetic LoddarStats", failures);
        CheckNear(normal.PeasoStats, 41.0, .0001, "synthetic PeasoStats", failures);
        CheckNear(normal.VnukStats, 11.0, .0001, "synthetic VnukStats", failures);
        CheckNear(normal.HTitaVal, 344.4, .0001, "synthetic HTitaVal", failures);
        CheckNear(normal.GardierStats, 410.0, .0001, "synthetic GardierStats", failures);

        var engine = new FoxtrickEngine();
        var players = Enumerable.Range(1, 11).Select(i => new Player(i, $"P{i}", 1, 10, 10, 10, 10, 10, 7, 7, 5)).ToList();
        var slots = new[] { "GK", "DEF-L", "DEF-CL", "DEF-C", "DEF-CR", "W-L", "IM-L", "IM-C", "IM-R", "W-R", "FW-C" }
            .Select((code, i) => new Slot(code, code, "stage4", players[i].Name, players[i].Id, 0, 0, 0)).ToList();
        var result = engine.Calculate(new RatingEngineRequest(new Lineup("Stage4", "3-5-2", slots), players, RatingContext.Default));
        Check(result.Engine == RatingEngineKind.Foxtrick, "engine kind", failures);
        Check(result.HatStats.HasValue && result.LoddarStats.HasValue, "common stats returned", failures);

        using var document = JsonDocument.Parse(File.ReadAllText(FixturePath));
        var root = document.RootElement;
        var fixturePlayers = root.GetProperty("players").EnumerateArray().Select(ToPlayer).ToList();
        var fixtureSlots = root.GetProperty("players").EnumerateArray().Select((p, i) =>
        {
            var id = p.GetProperty("id").GetInt32();
            var name = p.GetProperty("name").GetString() ?? id.ToString();
            var code = p.GetProperty("slot").GetString() ?? throw new InvalidOperationException("Missing slot code.");
            var order = (PlayerOrder)p.GetProperty("order").GetInt32();
            return new Slot(code, code, "real CHPP fixture", name, id, 0, i, 0, order);
        }).ToList();
        var lineup = new Lineup(root.GetProperty("teamName").GetString() ?? "RealFixture", root.GetProperty("formation").GetString() ?? "3-5-2", fixtureSlots);
        var request = new RatingEngineRequest(lineup, fixturePlayers, RatingContext.Default);
        var fox = engine.Calculate(request);
        var dash = new HattrickDashEngine().Calculate(request);
        var ho = new HOEngineAdapter().Calculate(request);
        var foxValues = RawValues(fox.Rating).ToArray();
        var dashValues = Values(dash.Rating).ToArray();
        var hoValues = Values(ho.Rating).ToArray();
        for (var i = 0; i < ExpectedV5RawSectors.Length; i++)
        {
            CheckNear(foxValues[i], ExpectedV5RawSectors[i], 1e-9, $"Foxtrick/V5 raw sector {i}", failures);
            CheckNear(dashValues[i], ExpectedDashSectors[i], 1e-9, $"Dash sector {i}", failures);
        }
        var expectedHo = new[] { 8.9278407632371284, 14.241616934311249, 8.8845667124997263, 5.437522215250981, 8.1350636338080307, 9.5241231738830496, 7.647351119659394 };
        for (var i = 0; i < expectedHo.Length; i++) CheckNear(hoValues[i], expectedHo[i], 1e-9, $"HO sector {i}", failures);

        CheckNear(dash.HatStats!.Value, ExpectedDashHatStats, 1e-9, "Dash HatStats", failures);
        CheckNear(dash.LoddarStats!.Value, ExpectedDashLoddarStats, 1e-9, "Dash LoddarStats", failures);
        CheckNear(fox.HatStats!.Value, ExpectedFoxHatStats, 1e-9, "Foxtrick HatStats", failures);
        CheckNear(fox.LoddarStats!.Value, ExpectedFoxLoddarStats, 1e-9, "Foxtrick LoddarStats", failures);
        var foxStats = FoxtrickRatingStatistics.Calculate(fox.Rating, TeamTactic.Normal);
        CheckNear(foxStats.PeasoStats, ExpectedFoxPeasoStats, 1e-9, "Foxtrick PeasoStats", failures);
        CheckNear(foxStats.VnukStats, ExpectedFoxVnukStats, 1e-9, "Foxtrick VnukStats", failures);
        CheckNear(foxStats.HTitaVal, ExpectedFoxHTitaVal, 1e-9, "Foxtrick HTitaVal", failures);
        CheckNear(foxStats.GardierStats, ExpectedFoxGardierStats, 1e-9, "Foxtrick GardierStats", failures);

        if (failures.Count > 0)
        {
            Console.Error.WriteLine("FoxtrickEngineStage4Regression FAILED");
            foreach (var failure in failures) Console.Error.WriteLine($" - {failure}");
            return 1;
        }
        Console.WriteLine("FoxtrickEngineStage4Regression PASS");
        Console.WriteLine("Real CHPP fixture locked: V5/Foxtrick vs HO vs HattrickDash.");
        return 0;
    }

    private static void Check(bool condition, string label, ICollection<string> failures)
    {
        if (!condition) failures.Add($"{label}: condition was false");
    }

    private static Player ToPlayer(JsonElement p) => new(
        p.GetProperty("id").GetInt32(), p.GetProperty("name").GetString() ?? "",
        p.GetProperty("keeper").GetInt32(), p.GetProperty("defending").GetInt32(), p.GetProperty("playmaking").GetInt32(),
        p.GetProperty("passing").GetInt32(), p.GetProperty("winger").GetInt32(), p.GetProperty("scoring").GetInt32(),
        p.GetProperty("stamina").GetInt32(), p.GetProperty("form").GetInt32(), p.GetProperty("experience").GetInt32(),
        p.GetProperty("loyalty").GetInt32(), -1, (PlayerSpecialty)p.GetProperty("specialty").GetInt32(), 0);

    private static IEnumerable<double> RawValues(RegionalRatingSnapshot s)
    {
        yield return s.RawLeftDefence; yield return s.RawCentralDefence; yield return s.RawRightDefence; yield return s.RawMidfield;
        yield return s.RawLeftAttack; yield return s.RawCentralAttack; yield return s.RawRightAttack;
    }

    private static IEnumerable<double> Values(RegionalRatingSnapshot s)
    {
        yield return s.LeftDefence; yield return s.CentralDefence; yield return s.RightDefence; yield return s.Midfield;
        yield return s.LeftAttack; yield return s.CentralAttack; yield return s.RightAttack;
    }

    private static void CheckNear(double actual, double expected, double tolerance, string label, ICollection<string> failures)
    {
        if (Math.Abs(actual - expected) > tolerance) failures.Add($"{label}: expected {expected:0.############}, got {actual:0.############}");
    }
}
