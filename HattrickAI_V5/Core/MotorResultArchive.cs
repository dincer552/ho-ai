using System.Text.Json;

namespace HattrickAI.V5.Core;

public static class MotorResultArchive
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string RootDirectory => Path.Combine(AppContext.BaseDirectory, "motor-db");

    public static string Save(string runId, object analysis, object? motorLog, string build)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId boş olamaz.", nameof(runId));
        var safeRunId = string.Concat(runId.Where(char.IsLetterOrDigit));
        if (safeRunId.Length == 0) safeRunId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

        var payload = new
        {
            schema = "hattrickai-v5-motor-database-v1",
            savedAt = DateTimeOffset.UtcNow,
            build,
            runId,
            source = "HattrickAI V5 web analysis",
            analysis,
            motorLog
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        Directory.CreateDirectory(RootDirectory);
        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMdd_HHmmssfff}_{safeRunId}.json";
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
