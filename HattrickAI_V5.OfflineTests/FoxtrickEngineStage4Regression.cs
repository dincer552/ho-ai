using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class FoxtrickEngineStage4Regression
{
    public static int Run()
    {
        var r = new RegionalRatingSnapshot(
            10,10,10,10,10,10,10,
            10,10,10,10,10,10,10);
        var normal = FoxtrickRatingStatistics.Calculate(r, TeamTactic.Normal);
        var failures = new List<string>();
        CheckNear(normal.HatStats, 861.0, .0001, "HatStats", failures);
        CheckNear(normal.LoddarStats, 36.74065979099788, .0001, "LoddarStats", failures);
        CheckNear(normal.PeasoStats, 41.0, .0001, "PeasoStats", failures);
        CheckNear(normal.VnukStats, 11.0, .0001, "VnukStats", failures);
        CheckNear(normal.HTitaVal, 344.4, .0001, "HTitaVal", failures);
        CheckNear(normal.GardierStats, 410.0, .0001, "GardierStats", failures);

        var engine = new FoxtrickEngine();
        var players = Enumerable.Range(1, 11).Select(i =>
            new Player(i, $"P{i}", 1,10,10,10,10,10,7,7,5)).ToList();
        var slots = new[] { "GK", "DEF-L", "DEF-CL", "DEF-C", "DEF-CR", "W-L", "IM-L", "IM-C", "IM-R", "W-R", "FW-C" }
            .Select((code,i) => new Slot(code, code, "stage4", players[i].Name, players[i].Id, 0, 0, 0)).ToList();
        var result = engine.Calculate(new RatingEngineRequest(
            new Lineup("Stage4", "3-5-2", slots), players, RatingContext.Default));
        Check(result.Engine == RatingEngineKind.Foxtrick, "engine kind", failures);
        Check(result.HatStats.HasValue && result.LoddarStats.HasValue, "common stats returned", failures);

        if (failures.Count > 0)
        {
            Console.Error.WriteLine("FoxtrickEngineStage4Regression FAILED");
            foreach (var failure in failures) Console.Error.WriteLine($" - {failure}");
            return 1;
        }
        Console.WriteLine("FoxtrickEngineStage4Regression PASS");
        return 0;
    }

    private static void CheckNear(double actual, double expected, double tolerance, string label, ICollection<string> failures)
    {
        if (Math.Abs(actual - expected) > tolerance) failures.Add($"{label}: expected {expected:0.####}, got {actual:0.####}");
    }

    private static void Check(bool condition, string message, ICollection<string> failures)
    {
        if (!condition) failures.Add(message);
    }
}
