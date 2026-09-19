using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage5ExperienceLoyaltyRegression
{
    public static int Run()
    {
        var failures = new List<string>();
        var fixedEngine = new RegionalRatingEngineFixed();
        var researchedEngine = new RegionalRatingEngine();

        // Experience is player metadata for this V5 rating layer, not a direct
        // sector-contribution coefficient. Verify that changing only Experience
        // leaves every sector unchanged for all 14 canonical field slots.
        foreach (var slot in Slots())
        {
            var basePlayer = PlayerFor(slot, 1);
            var experiencedPlayer = PlayerFor(slot, 20);

            var fixedBase = fixedEngine.Calculate(new[] { basePlayer });
            var fixedExperienced = fixedEngine.Calculate(new[] { experiencedPlayer });
            CheckSame(fixedBase, fixedExperienced, $"Fixed {slot}", failures);

            var researchedBase = researchedEngine.Calculate(new[] { basePlayer });
            var researchedExperienced = researchedEngine.Calculate(new[] { experiencedPlayer });
            CheckSame(researchedBase, researchedExperienced, $"Research {slot}", failures);
        }

        // Loyalty remains an independent player effect and must still work.
        var normal = PlayerFor("DEF-C", 1);
        var loyal = normal with { Loyalty = 19 };
        var baseline = fixedEngine.Calculate(new[] { normal });
        var loyalty = fixedEngine.Calculate(new[] { loyal });
        Check(loyalty.RawCentralDefence > baseline.RawCentralDefence, "loyalty increases central defence", failures);
        Check(loyalty.RawMidfield > baseline.RawMidfield, "loyalty increases midfield", failures);

        if (failures.Count == 0)
        {
            Console.WriteLine("PASS: Stage 5 loyalty preserved; experience removed from all 14 position contributions");
            return 0;
        }

        foreach (var failure in failures) Console.WriteLine("FAIL: " + failure);
        Console.WriteLine($"FAIL: Stage 5 ({failures.Count} assertion(s))");
        return 1;
    }

    private static IEnumerable<string> Slots() => new[]
    {
        "GK",
        "WB-L", "DEF-CL", "DEF-C", "DEF-CR", "WB-R",
        "W-L", "IM-L", "IM-C", "IM-R", "W-R",
        "FW-L", "FW-C", "FW-R"
    };

    private static RegionalPlayer PlayerFor(string slot, double experience)
    {
        var side = slot.EndsWith("-L", StringComparison.Ordinal) ? PlayerSide.Left
            : slot.EndsWith("-R", StringComparison.Ordinal) ? PlayerSide.Right
            : PlayerSide.Center;

        var position = slot switch
        {
            "GK" => RegionalPosition.Goalkeeper,
            "WB-L" or "WB-R" => RegionalPosition.WingBack,
            "DEF-CL" or "DEF-C" or "DEF-CR" => RegionalPosition.CentralDefender,
            "W-L" or "W-R" => RegionalPosition.Winger,
            "IM-L" or "IM-C" or "IM-R" => RegionalPosition.InnerMidfielder,
            "FW-L" or "FW-C" or "FW-R" => RegionalPosition.Forward,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };

        var order = PlayerOrder.Normal;
        return new RegionalPlayer(
            slot.GetHashCode(), position, side, order,
            4, 10, 10, 8, 8, 10, 7, 0, experience, 7, slot);
    }

    private static void CheckSame(RegionalRatingSnapshot a, RegionalRatingSnapshot b, string name, List<string> failures)
    {
        var av = Values(a);
        var bv = Values(b);
        for (var i = 0; i < av.Length; i++)
        {
            if (Math.Abs(av[i] - bv[i]) > 1e-12)
                failures.Add($"{name} sector {i} changed with experience: {av[i]:F12} -> {bv[i]:F12}");
        }
    }

    private static double[] Values(RegionalRatingSnapshot s) => new[]
    {
        s.RawLeftDefence, s.RawCentralDefence, s.RawRightDefence,
        s.RawMidfield, s.RawLeftAttack, s.RawCentralAttack, s.RawRightAttack
    };

    private static void Check(bool condition, string name, List<string> failures)
    {
        if (!condition) failures.Add(name);
    }
}
