namespace HattrickAI.V5.Core;

/// <summary>
/// Simple first-pass bench recommender. It intentionally does not enter the M6/M9
/// optimization path. It selects alternatives only from players outside the final XI.
/// </summary>
public sealed record BenchRecommendation(
    string Slot,
    IReadOnlyList<Player> Alternatives);

public sealed class BenchRecommendationEngine
{
    private static readonly (string Slot, string[] Codes)[] Slots =
    {
        ("Kaleci", new[] { "GK" }),
        ("Göbek defans", new[] { "DEF-C", "DEF-CR", "DEF-CL" }),
        ("Bek", new[] { "DEF-R", "DEF-L" }),
        ("İç orta saha", new[] { "IM-C", "IM-R", "IM-L" }),
        ("Forvet", new[] { "FW-C", "FW-R", "FW-L" }),
        ("Kanat", new[] { "W-R", "W-L" }),
        ("Ekstra", Array.Empty<string>())
    };

    public IReadOnlyList<BenchRecommendation> Recommend(
        IReadOnlyList<Player> allPlayers,
        IReadOnlyList<Slot> finalXi)
    {
        var xiIds = finalXi.Select(x => x.PlayerId).Where(id => id > 0).ToHashSet();
        var available = allPlayers.Where(p => !xiIds.Contains(p.Id)).ToList();
        var used = new HashSet<int>();
        var result = new List<BenchRecommendation>(Slots.Length);

        foreach (var (slot, codes) in Slots)
        {
            var pool = codes.Length == 0
                ? available.Where(p => !used.Contains(p.Id))
                : available.Where(p => !used.Contains(p.Id) && FitsSlot(p, codes));

            var selected = pool
                .OrderByDescending(p => PositionScore(p, codes))
                .ThenByDescending(p => GeneralQuality(p))
                .ThenBy(p => p.Id)
                .Take(2)
                .ToList();

            foreach (var player in selected) used.Add(player.Id);
            result.Add(new BenchRecommendation(slot, selected));
        }

        return result;
    }

    private static bool FitsSlot(Player p, IReadOnlyCollection<string> codes)
    {
        // First-pass role inference from player skills. This deliberately stays cheap;
        // the detailed M3 positional model remains the source of truth for XI selection.
        return codes.Any(code => code switch
        {
            "GK" => p.Keeper >= Max(p.Defender, p.Playmaker, p.Passing, p.Winger, p.Scorer),
            "DEF-C" or "DEF-CR" or "DEF-CL" => p.Defender >= Math.Max(p.Playmaker, p.Winger),
            "DEF-R" or "DEF-L" => p.Defender >= p.Winger && p.Defender >= p.Playmaker,
            "IM-C" or "IM-R" or "IM-L" => p.Playmaker >= p.Defender && p.Playmaker >= p.Winger,
            "FW-C" or "FW-R" or "FW-L" => p.Scorer >= Math.Max(p.Playmaker, p.Winger),
            "W-R" or "W-L" => p.Winger >= Math.Max(p.Playmaker, p.Scorer),
            _ => true
        });
    }

    private static double PositionScore(Player p, IReadOnlyCollection<string> codes)
    {
        if (codes.Count == 0) return GeneralQuality(p);
        return codes.Max(code => code switch
        {
            "GK" => p.Keeper,
            "DEF-C" or "DEF-CR" or "DEF-CL" => p.Defender,
            "DEF-R" or "DEF-L" => (p.Defender * 0.7) + (p.Winger * 0.3),
            "IM-C" or "IM-R" or "IM-L" => (p.Playmaker * 0.7) + (p.Passing * 0.3),
            "FW-C" or "FW-R" or "FW-L" => (p.Scorer * 0.7) + (p.Passing * 0.3),
            "W-R" or "W-L" => (p.Winger * 0.7) + (p.Passing * 0.3),
            _ => GeneralQuality(p)
        });
    }

    private static double GeneralQuality(Player p) =>
        (p.Keeper + p.Defender + p.Playmaker + p.Passing + p.Winger + p.Scorer) / 6.0;

    private static int Max(params int[] values) => values.Length == 0 ? 0 : values.Max();
}
