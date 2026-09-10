using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class ChppWrite03ValidationRegression
{
    public static int Run()
    {
        var now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
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
            ["Kaleci"] = 12,
            ["Göbek defans"] = 13,
            ["Bek"] = 14,
            ["İç orta saha"] = 15,
            ["Forvet"] = 16,
            ["Kanat"] = 17,
            ["Ekstra"] = 18
        };

        var payload = ChppMatchOrderPayloadBuilder.Build(own, bench, TeamTactic.Normal, TeamAttitude.Normal);
        var target = new ChppUpcomingMatchSnapshot(
            99, "Own", 12345, now.AddHours(2), 99, 100, "Own", "Opponent", 1,
            0, 0, false, Array.Empty<ChppMatchOrderPlayer>(), "test");

        var roster = Enumerable.Range(1, 18).ToArray();
        var valid = ChppMatchOrderValidator.Validate(payload, target, roster, now);
        Check(valid.Count == 0, "valid payload passes");
        Check(payload.Lineup.Positions.Count == 14, "14 position slots");
        Check(payload.Lineup.Positions.Count(x => x.Id > 0) == 11, "11 starters");
        Check(payload.Lineup.Bench.Count == 14, "14 bench slots");
        Check(payload.Lineup.Bench.Take(7).Count(x => x.Id > 0) == 7, "7 primary bench");
        Check(payload.Lineup.Bench.Skip(7).All(x => x.Id == 0), "backup bench empty");

        var duplicateBench = new Dictionary<string, int>(bench) { ["Ekstra"] = 12 };
        var duplicateBuildFailed = false;
        try { ChppMatchOrderPayloadBuilder.Build(own, duplicateBench, TeamTactic.Normal, TeamAttitude.Normal); }
        catch (InvalidOperationException) { duplicateBuildFailed = true; }
        Check(duplicateBuildFailed, "builder rejects bench duplicate");

        var lateTarget = target with { MatchDate = now.AddMinutes(10) };
        var late = ChppMatchOrderValidator.Validate(payload, lateTarget, roster, now);
        Check(late.Any(x => x.Contains("en az 20 dakika", StringComparison.OrdinalIgnoreCase)), "deadline guard");

        var outsider = ChppMatchOrderValidator.Validate(payload, target, Enumerable.Range(1, 17), now);
        Check(outsider.Any(x => x.Contains("kadrosunda olmayan", StringComparison.OrdinalIgnoreCase)), "roster membership guard");

        var notOwnMatch = target with { HomeTeamId = 200, AwayTeamId = 201 };
        var wrongMatch = ChppMatchOrderValidator.Validate(payload, notOwnMatch, roster, now);
        Check(wrongMatch.Any(x => x.Contains("takımın maçı değil", StringComparison.OrdinalIgnoreCase)), "target team guard");

        Console.WriteLine("=== CHPP WRITE-03 VALIDATION REGRESSION ===");
        foreach (var failure in failures) Console.WriteLine("FAIL: " + failure);
        Console.WriteLine(failures.Count == 0 ? "PASS: payload, roster, duplicate, target and deadline validation" : $"FAIL: {failures.Count} assertion(s)");
        return failures.Count == 0 ? 0 : 1;
    }

    private static readonly List<string> failures = new();
    private static void Check(bool condition, string name)
    {
        if (!condition) failures.Add(name);
    }
}
