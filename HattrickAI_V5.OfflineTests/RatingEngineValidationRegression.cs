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
        var canonical = ReadRating(analysis.GetProperty("ownRating"));
        var request = new RatingEngineRequest(lineup, players, RatingContext.Default, canonical);
        var registry = new RatingEngineRegistry();
        var results = registry.All.Select(engine => engine.Calculate(request)).ToDictionary(x => x.Engine);

        if (results.Count != 4) throw new InvalidOperationException($"Expected 4 rating engine results, got {results.Count}.");
        CheckRawSectors(canonical, ExpectedV5, "fixture V5 ground truth");
        CheckFinite(results[RatingEngineKind.V5].Rating, "V5 engine");
        CheckFinite(results[RatingEngineKind.HO].Rating, "HO engine");
        CheckFinite(results[RatingEngineKind.HattrickDash].Rating, "HattrickDash engine");
        CheckRawSectors(results[RatingEngineKind.Foxtrick].Rating, ExpectedV5, "Foxtrick canonical sector source");

        // Production display values must stay in each engine's native rating
        // scale. The old Stage-2 converter must never be applied to these values.
        foreach (var kind in Enum.GetValues<RatingEngineKind>())
        {
            CheckDisplayMatchesNativeScale(results[kind].Rating, $"{kind} engine display");
            var adjusted = ConfidenceRatingAdjuster.Apply(results[kind].Rating, 4);
            CheckDisplayMatchesNativeScale(adjusted, $"{kind} confidence-adjusted display");
        }

        foreach (var kind in new[] { RatingEngineKind.V5, RatingEngineKind.HO, RatingEngineKind.HattrickDash })
        {
            var values = DisplayValues(results[kind].Rating).ToArray();
            Console.WriteLine($"{kind}: {string.Join('/', values.Select(x => x.ToString("0.###")))}");
        }

        var fox = results[RatingEngineKind.Foxtrick];
        CheckNear(fox.HatStats!.Value, 317.36089615638735, 1e-9, "Fox HatStats");
        CheckNear(fox.LoddarStats!.Value, 23.78, 1e-9, "Fox LoddarStats");

        var comparison = new RatingEngineComparisonService(registry).Compare(request, RatingEngineKind.Foxtrick);
        if (comparison.Baseline != RatingEngineKind.V5 || comparison.Selected != RatingEngineKind.Foxtrick || comparison.Rows.Count != 4)
            throw new InvalidOperationException("Rating engine comparison contract drift.");
        var selected = comparison.Rows.Single(x => x.Engine == RatingEngineKind.Foxtrick);
        CheckRawSectors(selected.Rating, ExpectedV5, "comparison Foxtrick canonical source");
        foreach (var row in comparison.Rows)
        {
            CheckDisplayMatchesNativeScale(row.Rating, $"comparison {row.Engine} display");
            Console.WriteLine($"{row.Name}: MIDΔ={row.MidfieldDeltaVsV5:0.###} DEF-CΔ={row.CentralDefenceDeltaVsV5:0.###} ATT-CΔ={row.CentralAttackDeltaVsV5:0.###}");
        }

        Console.WriteLine("RatingEngineValidationRegression PASS");
        Console.WriteLine("Canonical full CHPP fixture validated across V5 / HO / HattrickDash / Foxtrick.");
        Console.WriteLine("Production display isolation validated: no Stage-2 nonlinear compression in the production rating path.");
        return 0;
    }

    private static void CheckFinite(RegionalRatingSnapshot snapshot, string label)
    {
        if (DisplayValues(snapshot).Any(x => !double.IsFinite(x))) throw new InvalidOperationException($"{label} returned non-finite sector rating.");
    }

    private static void CheckRawSectors(RegionalRatingSnapshot snapshot, double[] expected, string label)
    {
        var actual = RawValues(snapshot).ToArray();
        for (var i = 0; i < expected.Length; i++) CheckNear(actual[i], expected[i], 1e-9, $"{label} sector {i}");
    }

    private static void CheckDisplayMatchesNativeScale(RegionalRatingSnapshot snapshot, string label)
    {
        var raw = RawValues(snapshot).ToArray();
        var display = DisplayValues(snapshot).ToArray();
        for (var i = 0; i < raw.Length; i++)
        {
            var expected = RegionalRatingEngine.Display(raw[i]);
            CheckNear(display[i], expected, 1e-9, $"{label} sector {i}");
        }
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
