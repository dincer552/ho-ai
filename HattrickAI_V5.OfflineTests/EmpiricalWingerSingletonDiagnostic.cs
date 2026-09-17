using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Controlled winger singleton diagnostic against the 2026-09-14 / 2026-09-16
/// empirical screenshot set. This is intentionally diagnostic: the current
/// winger coefficients are not changed by this test. The output exposes the
/// per-order delta before a production calibration is accepted.
/// </summary>
public static class EmpiricalWingerSingletonDiagnostic
{
    public static int Run()
    {
        var engine = new RegionalRatingEngineFinal();
        var cases = new[]
        {
            Case("Gobiet-Normal", PlayerOrder.Normal, 1.75, 1.25, 0, 1.50, 5.25, 1.00, 0),
            Case("Gobiet-TowardsMiddle", PlayerOrder.TowardsMiddle, 1.50, 1.25, 0, 1.50, 4.25, 1.00, 0),
            Case("Gobiet-Defensive", PlayerOrder.Defensive, 2.50, 1.25, 0, 1.25, 4.00, 1.00, 0),
            Case("Gobiet-Offensive", PlayerOrder.Offensive, 1.50, 1.00, 0, 1.25, 5.75, 1.00, 0),
            Case("Gobiet-TowardsWing", PlayerOrder.TowardsWing, 1.75, 1.25, 0, 1.50, 5.00, 1.00, 0),
            Case("Dobovari-Normal", PlayerOrder.Normal, 1.25, 1.00, 0, 1.00, 5.25, 1.00, 0)
        };

        foreach (var c in cases)
        {
            var player = new RegionalPlayer(
                c.Id, RegionalPosition.Winger, c.Side, c.Order,
                0, c.Defending, c.Playmaking, c.Passing, c.Winger, c.Scoring,
                c.Form, 0, c.Experience, c.Stamina);

            var actual = engine.Calculate(new[] { player }, RatingContext.Default);
            var values = new[]
            {
                actual.LeftDefence, actual.CentralDefence, actual.RightDefence,
                actual.Midfield, actual.LeftAttack, actual.CentralAttack, actual.RightAttack
            };
            var expected = new[]
            {
                c.LeftDefence, c.CentralDefence, c.RightDefence,
                c.Midfield, c.LeftAttack, c.CentralAttack, c.RightAttack
            };
            var maxError = values.Zip(expected, (a, e) => Math.Abs(a - e)).Max();
            Console.WriteLine($"{c.Name}: V5={string.Join('/', values.Select(x => x.ToString(\"0.##\")))} DB={string.Join('/', expected.Select(x => x.ToString(\"0.##\")))} maxErr={maxError:0.##}");
        }

        Console.WriteLine("EmpiricalWingerSingletonDiagnostic COMPLETE");
        return 0;
    }

    private static TestCase Case(string name, PlayerOrder order,
        double leftDefence, double centralDefence, double rightDefence, double midfield,
        double leftAttack, double centralAttack, double rightAttack)
        => new(name, 474962854, 9, 11, 8, 15, 8, 7, 7, 6,
            PlayerSide.Left, order, leftDefence, centralDefence, rightDefence,
            midfield, leftAttack, centralAttack, rightAttack);

    private sealed record TestCase(
        string Name, int Id, int Defending, int Playmaking, int Passing, int Winger,
        int Scoring, int Form, int Experience, int Stamina, PlayerSide Side, PlayerOrder Order,
        double LeftDefence, double CentralDefence, double RightDefence, double Midfield,
        double LeftAttack, double CentralAttack, double RightAttack);
}
