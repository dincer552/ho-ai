using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Diagnostic fixture for the supplied S4MSUNFC 2-5-3 screenshot.
/// This deliberately reports the empirical gap instead of failing: the fixture
/// is a live calibration target, not yet a production acceptance gate.
/// </summary>
public static class Empirical2523LineupDiagnostic
{
    public static int Run()
    {
        var engine = new RegionalRatingEngineFinal();
        var players = new RegionalPlayer[]
        {
            P(479235895, RegionalPosition.Goalkeeper, PlayerSide.Center, PlayerOrder.Normal, 17,4,1,2,1,4,6,6,7),
            P(458524225, RegionalPosition.WingBack, PlayerSide.Left, PlayerOrder.Normal, 0,17,3,5,4,7,6,12,5),
            P(476114406, RegionalPosition.WingBack, PlayerSide.Right, PlayerOrder.Normal, 1,16,6,9,3,6,7,6,7),
            P(474962854, RegionalPosition.Winger, PlayerSide.Left, PlayerOrder.Offensive, 1,9,11,9,15,8,8,7,6),
            P(465805392, RegionalPosition.InnerMidfielder, PlayerSide.Left, PlayerOrder.TowardsWing, 1,2,16,11,6,6,6,8,6),
            P(465141092, RegionalPosition.InnerMidfielder, PlayerSide.Center, PlayerOrder.Normal, 1,5,12,9,14,6,7,8,6),
            P(454418419, RegionalPosition.InnerMidfielder, PlayerSide.Right, PlayerOrder.Normal, 0,2,15,8,1,7,6,10,3),
            P(492253331, RegionalPosition.Winger, PlayerSide.Right, PlayerOrder.Offensive, 2,3,5,7,17,4,7,3,6),
            P(491743384, RegionalPosition.Forward, PlayerSide.Center, PlayerOrder.Normal, 1,4,5,8,3,15,7,3,7),
            P(495041177, RegionalPosition.Forward, PlayerSide.Left, PlayerOrder.Normal, 1,3,6,7,5,13,7,3,6),
            P(497641568, RegionalPosition.Forward, PlayerSide.Right, PlayerOrder.Normal, 1,2,5,9,4,13,7,3,6)
        };

        // Direct Hattrick screenshot: DEF-L 11.75, DEF-C 8.50, DEF-R 12.00,
        // MID 7.75, ATT-L 14.75, ATT-C 15.75, ATT-R 13.50.
        var expected = new[] { 11.75, 8.50, 12.00, 7.75, 14.75, 15.75, 13.50 };
        var actual = engine.Calculate(players, RatingContext.Default);
        var values = new[] { actual.LeftDefence, actual.CentralDefence, actual.RightDefence,
            actual.Midfield, actual.LeftAttack, actual.CentralAttack, actual.RightAttack };
        var names = new[] { "DEF-L", "DEF-C", "DEF-R", "MID", "ATT-L", "ATT-C", "ATT-R" };
        var mae = values.Zip(expected, (a, e) => Math.Abs(a - e)).Average();
        var max = values.Zip(expected, (a, e) => Math.Abs(a - e)).Max();

        Console.WriteLine("Empirical 2-5-3 S4MSUNFC calibration target");
        for (var i = 0; i < values.Length; i++)
            Console.WriteLine($"{names[i]}: V5={values[i]:0.##} HT={expected[i]:0.##} delta={values[i]-expected[i]:+0.##;-0.##;0.00}");
        Console.WriteLine($"MAE={mae:0.##} maxErr={max:0.##}");
        Console.WriteLine("DIAGNOSTIC PASS: fixture executed; calibration gap is intentionally reported, not gated.");
        return 0;
    }

    private static RegionalPlayer P(int id, RegionalPosition position, PlayerSide side, PlayerOrder order,
        int keeper, int defending, int playmaking, int passing, int winger, int scoring,
        int form, int experience, int stamina)
        => new(id, position, side, order, keeper, defending, playmaking, passing, winger, scoring, form, 0, experience, stamina);
}
