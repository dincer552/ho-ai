using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage9FormationRegression
{
    public static int Run()
    {
        var failures = 0;
        Check("3-4-3", "DEF-L", RegionalPosition.CentralDefender, ref failures);
        Check("3-5-2", "DEF-R", RegionalPosition.CentralDefender, ref failures);
        Check("4-4-2", "DEF-L", RegionalPosition.WingBack, ref failures);
        Check("4-5-1", "DEF-R", RegionalPosition.WingBack, ref failures);
        Check("5-3-2", "DEF-L", RegionalPosition.WingBack, ref failures);
        Check("5-5-0", "DEF-R", RegionalPosition.WingBack, ref failures);
        Check("2-5-3", "DEF-L", RegionalPosition.WingBack, ref failures);
        Check("2-5-3", "DEF-R", RegionalPosition.WingBack, ref failures);

        // Explicit central slots remain central defenders in every formation.
        Check("4-4-2", "DEF-C", RegionalPosition.CentralDefender, ref failures);
        Check("5-3-2", "DEF-CL", RegionalPosition.CentralDefender, ref failures);
        Check("3-4-3", "DEF-CR", RegionalPosition.CentralDefender, ref failures);

        Check("3-4-3", "W-L", RegionalPosition.Winger, ref failures);
        Check("4-4-2", "IM-R", RegionalPosition.InnerMidfielder, ref failures);
        Check("5-5-0", "FW-C", RegionalPosition.Forward, ref failures);
        Check("3-4-3", "GK", RegionalPosition.Goalkeeper, ref failures);

        Console.WriteLine(failures == 0 ? "Stage 9 formation regression: PASS" : $"Stage 9 formation regression: FAIL ({failures})");
        return failures == 0 ? 0 : 1;
    }

    private static void Check(string formation, string slot, RegionalPosition expected, ref int failures)
    {
        var actual = RatingPositionResolver.Resolve(formation, slot);
        if (actual != expected)
        {
            Console.WriteLine($"FAIL {formation}/{slot}: expected {expected}, got {actual}");
            failures++;
        }
        else Console.WriteLine($"PASS {formation}/{slot}: {actual}");
    }
}
