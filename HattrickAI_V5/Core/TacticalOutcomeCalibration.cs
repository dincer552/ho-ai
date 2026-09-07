namespace HattrickAI.V5.Core;

public sealed record TacticalOutcomeSample(string MatchId, TeamTactic Tactic, double WinProbability, double DrawProbability, double LossProbability, int HomeGoals, int AwayGoals);
public sealed record TacticalOutcomeCalibrationMetrics(TeamTactic Tactic, int Samples, double BrierScore, double LogLoss, double Accuracy);
public sealed record TacticalOutcomeCalibrationReport(int InputSamples, int ValidSamples, int InvalidSamples, double BrierScore, double LogLoss, double Accuracy, IReadOnlyDictionary<TeamTactic, TacticalOutcomeCalibrationMetrics> ByTactic);

/// <summary>Historical W/D/L calibration diagnostics. It never changes M10/M11 or production coefficients.</summary>
public static class TacticalOutcomeCalibration
{
    public static TacticalOutcomeCalibrationReport Analyze(IEnumerable<TacticalOutcomeSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        var input = samples.ToArray();
        var valid = input.Where(IsValid).ToArray();
        var byTactic = valid.GroupBy(x => x.Tactic).OrderBy(g => g.Key).ToDictionary(g => g.Key, g => Calculate(g.Key, g), EqualityComparer<TeamTactic>.Default);
        return new(input.Length, valid.Length, input.Length - valid.Length, valid.Length == 0 ? 0 : valid.Average(Brier), valid.Length == 0 ? 0 : valid.Average(LogLoss), valid.Length == 0 ? 0 : valid.Average(Accuracy), byTactic);
    }
    private static TacticalOutcomeCalibrationMetrics Calculate(TeamTactic tactic, IEnumerable<TacticalOutcomeSample> samples)
    { var rows = samples.ToArray(); return new(tactic, rows.Length, rows.Average(Brier), rows.Average(LogLoss), rows.Average(Accuracy)); }
    private static bool IsValid(TacticalOutcomeSample x)
    {
        if (string.IsNullOrWhiteSpace(x.MatchId) || x.HomeGoals < 0 || x.AwayGoals < 0) return false;
        if (!double.IsFinite(x.WinProbability) || !double.IsFinite(x.DrawProbability) || !double.IsFinite(x.LossProbability)) return false;
        if (x.WinProbability < 0 || x.WinProbability > 1 || x.DrawProbability < 0 || x.DrawProbability > 1 || x.LossProbability < 0 || x.LossProbability > 1) return false;
        return Math.Abs(x.WinProbability + x.DrawProbability + x.LossProbability - 1.0) < 1e-9;
    }
    private static double Brier(TacticalOutcomeSample x) { var a = Actual(x); return Math.Pow(x.WinProbability-a.win,2)+Math.Pow(x.DrawProbability-a.draw,2)+Math.Pow(x.LossProbability-a.loss,2); }
    private static double LogLoss(TacticalOutcomeSample x) { var p = Actual(x) switch { (1,0,0)=>x.WinProbability, (0,1,0)=>x.DrawProbability, _=>x.LossProbability }; return -Math.Log(Math.Max(1e-15,p)); }
    private static double Accuracy(TacticalOutcomeSample x)
    {
        var a = Actual(x);
        var predicted = x.WinProbability >= x.DrawProbability && x.WinProbability >= x.LossProbability ? (1,0,0) : x.DrawProbability >= x.LossProbability ? (0,1,0) : (0,0,1);
        return predicted == a ? 1.0 : 0.0;
    }
    private static (int win,int draw,int loss) Actual(TacticalOutcomeSample x) => x.HomeGoals > x.AwayGoals ? (1,0,0) : x.HomeGoals == x.AwayGoals ? (0,1,0) : (0,0,1);
}
