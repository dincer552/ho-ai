using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class ChppWrite04MockWriteRegression
{
    public static int Run()
    {
        var own = new Lineup("Own", "3-5-2", new[]
        {
            new Slot("GK", "Kaleci", "Kaleci", "GK", 1, 6, 50, 10),
            new Slot("DEF-L", "Sol bek", "Sol bek", "DL", 2, 5, 12, 34),
            new Slot("DEF-C", "Stoper", "Stoper", "DC", 3, 6, 50, 34),
            new Slot("DEF-R", "Sağ bek", "Sağ bek", "DR", 4, 5, 88, 34),
            new Slot("IM-L", "Sol iç", "Sol iç", "IM-L", 5, 6, 34, 50),
            new Slot("IM-C", "Merkez", "Merkez", "IM-C", 6, 7, 50, 50),
            new Slot("IM-R", "Sağ iç", "Sağ iç", "IM-R", 7, 6, 66, 50),
            new Slot("W-L", "Sol kanat", "Sol kanat", "W-L", 8, 5, 12, 50),
            new Slot("W-R", "Sağ kanat", "Sağ kanat", "W-R", 9, 5, 88, 50),
            new Slot("FW-L", "Sol forvet", "Sol forvet", "FW-L", 10, 6, 38, 72),
            new Slot("FW-R", "Sağ forvet", "Sağ forvet", "FW-R", 11, 6, 62, 72)
        });
        var bench = new Dictionary<string, int>
        {
            ["Kaleci"] = 12, ["Göbek defans"] = 13, ["Bek"] = 14,
            ["İç orta saha"] = 15, ["Forvet"] = 16, ["Kanat"] = 17, ["Ekstra"] = 18
        };
        var payload = ChppMatchOrderPayloadBuilder.Build(own, bench, TeamTactic.AttackWings, TeamAttitude.Normal);

        var mock = new ChppMatchOrderMockWriteService();
        var result = mock.Write(12345, 67890, payload);
        var failures = new List<string>();
        Check(result.Request.File == "matchorders", "endpoint file", failures);
        Check(result.Request.Version == "3.1", "CHPP version", failures);
        Check(result.Request.Method == "POST", "write method", failures);
        Check(result.Request.TeamId == 12345 && result.Request.MatchId == 67890, "target ids", failures);
        Check(result.Request.LineupJson.Length > 0, "lineup JSON emitted", failures);

        using var json = JsonDocument.Parse(result.Request.LineupJson);
        Check(json.RootElement.GetProperty("positions").GetArrayLength() == 14, "request positions[14]", failures);
        Check(json.RootElement.GetProperty("bench").GetArrayLength() == 14, "request bench[14]", failures);
        Check(json.RootElement.GetProperty("positions")[0].GetProperty("Id").GetInt32() == 1, "GK player id preserved", failures);
        Check(json.RootElement.GetProperty("bench")[0].GetProperty("Id").GetInt32() == 12, "primary bench player id preserved", failures);
        Check(json.RootElement.GetProperty("bench")[7].GetProperty("Id").GetInt32() == 0, "backup bench remains empty", failures);
        Check(json.RootElement.GetProperty("settings").GetProperty("Tactic").GetString() == "4", "tactic code preserved", failures);

        Check(result.Response.OrdersSet, "mock response OrdersSet=true", failures);
        Check(result.Response.TeamId == 12345 && result.Response.MatchId == 67890, "response target ids", failures);
        Check(result.Response.TacticType == 4, "response tactic code", failures);
        Check(result.Response.Attitude == 0, "response attitude", failures);
        Check(result.RawResponse.Contains("OrdersSet=\"true\"", StringComparison.Ordinal), "raw response success marker", failures);

        Console.WriteLine("=== CHPP WRITE-04 MOCK WRITE REGRESSION ===");
        foreach (var failure in failures) Console.WriteLine("FAIL: " + failure);
        Console.WriteLine(failures.Count == 0 ? "PASS: mock request/response contract" : $"FAIL: {failures.Count} assertion(s)");
        return failures.Count == 0 ? 0 : 1;
    }

    private static void Check(bool condition, string name, List<string> failures)
    {
        if (!condition) failures.Add(name);
    }
}
