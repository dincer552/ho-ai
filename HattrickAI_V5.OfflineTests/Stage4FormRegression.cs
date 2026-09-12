using System.Reflection;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Stage 4 gate for the form-model reconstruction described in
/// RATING_MOTOR_RECONSTRUCTION_README.md.
///
/// A = existing baseline table normalized by 0.755.
/// B = researched form function 0.378 * sqrt(clamp(form - 1, 0, 7)),
///     normalized by the current fixed-engine baseline factor 0.756.
///
/// No rating coefficients are changed here. The test deliberately exposes
/// the A/B delta so CAL fixtures can decide which model explains ground truth.
/// </summary>
public static class Stage4FormRegression
{
    private const double BaselineNormalization = 0.755;
    private const double FixedEngineNormalization = 0.756;

    private static readonly double[] BaselineFormFactors =
        { 0.4, 0.5, 0.6, 0.68, 0.72, 0.755, 0.79, 0.82, 0.85, 0.88, 0.91 };

    public static int Run()
    {
        var failures = new List<string>();
        var researched = typeof(RegionalRatingEngineFixed)
            .GetMethod("FormFactor", BindingFlags.NonPublic | BindingFlags.Static);

        if (researched is null)
        {
            Console.WriteLine("FAIL: Stage 4 researched FormFactor is not present in RegionalRatingEngineFixed.");
            return 1;
        }

        Console.WriteLine("Stage 4 form A/B comparison");
        Console.WriteLine("Form | A baseline | B researched | B-A");

        for (var form = 1; form <= 10; form++)
        {
            var actual = (double)researched.Invoke(null, new object[] { (double)form })!;
            var expected = 0.378 * Math.Sqrt(Math.Clamp(form - 1.0, 0.0, 7.0));
            Check(Math.Abs(actual - expected) <= 1e-12,
                $"researched form function at form {form}", failures);

            var a = BaselineFormFactors[form] / BaselineNormalization;
            var b = actual / FixedEngineNormalization;
            Console.WriteLine($"{form,4} | {a,10:F4} | {b,12:F4} | {b - a,6:+0.0000;-0.0000;0.0000}");
        }

        Check(Math.Abs((double)researched.Invoke(null, new object[] { 1.0 })!) <= 1e-12,
            "form 1 has zero researched factor", failures);
        Check(Math.Abs((double)researched.Invoke(null, new object[] { 8.0 })! -
                       (double)researched.Invoke(null, new object[] { 9.0 })!) <= 1e-12,
            "researched model caps form contribution after form 8", failures);

        if (failures.Count > 0)
        {
            foreach (var failure in failures) Console.WriteLine("FAIL: " + failure);
            Console.WriteLine($"FAIL: Stage 4 ({failures.Count} assertion(s))");
            return 1;
        }

        Console.WriteLine("PASS: Stage 4 form model A/B regression gate");
        Console.WriteLine("PASS: Research model verified; no rating coefficients retuned.");
        Console.WriteLine("NOTE: CAL ground-truth comparison remains required before locking the form model.");
        return 0;
    }

    private static void Check(bool condition, string name, List<string> failures)
    {
        if (!condition) failures.Add(name);
    }
}
