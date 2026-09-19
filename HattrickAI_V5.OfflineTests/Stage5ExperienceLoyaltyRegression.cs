using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage5ExperienceLoyaltyRegression
{
    public static int Run()
    {
        var failures = new List<string>();
        var fixedEngine = new RegionalRatingEngineFixed();
        var researchedEngine = new RegionalRatingEngine();

        // Hattrick applies Experience as a flat skill-contribution bonus.
        // Verify the effect survives the complete 14-position contribution matrix.
        foreach (var slot in Slots())
        {
            var basePlayer = PlayerFor(slot, 1);
            var experiencedPlayer = PlayerFor(slot, 7);

            CheckExperienceEffect(
                fixedEngine.Calculate(new[] { basePlayer }),
                fixedEngine.Calculate(new[] { experiencedPlayer }),
                $"Fixed {slot}",
                failures);

            CheckExperienceEffect(
                researchedEngine.Calculate(new[] { basePlayer }),
                researchedEngine.Calculate(new[] { experiencedPlayer }),
                $"Research {slot}",
                failures);
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
            Console.WriteLine("PASS: Stage 5 loyalty preserved; experience bonus applied through the 14-position skill layer");
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

        return new RegionalPlayer(
            slot.GetHashCode(), position, side, PlayerOrder.Normal,
            4, 10, 10, 8, 8, 10, 7, 0, experience, 7, slot);
    }

    private static void CheckExperienceEffect(
        RegionalRatingSnapshot baseline,
        RegionalRatingSnapshot experienced,
        string name,
        List<string> failures)
    {
        var a = Values(baseline);
        var b = Values(experienced);
        var changed = false;

        for (var i = 0; i < a.Length; i++)
        {
            if (b[i] < a[i] - 1e-12)
                failures.Add($"{name} sector {i} decreased with experience: {a[i]:F12} -> {b[i]:F12}");
            if (b[i] > a[i] + 1e-12)
                changed = true;
        }

        if (!changed)
            failures.Add($"{name} has no rating-sector effect from Experience");
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
