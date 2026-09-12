using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage2RatingDisplayRegression
{
    public static int Run()
    {
        var failures = new List<string>();
        Check(HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftDefence, 0) == 1.0, "zero raw -> rating 1", failures);
        Check(HattrickRatingDisplayConverter.ToDisplay(RatingSector.Midfield, 10) == 1.98, "midfield raw 10 -> 1.98", failures);
        Check(HattrickRatingDisplayConverter.ToDisplay(RatingSector.CentralDefence, 10) == 2.73, "central defence raw 10 -> 2.73", failures);
        Check(HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftDefence, 10) == 4.19, "left defence raw 10 -> 4.19", failures);
        Check(HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftAttack, 10) == 3.21, "left attack raw 10 -> 3.21", failures);
        Check(HattrickRatingDisplayConverter.ToDisplay(RatingSector.CentralAttack, 10) == 2.78, "central attack raw 10 -> 2.78", failures);

        var raw = new RegionalRatingSnapshot(10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10);
        var converted = Stage2RegionalRatingEngine.Convert(raw);
        Check(converted.RawLeftDefence == 10 && converted.RawMidfield == 10, "raw ledger preserved", failures);
        Check(converted.LeftDefence == 4.19 && converted.Midfield == 1.98, "snapshot display fields use sector conversion", failures);
        Check(converted.CentralDefence < converted.LeftDefence, "sector scales are distinct", failures);
        Check(converted.LeftAttack > converted.CentralAttack, "side/central attack scales are distinct", failures);

        if (failures.Count == 0)
        {
            Console.WriteLine("PASS: Stage 2 raw-to-display rating conversion");
            return 0;
        }

        foreach (var failure in failures) Console.WriteLine("FAIL: " + failure);
        Console.WriteLine($"FAIL: Stage 2 ({failures.Count} assertion(s))");
        return 1;
    }

    private static void Check(bool condition, string name, List<string> failures)
    {
        if (!condition) failures.Add(name);
    }
}
