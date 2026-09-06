using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class C19MotorDatabaseJsonRegression
{
    public static int Run()
    {
        failures.Clear();
        var original = Environment.GetEnvironmentVariable("MOTOR_DB_PATH");
        var temp = Path.Combine(Path.GetTempPath(), "hattrickai-v5-c19-" + Guid.NewGuid().ToString("N"));
        try
        {
            Environment.SetEnvironmentVariable("MOTOR_DB_PATH", temp);

            var own = new Lineup("Own", "3-5-2", new[]
            {
                new Slot("GK", "Kaleci", "Kaleci", "Own GK", 1, 6, 50, 10),
                new Slot("DEF-L", "Sol bek", "Sol bek", "Own DL", 2, 5, 12, 34),
                new Slot("DEF-C", "Stoper", "Stoper", "Own DC", 3, 6, 50, 34),
                new Slot("DEF-R", "Sağ bek", "Sağ bek", "Own DR", 4, 5, 88, 34),
                new Slot("IM-L", "Sol iç", "Sol iç", "Own IM-L", 5, 6, 34, 50),
                new Slot("IM-C", "Merkez", "Merkez", "Own IM-C", 6, 7, 50, 50),
                new Slot("IM-R", "Sağ iç", "Sağ iç", "Own IM-R", 7, 6, 66, 50),
                new Slot("W-L", "Sol kanat", "Sol kanat", "Own W-L", 8, 5, 12, 50),
                new Slot("W-R", "Sağ kanat", "Sağ kanat", "Own W-R", 9, 5, 88, 50),
                new Slot("FW-L", "Sol forvet", "Sol forvet", "Own FW-L", 10, 6, 38, 72),
                new Slot("FW-R", "Sağ forvet", "Sağ forvet", "Own FW-R", 11, 6, 62, 72)
            });
            var opponent = own with { TeamName = "Opponent" };
            var rating = new RegionalRatingSnapshot(6, 6, 6, 6, 6, 6, 6, 5, 5, 5, 5, 5, 5, 5);
            var analysis = new Analysis("c19-build", "Own", "Opponent", "C19", own, opponent, rating, rating, MatchQuestionnaire.Default);

            var pipeline = new MotorPipelineResult(
                null!, null!, Array.Empty<PositionAssignmentCandidate>(), null!, null!, null!, null!, null!, null!, null!, null!)
            {
                CandidateDatabase1 = Array.Empty<CandidateEvaluationRecord>(),
                CandidateDatabase2 = Array.Empty<CandidateEvaluationRecord>(),
                CandidateDatabase1Count = 0,
                CandidateDatabase2Count = 0
            };

            var runId = "c19-" + Guid.NewGuid().ToString("N");
            var path = MotorResultArchive.Save(runId, analysis, pipeline, new { motor = "C19" }, "c19-build");

            Check(File.Exists(path), "run snapshot file created");
            Check(File.Exists(Path.Combine(temp, "latest.json")), "latest.json created");

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            foreach (var field in new[] { "schemaVersion", "savedAt", "build", "runId", "source", "analysis", "motorPipeline", "candidateDatabases", "motorLog" })
                Check(root.TryGetProperty(field, out _), "root field: " + field);

            Check(root.GetProperty("schemaVersion").GetString() == "hattrickai-v5-motor-database-v1", "schema version");
            Check(root.GetProperty("runId").GetString() == runId, "runId round-trip");
            Check(root.GetProperty("build").GetString() == "c19-build", "build round-trip");
            Check(root.GetProperty("source").GetString() == "HattrickAI V5 web analysis", "source marker");

            var db = root.GetProperty("candidateDatabases");
            Check(db.TryGetProperty("db1", out var db1) && db1.ValueKind == JsonValueKind.Array, "candidateDatabases.db1 array");
            Check(db.TryGetProperty("db2", out var db2) && db2.ValueKind == JsonValueKind.Array, "candidateDatabases.db2 array");
            Check(db.GetProperty("db1Count").GetInt32() == 0, "db1Count round-trip");
            Check(db.GetProperty("db2Count").GetInt32() == 0, "db2Count round-trip");

            var latest = File.ReadAllText(Path.Combine(temp, "latest.json"));
            Check(latest == File.ReadAllText(path), "latest.json equals run snapshot");

            var entries = MotorResultArchive.List();
            Check(entries.Count == 1, "List returns one run and excludes latest.json");
            Check(entries[0].RunId == runId, "List runId");

            Check(MotorResultArchive.TryGetLatest(out var latestApiJson), "TryGetLatest finds snapshot");
            Check(latestApiJson == latest, "TryGetLatest returns latest snapshot text");
            Check(MotorResultArchive.TryGetByRunId(runId, out var runJson), "TryGetByRunId finds snapshot");
            Check(runJson == latest, "TryGetByRunId returns run snapshot text");

            Console.WriteLine("=== C19 MOTOR DB JSON REGRESSION ===");
            Console.WriteLine("PASS: archive writer + schema + latest/list/run lookup");
            return failures.Count == 0 ? 0 : Report();
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL: C19 exception: " + ex.Message);
            return 1;
        }
        finally
        {
            Environment.SetEnvironmentVariable("MOTOR_DB_PATH", original);
            try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch { }
        }
    }

    private static readonly List<string> failures = new();
    private static void Check(bool condition, string name) { if (!condition) failures.Add(name); }
    private static int Report() { foreach (var f in failures) Console.WriteLine("FAIL: " + f); Console.WriteLine($"FAIL: C19 ({failures.Count} assertion(s))"); return 1; }
}
