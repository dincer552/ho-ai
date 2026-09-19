using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Permanent regression gate for the user's real 0-1-0 Normal W-L winger
/// screenshots. Expected values are WHITE Hattrick ratings only; green delta
/// values are never calibration targets. V5 engine outputs stay raw.
/// </summary>
public static class EmpiricalWingerSingletonRegression
{
    private const double Tolerance = 0.25;

    public static int Run()
    {
        var engine = new RegionalRatingEngineFinal();

        var cases = new[]
        {
            Case("Savlet-Normal", 495027585, 4, 5, 6, 5, 7, 4, 3, 1.25, 1.00, 0, 1.00, 1.75, 1.00, 0),
            Case("Pehlivanlar-Normal", 493851210, 4, 4, 7, 5, 7, 7, 2, 1.25, 1.00, 0, 1.00, 2.25, 1.00, 0),
            Case("Beta-Normal", 491743384, 4, 5, 9, 3, 15, 7, 3, 1.25, 1.00, 0, 1.00, 2.00, 1.00, 0),
            Case("Thiebault-Normal", 498487535, 4, 5, 9, 4, 12, 8, 2, 1.25, 1.00, 0, 1.00, 2.25, 1.00, 0),
            Case("Gozay-Normal", 513885774, 6, 5, 4, 5, 5, 7, 2, 1.50, 1.00, 0, 1.00, 2.25, 1.00, 0),
            Case("Nocon-Normal", 458524225, 17, 3, 5, 4, 7, 7, 12, 2.75, 1.50, 0, 1.50, 2.00, 1.00, 0)
        };

        var failures = 0;

        foreach (var c in cases)
        {
            var player = new RegionalPlayer(
                c.Id, RegionalPosition.Winger, PlayerSide.Left, PlayerOrder.Normal,
                0, c.Defending, c.Playmaking, c.Passing, c.Winger, c.Scoring,
                c.Form, 0, c.Experience, 7);

            var actual = engine.Calculate(new[] { player }, RatingContext.Default);
            var raw = new[]
            {
                actual.RawLeftDefence, actual.RawCentralDefence, actual.RawRightDefence,
                actual.RawMidfield, actual.RawLeftAttack, actual.RawCentralAttack,
                actual.RawRightAttack
            };

            var expected = new[]
            {
                c.LeftDefence, c.CentralDefence, c.RightDefence, c.Midfield,
                c.LeftAttack, c.CentralAttack, c.RightAttack
            };

            var errors = raw.Zip(expected, (a, e) =>
            {
                // Hattrick displays a minimum of 1.00 for non-zero team
                // sectors. A genuine zero sector remains zero.
                var visible = e == 0 ? a : Math.Max(1.0, a);
                return Math.Abs(visible - e);
            }).ToArray();

            var maxError = errors.Max();

            Console.WriteLine(
                $"{c.Name}: RAW={string.Join('/', raw.Select(x => x.ToString("0.000000")))} " +
                $"HT={string.Join('/', expected.Select(x => x.ToString("0.000000")))} " +
                $"maxErr={maxError:0.000000}");

            if (maxError > Tolerance)
                failures++;
        }

        if (failures > 0)
            throw new InvalidOperationException(
                $"Empirical winger screenshot regression failed: {failures}/{cases.Length} cases exceed {Tolerance:0.##} tolerance.");

        Console.WriteLine("EmpiricalWingerSingletonRegression PASS");
        return 0;
    }

    private static TestCase Case(
        string name, int id, int defending, int playmaking, int passing, int winger,
        int scoring, int form, int experience,
        double leftDefence, double centralDefence, double rightDefence, double midfield,
        double leftAttack, double centralAttack, double rightAttack)
        => new(name, id, defending, playmaking, passing, winger, scoring, form, experience,
            leftDefence, centralDefence, rightDefence, midfield, leftAttack, centralAttack, rightAttack);

    private sealed record TestCase(
        string Name, int Id, int Defending, int Playmaking, int Passing, int Winger,
        int Scoring, int Form, int Experience,
        double LeftDefence, double CentralDefence, double RightDefence, double Midfield,
        double LeftAttack, double CentralAttack, double RightAttack);
}