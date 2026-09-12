using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage6OvercrowdingRegression
{
    public static int Run()
    {
        var failures = new List<string>();
        var engine = new RegionalRatingEngineFixed();

        var cd = new RegionalPlayer(1, RegionalPosition.CentralDefender, PlayerSide.Center, PlayerOrder.Normal, 1, 12, 10, 8, 3, 6, 7, 0, 1, 7);
        var im = new RegionalPlayer(2, RegionalPosition.InnerMidfielder, PlayerSide.Left, PlayerOrder.Normal, 1, 8, 14, 10, 8, 6, 7, 0, 1, 7);
        var fw = new RegionalPlayer(3, RegionalPosition.Forward, PlayerSide.Left, PlayerOrder.Normal, 1, 4, 5, 6, 9, 13, 7, 0, 1, 7);

        var cd1 = engine.Calculate(new[] { cd });
        var cd2 = engine.Calculate(new[] { cd, cd with { Id = 4 } });
        var cd3 = engine.Calculate(new[] { cd, cd with { Id = 4 }, cd with { Id = 5 } });
        CheckRatio(cd2.RawCentralDefence, cd1.RawCentralDefence, 2 * .964, "CD central defence x2", failures);
        CheckRatio(cd2.RawMidfield, cd1.RawMidfield, 2 * .964, "CD midfield x2", failures);
        CheckRatio(cd3.RawCentralDefence, cd1.RawCentralDefence, 3 * .900, "CD central defence x3", failures);

        var im1 = engine.Calculate(new[] { im });
        var im2 = engine.Calculate(new[] { im, im with { Id = 6 } });
        var im3 = engine.Calculate(new[] { im, im with { Id = 6 }, im with { Id = 7 } });
        CheckRatio(im2.RawCentralDefence, im1.RawCentralDefence, 2 * .935, "IM central defence x2", failures);
        CheckRatio(im2.RawCentralAttack, im1.RawCentralAttack, 2 * .935, "IM central attack x2", failures);
        CheckRatio(im2.RawMidfield, im1.RawMidfield, 2 * .935, "IM midfield x2", failures);
        CheckRatio(im3.RawMidfield, im1.RawMidfield, 3 * .825, "IM midfield x3", failures);

        var fw1 = engine.Calculate(new[] { fw });
        var fw2 = engine.Calculate(new[] { fw, fw with { Id = 8 } });
        var fw3 = engine.Calculate(new[] { fw, fw with { Id = 8 }, fw with { Id = 9 } });
        CheckRatio(fw2.RawCentralAttack, fw1.RawCentralAttack, 2 * .945, "FW central attack x2", failures);
        CheckRatio(fw2.RawMidfield, fw1.RawMidfield, 2 * .945, "FW midfield x2", failures);
        CheckRatio(fw3.RawCentralAttack, fw1.RawCentralAttack, 3 * .865, "FW central attack x3", failures);

        if (failures.Count == 0)
        {
            Console.WriteLine("PASS: Stage 6 overcrowding applies to full position contribution");
            return 0;
        }

        foreach (var failure in failures) Console.WriteLine("FAIL: " + failure);
        Console.WriteLine($"FAIL: Stage 6 ({failures.Count} assertion(s))");
        return 1;
    }

    private static void CheckRatio(double actual, double baseline, double expected, string name, List<string> failures)
    {
        var ratio = actual / baseline;
        if (Math.Abs(ratio - expected) > 1e-9)
            failures.Add($"{name}: expected ratio {expected:F6}, got {ratio:F6}");
    }
}
