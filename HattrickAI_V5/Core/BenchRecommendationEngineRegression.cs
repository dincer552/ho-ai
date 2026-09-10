namespace HattrickAI.V5.Core;

public static class BenchRecommendationEngineRegression
{
    public static void Run()
    {
        var players = new List<Player>
        {
            P(1, 10, 1, 1, 1, 1, 1, 1),
            P(2, 1, 10, 4, 3, 2, 1, 1),
            P(3, 1, 9, 3, 3, 2, 1, 1),
            P(4, 1, 7, 2, 2, 8, 1, 1),
            P(5, 1, 2, 10, 8, 2, 1, 1),
            P(6, 1, 2, 9, 7, 2, 1, 1),
            P(7, 1, 2, 2, 3, 2, 10, 1),
            P(8, 1, 2, 2, 3, 2, 9, 1),
            P(9, 1, 3, 4, 4, 8, 2, 1),
            P(10, 1, 3, 4, 4, 9, 2, 1),
            P(11, 1, 4, 5, 5, 5, 8, 1),
            P(12, 1, 5, 5, 5, 5, 5, 1),
            P(13, 1, 6, 6, 6, 6, 6, 1),
            P(14, 1, 6, 6, 6, 6, 6, 1),
            P(15, 1, 6, 6, 6, 6, 6, 1),
            P(16, 1, 6, 6, 6, 6, 6, 1),
            P(17, 1, 6, 6, 6, 6, 6, 1),
            P(18, 1, 6, 6, 6, 6, 6, 1)
        };

        var xi = new List<Slot>
        {
            SlotFor(1, "GK"), SlotFor(2, "DEF-C"), SlotFor(3, "DEF-C"), SlotFor(4, "DEF-R"),
            SlotFor(5, "IM-C"), SlotFor(6, "IM-C"), SlotFor(7, "W-R"), SlotFor(8, "W-L"),
            SlotFor(9, "FW-C"), SlotFor(10, "FW-L"), SlotFor(11, "FW-R")
        };

        var result = new BenchRecommendationEngine().Recommend(players, xi);
        if (result.Count != 7) throw new InvalidOperationException("YED-01: 7 slot üretilmedi.");
        if (result.Any(x => x.Alternatives.Count > 2)) throw new InvalidOperationException("YED-01: slot başına 2'den fazla alternatif.");
        if (result.SelectMany(x => x.Alternatives).Any(p => xi.Any(s => s.PlayerId == p.Id)))
            throw new InvalidOperationException("YED-01: ilk 11 oyuncusu yedek havuzuna girdi.");

        var ids = result.SelectMany(x => x.Alternatives).Select(p => p.Id).ToList();
        if (ids.Count != ids.Distinct().Count()) throw new InvalidOperationException("YED-01: aynı oyuncu birden fazla yedek slotuna atandı.");
    }

    private static Player P(int id, int keeper, int defender, int playmaker, int passing, int winger, int scorer, int stamina) =>
        new(id, $"P{id}", keeper, defender, playmaker, passing, winger, scorer, stamina, 7, 5, 1, 0, PlayerSpecialty.None, 1);

    private static Slot SlotFor(int id, string code) =>
        new(code, code, code, $"P{id}", id, 1.0, 50, 50, PlayerOrder.Normal, 5);
}
