using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage9FormationRegression
{
    public static int Run()
    {
        var failures = 0;

        foreach (var formation in new[] { "3-4-3", "3-5-2", "3-4-2-1", "3-3-4" })
        {
            Check(formation, "DEF-L", RegionalPosition.CentralDefender, ref failures);
            Check(formation, "DEF-R", RegionalPosition.CentralDefender, ref failures);
            Check(formation, "WB-L", RegionalPosition.WingBack, ref failures);
            Check(formation, "WB-R", RegionalPosition.WingBack, ref failures);
        }

        foreach (var formation in new[] { "4-4-2", "4-5-1", "4-3-3", "5-3-2", "5-5-0", "5-4-1", "5-2-3" })
        {
            // Canonical formation definitions now use explicit WB-L/WB-R.
            Check(formation, "WB-L", RegionalPosition.WingBack, ref failures);
            Check(formation, "WB-R", RegionalPosition.WingBack, ref failures);
            // Legacy DEF-L/DEF-R remain central-defender aliases and are not
            // automatically reinterpreted from formation text.
            Check(formation, "DEF-L", RegionalPosition.CentralDefender, ref failures);
            Check(formation, "DEF-R", RegionalPosition.CentralDefender, ref failures);
        }

        Check("2-5-3", "WB-L", RegionalPosition.WingBack, ref failures);
        Check("2-5-3", "WB-R", RegionalPosition.WingBack, ref failures);
        Check("2-5-3", "DEF-L", RegionalPosition.CentralDefender, ref failures);
        Check("2-5-3", "DEF-R", RegionalPosition.CentralDefender, ref failures);

        foreach (var formation in new[] { "3-4-3", "4-4-2", "5-3-2" })
        {
            Check(formation, "DEF-C", RegionalPosition.CentralDefender, ref failures);
            Check(formation, "DEF-CL", RegionalPosition.CentralDefender, ref failures);
            Check(formation, "DEF-CR", RegionalPosition.CentralDefender, ref failures);
        }

        Check("3-4-3", "GK", RegionalPosition.Goalkeeper, ref failures);
        Check("5-5-0", "W-L", RegionalPosition.Winger, ref failures);
        Check("4-4-2", "IM-R", RegionalPosition.InnerMidfielder, ref failures);
        Check("5-5-0", "FW-C", RegionalPosition.Forward, ref failures);

        Console.WriteLine(failures == 0
            ? "Stage 9 formation regression: PASS"
            : $"Stage 9 formation regression: FAIL ({failures})");

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
    }
}
