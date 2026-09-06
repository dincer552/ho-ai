using System.Text.Json;

namespace HattrickAI.V5.Core;

public sealed record MotorResultArchiveSnapshot(
    string SchemaVersion,
    DateTimeOffset SavedAt,
    string Build,
    string RunId,
    string Source,
    object Analysis,
    object MotorPipeline,
    object CandidateDatabases,
    object? MotorLog);

public static class MotorResultArchive
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string RootDirectory
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("MOTOR_DB_PATH");
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(AppContext.BaseDirectory, "motor-db")
                : Path.GetFullPath(configured.Trim());
        }
    }

    public static string Save(string runId, Analysis analysis, MotorPipelineResult pipeline, object? motorLog, string build)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId boş olamaz.", nameof(runId));
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(pipeline);

        var safeRunId = string.Concat(runId.Where(char.IsLetterOrDigit));
        if (safeRunId.Length == 0) safeRunId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        var savedAt = DateTimeOffset.UtcNow;
        var payload = new MotorResultArchiveSnapshot(
            "hattrickai-v5-motor-database-v1",
            savedAt,
            build,
            runId,
            "HattrickAI V5 web analysis",
            analysis,
            pipeline,
            new
            {
                db1 = pipeline.CandidateDatabase1,
                db2 = pipeline.CandidateDatabase2,
                db1Count = pipeline.CandidateDatabase1Count,
                db2Count = pipeline.CandidateDatabase2Count
            },
            motorLog);

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        Directory.CreateDirectory(RootDirectory);
        var fileName = $"{savedAt:yyyyMMdd_HHmmssfff}_{safeRunId}.json";
        var path = Path.Combine(RootDirectory, fileName);

        lock (Gate)
        {
            File.WriteAllText(path, json);
            File.WriteAllText(Path.Combine(RootDirectory, "latest.json"), json);
        }

        return path;
    }

    public static bool TryGetLatest(out string json)
    {
        var path = Path.Combine(RootDirectory, "latest.json");
        lock (Gate)
        {
            if (!File.Exists(path))
            {
                json = string.Empty;
                return false;
            }

            json = File.ReadAllText(path);
            return true;
        }
    }
}
