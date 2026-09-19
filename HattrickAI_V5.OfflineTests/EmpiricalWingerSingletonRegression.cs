using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Controlled winger singleton calibration gate against real Hattrick screenshot
/// observations. Expected values are the WHITE Hattrick ratings only; green
/// delta values are never calibration targets. The engine remains raw: the
/// test applies Hattrick's visible minimum of 1.00 only when comparing the
/// raw sector result to the screenshot display.
/// </summary>
public static class EmpiricalWingerSingletonRegression
{
    private const double Tolerance = 0.25;

    public static int Run()
    {
        var engine = new RegionalRatingEngineFinal();
        var cases = new[]
        {
            // Existing calibration captures.
            Case("Gobiet-Normal", 474962854, 9, 11, 8, 15, 8, 7, 7, PlayerOrder.Normal, 1.75, 1.25, 0, 1.50, 5.25, 1.00, 0),
            Case("Gobiet-TowardsMiddle", 474962854, 9, 11, 8, 15, 8, 7, 7, PlayerOrder.TowardsMiddle, 1.50, 1.25, 0, 1.50, 4.25, 1.00, 0),
            Case("Gobiet-Defensive", 474962854, 9, 11, 8, 15, 8, 7, 7, PlayerOrder.Defensive, 2.50, 1.25, 0, 1.25, 4.00, 1.00, 0),
            Case("Gobiet-Offensive", 474962854, 9, 11, 8, 15, 8, 7, 7, PlayerOrder.Offensive, 1.50, 1.00, 0, 1.25, 5.75, 1.00, 0),
            Case("Gobiet-TowardsWing", 474962854, 9, 11, 8, 15, 8, 7, 7, PlayerOrder.TowardsWing, 1.75, 1.25, 0, 1.50, 5.00, 1.00, 0),
            Case("Dobovari-Normal", 492253331, 3, 5, 8, 17, 4, 7, 3, PlayerOrder.Normal, 1.25, 1.00, 0, 1.00, 5.25, 1.00, 0),

            // User-provided 0-1-0 Normal W-L screenshots. WHITE values only.
            Case("Savlet-Normal", 495027585, 4, 5, 6, 5, 7, 4, 3, PlayerOrder.Normal, 1.25, 1.00, 0, 1.00, 1.75, 1.00, 0),
            Case("Pehlivanlar-Normal", 493851210, 4, 4, 7, 5, 7, 7, 2, PlayerOrder.Normal, 1.25, 1.00, 0, 1.00, 2.25, 1.00, 0),
            Case("Beta-Normal", 491743384, 4, 5, 9, 3, 15, 7, 3, PlayerOrder.Normal, 1.25, 1.00, 0, 1.00, 2.00, 1.00, 0),
            Case("Thiebault-Normal", 498487535, 4, 5, 9, 4, 12, 8, 2, PlayerOrder.Normal, 1.25, 1.00, 0, 1.00, 2.25, 1.00, 0),
            Case("Gozay-Normal", 513885774, 6, 5, 4, 5, 5, 7, 2, PlayerOrder.Normal, 1.50, 1.00, 0, 1.00, 2.25, 1.00, 0),
            Case("Nocon-Normal", 458524225, 17, 3, 5, 4, 7, 7, 12, PlayerOrder.Normal, 2.75, 1.50, 0, 1.50, 2.00, 1.00, 0)
        };

        var failures = 0;
        foreach (var c in cases)
        {
            var player = new RegionalPlayer(c.Id, RegionalPosition.Winger, PlayerSide.Left, c.Order,
                0, c.Defending, c.Playmaking, c.Passing, c.Winger, c.Scoring,
                c.Form, 0, c.Experience, 7);
            var actual = engine.Calculate(new[] { player }, RatingContext.Default);
            var raw = new[] { actual.RawLeftDefence, actual.RawCentralDefence, actual.RawRightDefence,
                actual.RawMidfield, actual.RawLeftAttack, actual.RawCentralAttack, actual.RawRightAttack };
            var expected = new[] { c.LeftDefence, c.CentralDefence, c.RightDefence, c.Midfield,
                c.LeftAttack, c.CentralAttack, c.RightAttack };

            // Hattrick screenshots show a visible minimum of 1.00. Do not
            // round or quantize the V5 engine output itself.
            var visible = raw.Select(x => Math.Max(1.0, x)).ToArray();
            var maxError = visible.Zip(expected, (a, e) => Math.Abs(a - e)).Max();

            Console.WriteLine(
                $"{c.Name}: RAW={string.Join('/', raw.Select(x => x.ToString("0.000000")))} " +
                $"VISIBLE={string.Join('/', visible.Select(x => x.ToString("0.000000")))} " +
                $"HT={string.Join('/', expected.Select(x => x.ToString("0.000000")))} maxErr={maxError:0.000000}");

            if (maxError > Tolerance) failures++;
        }

        if (failures > 0)
            throw new InvalidOperationException(
                $"Empirical winger singleton regression failed: {failures}/{cases.Length} cases exceed {Tolerance:0.##} tolerance.");

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