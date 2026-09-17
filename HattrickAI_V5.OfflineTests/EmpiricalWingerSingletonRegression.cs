using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Controlled winger-order calibration gate.
/// Ground truth is the 2026-09-16 M. Gobiet W-L singleton set: the same
/// player and slot captured as Normal, Towards Middle, Offensive and Defensive.
/// </summary>
public static class EmpiricalWingerSingletonRegression
{
    private const double Tolerance = 0.25;

    public static int Run()
    {
        var engine = new RegionalRatingEngineFinal();
        var cases = new[]
        {
            Case("Gobiet-Normal", PlayerOrder.Normal, 1.50, 1.25, 0, 2.00, 4.00, 1.25, 0),
            Case("Gobiet-TowardsMiddle", PlayerOrder.TowardsMiddle, 1.25, 1.50, 0, 2.25, 1.25, 1.75, 0),
            Case("Gobiet-Offensive", PlayerOrder.Offensive, 1.00, 1.00, 0, 2.25, 1.50, 2.25, 0),
            Case("Gobiet-Defensive", PlayerOrder.Defensive, 1.50, 1.75, 0, 2.25, 1.25, 1.25, 0)
        };

        var failures = 0;
        foreach (var c in cases)
        {
            var player = new RegionalPlayer(474962854, RegionalPosition.Winger, PlayerSide.Left, c.Order,
                0, 9, 11, 9, 15, 8, 8, 0, 7, 6);
            var actual = engine.Calculate(new[] { player }, RatingContext.Default);
            var values = new[] { actual.LeftDefence, actual.CentralDefence, actual.RightDefence, actual.Midfield,
                actual.LeftAttack, actual.CentralAttack, actual.RightAttack };
            var expected = new[] { c.LeftDefence, c.CentralDefence, c.RightDefence, c.Midfield,
                c.LeftAttack, c.CentralAttack, c.RightAttack };
            var maxError = values.Zip(expected, (a, e) => Math.Abs(a - e)).Max();
            Console.WriteLine($"{c.Name}: V5={string.Join('/', values.Select(x => x.ToString("0.##")))} DB={string.Join('/', expected.Select(x => x.ToString("0.##")))} maxErr={maxError:0.##}");
            if (maxError > Tolerance) failures++;
        }

        if (failures > 0) throw new InvalidOperationException($"Empirical winger singleton regression failed: {failures}/{cases.Length} cases exceed {Tolerance:0.##} tolerance.");
        Console.WriteLine("EmpiricalWingerSingletonRegression PASS");
        return 0;
    }

    private static TestCase Case(string name, PlayerOrder order, double leftDefence, double centralDefence, double rightDefence,
        double midfield, double leftAttack, double centralAttack, double rightAttack)
        => new(name, order, leftDefence, centralDefence, rightDefence, midfield, leftAttack, centralAttack, rightAttack);

    private sealed record TestCase(string Name, PlayerOrder Order, double LeftDefence, double CentralDefence, double RightDefence,
        double Midfield, double LeftAttack, double CentralAttack, double RightAttack);
}
