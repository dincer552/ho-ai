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
        double formMultiplier,
        double experienceBonus = 0.0,
        Action<RegionalPlayer, string, IReadOnlyDictionary<RatingSector, double>>? trace = null)
    {
        var slot = CanonicalSlot(p);
        var beforeTrace = new Dictionary<RatingSector, double>(sectors);

        void FinishTrace()
        {
            if (trace is null) return;
            var delta = Enum.GetValues<RatingSector>()
                .ToDictionary(x => x, x => sectors[x] - beforeTrace[x]);
            trace(p, RatingCalculationFormulaCatalog.RouteFor(slot, p.Order, p.Side), delta);
        }

        switch (slot)
        {
            case "GK":
                Add(sectors, RatingSector.CentralDefence, keeper * .165 + defending * .079, formMultiplier);
                AddBothSides(sectors, RatingSector.LeftDefence, RatingSector.RightDefence,
                    keeper * .183 + defending * .082, formMultiplier);
                FinishTrace();
                return;
case "WB-L":
            case "WB-R":
                AddWingBack(sectors, p, p.Order, p.Side, defending, playmaking, winger, formMultiplier, experienceBonus);
                FinishTrace();
                return;
case "DEF-CL":
            case "DEF-C":
            case "DEF-CR":
                AddCentralDefender(sectors, slot, p.Order, defending, playmaking, passing, formMultiplier);
                FinishTrace();
                return;
case "W-L":
            case "W-R":
                AddWinger(sectors, p.Order, p.Side, defending, playmaking, passing, winger, p.Form, formMultiplier);
                FinishTrace();
                return;
case "IM-L":
            case "IM-C":
            case "IM-R":
                AddInnerMidfielder(sectors, p.Order, p.Side, defending, playmaking, passing, winger, scoring, p.Form, formMultiplier);
                FinishTrace();
                return;
case "FW-L":
            case "FW-C":
            case "FW-R":
                AddForward(sectors, p.Order, p.Side, defending, playmaking, passing, winger, scoring, formMultiplier, experienceBonus);
                FinishTrace();
                return;
default:
                throw new InvalidOperationException($"Unsupported V5 rating slot: {slot}");
        }
    }

    private static void AddCentralDefender(
        Dictionary<RatingSector, double> s, string slot, PlayerOrder order,
        double defending, double playmaking, double passing, double form)
    {
        var central = order switch
        {
            PlayerOrder.Offensive => .130,
            PlayerOrder.TowardsWing => .133,
            _ => .186
        };

        var side = order switch
        {
            PlayerOrder.TowardsWing => .217,
            PlayerOrder.Offensive => .058,
            _ => .077
        };

        var midfield = order switch
        {
            PlayerOrder.Offensive => .047,
            PlayerOrder.TowardsWing => .023,
            _ => .035
        };

        Add(s, RatingSector.CentralDefence, defending * central, form);
        if (slot == "DEF-C")
            AddBothSides(s, RatingSector.LeftDefence, RatingSector.RightDefence, defending * side, form);
        else
            Add(s, slot == "DEF-CL" ? RatingSector.LeftDefence : RatingSector.RightDefence, defending * side, form);

        Add(s, RatingSector.Midfield, playmaking * midfield, form);

        if (order == PlayerOrder.TowardsWing && slot != "DEF-C")
        {
            var attack = slot == "DEF-CL"
                ? RatingSector.LeftAttack
                : RatingSector.RightAttack;
            Add(s, attack, passing * .063, form);
        }
    }

    private static void AddWingBack(
        Dictionary<RatingSector, double> s, RegionalPlayer p, PlayerOrder order, PlayerSide side,
        double defending, double playmaking, double winger, double formMultiplier,
        double experienceBonus)
    {
        var defence = side == PlayerSide.Left ? RatingSector.LeftDefence : RatingSector.RightDefence;
        var attack = side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack;

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

        var midfieldCoefficient = order switch
        {
            PlayerOrder.Defensive => .009,
            PlayerOrder.Offensive => .032,
            _ => .023
        };

        var sideAttackCoefficient = order switch
        {
            PlayerOrder.Defensive => .082,
            PlayerOrder.TowardsMiddle => .072,
            PlayerOrder.Offensive => .163,
            _ => .129
        };

        Add(s, RatingSector.CentralDefence, defending * centralDef, formMultiplier);
        Add(s, defence, defending * sideDef, formMultiplier);
        Add(s, RatingSector.Midfield, playmaking * midfieldCoefficient, formMultiplier);
        Add(s, attack, winger * sideAttackCoefficient, formMultiplier);
    }

    private static void AddInnerMidfielder(
        Dictionary<RatingSector, double> s, PlayerOrder order, PlayerSide side,
        double defending, double playmaking, double passing, double winger,
        double scoring, double playerForm, double form)
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
        var sideWinger = winger * v.SideWinger;
        if (side == PlayerSide.Center)
        {
            AddBothSides(s, RatingSector.LeftAttack, RatingSector.RightAttack, sidePass + sideWinger, form);
        }
        else
        {
            Add(s, side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack,
                sidePass + sideWinger, form);
        }

        Add(s, RatingSector.CentralAttack, passing * v.CenterPassing + scoring * v.CenterScoring, form);
    }

    private static void AddWinger(
        Dictionary<RatingSector, double> s, PlayerOrder order, PlayerSide side,
        double defending, double playmaking, double passing, double winger,
        double playerForm, double form)
    {
        var v = order switch
        {
            PlayerOrder.Defensive => new WingerMatrix(.050, .148, .054, .185, .044, .009),
            PlayerOrder.TowardsMiddle => new WingerMatrix(.047, .093, .082, .160, .043, .026),
            PlayerOrder.Offensive => new WingerMatrix(.016, .055, .054, .247, .062, .024),
            _ => new WingerMatrix(.037, .104, .065, .219, .054, .018)
        };

        Add(s, RatingSector.CentralDefence, defending * v.CentralDefence, form);
        Add(s, side == PlayerSide.Left ? RatingSector.LeftDefence : RatingSector.RightDefence,
            defending * v.SideDefence, form);
        Add(s, RatingSector.Midfield, playmaking * v.Midfield, form);
        Add(s, side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack,
            passing * v.SidePassing + winger * v.SideWinger, form);
        Add(s, RatingSector.CentralAttack, passing * v.CenterPassing, form);
    }

    private static void AddForward(
        Dictionary<RatingSector, double> s, PlayerOrder order, PlayerSide side,
        double defending, double playmaking, double passing, double winger, double scoring,
        double form, double experienceBonus)
    {
        var own = side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack;
        var other = side == PlayerSide.Left ? RatingSector.RightAttack : RatingSector.LeftAttack;

        switch (order)
        {
            case PlayerOrder.TowardsWing:
                Add(s, RatingSector.Midfield, playmaking * .024, form);

                // Towards-wing forward: the stronger side gets the marked
                // side-attack contribution; the opposite side receives only
                // the smaller winger contribution from the wiki table.
                var ownToward = scoring * .093 + passing * .101 + winger * .044;
                var otherToward = winger * .017;
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
                var normalSide = passing * .032 + scoring * .048 + winger * .178;
                var normalCentral = passing * .178 + scoring * .066;

                if (side == PlayerSide.Center)
                {
                    AddBothSides(s, RatingSector.LeftAttack, RatingSector.RightAttack, normalSide, form);
                }
                else
                {
                    Add(s, own, normalSide, form);
                    Add(s, other, normalSide, form);
                }

                Add(s, RatingSector.CentralAttack, normalCentral, form);
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
