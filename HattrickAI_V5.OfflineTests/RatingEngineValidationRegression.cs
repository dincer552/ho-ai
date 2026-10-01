using System;
using System.Linq;
using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>Validates real-fixture production output through the HatFor-only registry.</summary>
public static class RatingEngineValidationRegression
{
    private const string FixturePath = "TestJSON/HattrickAI_V5_CHPP_FullOffline_2026-09-01.json";

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
        if (registry.All.Count != 1 || registry.All[0].Kind != RatingEngineKind.HatFor)
            throw new InvalidOperationException("Production validation requires a single HatFor registry entry.");

        var routed = registry.Calculate(RatingEngineKind.HatFor, request);
        var direct = new HatForRatingEngine().Calculate(request);
        if (routed.Engine != RatingEngineKind.HatFor || direct.Engine != RatingEngineKind.HatFor)
            throw new InvalidOperationException("Real fixture did not execute HatFor.");
        CheckFinite(routed.Rating);
        CheckSame(routed.Rating, direct.Rating, "registry/direct HatFor parity");
        CheckNativeDisplay(routed.Rating, "HatFor fixture display");

        var confidenceNeutral = ConfidenceRatingAdjuster.Apply(routed.Rating, 4);
        CheckSame(routed.Rating, confidenceNeutral, "neutral confidence must preserve HatFor output");
        CheckNativeDisplay(confidenceNeutral, "HatFor confidence-adjusted display");

        foreach (var alias in new[] { "V5", "HO", "HattrickDash", "Foxtrick" })
        {
            if (!RatingEngineKindParse.TryParse(alias, out var kind) || kind != RatingEngineKind.HatFor)
                throw new InvalidOperationException($"{alias} did not normalize to HatFor.");
            if (registry.Get(kind).Kind != RatingEngineKind.HatFor)
                throw new InvalidOperationException($"{alias} did not route to HatFor.");
        }

        Console.WriteLine("RatingEngineValidationRegression PASS");
        Console.WriteLine("Real CHPP fixture and legacy aliases execute the HatFor production engine only.");
        return 0;
    }

    private static void CheckFinite(RegionalRatingSnapshot s)
    {
        if (Values(s).Any(x => !double.IsFinite(x)))
            throw new InvalidOperationException("HatFor returned a non-finite sector rating.");
    }

    private static void CheckNativeDisplay(RegionalRatingSnapshot s, string label)
    {
        var raw = RawValues(s).ToArray();
        var display = Values(s).ToArray();
        for (var i = 0; i < raw.Length; i++) CheckNear(display[i], raw[i], 1e-12, $"{label} sector {i}");
    }

    private static void CheckSame(RegionalRatingSnapshot a, RegionalRatingSnapshot b, string label)
    {
        var av = RawValues(a).Concat(Values(a)).ToArray();
        var bv = RawValues(b).Concat(Values(b)).ToArray();
        for (var i = 0; i < av.Length; i++) CheckNear(av[i], bv[i], 1e-12, $"{label} sector {i}");
    }

    private static void CheckNear(double actual, double expected, double tolerance, string label)
    {
        if (Math.Abs(actual - expected) > tolerance)
            throw new InvalidOperationException($"{label}: expected {expected:R}, got {actual:R}.");
    }

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
}