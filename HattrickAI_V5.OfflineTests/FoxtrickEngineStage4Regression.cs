using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class FoxtrickEngineStage4Regression
{
    private const string FixturePath = "TestJSON/HattrickAI_V5_CHPP_FullOffline_2026-09-01.json";
    private static readonly double[] ExpectedV5RawSectors = { 8.814876331125825, 15.131275059602647, 8.755167139072846, 5.3073582240775785, 9.3881423692354, 10.733139483443706, 8.34554898438368 };
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
        CheckNear(normal.LoddarStats, 36.74, .0001, "synthetic LoddarStats", failures);
        CheckNear(normal.PeasoStats, 41.0, .0001, "synthetic PeasoStats", failures);
        CheckNear(normal.VnukStats, 11.0, .0001, "synthetic VnukStats", failures);
        CheckNear(normal.HTitaVal, 344.4, .0001, "synthetic HTitaVal", failures);
        CheckNear(normal.GardierStats, 410.0, .0001, "synthetic GardierStats", failures);

        using var document = JsonDocument.Parse(File.ReadAllText(FixturePath));
        var root = document.RootElement;
        var normalized = root.GetProperty("normalized");
        var analysis = root.GetProperty("v5Analysis");
        var players = normalized.GetProperty("ownPlayers").EnumerateArray().Select(ToPlayer).ToList();
        var lineup = ReadLineup(analysis.GetProperty("ownLineup"));
        var canonical = ReadRating(analysis.GetProperty("ownRating"));
        var request = new RatingEngineRequest(lineup, players, RatingContext.Default, canonical);

        var fixtureRawValues = RawValues(canonical).ToArray();
        for (var i = 0; i < ExpectedV5RawSectors.Length; i++)
            CheckNear(fixtureRawValues[i], ExpectedV5RawSectors[i], 1e-9, $"fixture V5 raw sector {i}", failures);

        var engine = new FoxtrickEngine();
        var fox = engine.Calculate(request);
        var dash = new HattrickDashEngine().Calculate(request);
        var ho = new HOEngineAdapter().Calculate(request);
        var foxValues = RawValues(fox.Rating).ToArray();
        for (var i = 0; i < ExpectedV5RawSectors.Length; i++)
            CheckNear(foxValues[i], ExpectedV5RawSectors[i], 1e-9, $"Foxtrick/V5 raw sector {i}", failures);

        var hoText = string.Join("/", Values(ho.Rating).Select(x => x.ToString("0.###")));
        var dashText = string.Join("/", Values(dash.Rating).Select(x => x.ToString("0.###")));
        Check(Values(dash.Rating).All(double.IsFinite), "Dash real-fixture rating finite", failures);
        Check(Values(ho.Rating).All(double.IsFinite), "HO real-fixture rating finite", failures);
        Console.WriteLine("Real fixture engine comparison: HO=" + hoText + " | Dash=" + dashText);

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
        Console.WriteLine("Real CHPP V5 fixture locked: canonical V5 rating -> Foxtrick statistics; HO/Dash comparison finite.");
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
        p.TryGetProperty("loyalty", out var loyalty) ? loyalty.GetInt32() : 0,
        p.TryGetProperty("injuryLevel", out var injury) ? injury.GetInt32() : -1,
        p.TryGetProperty("specialty", out var specialty) ? (PlayerSpecialty)specialty.GetInt32() : PlayerSpecialty.None,
        p.TryGetProperty("setPiecesSkill", out var setPieces) ? setPieces.GetInt32() : 0);

    private static Lineup ReadLineup(JsonElement e)
    {
        var slots = e.GetProperty("slots").EnumerateArray().Select(s => new Slot(
            s.GetProperty("code").GetString() ?? "",
            s.TryGetProperty("label", out var label) ? label.GetString() ?? "" : "",
            s.TryGetProperty("description", out var description) ? description.GetString() ?? "" : "",
            s.TryGetProperty("playerName", out var name) ? name.GetString() : null,
            s.GetProperty("playerId").GetInt32(),
            s.TryGetProperty("rating", out var rating) ? rating.GetDouble() : 0,
            s.TryGetProperty("x", out var x) ? x.GetDouble() : 0,
            s.TryGetProperty("y", out var y) ? y.GetDouble() : 0)).ToList();
        return new Lineup(e.GetProperty("teamName").GetString() ?? "", e.GetProperty("formation").GetString() ?? "", slots);
    }

    private static RegionalRatingSnapshot ReadRating(JsonElement e) => new(
        e.GetProperty("rawLeftDefence").GetDouble(), e.GetProperty("rawCentralDefence").GetDouble(), e.GetProperty("rawRightDefence").GetDouble(), e.GetProperty("rawMidfield").GetDouble(),
        e.GetProperty("rawLeftAttack").GetDouble(), e.GetProperty("rawCentralAttack").GetDouble(), e.GetProperty("rawRightAttack").GetDouble(),
        e.GetProperty("leftDefence").GetDouble(), e.GetProperty("centralDefence").GetDouble(), e.GetProperty("rightDefence").GetDouble(), e.GetProperty("midfield").GetDouble(),
        e.GetProperty("leftAttack").GetDouble(), e.GetProperty("centralAttack").GetDouble(), e.GetProperty("rightAttack").GetDouble());

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
