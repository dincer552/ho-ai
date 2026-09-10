namespace HattrickAI.V5.Core;

/// <summary>Simple first-pass bench recommendation engine; independent of M6/M9 optimization.</summary>
public sealed class BenchRecommendationEngineV2
{
    private static readonly (string Slot, Func<Player, double> Score)[] Slots =
    {
        ("Kaleci", p => p.KeeperSkill),
        ("Göbek defans", p => p.DefenderSkill + p.PassingSkill * 0.20),
        ("Bek", p => p.DefenderSkill * 0.65 + p.WingerSkill * 0.35),
        ("İç orta saha", p => p.PlaymakerSkill * 0.65 + p.PassingSkill * 0.35),
        ("Forvet", p => p.ScorerSkill * 0.70 + p.PassingSkill * 0.30),
        ("Kanat", p => p.WingerSkill * 0.70 + p.PassingSkill * 0.30),
        ("Ekstra", GeneralScore)
    };

    public IReadOnlyList<BenchRecommendation> Recommend(IReadOnlyList<Player> allPlayers, IReadOnlyCollection<int> finalXiIds)
    {
        var used = finalXiIds.ToHashSet();
        var available = allPlayers.Where(p => p.Id > 0 && !used.Contains(p.Id)).ToList();
        var result = new List<BenchRecommendation>(7);

        foreach (var (slot, score) in Slots)
        {
            var selected = available
                .Where(p => !used.Contains(p.Id) && IsPlausible(p, slot))
                .OrderByDescending(score)
                .ThenByDescending(GeneralScore)
                .ThenBy(p => p.Id)
                .Take(2)
                .ToList();
            foreach (var p in selected) used.Add(p.Id);
            result.Add(new BenchRecommendation(slot, selected));
        }
        return result;
    }

    private static bool IsPlausible(Player p, string slot) => slot switch
    {
        "Kaleci" => p.KeeperSkill >= Math.Max(p.DefenderSkill, Math.Max(p.PlaymakerSkill, Math.Max(p.WingerSkill, p.ScorerSkill))),
        "Göbek defans" => p.DefenderSkill >= p.WingerSkill && p.DefenderSkill >= p.ScorerSkill,
        "Bek" => p.DefenderSkill >= 3,
        "İç orta saha" => p.PlaymakerSkill >= p.DefenderSkill || p.PlaymakerSkill >= p.WingerSkill,
        "Forvet" => p.ScorerSkill >= 3,
        "Kanat" => p.WingerSkill >= 3,
        _ => true
    };

    private static double GeneralScore(Player p) =>
        (p.KeeperSkill + p.DefenderSkill + p.PlaymakerSkill + p.PassingSkill + p.WingerSkill + p.ScorerSkill) / 6.0;
}
