using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>T10 gate: validates a tactic-labelled historical W/D/L corpus without inventing missing data.</summary>
public static class C24TacticalOutcomeCalibrationCorpusRegression
{
    public static int Run(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Console.WriteLine($"C24 tactical outcome calibration: SKIP | corpus not supplied: {path}");
            return 0;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var schema = root.TryGetProperty("schema", out var schemaNode) ? schemaNode.GetString() : null;
        if (!string.Equals(schema, "hattrickai-v5-tactical-outcome-calibration-v1", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("C24 unsupported calibration corpus schema.");
        if (!root.TryGetProperty("samples", out var rows) || rows.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("C24 samples array is missing.");

        var samples = new List<TacticalOutcomeSample>();
        foreach (var row in rows.EnumerateArray())
        {
            if (!Enum.TryParse<TeamTactic>(GetString(row, "tactic"), true, out var tactic))
                throw new InvalidOperationException("C24 invalid tactic.");
            samples.Add(new TacticalOutcomeSample(
                GetString(row, "matchId"), tactic,
                GetDouble(row, "winProbability"), GetDouble(row, "drawProbability"), GetDouble(row, "lossProbability"),
                GetInt(row, "homeGoals"), GetInt(row, "awayGoals")));
        }

        var report = TacticalOutcomeCalibration.Analyze(samples);
        Check(report.ValidSamples == samples.Count, "all supplied rows are valid");
        Check(report.ByTactic.Count == 7, "all seven tactics are represented");
        foreach (var tactic in Enum.GetValues<TeamTactic>())
            Check(report.ByTactic[tactic].Samples > 0, $"tactic {tactic} has calibration samples");
        Check(double.IsFinite(report.BrierScore) && double.IsFinite(report.LogLoss) && double.IsFinite(report.Accuracy), "global metrics are finite");

        var rerun = TacticalOutcomeCalibration.Analyze(samples);
        Check(Math.Abs(rerun.BrierScore - report.BrierScore) < 1e-15, "Brier score is deterministic");
        Check(Math.Abs(rerun.LogLoss - report.LogLoss) < 1e-15, "log loss is deterministic");
        Check(Math.Abs(rerun.Accuracy - report.Accuracy) < 1e-15, "accuracy is deterministic");

        Console.WriteLine($"PASS: C24 tactical outcome calibration corpus | samples={report.ValidSamples}; Brier={report.BrierScore:F6}; LogLoss={report.LogLoss:F6}; Accuracy={report.Accuracy:F6}");
        foreach (var metric in report.ByTactic.Values.OrderBy(x => x.Tactic))
            Console.WriteLine($"  {metric.Tactic}: n={metric.Samples}; Brier={metric.BrierScore:F6}; LogLoss={metric.LogLoss:F6}; Accuracy={metric.Accuracy:F6}");
        return 0;
    }

    private static string GetString(JsonElement row, string name) => row.TryGetProperty(name, out var n) ? n.GetString() ?? string.Empty : string.Empty;
    private static double GetDouble(JsonElement row, string name) => row.TryGetProperty(name, out var n) && n.TryGetDouble(out var v) ? v : double.NaN;
    private static int GetInt(JsonElement row, string name) => row.TryGetProperty(name, out var n) && n.TryGetInt32(out var v) ? v : -1;
    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}
