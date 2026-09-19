using System;
using System.Collections.Generic;

namespace HattrickAI.V5.Core;

/// <summary>
/// The 14 canonical Hattrick field slots. Legacy DEF-L/DEF-R are accepted as
/// aliases for the left/right central-defender slots; WB-L/WB-R are explicit.
/// </summary>
public static class RatingPositionMatrix
{
    public static readonly string[] CanonicalSlots =
    [
        "GK",
        "WB-L", "DEF-CL", "DEF-C", "DEF-CR", "WB-R",
        "W-L", "IM-L", "IM-C", "IM-R", "W-R",
        "FW-L", "FW-C", "FW-R"
    ];

    public static string CanonicalSlot(RegionalPlayer p)
    {
        if (!string.IsNullOrWhiteSpace(p.SlotCode))
            return p.SlotCode switch
            {
                "DEF-L" => "DEF-CL",
                "DEF-R" => "DEF-CR",
                _ => p.SlotCode
            };

        return p.Position switch
        {
            RegionalPosition.Goalkeeper => "GK",
            RegionalPosition.WingBack => p.Side == PlayerSide.Left ? "WB-L" : "WB-R",
            RegionalPosition.CentralDefender => p.Side switch
            {
                PlayerSide.Left => "DEF-CL",
                PlayerSide.Right => "DEF-CR",
                _ => "DEF-C"
            },
            RegionalPosition.Winger => p.Side == PlayerSide.Left ? "W-L" : "W-R",
            RegionalPosition.InnerMidfielder => p.Side switch
            {
                PlayerSide.Left => "IM-L",
                PlayerSide.Right => "IM-R",
                _ => "IM-C"
            },
            RegionalPosition.Forward => p.Side switch
            {
                PlayerSide.Left => "FW-L",
                PlayerSide.Right => "FW-R",
                _ => "FW-C"
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static bool IsCentralRole(string slot) =>
        slot is "DEF-CL" or "DEF-C" or "DEF-CR" or "IM-L" or "IM-C" or "IM-R" or "FW-L" or "FW-C" or "FW-R";

    public static bool IsDefender(string slot) =>
        slot is "WB-L" or "DEF-CL" or "DEF-C" or "DEF-CR" or "WB-R";

    public static bool IsMidfielder(string slot) =>
        slot is "W-L" or "IM-L" or "IM-C" or "IM-R" or "W-R";

    public static bool IsForward(string slot) =>
        slot is "FW-L" or "FW-C" or "FW-R";

    public static void AddContribution(
        Dictionary<RatingSector, double> sectors,
        RegionalPlayer p,
        double keeper,
        double defending,
        double playmaking,
        double passing,
        double winger,
        double scoring,
        double formMultiplier)
    {
        var slot = CanonicalSlot(p);

        switch (slot)
        {
            case "GK":
                Add(sectors, RatingSector.CentralDefence, keeper * .165 + defending * .079, formMultiplier);
                AddBothSides(sectors, RatingSector.LeftDefence, RatingSector.RightDefence,
                    keeper * .183 + defending * .082, formMultiplier);
                return;

            case "WB-L":
            case "WB-R":
                AddWingBack(sectors, p.Order, p.Side, defending, playmaking, winger, formMultiplier);
                return;

            case "DEF-CL":
            case "DEF-C":
            case "DEF-CR":
                AddCentralDefender(sectors, slot, p.Order, defending, playmaking, passing, formMultiplier);
                return;

            case "W-L":
            case "W-R":
                AddWinger(sectors, p.Order, p.Side, defending, playmaking, passing, winger, formMultiplier);
                return;

            case "IM-L":
            case "IM-C":
            case "IM-R":
                AddInnerMidfielder(sectors, p.Order, p.Side, defending, playmaking, passing, winger, scoring, formMultiplier);
                return;

            case "FW-L":
            case "FW-C":
            case "FW-R":
                AddForward(sectors, p.Order, p.Side, playmaking, passing, winger, scoring, formMultiplier);
                return;

            default:
                throw new InvalidOperationException($"Unsupported V5 rating slot: {slot}");
        }
    }

    private static void AddCentralDefender(
        Dictionary<RatingSector, double> s, string slot, PlayerOrder order,
        double defending, double playmaking, double passing, double form)
    {
        // DEF-CL normal-position calibration from 10 controlled live Hattrick
        // 1-0-0 observations. The observed sector values are the ground truth
        // for this canonical slot; other defender orders retain the researched
        // contribution matrix until their own calibration set is available.
        if ((slot == "DEF-CL" || slot == "DEF-CR") && order == PlayerOrder.Normal)
        {
            Add(s, RatingSector.CentralDefence,
                .19759418 * defending + .73256248, 1.0);
            Add(s, slot == "DEF-CL" ? RatingSector.LeftDefence : RatingSector.RightDefence,
                .16906005 * defending + .74086162, 1.0);
        }
        else
        {
            var central = order switch
            {
                PlayerOrder.Offensive => defending * .130,
                PlayerOrder.TowardsWing => defending * .133,
                _ => defending * .186
            };

            var side = order switch
            {
                PlayerOrder.TowardsWing => defending * .217,
                PlayerOrder.Offensive => defending * .058,
                _ => defending * .077
            };

            Add(s, RatingSector.CentralDefence, central, form);
            if (slot == "DEF-C")
                AddBothSides(s, RatingSector.LeftDefence, RatingSector.RightDefence, side, form);
            else
                Add(s, slot == "DEF-CL" ? RatingSector.LeftDefence : RatingSector.RightDefence, side, form);
        }

        var midfield = order switch
        {
            PlayerOrder.Offensive => playmaking * .047,
            PlayerOrder.TowardsWing => playmaking * .023,
            _ => playmaking * .035
        };

        Add(s, RatingSector.Midfield, midfield, form);

        if (order == PlayerOrder.TowardsWing)
        {
            var attack = slot == "DEF-CL"
                ? RatingSector.LeftAttack
                : slot == "DEF-CR"
                    ? RatingSector.RightAttack
                    : RatingSector.LeftAttack;
            if (slot != "DEF-C")
                Add(s, attack, passing * .063, form);
        }
    }

    private static void AddWingBack(
        Dictionary<RatingSector, double> s, PlayerOrder order, PlayerSide side,
        double defending, double playmaking, double winger, double form)
    {
        var centralDef = order switch
        {
            PlayerOrder.Defensive => .089,
            PlayerOrder.TowardsMiddle => .126,
            PlayerOrder.Offensive => .071,
            _ => .083
        };

        var sideDef = order switch
        {
            PlayerOrder.Defensive => .284,
            PlayerOrder.TowardsMiddle => .209,
            PlayerOrder.Offensive => .175,
            _ => .268
        };

        var midfield = order switch
        {
            PlayerOrder.Defensive => .009,
            PlayerOrder.Offensive => .032,
            _ => .023
        };

        var sideAttack = order switch
        {
            PlayerOrder.Defensive => .082,
            PlayerOrder.TowardsMiddle => .072,
            PlayerOrder.Offensive => .163,
            _ => .129
        };

        var defence = side == PlayerSide.Left ? RatingSector.LeftDefence : RatingSector.RightDefence;
        var attack = side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack;

        Add(s, RatingSector.CentralDefence, defending * centralDef, form);
        Add(s, defence, defending * sideDef, form);
        Add(s, RatingSector.Midfield, playmaking * midfield, form);
        Add(s, attack, winger * sideAttack, form);
    }

    private static void AddInnerMidfielder(
        Dictionary<RatingSector, double> s, PlayerOrder order, PlayerSide side,
        double defending, double playmaking, double passing, double winger,
        double scoring, double form)
    {
        var v = order switch
        {
            PlayerOrder.Defensive => new ImMatrix(.115, .040, .131, .018, .039, .028, 0),
            PlayerOrder.Offensive => new ImMatrix(.115, .040, .131, .018, .039, .025, 0),
            PlayerOrder.TowardsWing => new ImMatrix(.059, .068, .113, .064, .038, 0, .117),
            _ => new ImMatrix(.070, .028, .139, .028, .057, .038, 0)
        };

        Add(s, RatingSector.CentralDefence, defending * v.CentralDefence, form);

        var sideDef = side == PlayerSide.Left ? RatingSector.LeftDefence : RatingSector.RightDefence;
        if (side == PlayerSide.Center)
            AddBothSides(s, RatingSector.LeftDefence, RatingSector.RightDefence, defending * v.SideDefence, form);
        else
            Add(s, sideDef, defending * v.SideDefence, form);

        Add(s, RatingSector.Midfield, playmaking * v.Midfield, form);

        var sidePass = passing * v.SidePassing;
        if (side == PlayerSide.Center)
            AddBothSides(s, RatingSector.LeftAttack, RatingSector.RightAttack, sidePass, form);
        else
            Add(s, side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack, sidePass, form);

        Add(s, RatingSector.CentralAttack, passing * v.CenterPassing + scoring * v.CenterScoring, form);

        if (v.SideWinger > 0)
        {
            if (side == PlayerSide.Center)
                AddBothSides(s, RatingSector.LeftAttack, RatingSector.RightAttack, winger * v.SideWinger, form);
            else
                Add(s, side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack, winger * v.SideWinger, form);
        }
    }

    private static void AddWinger(
        Dictionary<RatingSector, double> s, PlayerOrder order, PlayerSide side,
        double defending, double playmaking, double passing, double winger, double form)
    {
        var v = order switch
        {
            PlayerOrder.Defensive => new WingerMatrix(.050, .148, .054, .185, .044, .009),
            PlayerOrder.TowardsMiddle => new WingerMatrix(.047, .093, .082, .160, .043, .026),
            PlayerOrder.Offensive => new WingerMatrix(.016, .055, .054, .247, .062, .024),
            _ => new WingerMatrix(.037, .104, .065, .219, .054, .018)
        };

        Add(s, RatingSector.CentralDefence, defending * v.CentralDefence, form);
        Add(s, side == PlayerSide.Left ? RatingSector.LeftDefence : RatingSector.RightDefence, defending * v.SideDefence, form);
        Add(s, RatingSector.Midfield, playmaking * v.Midfield, form);
        Add(s, side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack,
            passing * v.SidePassing + winger * v.SideWinger, form);
        Add(s, RatingSector.CentralAttack, passing * v.CenterPassing, form);
    }

    private static void AddForward(
        Dictionary<RatingSector, double> s, PlayerOrder order, PlayerSide side,
        double playmaking, double passing, double winger, double scoring, double form)
    {
        var own = side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack;
        var other = side == PlayerSide.Left ? RatingSector.RightAttack : RatingSector.LeftAttack;

        switch (order)
        {
            case PlayerOrder.TowardsWing:
                Add(s, RatingSector.Midfield, playmaking * .024, form);
                var ownToward = scoring * .093 + passing * .101 + winger * .044;
                var otherToward = scoring * .018 + passing * .034;
                if (side == PlayerSide.Center)
                {
                    AddBothSides(s, RatingSector.LeftAttack, RatingSector.RightAttack, ownToward, form);
                }
                else
                {
                    Add(s, own, ownToward, form);
                    Add(s, other, otherToward, form);
                }
                Add(s, RatingSector.CentralAttack, passing * .102 + scoring * .044, form);
                break;

            case PlayerOrder.Defensive:
                Add(s, RatingSector.Midfield, playmaking * .058, form);
                var ownDef = scoring * .030 + passing * .033 + winger * .059;
                var otherDef = scoring * .030 + passing * .033;
                if (side == PlayerSide.Center)
                {
                    AddBothSides(s, RatingSector.LeftAttack, RatingSector.RightAttack, ownDef, form);
                }
                else
                {
                    Add(s, own, ownDef, form);
                    Add(s, other, otherDef, form);
                }
                Add(s, RatingSector.CentralAttack, scoring * .102 + passing * .108, form);
                break;

            default:
                Add(s, RatingSector.Midfield, playmaking * .041, form);
                var ownNormal = scoring * .058 + passing * .048 + winger * .032;
                var otherNormal = scoring * .058 + passing * .048;
                if (side == PlayerSide.Center)
                {
                    AddBothSides(s, RatingSector.LeftAttack, RatingSector.RightAttack, ownNormal, form);
                }
                else
                {
                    Add(s, own, ownNormal, form);
                    Add(s, other, otherNormal, form);
                }
                Add(s, RatingSector.CentralAttack, scoring * .178 + passing * .066, form);
                break;
        }
    }

    private static void Add(Dictionary<RatingSector, double> s, RatingSector sector, double value, double form)
        => s[sector] += value * form;

    private static void AddBothSides(Dictionary<RatingSector, double> s, RatingSector left, RatingSector right, double value, double form)
    {
        s[left] += value * form;
        s[right] += value * form;
    }

    private readonly record struct ImMatrix(double CentralDefence, double SideDefence, double Midfield,
        double SidePassing, double CenterPassing, double CenterScoring, double SideWinger);

    private readonly record struct WingerMatrix(double CentralDefence, double SideDefence, double Midfield,
        double SidePassing, double SideWinger, double CenterPassing);
}
