using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Stage 3 gate: verifies that the reconstructed rating path uses skill
/// levels as skill-equivalent values (skill - 1), never raw displayed skill
/// numbers. The regression exercises the public V5 rating path instead of
/// reaching into engine internals.
/// </summary>
public static class Stage3SkillNormalizationRegression
{
    public static int Run()
    {
        var engine = new RegionalRatingEngineFixed();
        const double form = 6;
        var expectedPerNormalizedDefending = 0.186 * (0.378 * Math.Sqrt(form - 1.0) / 0.756);
        var cases = new (int Skill, double Expected)[]
        {
            (0, 0),
            (1, 0),
            (2, expectedPerNormalizedDefending),
            (5, expectedPerNormalizedDefending * 4),
            (10, expectedPerNormalizedDefending * 9),
            (17, expectedPerNormalizedDefending * 16),
            (20, expectedPerNormalizedDefending * 19)
        };

        foreach (var test in cases)
        {
            var player = new RegionalPlayer(1, RegionalPosition.CentralDefender, PlayerSide.Center, PlayerOrder.Normal,
                0, test.Skill, 0, 0, 0, 0, form, 0, 0, 9.4);
            var actual = engine.Calculate(new[] { player }).RawCentralDefence;
            if (Math.Abs(actual - test.Expected) > 1e-12)
            {
                Console.WriteLine($"FAIL: skill normalization {test.Skill} -> {actual}, expected {test.Expected}");
                return 1;
            }
        }

        Console.WriteLine("PASS: Stage 3 skill normalization | max(0, skill - 1)");
        Console.WriteLine("PASS: Public engine path regression; no coefficient retuning performed.");
        return 0;
    }
}
