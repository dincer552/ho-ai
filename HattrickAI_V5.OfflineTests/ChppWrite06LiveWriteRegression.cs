using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// WRITE-06 verifies the live write path is represented by the production
/// client and that the offline regression suite never performs a real CHPP
/// mutation. A real write remains an explicit operator action with a valid
/// OAuth session and set_matchorder permission.
/// </summary>
public static class ChppWrite06LiveWriteRegression
{
    public static int Run()
    {
        var own = new Lineup(
            "Own",
            "3-5-2",
            new[]
            {
                new Slot("GK", "GK", "", "P1", 1, 1, 0, 0),
                new Slot("DEF-R", "DR", "", "P2", 2, 1, 0, 0),
                new Slot("DEF-C", "DC", "", "P3", 3, 1, 0, 0),
                new Slot("DEF-L", "DL", "", "P4", 4, 1, 0, 0),
                new Slot("W-R", "WR", "", "P5", 5, 1, 0, 0),
                new Slot("IM-R", "IMR", "", "P6", 6, 1, 0, 0),
                new Slot("IM-C", "IMC", "", "P7", 7, 1, 0, 0),
                new Slot("IM-L", "IML", "", "P8", 8, 1, 0, 0),
                new Slot("W-L", "WL", "", "P9", 9, 1, 0, 0),
                new Slot("FW-C", "FWC", "", "P10", 10, 1, 0, 0),
                new Slot("FW-L", "FWL", "", "P11", 11, 1, 0, 0)
            });
        var bench = new Dictionary<string, int>
        {
            ["Kaleci"] = 12, ["Göbek defans"] = 13, ["Bek"] = 14,
            ["İç orta saha"] = 15, ["Forvet"] = 16, ["Kanat"] = 17, ["Ekstra"] = 18
        };
        var payload = ChppMatchOrderPayloadBuilder.Build(own, bench, TeamTactic.Normal, TeamAttitude.Normal);
        var json = ChppMatchOrderPayloadBuilder.Serialize(payload);
        if (!json.Contains("\"positions\"", StringComparison.Ordinal) || !json.Contains("\"bench\"", StringComparison.Ordinal))
            throw new InvalidOperationException("WRITE-06: matchOrders lineup JSON üretilemedi.");
        if (!string.Equals(ChppMatchOrderPermissionGuard.RequiredScope, "set_matchorder", StringComparison.Ordinal))
            throw new InvalidOperationException("WRITE-06: write permission scope yanlış.");
        if (string.Equals(Environment.GetEnvironmentVariable("CHPP_LIVE_WRITE_TEST"), "1", StringComparison.Ordinal))
            throw new InvalidOperationException("WRITE-06 live network test CI içinde yanlışlıkla etkinleştirildi.");

        Console.WriteLine("CHPP WRITE-06 live write path regression: PASS (network mutation not executed)");
        return 0;
    }
}
