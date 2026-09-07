using System.Text.Json;
using System.Text.Json.Serialization;

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

public sealed record MotorResultArchiveEntry(
    string FileName,
    string RunId,
    string SavedAt,
    string Build,
    long SizeBytes);

public static class MotorResultArchive
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
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

        // Keep the existing archive contract, but only persist the small, stable
        // inspection data needed by the web UI. Never walk the full analysis graph,
        // M3-M6 search graphs, candidate lineups or runtime motor telemetry here.
        // This keeps the final archive allocation tiny on the 900 MiB production VM.
        var analysisSummary = new
        {
            analysis.Build,
            analysis.TeamName,
            analysis.OpponentName,
            analysis.MatchTitle,
            OwnFormation = analysis.Own.Formation,
            OpponentFormation = analysis.Opponent.Formation
        };

        var candidateDb1 = pipeline.CandidateDatabase1.Select(x => new
        {
            x.CandidateId,
            x.Formation,
            x.M5SuitabilityScore,
            x.M5StructuralScore,
            x.TacticalScore,
            x.Rating,
            x.RankingScore,
            x.Stage
        }).ToArray();

        var candidateDb2 = pipeline.CandidateDatabase2.Select(x => new
        {
            x.CandidateId,
            x.Formation,
            x.M5SuitabilityScore,
            x.M5StructuralScore,
            x.TacticalScore,
            x.Rating,
            x.RankingScore,
            x.Stage
        }).ToArray();

        var payload = new MotorResultArchiveSnapshot(
            "hattrickai-v5-motor-database-v1",
            savedAt,
            build,
            runId,
            "HattrickAI V5 web analysis",
            analysisSummary,
            new
            {
                candidateDatabase1Count = pipeline.CandidateDatabase1Count,
                candidateDatabase2Count = pipeline.CandidateDatabase2Count,
                selectedMatchApproach = pipeline.SelectedMatchApproach,
                tacticComparisons = pipeline.TacticComparisons
            },
            new
            {
                db1 = candidateDb1,
                db2 = candidateDb2,
                db1Count = candidateDb1.Length,
                db2Count = candidateDb2.Length
            },
            null);

        // Serialize only this deliberately small summary payload.
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
            return TryReadFile(path, out json);
        }
    }

    public static IReadOnlyList<MotorResultArchiveEntry> List()
    {
        if (!Directory.Exists(RootDirectory)) return Array.Empty<MotorResultArchiveEntry>();

        lock (Gate)
        {
            var entries = new List<MotorResultArchiveEntry>();
            foreach (var path in Directory.EnumerateFiles(RootDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(Path.GetFileName(path), "latest.json", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    using var stream = File.OpenRead(path);
                    using var document = JsonDocument.Parse(stream);
                    var root = document.RootElement;
                    var runId = GetString(root, "runId");
                    var savedAt = GetString(root, "savedAt");
                    var currentBuild = GetString(root, "build");
                    if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(savedAt)) continue;

                    entries.Add(new MotorResultArchiveEntry(
                        Path.GetFileName(path),
                        runId,
                        savedAt,
                        currentBuild,
                        new FileInfo(path).Length));
                }
                catch (JsonException)
                {
                }
                catch (IOException)
                {
                }
            }

            return entries
                .OrderByDescending(x => x.SavedAt, StringComparer.Ordinal)
                .ThenByDescending(x => x.FileName, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public static bool TryGetByRunId(string runId, out string json)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            json = string.Empty;
            return false;
        }

        lock (Gate)
        {
            if (!Directory.Exists(RootDirectory))
            {
                json = string.Empty;
                return false;
            }

            foreach (var path in Directory.EnumerateFiles(RootDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                if (string.Equals(Path.GetFileName(path), "latest.json", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    using var stream = File.OpenRead(path);
                    using var document = JsonDocument.Parse(stream);
                    var storedRunId = GetString(document.RootElement, "runId");
                    if (string.Equals(storedRunId, runId, StringComparison.Ordinal))
                    {
                        json = File.ReadAllText(path);
                        return true;
                    }
                }
                catch (JsonException)
                {
                }
                catch (IOException)
                {
                }
            }
        }

        json = string.Empty;
        return false;
    }

    private static bool TryReadFile(string path, out string json)
    {
        if (!File.Exists(path))
        {
            json = string.Empty;
            return false;
        }

        try
        {
            json = File.ReadAllText(path);
            return true;
        }
        catch (IOException)
        {
            json = string.Empty;
            return false;
        }
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }
}
