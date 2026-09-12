using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage5ExperienceLoyaltyRegression
{
    public static int Run()
    {
        var failures = new List<string>();
        var engine = new RegionalRatingEngineFixed();
        var normal = new RegionalPlayer(1, RegionalPosition.CentralDefender, PlayerSide.Center, PlayerOrder.Normal, 0, 10, 10, 5, 3, 5, 5, 0, 1, 7);
        var loyal = normal with { Loyalty = 19 };
        var experienced = normal with { Experience = 7 };

        var baseline = engine.Calculate(new[] { normal });
        var loyalty = engine.Calculate(new[] { loyal });
        var experience = engine.Calculate(new[] { experienced });

        Check(loyalty.RawCentralDefence > baseline.RawCentralDefence, "loyalty increases central defence", failures);
        Check(loyalty.RawMidfield > baseline.RawMidfield, "loyalty increases midfield", failures);
        Check(experience.RawCentralDefence > baseline.RawCentralDefence, "experience increases central defence", failures);
        Check(experience.RawMidfield > baseline.RawMidfield, "experience increases midfield", failures);
        Check(experience.RawCentralAttack == baseline.RawCentralAttack, "experience does not affect unrelated central attack", failures);

        if (failures.Count == 0)
        {
            Console.WriteLine("PASS: Stage 5 loyalty and experience separation");
            return 0;
        }

        foreach (var failure in failures) Console.WriteLine("FAIL: " + failure);
        Console.WriteLine($"FAIL: Stage 5 ({failures.Count} assertion(s))");
        return 1;
    }

    private static void Check(bool condition, string name, List<string> failures)
    {
        if (!condition) failures.Add(name);
    }
}
