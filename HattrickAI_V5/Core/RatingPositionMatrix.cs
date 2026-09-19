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
                AddWinger(sectors, p.Order, p.Side, defending, playmaking, passing, winger, p.Form, formMultiplier);
                return;

            case "IM-L":
            case "IM-C":
            case "IM-R":
                AddInnerMidfielder(sectors, p.Order, p.Side, defending, playmaking, passing, winger, scoring, p.Form, formMultiplier);
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
                .19759418 * defending + .93015666, 1.0);
            Add(s, slot == "DEF-CL" ? RatingSector.LeftDefence : RatingSector.RightDefence,
                .16906005 * defending + .90992167, 1.0);
        }
        else if (slot == "DEF-C" && order == PlayerOrder.Normal)
        {
            // DEF-C calibration from controlled 1-0-0 live observations.
            // The low-defending/high-experience correction captures the
            // observed central-defense uplift in the central sector.
            var central = .19759418 * defending + .73256248;
            var side = .06497175 * defending + .93079096;
            Add(s, RatingSector.CentralDefence, central, 1.0);
            AddBothSides(s, RatingSector.LeftDefence, RatingSector.RightDefence, side, 1.0);
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
        double scoring, double playerForm, double form)
    {
        if (order == PlayerOrder.Normal)
        {
            if (side == PlayerSide.Left)
            {
                // IM-L calibration from six user-supplied 0-1-0 Hattrick screenshots
                // dated 2026-09-19. Only the displayed white rating values are
                // ground truth; green delta values are intentionally excluded.
                //
                // The coefficients are a low-complexity regression against the
                // supplied CHPP player skills. RegionalRatingEngineFinal applies
                // the Hattrick quarter-step display quantization afterwards.
                var leftDefenceImL =
                    .90744157
                    + .11531365 * defending;
                var centralDefenceImL =
                    .87023370
                    + .03874539 * defending;

                // The six screenshots all show the isolated IM-L midfield at
                // the 1.00 display floor. Keep a monotonic playmaking relation
                // so stronger players can still rise above the floor.
                var midfieldImL = 1.20689655 * (.80000000 + .05000000 * playmaking);

                // IM-L side attack regression: passing + visible form + experience.
                var leftAttackImL =
                    1.12871058
                    - .03132860 * passing
                    + .11491215 * playerForm;

                // The supplied IM-L screenshots all display CA=1.00. Keep the
                // central-attack relation conservative and monotonic in passing
                // and scoring instead of importing the IM-C calibration.
                var centralAttackImL =
                    .68000000
                    + .02000000 * passing
                    + .01000000 * scoring;

                Add(s, RatingSector.LeftDefence, leftDefenceImL, 1.0);
                Add(s, RatingSector.CentralDefence, centralDefenceImL, 1.0);
                Add(s, RatingSector.Midfield, midfieldImL, 1.0);
                Add(s, RatingSector.LeftAttack, leftAttackImL, 1.0);
                Add(s, RatingSector.CentralAttack, centralAttackImL, 1.0);
                return;
            }

            // Empirical normal-IM calibration from controlled 1-player
            // Hattrick observations. The formula is deliberately shared by
            // IM-L and IM-R; only the side-facing sectors are mirrored.
            var ff = .378 * Math.Sqrt(Math.Clamp(playerForm - 1.0, 0.0, 7.0));
            // Refit from the 9 controlled live Hattrick 0-1-0 IM-C
            // observations. Inputs are normalized by SkillRating().
            //
            // Defence regression: Defending + experience + form terms.
            // The left/right defensive contribution is intentionally shared
            // because a central IM has no field-side bias.
            var centralDefence =
                .54075966
                - 1.19179503 * defending
                - .98523049 * ff
                + 1.12415605 * defending * ff
                + 2.78479513 * ff * ff;

            var sideDefence =
                .94845587
                - .18220788 * defending
                - 1.34650600 * ff
                + .25017938 * defending * ff
                + 1.07246761 * ff * ff;

            // MID regression refit from the latest 9 full-sector
            // Hattrick 0-1-0 screenshots. The observed rating is the
            // quarter-step displayed team rating, so the regression is
            // calibrated to the underlying displayed-sector targets.
            var midfieldFinal =
                1.80011637
                - .64684156 * playmaking
                - .00641282 * playmaking * playmaking
                - 1.29157070 * ff
                + .87835277 * playmaking * ff;
            var midfield = Math.Max(0.0, midfieldFinal) / .8285714285714286;

            // Left/right attack regression from the same 9 screenshots.
            // The two side models are kept separate because the live
            // observations include a one-quarter-step L/R difference.
            var leftAttack =
                .53108315
                - .04183457 * passing
                + .39817775 * ff
                + .01200582 * passing * ff;

            var rightAttack =
                1.08061438
                - .15864599 * passing
                - .11976863 * ff
                + .14902802 * passing * ff;

            // CA-02: simplified normal-IM central-attack calibration.
            // The previous high-order polynomial was fitted to a corrupted
            // sector corpus and produced non-monotonic extrapolation (notably
            // Patrik/Münir). The refreshed 2026-09-19 screenshots give a stable
            // low-complexity relation using normalized Passing, Scoring and
            // visible Form. Experience is intentionally not folded into this
            // empirical CA term until an orthogonal experience corpus exists.
            //
            // The coefficients are fitted to the seven verified 0-1-0
            // normal-IM-C screenshots and the result is then passed through
            // the normal quarter-step display quantization.
            var centralAttack =
                .42767567
                + .09905346 * passing
                + .02230971 * scoring
                + .46394146 * ff;

            Add(s, RatingSector.CentralDefence, centralDefence, 1.0);
            if (side == PlayerSide.Left)
                Add(s, RatingSector.LeftDefence, sideDefence, 1.0);
            else if (side == PlayerSide.Right)
                Add(s, RatingSector.RightDefence, sideDefence, 1.0);
            else
            {
                Add(s, RatingSector.LeftDefence, sideDefence, 1.0);
                Add(s, RatingSector.RightDefence, sideDefence, 1.0);
            }

            Add(s, RatingSector.Midfield, midfield, 1.0);
            if (side == PlayerSide.Left)
                Add(s, RatingSector.LeftAttack, leftAttack, 1.0);
            else if (side == PlayerSide.Right)
                Add(s, RatingSector.RightAttack, rightAttack, 1.0);
            else
            {
                Add(s, RatingSector.LeftAttack, leftAttack, 1.0);
                Add(s, RatingSector.RightAttack, rightAttack, 1.0);
            }
            Add(s, RatingSector.CentralAttack, centralAttack, 1.0);
            return;
        }

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
        double defending, double playmaking, double passing, double winger,
        double playerForm, double form)
    {
        if (order == PlayerOrder.Normal)
        {
            // Empirical normal-wing calibration from controlled 0-1-0 Hattrick
            // observations. The same matrix is mirrored for W-L and W-R.
            var ff = .378 * Math.Sqrt(Math.Clamp(playerForm - 1.0, 0.0, 7.0));

            var centralDef = .03645194 * defending
                - .00219966 * ff + .82810804;
            var sideDef = .10634512 * defending
                + .34313663 * ff + .58486461;
            var midfield = .07360550 * playmaking
                + .13827052 * ff + .66355907;
            var sideAttack = .05458062 * passing + .17877963 * winger
                + 1.99577707 * ff - 1.37872667;

            Add(s, RatingSector.CentralDefence, centralDef, 1.0);
            Add(s, side == PlayerSide.Left ? RatingSector.LeftDefence : RatingSector.RightDefence, sideDef, 1.0);
            Add(s, RatingSector.Midfield, midfield, 1.0);
            Add(s, side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack, sideAttack, 1.0);

            // Central attack remains on the researched contribution matrix;
            // single-player team ratings are floored to 1.00 by the display.
            Add(s, RatingSector.CentralAttack, Math.Max(0.0, (passing - 1.0) * .018), form);
            return;
        }

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
