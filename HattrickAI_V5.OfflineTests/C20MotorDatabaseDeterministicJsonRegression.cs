using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class C20MotorDatabaseDeterministicJsonRegression
{
    public static int Run()
    {
        var original = Environment.GetEnvironmentVariable("MOTOR_DB_PATH");
        var temp = Path.Combine(Path.GetTempPath(), "hattrickai-v5-c20-" + Guid.NewGuid().ToString("N"));
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
            var analysis = new Analysis("c20-build", "Own", "Opponent", "C20", own, opponent, rating, rating, MatchQuestionnaire.Default);
            var pipeline = new MotorPipelineResult(
                null!, null!, Array.Empty<PositionAssignmentCandidate>(), null!, null!, null!, null!, null!, null!, null!, null!)
            {
                CandidateDatabase1 = Array.Empty<CandidateEvaluationRecord>(),
                CandidateDatabase2 = Array.Empty<CandidateEvaluationRecord>(),
                CandidateDatabase1Count = 0,
                CandidateDatabase2Count = 0
            };

            var runId = "c20-fixed-run";
            MotorResultArchive.Save(runId, analysis, pipeline, new { motor = "C20" }, "c20-build");
            var first = File.ReadAllText(Path.Combine(temp, "latest.json"));

            Thread.Sleep(2);
            MotorResultArchive.Save(runId, analysis, pipeline, new { motor = "C20" }, "c20-build");
            var second = File.ReadAllText(Path.Combine(temp, "latest.json"));

            var normalizedFirst = Normalize(first);
            var normalizedSecond = Normalize(second);
            var hashFirst = Hash(normalizedFirst);
            var hashSecond = Hash(normalizedSecond);

            Check(hashFirst == hashSecond, "same payload produced different normalized JSON fingerprints");
            Check(normalizedFirst == normalizedSecond, "normalized JSON payload changed between saves");
            Check(Get(first, "savedAt") != Get(second, "savedAt"), "savedAt did not remain per-save metadata");
            Check(Get(first, "runId") == runId && Get(second, "runId") == runId, "runId changed");
            Check(Get(first, "build") == "c20-build" && Get(second, "build") == "c20-build", "build changed");
            Check(Get(first, "schemaVersion") == "hattrickai-v5-motor-database-v1" && Get(second, "schemaVersion") == "hattrickai-v5-motor-database-v1", "schema version changed");

            Console.WriteLine("=== C20 MOTOR DB DETERMINISTIC JSON REGRESSION ===");
            Console.WriteLine($"Fingerprint={hashFirst}");
            Console.WriteLine("PASS: deterministic payload; savedAt remains metadata");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL: C20 exception: " + ex.Message);
            return 1;
        }
        finally
        {
            Environment.SetEnvironmentVariable("MOTOR_DB_PATH", original);
            try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch { }
        }
    }

    private static string Normalize(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("savedAt")) continue;
                if (property.NameEquals("runId")) continue;
                property.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string Get(string json, string property)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty(property).GetString() ?? string.Empty;
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }
}
