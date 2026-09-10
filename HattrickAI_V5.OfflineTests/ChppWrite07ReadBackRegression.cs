using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class ChppWrite07ReadBackRegression
{
    public static int Run()
    {
        var positions = Enumerable.Range(1, 14)
            .Select(i => new ChppPlayerSlot(i <= 11 ? i : 0, i <= 11 ? i % 5 : 0))
            .ToArray();
        var bench = Enumerable.Range(12, 7)
            .Select(i => new ChppPlayerSlot(i, 0))
            .Concat(Enumerable.Repeat(new ChppPlayerSlot(0, 0), 7))
            .ToArray();
        var payload = new ChppMatchOrderPayload(
            "synthetic",
            TeamTactic.AttackWings,
            TeamAttitude.Normal,
            new ChppLineupJson(
                positions,
                bench,
                Enumerable.Repeat(new ChppPlayerSlot(0, 0), 11).ToArray(),
                string.Empty,
                string.Empty,
                new ChppLineupSettings("4", "0", string.Empty, string.Empty, string.Empty, string.Empty),
                Array.Empty<object>()));

        var players = new List<ChppMatchOrderPlayer>();
        for (var i = 0; i < 14; i++)
        {
            var slot = positions[i];
            if (slot.Id > 0) players.Add(new ChppMatchOrderPlayer(slot.Id, 100 + i, i + 1, slot.Behaviour, $"P{slot.Id}"));
        }
        for (var i = 0; i < 7; i++)
            players.Add(new ChppMatchOrderPlayer(12 + i, 114 + i, 0, 0, $"B{12 + i}"));

        var snapshot = new ChppUpcomingMatchSnapshot(
            67890, "Test", 12345, DateTimeOffset.UtcNow.AddHours(2),
            67890, 99999, "Test", "Opponent", 1, 4, 0, true, players, "fixture");

        var failures = new List<string>();
        var ok = ChppMatchOrderReadBackVerifier.Verify(payload, snapshot);
        Check(ok.Verified, "matching payload verifies", failures);

        var tacticMismatch = snapshot with { TacticType = 0 };
        Check(!ChppMatchOrderReadBackVerifier.Verify(payload, tacticMismatch).Verified, "tactic mismatch rejected", failures);

        var benchMismatch = snapshot with
        {
            Players = snapshot.Players.Select(p => p.RoleId == 114 ? p with { PlayerId = 99 } : p).ToList()
        };
        Check(!ChppMatchOrderReadBackVerifier.Verify(payload, benchMismatch).Verified, "bench mismatch rejected", failures);

        var xiMismatch = snapshot with
        {
            Players = snapshot.Players.Select(p => p.RoleId == 100 ? p with { PlayerId = 99 } : p).ToList()
        };
        Check(!ChppMatchOrderReadBackVerifier.Verify(payload, xiMismatch).Verified, "XI mismatch rejected", failures);

        const string xml = "<HattrickData><MatchData OrdersSet=\"true\"><Attitude>0</Attitude><TacticType>4</TacticType><Lineup><Positions><Player><PlayerID>1</PlayerID><RoleID>100</RoleID><Behaviour>0</Behaviour></Player></Positions><Bench><Player><PlayerID>12</PlayerID><RoleID>114</RoleID></Player></Bench></Lineup></MatchData></HattrickData>";
        var parsed = ChppMatchOrderReadService.ParseSnapshot(67890, "Test", 12345, DateTimeOffset.UtcNow.AddHours(2), 67890, 99999, "Test", "Opponent", 1, xml);
        Check(parsed.OrdersSet == true, "OrdersSet attribute parsed", failures);
        Check(parsed.Players.Count == 2 && parsed.Players[0].RoleId == 100 && parsed.Players[1].RoleId == 114, "current matchorders role IDs parsed", failures);

        Console.WriteLine("=== CHPP WRITE-07 READ-BACK REGRESSION ===");
        foreach (var failure in failures) Console.WriteLine("FAIL: " + failure);
        Console.WriteLine(failures.Count == 0 ? "PASS: read-back verification contract" : $"FAIL: {failures.Count} assertion(s)");
        return failures.Count == 0 ? 0 : 1;
    }

    private static void Check(bool condition, string name, List<string> failures)
    {
        if (!condition) failures.Add(name);
    }
}
