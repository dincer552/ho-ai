using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// V5 regional rating engine. Skill contribution is calculated from the
/// canonical 14 Hattrick field slots independently; context, loyalty,
/// stamina and central-position crowding are applied as separate layers afterwards.
/// </summary>
public sealed class RegionalRatingEngineFixed
{
    private const double BaselineFormFactor = .756;
    private const double ReferenceMidfieldCalibration = .8285714285714286;
    private const double ReferenceLeftAttackCalibration = 1.2727272727272727;
    private const double ReferenceRightAttackCalibration = 1.2258064516129032;

    public RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context = null)
    {
        context ??= RatingContext.Default;
        var sectors = Empty();

        var centralDefenders = players.Count(p => RatingPositionMatrix.CanonicalSlot(p) is "DEF-CL" or "DEF-C" or "DEF-CR");
        var centralMidfielders = players.Count(p => RatingPositionMatrix.CanonicalSlot(p) is "IM-L" or "IM-C" or "IM-R");
        var forwards = players.Count(p => RatingPositionMatrix.CanonicalSlot(p) is "FW-L" or "FW-C" or "FW-R");

        foreach (var p in players)
        {
            var formMultiplier = FormFactor(p.Form) / BaselineFormFactor;
            formMultiplier *= StaminaMatchMultiplier(p.Stamina, context.MatchMinute);
            var loyalty = LoyaltyEffect(p.Loyalty);

            var k = new EffectiveSkillsFixed(
                SkillRating(p.Keeper) + loyalty,
                SkillRating(p.Defending) + loyalty,
                SkillRating(p.Playmaking) + loyalty,
                SkillRating(p.Passing) + loyalty,
                SkillRating(p.Winger) + loyalty,
                SkillRating(p.Scoring) + loyalty,
                formMultiplier);

            var before = new Dictionary<RatingSector, double>(sectors);
            RatingPositionMatrix.AddContribution(
                sectors, p, k.Keeper, k.Defending, k.Playmaking, k.Passing,
                k.Winger, k.Scoring, k.FormMultiplier);

            var crowding = PositionCrowding(RatingPositionMatrix.CanonicalSlot(p), centralDefenders, centralMidfielders, forwards);
            if (crowding != 1.0)
            {
                foreach (var sector in Enum.GetValues<RatingSector>())
                    sectors[sector] = before[sector] + (sectors[sector] - before[sector]) * crowding;
            }

        }

        ApplyContext(sectors, context);
        ApplyReferenceCalibration(sectors);
        return ToSnapshot(sectors);
    }

    public RegionalRatingSnapshot CalculateLineup(Lineup lineup, IReadOnlyList<Player> players, RatingContext? context = null)
    {
        var byId = players.ToDictionary(p => p.Id);
        var mapped = lineup.Slots
            .Where(s => s.PlayerId > 0 && byId.ContainsKey(s.PlayerId))
            .Select(s => ToRegionalPlayer(lineup.Formation, s, byId[s.PlayerId]))
            .ToList();

        return Calculate(mapped, context);
    }

    public RegionalRatingPair CalculatePair(
        Lineup ownLineup, IReadOnlyList<Player> ownPlayers,
        Lineup opponentLineup, IReadOnlyList<Player> opponentPlayers,
        RatingContext? ownContext = null, RatingContext? opponentContext = null)
        => new(
            CalculateLineup(ownLineup, ownPlayers, ownContext),
            CalculateLineup(opponentLineup, opponentPlayers, opponentContext));

    internal static double SkillRating(double skill) => Math.Max(0.0, skill - 1.0);

    internal static double StaminaMatchMultiplier(double stamina, int minute)
    {
        if (minute <= 0) return 1.0;

        var m = Math.Clamp(minute, 0, 120);
        var factor45 = StaminaAtPoint(stamina, 45);
        var factor90 = StaminaAtPoint(stamina, 90);
        var factor120 = StaminaAtPoint(stamina, 120);

        if (m <= 45) return Lerp(1.0, factor45, m / 45.0);
        if (m <= 90) return Lerp(factor45, factor90, (m - 45) / 45.0);
        return Lerp(factor90, factor120, (m - 90) / 30.0);
    }

    private static double StaminaAtPoint(double stamina, int minute)
    {
        var levels = new[] { 1.7, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.4 };
        var at45 = new[] { .5886, .608, .640, .671, .703, .735, .767, .799, .831, .863, .894, .926, .958, .990, 1.000, 1.000, 1.000 };
        var at90 = new[] { .265, .294, .344, .393, .442, .491, .541, .590, .639, .688, .737, .787, .836, .885, .956, 1.000, 1.000 };
        var at120 = new[] { .100, .100, .100, .100, .156, .218, .281, .344, .406, .469, .532, .595, .657, .720, .791, .863, .920 };
        var table = minute <= 45 ? at45 : minute <= 90 ? at90 : at120;
        var x = Math.Clamp(stamina, levels[0], levels[^1]);

        for (var i = 1; i < levels.Length; i++)
        {
            if (x <= levels[i])
            {
                var t = (x - levels[i - 1]) / (levels[i] - levels[i - 1]);
                return Lerp(table[i - 1], table[i], t);
            }
        }

        return table[^1];
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static Dictionary<RatingSector, double> Empty()
        => Enum.GetValues<RatingSector>().ToDictionary(x => x, _ => 0d);

    private static double CentralDefenderCrowding(int count) => count == 2 ? .964 : count >= 3 ? .900 : 1.0;
    private static double InnerMidfielderCrowding(int count) => count == 2 ? .935 : count >= 3 ? .825 : 1.0;
    private static double ForwardCrowding(int count) => count == 2 ? .945 : count >= 3 ? .865 : 1.0;

    private static double PositionCrowding(string slot, int cds, int ims, int fws) => slot switch
    {
        "DEF-CL" or "DEF-C" or "DEF-CR" => CentralDefenderCrowding(cds),
        "IM-L" or "IM-C" or "IM-R" => InnerMidfielderCrowding(ims),
        "FW-L" or "FW-C" or "FW-R" => ForwardCrowding(fws),
        _ => 1.0
    };

    private static void ApplyContext(Dictionary<RatingSector, double> s, RatingContext c)
    {
        var midfield = c.MatchLocation switch
        {
            MatchLocation.Home => 1.19892,
            MatchLocation.DerbyAway => 1.11493,
            _ => 1.0
        };

        midfield *= c.Attitude switch
        {
            TeamAttitude.MatchOfTheSeason => 1.1149,
            TeamAttitude.PlayItCool => .83945,
            _ => 1.0
        };

        if (c.Tactic == TeamTactic.CounterAttack)
            midfield *= .93;

        switch (c.Tactic)
        {
            case TeamTactic.AttackMiddle:
                s[RatingSector.LeftDefence] *= .85;
                s[RatingSector.RightDefence] *= .85;
                break;
            case TeamTactic.AttackWings:
                s[RatingSector.CentralDefence] *= .85;
                break;
            case TeamTactic.Creative:
                s[RatingSector.LeftDefence] *= .93;
                s[RatingSector.CentralDefence] *= .93;
                s[RatingSector.RightDefence] *= .93;
                break;
            case TeamTactic.LongShots:
                s[RatingSector.LeftAttack] *= .96;
                s[RatingSector.CentralAttack] *= .96;
                s[RatingSector.RightAttack] *= .96;
                break;
        }

        if (c.MatchMinute > 0)
            s[RatingSector.Midfield] *= 1.0 - .10 * Math.Clamp(c.MatchMinute / 90.0, 0, 1);

        s[RatingSector.Midfield] *= midfield;

        if (c.GoalDifference >= 2 && !c.IgnoreLeadRetreat)
        {
            var steps = Math.Min(c.GoalDifference - 1, 7);
            var protection = 1.0 + steps * .075;
            var attack = 1.0 - steps * .09;

            s[RatingSector.LeftDefence] *= protection;
            s[RatingSector.CentralDefence] *= protection;
            s[RatingSector.RightDefence] *= protection;
            s[RatingSector.LeftAttack] *= attack;
            s[RatingSector.CentralAttack] *= attack;
            s[RatingSector.RightAttack] *= attack;
        }
    }

    private static void ApplyReferenceCalibration(Dictionary<RatingSector, double> s)
    {
        s[RatingSector.Midfield] *= ReferenceMidfieldCalibration;
        s[RatingSector.LeftAttack] *= ReferenceLeftAttackCalibration;
        s[RatingSector.RightAttack] *= ReferenceRightAttackCalibration;
    }

    private static double LoyaltyEffect(double loyalty)
        => loyalty >= 20 ? 1.5 : Math.Clamp(loyalty / 19.0, 0.0, 1.0);

    private static double FormFactor(double form)
        => .378 * Math.Sqrt(Math.Clamp(form - 1.0, 0.0, 7.0));

    private static RegionalRatingSnapshot ToSnapshot(Dictionary<RatingSector, double> s)
        => new(
            s[RatingSector.LeftDefence],
            s[RatingSector.CentralDefence],
            s[RatingSector.RightDefence],
            s[RatingSector.Midfield],
            s[RatingSector.LeftAttack],
            s[RatingSector.CentralAttack],
            s[RatingSector.RightAttack],
            s[RatingSector.LeftDefence],
            s[RatingSector.CentralDefence],
            s[RatingSector.RightDefence],
            s[RatingSector.Midfield],
            s[RatingSector.LeftAttack],
            s[RatingSector.CentralAttack],
            s[RatingSector.RightAttack]);

    private static RegionalPlayer ToRegionalPlayer(string formation, Slot slot, Player p)
    {
        var position = RatingPositionResolver.Resolve(formation, slot.Code);
        var side = slot.Code.EndsWith("-L", StringComparison.Ordinal)
            ? PlayerSide.Left
            : slot.Code.EndsWith("-R", StringComparison.Ordinal)
                ? PlayerSide.Right
                : PlayerSide.Center;

        return new RegionalPlayer(
            p.Id, position, side, slot.Order,
            p.Keeper, p.Defending, p.Playmaking, p.Passing, p.Winger, p.Scoring,
            p.Form, p.Loyalty, p.Experience, p.Stamina, slot.Code);
    }

    private readonly record struct EffectiveSkillsFixed(
        double Keeper, double Defending, double Playmaking, double Passing,
        double Winger, double Scoring, double FormMultiplier);
}
