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
                => $"RatingPositionMatrix.AddCentralDefender → {slot} / Normal / calibrated normal defender",
            "DEF-C" when order == PlayerOrder.Normal
                => "RatingPositionMatrix.AddCentralDefender → DEF-C / Normal / calibrated central defender",
            "DEF-CL" or "DEF-C" or "DEF-CR"
                => $"RatingPositionMatrix.AddCentralDefender → {slot} / {orderName} matrix",
            "W-L" or "W-R" when order == PlayerOrder.Normal
                => $"RatingPositionMatrix.AddWinger → {slot} / Normal / empirical normal winger calibration",
            "W-L" or "W-R"
                => $"RatingPositionMatrix.AddWinger → {slot} / {orderName} matrix",
            "IM-L" when order == PlayerOrder.Normal
                => "RatingPositionMatrix.AddInnerMidfielder → IM-L / Normal / calibrated IM-L",
            "IM-C" or "IM-R" when order == PlayerOrder.Normal
                => $"RatingPositionMatrix.AddInnerMidfielder → {slot} / Normal / calibrated central/right IM",
            "IM-L" or "IM-C" or "IM-R"
                => $"RatingPositionMatrix.AddInnerMidfielder → {slot} / {orderName} matrix",
            "FW-C" when order == PlayerOrder.Normal
                => "RatingPositionMatrix.AddForward → FW-C / Normal / calibrated center-forward fit",
            "FW-L" or "FW-R" when order == PlayerOrder.Normal
                => $"RatingPositionMatrix.AddForward → {slot} / Normal / calibrated side-forward fit",
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
                => "CD = 0.19759418×Defending + 0.93015666; own-side DEF = 0.16906005×Defending + 0.90992167; MID = Playmaking×0.035.",
            "DEF-C" when order == PlayerOrder.Normal
                => "CD = 0.19759418×Defending + 0.73256248; LD/RD = 0.06497175×Defending + 0.93079096; MID = Playmaking×0.035.",
            "DEF-CL" or "DEF-C" or "DEF-CR"
                => DefenderMatrixFormula(order, slot),
            "W-L" or "W-R" when order == PlayerOrder.Normal
                => "Normal winger empirical fit: CD = 0.47533365 + 0.13610538D + 0.38235880F − 0.10500311DF; side DEF = 0.46030951 + 0.19618792D + 0.35894517F − 0.08738294DF; MID = 1.35839805 − 0.02179095PM + 1.21013647F − 0.25824624PMF; side ATT = 0.49722642 + 0.06380086P − 0.09069453W + 0.50250568F − 0.04769162PF + 0.24645510WF.",
            "W-L" or "W-R"
                => WingerMatrixFormula(order, side),
            "IM-L" when order == PlayerOrder.Normal
                => "IM-L normal fit: LD = 0.86876582 + 0.05128852D; CD = 0.53590643 + 0.05304477D + 0.56421781F; MID = −2.60881793 + 0.16428439PM + 3.81099431F; LA = 0.58959034 + 0.03221075P + 0.14963856F; CA = 0.23940566 + 0.05047427P + 0.04472221S + 0.69342408F.",
            "IM-C" or "IM-R" when order == PlayerOrder.Normal
                => "Central/right IM normal fit: CD = 0.43837692 + 0.06418087D + 0.58468111F; side DEF = 0.92171186 + 0.01960455D + 0.01232717F; MID = max(0, 0.55041714 + 0.10480606PM + 0.28726477F) / 0.8285714286; side ATT = 0.78571429; CA = 0.30879015 + 0.07563828P + 0.02836967S + 0.64904194F.",
            "IM-L" or "IM-C" or "IM-R"
                => InnerMidMatrixFormula(order),
            "FW-C" when order == PlayerOrder.Normal
                => "FW-C normal fit: side base = 0.93619861 + 0.01P×form + 0.05739950S×form + 0.04386850W×form − 0.02183898D + 0.28212060XP; CA = 1.07631807 + 0.01P×form + 0.19974124S×form − 0.05671134D + 0.52513407XP; left side ÷1.2727272727, right side ÷1.2258064516.",
            "FW-L" or "FW-R" when order == PlayerOrder.Normal
                => "FW-L/R normal fit: side = 0.99522254/form + 0.05215221P + 0.03750657S + 0.03364124W; CA = 0.99496221/form + 0.05239461P + 0.18757562S; left side ÷1.2727272727, right side ÷1.2258064516.",
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
            PlayerOrder.Defensive => (0.050,0.148,0.054,0.185,0.044,0.009),
            PlayerOrder.TowardsMiddle => (0.047,0.093,0.082,0.160,0.043,0.026),
            PlayerOrder.Offensive => (0.016,0.055,0.054,0.247,0.062,0.024),
            _ => (0.037,0.104,0.065,0.219,0.054,0.018)
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
            PlayerOrder.TowardsWing => $"{side} FW/TowardsWing: MID=PM×0.024; own ATT=S×0.093+P×0.101+W×0.044; opposite ATT=S×0.018+P×0.034; CA=P×0.102+S×0.044; each term × form multiplier.",
            PlayerOrder.Defensive => $"{side} FW/Defensive: MID=PM×0.058; own ATT=S×0.030+P×0.033+W×0.059; opposite ATT=S×0.030+P×0.033; CA=S×0.102+P×0.108; each term × form multiplier.",
            _ => $"{side} FW/Normal production matrix."
        };
    }
}
