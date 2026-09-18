using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// First controlled defensive calibration gate.
/// Ground truth comes from EmpiricalMotorObservationDb.json screenshots:
/// Pesalovo, Nocoń and Takyi in normal single-defender L/C/R positions.
/// A quarter-step tolerance of 0.25 is intentional because the source is
/// screenshot display data, not raw engine state.
/// </summary>
public static class EmpiricalDefensiveSingletonRegression
{
    private const double Tolerance = 0.25;

    public static int Run()
    {
        var engine = new RegionalRatingEngineFinal();
        var cases = new[]
        {
            Case("Pesalovo-L", 476114406, "Pesalovo", 16, 6, 7, 6, PlayerSide.Left, 6, 2, 0, 1),
            Case("Pesalovo-C", 476114406, "Pesalovo", 16, 6, 7, 6, PlayerSide.Center, 2, 4, 2, 1),
            Case("Pesalovo-R", 476114406, "Pesalovo", 16, 6, 7, 6, PlayerSide.Right, 0, 2, 6, 1),
            Case("Nocon-L", 458524225, "Dawid Nocoń", 17, 3, 6, 12, PlayerSide.Left, 5.75, 1.75, 0, 1),
            Case("Nocon-C", 458524225, "Dawid Nocoń", 17, 3, 6, 12, PlayerSide.Center, 2, 3.75, 2, 1),
            Case("Nocon-R", 458524225, "Dawid Nocoń", 17, 3, 6, 12, PlayerSide.Right, 0, 1.75, 5.75, 1),
            Case("Takyi-L", 468363070, "Abeiku Takyi", 17, 3, 6, 10, PlayerSide.Left, 5.75, 1.75, 0, 1),
            Case("Takyi-C", 468363070, "Abeiku Takyi", 17, 3, 6, 10, PlayerSide.Center, 2, 3.75, 2, 1),
            Case("Takyi-R", 468363070, "Abeiku Takyi", 17, 3, 6, 10, PlayerSide.Right, 0, 1.75, 5.75, 1)
        };

        var failures = 0;
        foreach (var c in cases)
        {
            var player = new RegionalPlayer(c.Id, RegionalPosition.CentralDefender, c.Side, PlayerOrder.Normal,
                0, c.Defending, c.Playmaking, 0, 0, 0, c.Form, 0, c.Experience, 7);
            var actual = engine.Calculate(new[] { player }, RatingContext.Default);
            var values = new[] { actual.RawLeftDefence, actual.RawCentralDefence, actual.RawRightDefence, actual.RawMidfield, actual.RawLeftAttack, actual.RawCentralAttack, actual.RawRightAttack };
            var expected = new[] { c.LeftDefence, c.CentralDefence, c.RightDefence, c.Midfield, 0, 0, 0 };
            var maxError = values.Zip(expected, (a, e) => Math.Abs(a - e)).Max();
            Console.WriteLine($"{c.Name}: V5={string.Join('/', values.Select(x => x.ToString("0.000000")))} DB={string.Join('/', expected.Select(x => x.ToString("0.000000")))} maxErr={maxError:0.000000}");
            if (maxError > Tolerance) failures++;
        }

        if (failures > 0) throw new InvalidOperationException($"Empirical defensive singleton regression failed: {failures}/{cases.Length} cases exceed {Tolerance:0.##} tolerance.");
        Console.WriteLine("EmpiricalDefensiveSingletonRegression PASS");
        return 0;
    }

    private static TestCase Case(string name, int id, string playerName, int defending, int playmaking, int form, int experience,
        PlayerSide side, double leftDefence, double centralDefence, double rightDefence, double midfield)
        => new(name, id, playerName, defending, playmaking, form, experience, side, leftDefence, centralDefence, rightDefence, midfield);

    private sealed record TestCase(string Name, int Id, string PlayerName, int Defending, int Playmaking, int Form, int Experience,
        PlayerSide Side, double LeftDefence, double CentralDefence, double RightDefence, double Midfield);
}
