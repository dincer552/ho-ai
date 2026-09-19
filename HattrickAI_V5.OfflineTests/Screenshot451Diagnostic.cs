using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Screenshot451Diagnostic
{
    public static int Run()
    {
        var players = new[]
        {
            P(497641568, RegionalPosition.Goalkeeper, PlayerSide.Center, "GK", 1, 2, 5, 9, 4, 13, 6, 8, 3, 6),
            P(468363070, RegionalPosition.WingBack, PlayerSide.Left, "WB-L", 1, 17, 3, 8, 4, 7, 6, 6, 10, 6),
            P(465141092, RegionalPosition.CentralDefender, PlayerSide.Center, "DEF-CL", 1, 5, 12, 9, 14, 6, 8, 8, 8, 6),
            P(474962854, RegionalPosition.CentralDefender, PlayerSide.Center, "DEF-CR", 1, 9, 11, 9, 15, 8, 8, 7, 7, 6),
            P(458524225, RegionalPosition.WingBack, PlayerSide.Right, "WB-R", 0, 17, 3, 5, 4, 7, 7, 7, 12, 5),
            P(476114406, RegionalPosition.Winger, PlayerSide.Left, "W-L", 1, 16, 6, 9, 3, 6, 7, 7, 6, 7),
            P(495041177, RegionalPosition.InnerMidfielder, PlayerSide.Left, "IM-L", 1, 3, 6, 8, 5, 13, 6, 3, 3, 7),
            P(465805392, RegionalPosition.InnerMidfielder, PlayerSide.Center, "IM-C", 1, 2, 16, 12, 6, 6, 7, 8, 8, 6),
            P(491743384, RegionalPosition.InnerMidfielder, PlayerSide.Right, "IM-R", 1, 4, 5, 9, 3, 15, 7, 3, 3, 7),
            P(492253331, RegionalPosition.Winger, PlayerSide.Right, "W-R", 2, 3, 5, 8, 17, 4, 7, 3, 3, 7),
            P(479235895, RegionalPosition.Forward, PlayerSide.Center, "FW-C", 17, 4, 1, 2, 1, 4, 5, 6, 6, 7),
        };

        var engine = new RegionalRatingEngineFixed();
        var trace = engine.CalculateWithTrace(players, new RatingContext(MatchLocation.Away, TeamAttitude.Normal, TeamTactic.Normal));

        Console.WriteLine("V5 4-5-1 screenshot lineup diagnostic");
        Console.WriteLine("GK E. Akşın | WB-L A. Takyi | DEF-CL M. Bozev | DEF-CR M. Gobiet | WB-R D. Nocoń");
        Console.WriteLine("W-L C. Pesalovo | IM-L A. Nahasepp | IM-C B. Doktor | IM-R A. Beţa | W-R N. Dobóvári | FW-C E. Bultot");
        Console.WriteLine($"LD={trace.FinalRating.LeftDefence:F2} CD={trace.FinalRating.CentralDefence:F2} RD={trace.FinalRating.RightDefence:F2} MID={trace.FinalRating.Midfield:F2} LA={trace.FinalRating.LeftAttack:F2} CA={trace.FinalRating.CentralAttack:F2} RA={trace.FinalRating.RightAttack:F2}");

        foreach (var p in trace.Players)
            Console.WriteLine($"{p.PlayerName,-20} {p.Slot,-6} crowd={p.CrowdingMultiplier:F3} MID={p.CrowdingAdjustedContributions.GetValueOrDefault("Midfield"):F3}");

        return 0;
    }

    private static RegionalPlayer P(
        int id, RegionalPosition position, PlayerSide side, string slot,
        double keeper, double defending, double playmaking, double passing, double winger,
        double scoring, double form, double experience, double loyalty, double stamina)
        => new(id, position, side, PlayerOrder.Normal, keeper, defending, playmaking, passing, winger, scoring, form, loyalty, experience, stamina, slot);
}
