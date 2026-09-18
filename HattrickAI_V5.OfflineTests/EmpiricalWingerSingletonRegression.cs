using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Controlled winger singleton calibration gate against the empirical DB.
/// Gobiet provides the four order captures plus Towards Wing; Dobovari gives
/// an independent Normal singleton check for the same role.
/// </summary>
public static class EmpiricalWingerSingletonRegression
{
    private const double Tolerance = 0.25;

    public static int Run()
    {
        var engine = new RegionalRatingEngineFinal();
        var cases = new[]
        {
            Case("Gobiet-Normal", 474962854, 9, 11, 8, 15, 8, 7, 7, PlayerOrder.Normal, 1.75, 1.25, 0, 1.50, 5.25, 1.00, 0),
            Case("Gobiet-TowardsMiddle", 474962854, 9, 11, 8, 15, 8, 7, 7, PlayerOrder.TowardsMiddle, 1.50, 1.25, 0, 1.50, 4.25, 1.00, 0),
            Case("Gobiet-Defensive", 474962854, 9, 11, 8, 15, 8, 7, 7, PlayerOrder.Defensive, 2.50, 1.25, 0, 1.25, 4.00, 1.00, 0),
            Case("Gobiet-Offensive", 474962854, 9, 11, 8, 15, 8, 7, 7, PlayerOrder.Offensive, 1.50, 1.00, 0, 1.25, 5.75, 1.00, 0),
            Case("Gobiet-TowardsWing", 474962854, 9, 11, 8, 15, 8, 7, 7, PlayerOrder.TowardsWing, 1.75, 1.25, 0, 1.50, 5.00, 1.00, 0),
            Case("Dobovari-Normal", 492253331, 2, 5, 7, 17, 4, 6, 3, PlayerOrder.Normal, 1.25, 1.00, 0, 1.00, 5.25, 1.00, 0)
        };

        var failures = 0;
        foreach (var c in cases)
        {
            var player = new RegionalPlayer(c.Id, RegionalPosition.Winger, PlayerSide.Left, c.Order,
                0, c.Defending, c.Playmaking, c.Passing, c.Winger, c.Scoring,
                c.Form, 0, c.Experience, 7);
            var actual = engine.Calculate(new[] { player }, RatingContext.Default);
            var values = new[] { actual.LeftDefence, actual.CentralDefence, actual.RightDefence, actual.Midfield,
                actual.LeftAttack, actual.CentralAttack, actual.RightAttack };
            var expected = new[] { c.LeftDefence, c.CentralDefence, c.RightDefence, c.Midfield,
                c.LeftAttack, c.CentralAttack, c.RightAttack };
            var maxError = values.Zip(expected, (a, e) => Math.Abs(a - e)).Max();
            Console.WriteLine($"{c.Name}: V5={string.Join('/', values.Select(x => x.ToString("0.000000")))} DB={string.Join('/', expected.Select(x => x.ToString("0.000000")))} maxErr={maxError:0.000000}");
            if (maxError > Tolerance) failures++;
        }

        if (failures > 0) throw new InvalidOperationException($"Empirical winger singleton regression failed: {failures}/{cases.Length} cases exceed {Tolerance:0.##} tolerance.");
        Console.WriteLine("EmpiricalWingerSingletonRegression PASS");
        return 0;
    }

    private static TestCase Case(string name, int id, int defending, int playmaking, int passing, int winger,
        int scoring, int form, int experience, PlayerOrder order,
        double leftDefence, double centralDefence, double rightDefence, double midfield,
        double leftAttack, double centralAttack, double rightAttack)
        => new(name, id, defending, playmaking, passing, winger, scoring, form, experience, order,
            leftDefence, centralDefence, rightDefence, midfield, leftAttack, centralAttack, rightAttack);

    private sealed record TestCase(string Name, int Id, int Defending, int Playmaking, int Passing, int Winger,
        int Scoring, int Form, int Experience, PlayerOrder Order,
        double LeftDefence, double CentralDefence, double RightDefence, double Midfield,
        double LeftAttack, double CentralAttack, double RightAttack);
}
