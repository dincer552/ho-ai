using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// CAL-001 diagnostic matrix. Production engines are not retuned; the test
/// compares controlled integer skill/experience hypotheses and the researched engine.
/// </summary>
public static class CAL001ModelVariantRegression
{
    private static readonly double[] GroundTruth = { 13.0, 12.75, 13.25, 7.0, 15.75, 13.75, 13.5 };
    private static readonly string[] Labels = { "LD", "CD", "RD", "MID", "LA", "CA", "RA" };

    public static int Run()
    {
        var players = BuildPlayers();
        var slots = new[]
        {
            Slot("GK", players[0]), Slot("DEF-L", players[1]), Slot("DEF-C", players[2]), Slot("DEF-R", players[3]),
            Slot("W-L", players[4]), Slot("IM-L", players[5]), Slot("IM-R", players[6]), Slot("W-R", players[7]),
            Slot("FW-L", players[8]), Slot("FW-C", players[9]), Slot("FW-R", players[10])
        };
        var lineup = new Lineup("CAL-001", "3-4-3", slots);
        var fixedEngine = new RegionalRatingEngineFixed();
        var researchedEngine = new RegionalRatingEngine();

        var variants = new[]
        {
            new Variant("A Fixed: skill-1 + separate experience", fixedEngine.CalculateLineup(lineup, players, RatingContext.Default)),
            new Variant("B Fixed: raw skill + separate experience", fixedEngine.CalculateLineup(lineup, ShiftSkills(players, +1, keepExperience: true), RatingContext.Default)),
            new Variant("C Fixed: skill-1 + no experience", fixedEngine.CalculateLineup(lineup, ShiftSkills(players, 0, keepExperience: false), RatingContext.Default)),
            new Variant("D Fixed: raw skill + no experience", fixedEngine.CalculateLineup(lineup, ShiftSkills(players, +1, keepExperience: false), RatingContext.Default)),
            new Variant("E Research engine: raw skill + effective experience delta", researchedEngine.CalculateLineup(lineup, players, RatingContext.Default)),
            new Variant("F Research engine: skill-1 + effective experience delta", researchedEngine.CalculateLineup(lineup, ShiftSkills(players, -1, keepExperience: true), RatingContext.Default))
        };

        foreach (var variant in variants)
        {
            var values = Values(variant.Snapshot);
            var errors = values.Zip(GroundTruth, (actual, gt) => actual - gt).ToArray();
            var mae = errors.Select(Math.Abs).Average();
            var bias = errors.Average();
            var rmse = Math.Sqrt(errors.Select(x => x * x).Average());
            Console.WriteLine($"{variant.Name}: MAE={mae:F4} Bias={bias:+0.0000;-0.0000;0.0000} RMSE={rmse:F4}");
            for (var i = 0; i < Labels.Length; i++)
                Console.WriteLine($"  {Labels[i]}={values[i]:F4} gt={GroundTruth[i]:F2} err={errors[i]:+0.0000;-0.0000;0.0000}");
        }

        var baseline = fixedEngine.CalculateLineup(lineup, players, RatingContext.Default);
        var baselineRaw = new[]
        {
            baseline.RawLeftDefence, baseline.RawCentralDefence, baseline.RawRightDefence,
            baseline.RawMidfield, baseline.RawLeftAttack, baseline.RawCentralAttack, baseline.RawRightAttack
        };
        var expectedRaw = new[]
        {
            7.547173492520718, 15.38792826270137, 8.722175800147264,
            8.356595269523073, 9.736919924603303, 12.138912750812356, 9.931162425931916
        };
        var failures = baselineRaw.Where((value, i) => Math.Abs(value - expectedRaw[i]) > 1e-12).Count();
        Console.WriteLine(failures == 0
            ? "PASS: CAL-001 model variant matrix; production baseline unchanged"
            : $"FAIL: CAL-001 production baseline changed ({failures} raw mismatches)");
        return failures == 0 ? 0 : 1;
    }

    private static Player[] ShiftSkills(Player[] source, int delta, bool keepExperience)
    {
        return source.Select(p => new Player(
            p.Id, p.Name,
            Math.Max(0, p.Keeper + delta),
            Math.Max(0, p.Defending + delta),
            Math.Max(0, p.Playmaking + delta),
            Math.Max(0, p.Passing + delta),
            Math.Max(0, p.Winger + delta),
            Math.Max(0, p.Scoring + delta),
            p.Stamina, p.Form, keepExperience ? p.Experience : 1)).ToArray();
    }

    private static double[] Values(RegionalRatingSnapshot s) => new[]
    {
        s.LeftDefence, s.CentralDefence, s.RightDefence,
        s.Midfield, s.LeftAttack, s.CentralAttack, s.RightAttack
    };

    private static Player[] BuildPlayers() => new[]
    {
        new Player(479235895, "Enzo Bultot", 17, 4, 1, 2, 1, 4, 7, 6, 6),
        new Player(468363070, "Abeiku Takyi", 1, 17, 3, 8, 4, 7, 6, 5, 10),
        new Player(458524225, "Dawid Nocoń", 0, 17, 3, 5, 4, 7, 5, 6, 12),
        new Player(476114406, "Cristian Pesalovo", 1, 16, 6, 9, 3, 6, 7, 7, 6),
        new Player(492052456, "Felix Gustavsson", 1, 3, 3, 8, 17, 5, 7, 7, 4),
        new Player(465805392, "Bertalan Doktor", 1, 2, 16, 11, 6, 6, 6, 6, 8),
        new Player(465141092, "Milen Bozev", 1, 5, 12, 9, 14, 6, 6, 7, 8),
        new Player(474962854, "Manuel Gobiet", 1, 9, 11, 9, 15, 8, 6, 8, 7),
        new Player(497641568, "Ersin Akşin", 1, 2, 5, 9, 4, 13, 6, 7, 3),
        new Player(491743384, "Adrian Beţa", 1, 4, 5, 8, 3, 15, 7, 7, 3),
        new Player(495041177, "Andres Nahasepp", 1, 3, 6, 7, 5, 13, 7, 7, 3)
    };

    private static Slot Slot(string code, Player player) => new(code, code, "CAL-001", player.Name, player.Id, 0, 0, 0, PlayerOrder.Normal);

    private sealed record Variant(string Name, RegionalRatingSnapshot Snapshot);
}
