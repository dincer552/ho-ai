using System.Collections.Concurrent;

namespace HattrickAI.V5.Core;

public sealed record BenchPlayerOption(int Id,string Name,string[] EligibleSlots);
public sealed record BenchSelectionPayload(IReadOnlyList<BenchRecommendation> Recommendations,IReadOnlyList<BenchPlayerOption> Candidates);

public static class BenchSelectionState
{
    private static readonly ConcurrentDictionary<string, BenchSelectionPayload> State = new(StringComparer.Ordinal);

    public static BenchSelectionPayload Build(IReadOnlyList<Player> players, IReadOnlyList<Slot> finalXi)
    {
        var recommendations = new BenchRecommendationEngine().Recommend(players, finalXi);
        var xiIds = finalXi.Select(x => x.PlayerId).Where(x => x > 0).ToHashSet();
        var candidates = players.Where(p => !xiIds.Contains(p.Id))
            .Select(p => new BenchPlayerOption(p.Id,p.Name,EligibleSlots(p).ToArray()))
            .OrderBy(p => p.Name,StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Id).ToList();
        return new BenchSelectionPayload(recommendations,candidates);
    }

    public static void Save(string ownerKey,IReadOnlyList<Player> players,IReadOnlyList<Slot> finalXi)
        => State[ownerKey] = Build(players,finalXi);

    public static bool TryGet(string ownerKey,out BenchSelectionPayload payload)
        => State.TryGetValue(ownerKey,out payload!);

    private static IEnumerable<string> EligibleSlots(Player p)
    {
        if(p.Keeper>=Max(p.Defending,p.Playmaking,p.Passing,p.Winger,p.Scoring))yield return "Kaleci";
        if(p.Defending>=Math.Max(p.Playmaking,p.Winger))yield return "Göbek defans";
        if(p.Defending>=p.Winger&&p.Defending>=p.Playmaking)yield return "Bek";
        if(p.Playmaking>=p.Defending&&p.Playmaking>=p.Winger)yield return "İç orta saha";
        if(p.Scoring>=Math.Max(p.Playmaking,p.Winger))yield return "Forvet";
        if(p.Winger>=Math.Max(p.Playmaking,p.Scoring))yield return "Kanat";
        yield return "Ekstra";
    }

    private static int Max(params int[] values)=>values.Length==0?0:values.Max();
}
