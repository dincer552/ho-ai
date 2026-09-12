using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage9FormationRegression
{
    public static int Run()
    {
        var failures = 0;

        // Three-defender formations: DEF-L/DEF-R are central defenders, not wing-backs.
        foreach (var formation in new[] { "3-4-3", "3-5-2", "3-4-2-1", "3-3-4" })
        {
            Check(formation, "DEF-L", RegionalPosition.CentralDefender, ref failures);
            Check(formation, "DEF-R", RegionalPosition.CentralDefender, ref failures);
        }

        // Four- and five-defender formations: DEF-L/DEF-R are wing-backs.
        foreach (var formation in new[] { "4-4-2", "4-5-1", "4-3-3", "5-3-2", "5-5-0", "5-4-1", "5-2-3" })
        {
            Check(formation, "DEF-L", RegionalPosition.WingBack, ref failures);
            Check(formation, "DEF-R", RegionalPosition.WingBack, ref failures);
        }

        // Two-defender formation is explicitly covered by the reconstruction plan.
        Check("2-5-3", "DEF-L", RegionalPosition.WingBack, ref failures);
        Check("2-5-3", "DEF-R", RegionalPosition.WingBack, ref failures);

        // Explicit central defender slots remain central regardless of formation.
        foreach (var formation in new[] { "3-4-3", "4-4-2", "5-3-2" })
        {
            Check(formation, "DEF-C", RegionalPosition.CentralDefender, ref failures);
            Check(formation, "DEF-CL", RegionalPosition.CentralDefender, ref failures);
            Check(formation, "DEF-CR", RegionalPosition.CentralDefender, ref failures);
        }

        // Non-defensive slots must retain their semantic position independent of formation.
        Check("3-4-3", "GK", RegionalPosition.Goalkeeper, ref failures);
        Check("5-5-0", "W-L", RegionalPosition.Winger, ref failures);
        Check("4-4-2", "IM-R", RegionalPosition.InnerMidfielder, ref failures);
        Check("5-5-0", "FW-C", RegionalPosition.Forward, ref failures);

        // Formation text is trimmed before the three-defender decision.
        Check(" 3-4-3 ", "DEF-L", RegionalPosition.CentralDefender, ref failures);
        Check(" 4-4-2 ", "DEF-L", RegionalPosition.WingBack, ref failures);

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
