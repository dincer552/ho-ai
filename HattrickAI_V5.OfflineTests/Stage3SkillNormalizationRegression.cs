using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Stage 3 gate: verifies that the reconstructed rating path uses skill
/// levels as skill-equivalent values (skill - 1), never raw displayed skill
/// numbers. This is intentionally a regression gate before any coefficient
/// retuning is attempted.
/// </summary>
public static class Stage3SkillNormalizationRegression
{
    public static int Run()
    {
        var cases = new (double Skill, double Expected)[]
        {
            (0, 0),
            (1, 0),
            (2, 1),
            (5, 4),
            (10, 9),
            (17, 16),
            (20, 19)
        };

        foreach (var test in cases)
        {
            var actual = RegionalRatingEngineFixed.SkillRating(test.Skill);
            if (Math.Abs(actual - test.Expected) > 1e-12)
            {
                Console.WriteLine($"FAIL: skill normalization {test.Skill} -> {actual}, expected {test.Expected}");
                return 1;
            }
        }

        Console.WriteLine("PASS: Stage 3 skill normalization | max(0, skill - 1)");
        Console.WriteLine("PASS: No coefficient retuning performed; normalization gate established before calibration.");
        return 0;
    }
}
