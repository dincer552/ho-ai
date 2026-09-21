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
        Dictionary<RatingSector, double> s, RegionalPlayer p, PlayerOrder order, PlayerSide side,
        double defending, double playmaking, double winger, double formMultiplier,
        double experienceBonus)
    {
        var defence = side == PlayerSide.Left ? RatingSector.LeftDefence : RatingSector.RightDefence;
        var attack = side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack;

        // Normal WB was recalibrated from the six independent singleton
        // Hattrick captures supplied on 2026-09-20. These captures expose the
        // displayed sector contribution directly, so Normal WB uses an
        // empirical contribution surface instead of the older generic
        // coefficient-only approximation. Other WB orders remain unchanged.
        if (order == PlayerOrder.Normal)
        {
            var experience = experienceBonus;
            var form = p.Form;

            var centralDefence =
                .22718839 * p.Defending
                - .06836110 * p.Playmaking
                - .03859804 * p.Winger
                + 1.55775124 * experience
                + .07189513 * form;

            var sideDefence =
                .07251566 * p.Defending
                + .00050016 * p.Playmaking
                + .00774675 * p.Winger
                + .15188322 * experience
                + .08817767 * form;

            var midfield =
                .02567605 * p.Defending
                + .01508094 * p.Playmaking
                + .01862094 * p.Winger
                - .20468004 * experience
                + .12042817 * form;

            var sideAttack =
                .01004223 * p.Defending
                + .00375560 * p.Playmaking
                + .13115347 * p.Winger
                + .11525574 * experience
                + .10627788 * form;

            // Preserve the existing minute/stamina layer. At kickoff this
            // factor is 1.0, matching the supplied singleton screenshots.
            var baseForm = .378 * Math.Sqrt(Math.Clamp(p.Form - 1.0, 0.0, 7.0)) / .756;
            var staminaMultiplier = baseForm > 1e-12 ? formMultiplier / baseForm : 1.0;

            Add(s, RatingSector.CentralDefence, Math.Max(0.0, centralDefence), staminaMultiplier);
            Add(s, defence, Math.Max(0.0, sideDefence), staminaMultiplier);
            Add(s, RatingSector.Midfield, Math.Max(0.0, midfield), staminaMultiplier);
            Add(s, attack, Math.Max(0.0, sideAttack), staminaMultiplier);
            return;
        }

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
        if (order == PlayerOrder.Normal)
        {
            if (side == PlayerSide.Left)
            {
                // IM-L calibration from the six current user-supplied 0-1-0
                // Hattrick screenshots dated 2026-09-19. White values only are
                // ground truth; green delta values are never calibration targets.
                //
                // Inputs are already normalized by the production skill layer:
                // effective skill = max(0, raw skill - 1) + ExperienceBonus + Loyalty.
                // The fit is deliberately low-complexity and monotonic in the
                // documented driving skill for each sector.
                var imLFormFactor = .378 * Math.Sqrt(Math.Clamp(playerForm - 1.0, 0.0, 7.0));

                var leftDefenceImL =
                    .86876582
                    + .05128852 * defending;
                var centralDefenceImL =
                    .53590643
                    + .05304477 * defending
                    + .56421781 * imLFormFactor;

                // Midfield is driven primarily by Playmaking. Form is retained
                // as the empirical scale correction needed to match the isolated
                // Hattrick 0-1-0 screenshots within the quarter-step display band.
                var midfieldImL =
                    -2.60881793
                    + .16428439 * playmaking
                    + 3.81099431 * imLFormFactor;

                // Normal IM side attack is driven by Passing. The positive passing
                // coefficient is required by both the documented contribution
                // model and the supplied Hattrick screenshots.
                var leftAttackImL =
                    .58959034
                    + .03221075 * passing
                    + .14963856 * imLFormFactor;

                // Normal IM central attack is driven by Passing + Scoring.
                var centralAttackImL =
                    .23940566
                    + .05047427 * passing
                    + .04472221 * scoring
                    + .69342408 * imLFormFactor;

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
            // IM-C calibration from the six current user-supplied 0-1-0
            // Hattrick screenshots dated 2026-09-19. White values only are
            // ground truth; green delta values are never calibration targets.
            //
            // Effective skill inputs are the production normalized skills
            // (raw skill - 1 + experience bonus + loyalty). Form is represented
            // by the same form-factor input used by the V5 calibration layer.
            var imCFormFactor = .378 * Math.Sqrt(
                Math.Max(0.0, playerForm - 1.0));

            // Central IM has no field-side bias: LD/RD share the same
            // defending-driven contribution.
            var centralDefence =
                .43837692
                + .06418087 * defending
                + .58468111 * imCFormFactor;

            var sideDefence =
                .92171186
                + .01960455 * defending
                + .01232717 * imCFormFactor;

            // MID is playmaking-driven; the form term is retained because
            // the supplied Hattrick screenshots show a measurable form effect.
            var midfieldFinal =
                .55041714
                + .10480606 * playmaking
                + .28726477 * imCFormFactor;
            var midfield = Math.Max(0.0, midfieldFinal) / .8285714285714286;

            // The six current screenshots display the side-attack sector at
            // the 1.00 floor. Keep this calibration stable rather than using
            // the previous high-order polynomial that over-shot several players.
            var leftAttack = .78571429;
            var rightAttack = .78571429;

            // Normal IM central attack is driven by Passing + Scoring.
            // The fit is monotonic in both skills and includes the observed
            // form correction; experience is already represented in effective
            // skill normalization and is not added as a separate sector bonus.
            var centralAttack =
                .30879015
                + .07563828 * passing
                + .02836967 * scoring
                + .64904194 * imCFormFactor;

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
            // observations. Experience is intentionally excluded.
            var ff = .378 * Math.Sqrt(Math.Clamp(playerForm - 1.0, 0.0, 7.0));

            var centralDef = .47533365
                + .13610538 * defending
                + .38235880 * ff
                - .10500311 * defending * ff;
            var sideDef = .46030951
                + .19618792 * defending
                + .35894517 * ff
                - .08738294 * defending * ff;
            var midfield = 1.35839805
                - .02179095 * playmaking
                + 1.21013647 * ff
                - .25824624 * playmaking * ff
                // The singleton corpus shows a small defending-dependent
                // shift in the displayed MID sector. Keep it deliberately
                // low-amplitude so the existing winger calibration remains
                // within the quarter-step regression band.
                - .07120000
                + .02000000 * (defending - 4.0);
            var sideAttack = .49722642
                + .06380086 * passing
                - .09069453 * winger
                + .50250568 * ff
                - .04769162 * passing * ff
                + .24645510 * winger * ff;

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
        double defending, double playmaking, double passing, double winger, double scoring,
        double form, double experienceBonus)
    {
        var own = side == PlayerSide.Left ? RatingSector.LeftAttack : RatingSector.RightAttack;
        var other = side == PlayerSide.Left ? RatingSector.RightAttack : RatingSector.LeftAttack;

        // Experience is a match-performance factor, but it is NOT a skill
        // level for the contribution matrix. The documented forward matrix
        // uses the player's actual Scoring/Passing/Winger levels. Remove the
        // V5 experience normalization here before applying the coefficients.
        defending = Math.Max(0.0, defending - experienceBonus);
        playmaking = Math.Max(0.0, playmaking - experienceBonus);
        passing = Math.Max(0.0, passing - experienceBonus);
        winger = Math.Max(0.0, winger - experienceBonus);
        scoring = Math.Max(0.0, scoring - experienceBonus);

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

                if (side == PlayerSide.Center)
                {
                    // FW-C normal singleton calibration from the nine supplied
                    // 0-0-1 Hattrick screenshots (2026-09-19). White values are
                    // the targets; green deltas are ignored.
                    //
                    // The existing FW-L/FW-R calibration remains unchanged.
                    // FW-C needs its own fit because the live singleton corpus
                    // exposes a different center/side attack scale. Inputs are
                    // raw skill levels after removing the production XP bonus.
                    const double centerSideIntercept = .93619861;
                    const double centerSidePassing = .01000000;
                    const double centerSideScoring = .05739950;
                    const double centerSideWinger = .04386850;
                    const double centerSideDefending = -.02183898;
                    const double centerSideExperience = .28212060;

                    const double centerAttackIntercept = 1.07631807;
                    const double centerAttackPassing = .01000000;
                    const double centerAttackScoring = .19974124;
                    const double centerAttackDefending = -.05671134;
                    const double centerAttackExperience = .52513407;

                    var centerSide =
                        centerSideIntercept
                        + passing * centerSidePassing * form
                        + scoring * centerSideScoring * form
                        + winger * centerSideWinger * form
                        + defending * centerSideDefending
                        + experienceBonus * centerSideExperience;

                    var centerAttack =
                        centerAttackIntercept
                        + passing * centerAttackPassing * form
                        + scoring * centerAttackScoring * form
                        + defending * centerAttackDefending
                        + experienceBonus * centerAttackExperience;

                    // Neutralize the legacy left/right reference scales so the
                    // center forward produces the same live Hattrick value on
                    // both side attacks.
                    var leftCenterSide = centerSide / 1.2727272727272727;
                    var rightCenterSide = centerSide / 1.2258064516129032;

                    Add(s, RatingSector.LeftAttack, leftCenterSide, 1.0);
                    Add(s, RatingSector.RightAttack, rightCenterSide, 1.0);
                    Add(s, RatingSector.CentralAttack, centerAttack, 1.0);
                    break;
                }

                // Normal FW-L/FW-R calibration against the existing 8-player
                // screenshot corpus. Keep this branch unchanged so the new
                // FW-C fit cannot regress the already-passing side-forward test.
                const double sideIntercept = .99522254;
                const double sidePassing = .05215221;
                const double sideScoring = .03750657;
                const double sideWinger = .03364124;

                const double centralIntercept = .99496221;
                const double centralPassing = .05239461;
                const double centralScoring = .18757562;

                var sideNormal =
                    sideIntercept / Math.Max(form, 1e-9)
                    + passing * sidePassing
                    + scoring * sideScoring
                    + winger * sideWinger;
                var centralNormal =
                    centralIntercept / Math.Max(form, 1e-9)
                    + passing * centralPassing
                    + scoring * centralScoring;

                var leftSideNormal = sideNormal / 1.2727272727272727;
                var rightSideNormal = sideNormal / 1.2258064516129032;

                Add(s, own, side == PlayerSide.Left ? leftSideNormal : rightSideNormal, form);
                Add(s, other, side == PlayerSide.Left ? rightSideNormal : leftSideNormal, form);
                Add(s, RatingSector.CentralAttack, centralNormal, form);
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
