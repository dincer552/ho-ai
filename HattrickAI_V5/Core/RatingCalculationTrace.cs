using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

public sealed record PlayerRatingCalculationTrace(
    int PlayerId,
    string PlayerName,
    string Slot,
    string Side,
    string Order,
    string CalculationRoute,
    string FormulaReference,
    IReadOnlyDictionary<string, double> RawSkills,
    IReadOnlyDictionary<string, double> EffectiveSkills,
    double Form,
    double FormMultiplier,
    double StaminaMultiplier,
    double LoyaltyBonus,
    double ExperienceBonus,
    double CrowdingMultiplier,
    IReadOnlyDictionary<string, double> DirectContributions,
    IReadOnlyDictionary<string, double> SkillContributionsBeforeExperience,
    IReadOnlyDictionary<string, double> ExperienceContributions,
    IReadOnlyDictionary<string, double> CrowdingAdjustedContributions);

public sealed record RatingSectorCalculationTrace(
    string Sector,
    double MatrixSubtotal,
    double ContextMultiplier,
    double ReferenceCalibration,
    double RawFinal,
    double DisplayFinal,
    IReadOnlyDictionary<string, double> PlayerContributions);

public sealed record RatingCalculationTraceResult(
    string Engine,
    string TeamName,
    string Formation,
    string MatchLocation,
    string Attitude,
    string Tactic,
    int MatchMinute,
    int GoalDifference,
    double BaselineFormFactor,
    int CentralDefenderCount,
    int CentralMidfielderCount,
    int ForwardCount,
    double ConfidenceLevel,
    double ConfidenceAttackMultiplier,
    RegionalRatingSnapshot EngineRatingBeforeConfidence,
    RegionalRatingSnapshot FinalRating,
    IReadOnlyList<PlayerRatingCalculationTrace> Players,
    IReadOnlyList<RatingSectorCalculationTrace> Sectors);

public static class RatingCalculationFormulaCatalog
{
    public static string RouteFor(string slot, PlayerOrder order, PlayerSide side)
    {
        var orderName = order.ToString();
        return slot switch
        {
            "GK" => $"RatingPositionMatrix.AddContribution → GK / {orderName} matrix",
            "WB-L" or "WB-R" => $"RatingPositionMatrix.AddWingBack → {slot} / {orderName} matrix",
            "DEF-CL" or "DEF-CR" when order == PlayerOrder.Normal
                => $"RatingPositionMatrix.AddCentralDefender → {slot} / Normal / wiki contribution matrix",
            "DEF-C" when order == PlayerOrder.Normal
                => "RatingPositionMatrix.AddCentralDefender → DEF-C / Normal / wiki contribution matrix",
            "DEF-CL" or "DEF-C" or "DEF-CR"
                => $"RatingPositionMatrix.AddCentralDefender → {slot} / {orderName} matrix",
            "W-L" or "W-R" when order == PlayerOrder.Normal
                => $"RatingPositionMatrix.AddWinger → {slot} / Normal / wiki contribution matrix",
            "W-L" or "W-R"
                => $"RatingPositionMatrix.AddWinger → {slot} / {orderName} matrix",
            "IM-L" when order == PlayerOrder.Normal
                => "RatingPositionMatrix.AddInnerMidfielder → IM-L / Normal / wiki contribution matrix",
            "IM-C" or "IM-R" when order == PlayerOrder.Normal
                => $"RatingPositionMatrix.AddInnerMidfielder → {slot} / Normal / wiki contribution matrix",
            "IM-L" or "IM-C" or "IM-R"
                => $"RatingPositionMatrix.AddInnerMidfielder → {slot} / {orderName} matrix",
            "FW-C" when order == PlayerOrder.Normal
                => "RatingPositionMatrix.AddForward → FW-C / Normal / wiki contribution matrix",
            "FW-L" or "FW-R" when order == PlayerOrder.Normal
                => $"RatingPositionMatrix.AddForward → {slot} / Normal / wiki contribution matrix",
            "FW-L" or "FW-C" or "FW-R"
                => $"RatingPositionMatrix.AddForward → {slot} / {orderName} matrix",
            _ => $"RatingPositionMatrix.AddContribution → {slot} / {orderName}"
        };
    }

    public static string FormulaFor(string slot, PlayerOrder order, PlayerSide side)
    {
        return slot switch
        {
            "GK" => "CD = Keeper×0.165 + Defending×0.079; LD/RD = Keeper×0.183 + Defending×0.082; each term × form multiplier.",
            "WB-L" or "WB-R" => WingBackFormula(order, side),
            "DEF-CL" or "DEF-CR" when order == PlayerOrder.Normal
                => "CD=D×0.186; own-side DEF=D×0.077; MID=PM×0.035.",
            "DEF-C" when order == PlayerOrder.Normal
                => "CD=D×0.186; LD/RD=D×0.077; MID=PM×0.035.",
            "DEF-CL" or "DEF-C" or "DEF-CR"
                => DefenderMatrixFormula(order, slot),
            "W-L" or "W-R" when order == PlayerOrder.Normal
                => WingerMatrixFormula(order, side),
            "W-L" or "W-R"
                => WingerMatrixFormula(order, side),
            "IM-L" when order == PlayerOrder.Normal
                => InnerMidMatrixFormula(order),
            "IM-C" or "IM-R" when order == PlayerOrder.Normal
                => InnerMidMatrixFormula(order),
            "IM-L" or "IM-C" or "IM-R"
                => InnerMidMatrixFormula(order),
            "FW-C" when order == PlayerOrder.Normal
                => ForwardMatrixFormula(order, side),
            "FW-L" or "FW-R" when order == PlayerOrder.Normal
                => ForwardMatrixFormula(order, side),
            "FW-L" or "FW-C" or "FW-R"
                => ForwardMatrixFormula(order, side),
            _ => "Production contribution matrix branch; numeric sector contribution below is captured directly from RatingPositionMatrix."
        };
    }

    private static string WingBackFormula(PlayerOrder order, PlayerSide side)
    {
        var (cd, sd, mid, att) = order switch
        {
            PlayerOrder.Defensive => (0.089, 0.284, 0.009, 0.082),
            PlayerOrder.TowardsMiddle => (0.126, 0.209, 0.023, 0.072),
            PlayerOrder.Offensive => (0.071, 0.175, 0.032, 0.163),
            _ => (0.083, 0.268, 0.023, 0.129)
        };
        return $"{side} WB: CD = D×{cd:0.###}; own-side DEF = D×{sd:0.###}; MID = PM×{mid:0.###}; own-side ATT = W×{att:0.###}; each term × form multiplier.";
    }

    private static string DefenderMatrixFormula(PlayerOrder order, string slot)
    {
        var central = order switch { PlayerOrder.Offensive => .130, PlayerOrder.TowardsWing => .133, _ => .186 };
        var side = order switch { PlayerOrder.TowardsWing => .217, PlayerOrder.Offensive => .058, _ => .077 };
        var mid = order switch { PlayerOrder.Offensive => .047, PlayerOrder.TowardsWing => .023, _ => .035 };
        var attack = order == PlayerOrder.TowardsWing && slot != "DEF-C" ? " + P×0.063 to own attack" : "";
        return $"{slot}/{order}: CD=D×{central:0.###}; side DEF=D×{side:0.###}; MID=PM×{mid:0.###}{attack}; each term × form multiplier.";
    }

    private static string WingerMatrixFormula(PlayerOrder order, PlayerSide side)
    {
        var m = order switch
        {
            PlayerOrder.Defensive => (0.050,0.148,0.054,0.044,0.185,0.009),
            PlayerOrder.TowardsMiddle => (0.047,0.093,0.082,0.043,0.160,0.026),
            PlayerOrder.Offensive => (0.016,0.055,0.054,0.062,0.247,0.024),
            _ => (0.037,0.104,0.065,0.054,0.219,0.018)
        };
        return $"{side} W/{order}: CD=D×{m.Item1:0.###}; own DEF=D×{m.Item2:0.###}; MID=PM×{m.Item3:0.###}; own ATT=P×{m.Item4:0.###}+W×{m.Item5:0.###}; CA=P×{m.Item6:0.###}; each term × form multiplier.";
    }

    private static string InnerMidMatrixFormula(PlayerOrder order)
    {
        var v = order switch
        {
            PlayerOrder.Defensive => (.115,.040,.131,.018,.039,.028,0d),
            PlayerOrder.Offensive => (.115,.040,.131,.018,.039,.025,0d),
            PlayerOrder.TowardsWing => (.059,.068,.113,.064,.038,0d,.117),
            _ => (.070,.028,.139,.028,.057,.038,0d)
        };
        return $"IM/{order}: CD=D×{v.Item1:0.###}; side DEF=D×{v.Item2:0.###}; MID=PM×{v.Item3:0.###}; side ATT=P×{v.Item4:0.###}+W×{v.Item7:0.###}; CA=P×{v.Item5:0.###}+S×{v.Item6:0.###}; each term × form multiplier.";
    }

    private static string ForwardMatrixFormula(PlayerOrder order, PlayerSide side)
    {
        return order switch
        {
            PlayerOrder.TowardsWing => $"{side} FW/TowardsWing: MID=PM×0.024; own ATT=S×0.093+P×0.101+W×0.044; opposite ATT=W×0.017; CA=P×0.102+S×0.044; each term × form multiplier.",
            PlayerOrder.Defensive => $"{side} FW/Defensive: MID=PM×0.058; own ATT=S×0.030+P×0.033+W×0.059; opposite ATT=S×0.030+P×0.033; CA=S×0.102+P×0.108; each term × form multiplier.",
            _ => $"{side} FW/Normal production matrix."
        };
    }
}
