using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class RatingEngineValidationRegression
{
    private const string FixturePath = "TestJSON/HattrickAI_V5_CHPP_FullOffline_2026-09-01.json";
    private static readonly double[] ExpectedV5 = { 8.814876331125825, 15.131275059602647, 8.755167139072846, 5.3073582240775785, 9.3881423692354, 10.733139483443706, 8.34554898438368 };

    public static int Run()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(FixturePath));
        var root = document.RootElement;
        var normalized = root.GetProperty("normalized");
        var analysis = root.GetProperty("v5Analysis");
        var players = normalized.GetProperty("ownPlayers").EnumerateArray().Select(ToPlayer).ToList();
        var lineup = ReadLineup(analysis.GetProperty("ownLineup"));
        var request = new RatingEngineRequest(lineup, players, RatingContext.Default);
        var registry = new RatingEngineRegistry();
        var results = registry.All.Select(engine => engine.Calculate(request)).ToDictionary(x => x.Engine);

        if (results.Count != 4) throw new InvalidOperationException($"Expected 4 rating engine results, got {results.Count}.");
        var fixtureV5 = ReadRating(analysis.GetProperty("ownRating"));
        CheckRawSectors(fixtureV5, ExpectedV5, "fixture V5 ground truth");
        CheckRawSectors(results[RatingEngineKind.V5].Rating, ExpectedV5, "V5 engine");
        CheckRawSectors(results[RatingEngineKind.Foxtrick].Rating, ExpectedV5, "Foxtrick canonical sector source");

        foreach (var kind in new[] { RatingEngineKind.HO, RatingEngineKind.HattrickDash })
        {
            var values = DisplayValues(results[kind].Rating).ToArray();
            if (values.Any(x => !double.IsFinite(x))) throw new InvalidOperationException($"{kind} returned non-finite sector rating.");
            Console.WriteLine($"{kind}: {string.Join('/', values.Select(x => x.ToString("0.###")))}");
        }

        var fox = results[RatingEngineKind.Foxtrick];
        CheckNear(fox.HatStats!.Value, 317.36089615638735, 1e-9, "Fox HatStats");
        CheckNear(fox.LoddarStats!.Value, 23.78, 1e-9, "Fox LoddarStats");

        var comparison = new RatingEngineComparisonService(registry).Compare(request, RatingEngineKind.V5);
        if (comparison.Baseline != RatingEngineKind.V5 || comparison.Selected != RatingEngineKind.V5 || comparison.Rows.Count != 4)
            throw new InvalidOperationException("Rating engine comparison contract drift.");
        foreach (var row in comparison.Rows)
            Console.WriteLine($"{row.Name}: MIDΔ={row.MidfieldDeltaVsV5:0.###} DEF-CΔ={row.CentralDefenceDeltaVsV5:0.###} ATT-CΔ={row.CentralAttackDeltaVsV5:0.###}");

        Console.WriteLine("RatingEngineValidationRegression PASS");
        Console.WriteLine("Canonical full CHPP fixture validated across V5 / HO / HattrickDash / Foxtrick.");
        return 0;
    }

    private static void CheckRawSectors(RegionalRatingSnapshot snapshot, double[] expected, string label)
    {
        var actual = RawValues(snapshot).ToArray();
        for (var i = 0; i < expected.Length; i++) CheckNear(actual[i], expected[i], 1e-9, $"{label} sector {i}");
    }

    private static void CheckNear(double actual, double expected, double tolerance, string label)
    {
        if (Math.Abs(actual - expected) > tolerance) throw new InvalidOperationException($"{label}: expected {expected:R}, got {actual:R}");
    }

    private static IEnumerable<double> RawValues(RegionalRatingSnapshot s)
    {
        yield return s.RawLeftDefence; yield return s.RawCentralDefence; yield return s.RawRightDefence; yield return s.RawMidfield;
        yield return s.RawLeftAttack; yield return s.RawCentralAttack; yield return s.RawRightAttack;
    }

    private static IEnumerable<double> DisplayValues(RegionalRatingSnapshot s)
    {
        yield return s.LeftDefence; yield return s.CentralDefence; yield return s.RightDefence; yield return s.Midfield;
        yield return s.LeftAttack; yield return s.CentralAttack; yield return s.RightAttack;
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
}
