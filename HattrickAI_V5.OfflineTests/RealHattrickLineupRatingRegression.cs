using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class RealHattrickLineupRatingRegression
{
    private static readonly string[] FixturePaths =
    {
        "TestJSON/RealHattrickMatch_769648184_343_2026-09-13.json",
        "TestJSON/RealHattrickMatch_769648184_343_2026-09-13_v2.json"
    };

    public static int Run()
    {
        var failures = new List<string>();
        foreach (var fixturePath in FixturePaths)
            RunFixture(fixturePath, failures);

        if (failures.Count > 0)
        {
            Console.Error.WriteLine("RealHattrickLineupRatingRegression FAILED");
            foreach (var failure in failures) Console.Error.WriteLine(" - " + failure);
            return 1;
        }

        Console.WriteLine("RealHattrickLineupRatingRegression PASS");
        Console.WriteLine("Real Hattrick snapshots tested: " + FixturePaths.Length);
        return 0;
    }

    private static void RunFixture(string fixturePath, ICollection<string> failures)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(fixturePath));
        var root = document.RootElement;
        var players = root.GetProperty("players").EnumerateArray().Select(ToPlayer).ToList();
        var lineup = ReadLineup(root.GetProperty("lineup"));
        var expectedHattrick = Values(root.GetProperty("hattrickRating")).ToArray();
        var request = new RatingEngineRequest(lineup, players, RatingContext.Default);
        var registry = new RatingEngineRegistry();
        var label = Path.GetFileNameWithoutExtension(fixturePath);
        var hasBaseline = root.TryGetProperty("engineBaseline", out var baseline);

        Console.WriteLine($"=== Real Hattrick fixture: {label} ===");
        Console.WriteLine($"Hattrick UI: {string.Join(" / ", expectedHattrick.Select(v => v.ToString("0.##")))}");

        foreach (var engine in registry.All)
        {
            var result = engine.Calculate(request);
            var values = Values(result.Rating).ToArray();
            Check(values.Length == 7, $"{label}/{engine.Name}: seven sectors", failures);
            Check(values.All(double.IsFinite), $"{label}/{engine.Name}: finite sectors", failures);
            Check(result.Engine == engine.Kind, $"{label}/{engine.Name}: identity", failures);

            var mae = values.Zip(expectedHattrick, (actual, target) => Math.Abs(actual - target)).Average();
            Console.WriteLine($"{engine.Name}: {string.Join(" / ", values.Select(v => v.ToString("0.########")))} | MAE={mae:0.####}");

            if (hasBaseline && baseline.TryGetProperty(engine.Kind.ToString(), out var expectedEngine))
            {
                var locked = expectedEngine.EnumerateArray().Select(x => x.GetDouble()).ToArray();
                Check(locked.Length == 7, $"{label}/{engine.Name}: locked baseline length", failures);
                for (var i = 0; i < Math.Min(7, locked.Length); i++)
                    CheckNear(values[i], locked[i], 1e-7, $"{label}/{engine.Name} locked sector {i}", failures);
            }
            else if (!hasBaseline)
            {
                Console.WriteLine($"BASELINE_CAPTURE {engine.Kind}: [{string.Join(",", values.Select(v => v.ToString("0.########")))}]");
            }

            for (var i = 0; i < 7; i++)
                Check(double.IsFinite(values[i] - expectedHattrick[i]), $"{label}/{engine.Name} Hattrick delta {i} finite", failures);

            var rerun = engine.Calculate(request);
            var rerunValues = Values(rerun.Rating).ToArray();
            for (var i = 0; i < 7; i++)
                CheckNear(rerunValues[i], values[i], 1e-12, $"{label}/{engine.Name} deterministic sector {i}", failures);
        }
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

    private static IEnumerable<double> Values(RegionalRatingSnapshot s)
    {
        yield return s.LeftDefence; yield return s.CentralDefence; yield return s.RightDefence; yield return s.Midfield;
        yield return s.LeftAttack; yield return s.CentralAttack; yield return s.RightAttack;
    }

    private static IEnumerable<double> Values(JsonElement e)
    {
        yield return e.GetProperty("leftDefence").GetDouble(); yield return e.GetProperty("centralDefence").GetDouble(); yield return e.GetProperty("rightDefence").GetDouble(); yield return e.GetProperty("midfield").GetDouble();
        yield return e.GetProperty("leftAttack").GetDouble(); yield return e.GetProperty("centralAttack").GetDouble(); yield return e.GetProperty("rightAttack").GetDouble();
    }

    private static void Check(bool condition, string label, ICollection<string> failures)
    { if (!condition) failures.Add(label); }

    private static void CheckNear(double actual, double expected, double tolerance, string label, ICollection<string> failures)
    { if (Math.Abs(actual - expected) > tolerance) failures.Add($"{label}: expected {expected:0.########}, got {actual:0.########}"); }
}
