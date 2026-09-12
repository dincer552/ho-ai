using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class C25FormationAwareRatingPositionRegression
{
    public static int Run()
    {
        var failures = new List<string>();

        Check("3-4-3 DEF-L", RatingPositionResolver.Resolve("3-4-3", "DEF-L"), RegionalPosition.CentralDefender, failures);
        Check("3-4-3 DEF-R", RatingPositionResolver.Resolve("3-4-3", "DEF-R"), RegionalPosition.CentralDefender, failures);
        Check("3-5-2 DEF-L", RatingPositionResolver.Resolve("3-5-2", "DEF-L"), RegionalPosition.CentralDefender, failures);
        Check("3-5-2 DEF-R", RatingPositionResolver.Resolve("3-5-2", "DEF-R"), RegionalPosition.CentralDefender, failures);

        Check("4-4-2 DEF-L", RatingPositionResolver.Resolve("4-4-2", "DEF-L"), RegionalPosition.WingBack, failures);
        Check("5-5-0 DEF-R", RatingPositionResolver.Resolve("5-5-0", "DEF-R"), RegionalPosition.WingBack, failures);
        Check("2-5-3 DEF-L", RatingPositionResolver.Resolve("2-5-3", "DEF-L"), RegionalPosition.WingBack, failures);
        Check("2-5-3 DEF-R", RatingPositionResolver.Resolve("2-5-3", "DEF-R"), RegionalPosition.WingBack, failures);
        Check("3-5-2 DEF-CL", RatingPositionResolver.Resolve("3-5-2", "DEF-CL"), RegionalPosition.CentralDefender, failures);
        Check("3-5-2 DEF-C", RatingPositionResolver.Resolve("3-5-2", "DEF-C"), RegionalPosition.CentralDefender, failures);
        Check("3-5-2 DEF-CR", RatingPositionResolver.Resolve("3-5-2", "DEF-CR"), RegionalPosition.CentralDefender, failures);

        if (failures.Count == 0)
        {
            Console.WriteLine("=== C25 FORMATION-AWARE RATING POSITION REGRESSION ===");
            Console.WriteLine("PASS: DEF-L/DEF-R are central defenders in 3-defender formations and wing-backs otherwise.");
            return 0;
        }

        foreach (var failure in failures)
            Console.WriteLine("FAIL: " + failure);
        Console.WriteLine($"FAIL: C25 ({failures.Count} assertion(s))");
        return 1;
    }

    private static void Check(string name, RegionalPosition actual, RegionalPosition expected, List<string> failures)
    {
        if (actual != expected)
            failures.Add($"{name}: expected {expected}, got {actual}");
    }
}
