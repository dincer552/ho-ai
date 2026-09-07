using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>T9 regression for historical tactical outcome calibration metrics.</summary>
public static class C23TacticalOutcomeCalibrationRegression
{
    public static int Run()
    {
        var samples = new[]
        {
            new TacticalOutcomeSample("m1", TeamTactic.Creative, 0.70, 0.20, 0.10, 2, 1),
            new TacticalOutcomeSample("m2", TeamTactic.Creative, 0.20, 0.50, 0.30, 1, 1),
            new TacticalOutcomeSample("m3", TeamTactic.Normal, 0.10, 0.20, 0.70, 0, 2),
            new TacticalOutcomeSample("invalid", TeamTactic.Pressing, double.NaN, 0.2, 0.8, 1, 0)
        };

        var report = TacticalOutcomeCalibration.Analyze(samples);

        Check(report.InputSamples == 4, "all input samples are counted");
        Check(report.ValidSamples == 3 && report.InvalidSamples == 1, "invalid probability rows are excluded");
        Check(Math.Abs(report.BrierScore - (0.10 + 0.38 + 0.14) / 3.0) < 1e-12, "multiclass Brier score is deterministic");
        Check(Math.Abs(report.LogLoss - ((-Math.Log(0.70)) + (-Math.Log(0.50)) + (-Math.Log(0.70))) / 3.0) < 1e-12, "log loss is deterministic");
        Check(Math.Abs(report.Accuracy - 1.0) < 1e-12, "top-probability outcome accuracy is deterministic");
        Check(report.ByTactic.Count == 2, "valid tactics are grouped independently");
        Check(report.ByTactic[TeamTactic.Creative].Samples == 2, "creative sample count is preserved");
        Check(report.ByTactic[TeamTactic.Normal].Samples == 1, "normal sample count is preserved");

        var rerun = TacticalOutcomeCalibration.Analyze(samples);
        Check(Math.Abs(rerun.BrierScore - report.BrierScore) < 1e-15, "rerun remains deterministic");
        Check(Math.Abs(rerun.LogLoss - report.LogLoss) < 1e-15, "rerun log loss remains deterministic");

        Console.WriteLine("PASS: T9 tactical outcome calibration diagnostics | Brier + log loss + accuracy + tactic grouping");
        return 0;
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
}
