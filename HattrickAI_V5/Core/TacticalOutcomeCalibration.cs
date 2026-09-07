namespace HattrickAI.V5.Core;

public sealed record TacticalOutcomeSample(
    string MatchId,
    TeamTactic Tactic,
    double WinProbability,
    double DrawProbability,
    double LossProbability,
    int HomeGoals,
    int AwayGoals);

public sealed record TacticalOutcomeCalibrationMetrics(
    TeamTactic Tactic,
    int Samples,
    double BrierScore,
    double LogLoss,
    double Accuracy);

public sealed record TacticalOutcomeCalibrationReport(
    int InputSamples,
    int ValidSamples,
    int InvalidSamples,
    double BrierScore,
    double LogLoss,
    double Accuracy,
    IReadOnlyDictionary<TeamTactic, TacticalOutcomeCalibrationMetrics> ByTactic);

/// <summary>
/// Outcome-calibration diagnostics for historical match results.
/// This layer measures the existing M9 probabilities; it does not alter
/// M10/M11 thresholds, anti-lock rules, or production coefficients.
/// </summary>
public static class TacticalOutcomeCalibration
{
    public static TacticalOutcomeCalibrationReport Analyze(IEnumerable<TacticalOutcomeSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var input = samples.ToArray();
        var valid = input.Where(IsValid).ToArray();
        var invalid = input.Length - valid.Length;

        var byTactic = valid
            .GroupBy(x => x.Tactic)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => Calculate(g.Key, g), EqualityComparer<TeamTactic>.Default);

        return new TacticalOutcomeCalibrationReport(
            input.Length,
            valid.Length,
            invalid,
            valid.Length == 0 ? 0 : valid.Average(Brier),
            valid.Length == 0 ? 0 : valid.Average(LogLoss),
            valid.Length == 0 ? 0 : valid.Average(Accuracy),
            byTactic);
    }

    private static TacticalOutcomeCalibrationMetrics Calculate(TeamTactic tactic, IEnumerable<TacticalOutcomeSample> samples)
    {
        var rows = samples.ToArray();
        return new TacticalOutcomeCalibrationMetrics(
            tactic,
            rows.Length,
            rows.Average(Brier),
            rows.Average(LogLoss),
            rows.Average(Accuracy));
    }

    private static bool IsValid(TacticalOutcomeSample x)
    {
        if (string.IsNullOrWhiteSpace(x.MatchId) || x.HomeGoals < 0 || x.AwayGoals < 0) return false;
        if (!double.IsFinite(x.WinProbability) || !double.IsFinite(x.DrawProbability) || !double.IsFinite(x.LossProbability)) return false;
        if (x.WinProbability < 0 || x.WinProbability > 1 || x.DrawProbability < 0 || x.DrawProbability > 1 || x.LossProbability < 0 || x.LossProbability > 1) return false;
        return Math.Abs((x.WinProbability + x.DrawProbability + x.LossProbability) - 1.0) < 1e-9;
    }

    private static double Brier(TacticalOutcomeSample x)
    {
        var actual = Actual(x);
        return Math.Pow(x.WinProbability - actual.win, 2)
            + Math.Pow(x.DrawProbability - actual.draw, 2)
            + Math.Pow(x.LossProbability - actual.loss, 2);
    }

    private static double LogLoss(TacticalOutcomeSample x)
    {
        var probability = Actual(x) switch
        {
            (1, 0, 0) => x.WinProbability,
            (0, 1, 0) => x.DrawProbability,
            _ => x.LossProbability
        };
        return -Math.Log(Math.Max(1e-15, probability));
    }

    private static double Accuracy(TacticalOutcomeSample x)
    {
        var actual = Actual(x);
        var predicted = Math.Max(x.WinProbability, Math.Max(x.DrawProbability, x.LossProbability));
        var actualProbability = actual.win > 0 ? x.WinProbability : actual.draw > 0 ? x.DrawProbability : x.LossProbability;
        return Math.Abs(predicted - actualProbability) < 1e-15 ? 1.0 : 0.0;
    }

    private static (int win, int draw, int loss) Actual(TacticalOutcomeSample x)
        => x.HomeGoals > x.AwayGoals ? (1, 0, 0) : x.HomeGoals == x.AwayGoals ? (0, 1, 0) : (0, 0, 1);
}
