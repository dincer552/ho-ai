using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Locks the Foxtrick-compatible player-position calculation to the supplied
/// Bertalan Doktor reference: Offensive IM 10.99 and Normal IM 10.62.
/// </summary>
public static class FoxtrickPositionRegression
{
    public static int Run()
    {
        var player = new Player(
            Id: 465805392,
            Name: "Bertalan Doktor",
            Keeper: 1,
            Defending: 2,
            Playmaking: 17,
            Passing: 11,
            Winger: 6,
            Scoring: 6,
            Stamina: 6,
            Form: 5,
            Experience: 8,
            Loyalty: 20,
            InjuryLevel: -1,
            Specialty: PlayerSpecialty.Powerful);

        var engine = new FoxtrickPositionContributionEngine();
        var result = engine.Evaluate(player);

        var failures = new List<string>();
        Check(result.Contributions.Count == 20, "Foxtrick exposes all 20 position types", failures);
        Check(result.BestPositionCode == "imo", $"best position is imo, got {result.BestPositionCode}", failures);
        CheckNear(result.BestPositionValue, 10.99, .01, "best position value", failures);
        CheckNear(result.Contributions["im"], 10.62, .01, "normal IM", failures);
        CheckNear(result.Contributions["imd"], 10.22, .01, "defensive IM", failures);
        CheckNear(result.Contributions["imtw"], 10.13, .01, "IM toward wing", failures);
        CheckNear(result.Contributions["fwd"], 9.00, .01, "defensive forward", failures);
        CheckNear(result.Contributions["tdf"], 0.00, .01, "technical defensive forward for non-technical player", failures);

        if (failures.Count > 0)
        {
            Console.Error.WriteLine("FoxtrickPositionRegression FAILED");
            foreach (var failure in failures) Console.Error.WriteLine($" - {failure}");
            return 1;
        }

        Console.WriteLine("FoxtrickPositionRegression PASS");
        return 0;
    }

    private static void Check(bool condition, string message, ICollection<string> failures)
    {
        if (!condition) failures.Add(message);
    }

    private static void CheckNear(double actual, double expected, double tolerance, string label, ICollection<string> failures)
    {
        if (Math.Abs(actual - expected) > tolerance)
            failures.Add($"{label}: expected {expected:0.00}, got {actual:0.00}");
    }
}
