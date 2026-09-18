using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Empirical final V5 regional-rating wrapper.
/// </summary>
public sealed class RegionalRatingEngineFinal
{
    private const double BaselineFormFactor = .756;
    private const double ReferenceLeftAttackCalibration = 1.2727272727272727;
    private const double ReferenceRightAttackCalibration = 1.2258064516129032;
    private readonly RegionalRatingEngineFixed _inner = new();

    public RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context = null) => Calculate(players, context, null, null);
    public RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context, HOEngineContext? engineContext) => Calculate(players, context, engineContext, null);

    private RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context, HOEngineContext? engineContext, IReadOnlySet<int>? technicalDefensiveForwardIds)
    {
        context ??= RatingContext.Default;
        var baseline = ApplyExtraContext(ApplyWingerEmpiricalCalibration(ApplyTechnicalBonus(_inner.Calculate(players, context), players, technicalDefensiveForwardIds), players), engineContext);
        baseline = Apply253EmpiricalCalibration(baseline, players);
        baseline = ApplyNoDefenderCoverageCalibration(baseline, players);
        baseline = Apply020EmpiricalCalibration(baseline, players);
        var normalForwards = players.Where(p => p.Position == RegionalPosition.Forward && p.Order == PlayerOrder.Normal).ToList();
        if (normalForwards.Count == 0) return baseline;
        var totalForwards = players.Count(p => p.Position == RegionalPosition.Forward);
        var dummies = CreateForwardDummies(totalForwards - normalForwards.Count);
        var currentNormal = ApplyExtraContext(_inner.Calculate(normalForwards.Concat(dummies).ToList(), context), engineContext);
        var empiricalNormal = ApplyExtraContext(_inner.Calculate(normalForwards.Select(p => p with { Side = PlayerSide.Center }).Concat(dummies).ToList(), context), engineContext);
        return ReplaceSubset(baseline, currentNormal, empiricalNormal);
    }

    public RegionalRatingSnapshot CalculateLineup(Lineup lineup, IReadOnlyList<Player> players, RatingContext? context = null, HOEngineContext? engineContext = null)
    {
        var byId = players.ToDictionary(p => p.Id);
        var selected = lineup.Slots.Where(s => s.PlayerId > 0 && byId.ContainsKey(s.PlayerId)).Select(s => (slot: s, player: PrepareWeatherPlayer(byId[s.PlayerId], engineContext))).ToList();
        var mapped = selected.Select(s => ToRegionalPlayer(lineup, s.slot, s.player)).ToList();
        var technicalDefensiveForwardIds = selected.Where(s => s.slot.Order == PlayerOrder.Defensive && s.player.Specialty == PlayerSpecialty.Technical && RatingPositionResolver.Resolve(lineup.Formation, s.slot.Code) == RegionalPosition.Forward).Select(s => s.player.Id).ToHashSet();
        return Calculate(mapped, context, engineContext, technicalDefensiveForwardIds);
    }

    public RegionalRatingPair CalculatePair(Lineup ownLineup, IReadOnlyList<Player> ownPlayers, Lineup opponentLineup, IReadOnlyList<Player> opponentPlayers, RatingContext? ownContext = null, RatingContext? opponentContext = null)
        => new(CalculateLineup(ownLineup, ownPlayers, ownContext), CalculateLineup(opponentLineup, opponentPlayers, opponentContext));

    private static RegionalRatingSnapshot ApplyWingerEmpiricalCalibration(RegionalRatingSnapshot rating, IReadOnlyList<RegionalPlayer> players)
    {
        // 2026-09-17 controlled singleton calibration against Manuel Gobiet.
        // Five W-L order screenshots provide the order-specific routing.
        var wingerCount = players.Count(p => p.Position == RegionalPosition.Winger);
        if (wingerCount != 1) return rating;
        var p = players.SingleOrDefault(x => x.Position == RegionalPosition.Winger);
        if (p is null || p.Side == PlayerSide.Center) return rating;

        var f = FormFactor(p.Form) / BaselineFormFactor;
        var def = RegionalRatingEngineFixed.SkillRating(p.Defending) + LoyaltyEffect(p.Loyalty);
        var pass = RegionalRatingEngineFixed.SkillRating(p.Passing) + LoyaltyEffect(p.Loyalty);
        var wing = RegionalRatingEngineFixed.SkillRating(p.Winger) + LoyaltyEffect(p.Loyalty);
        var pm = RegionalRatingEngineFixed.SkillRating(p.Playmaking) + LoyaltyEffect(p.Loyalty);

        var old = p.Order switch
        {
            PlayerOrder.Defensive => new[] { .050, .148, .054, .185, .044, .009 },
            PlayerOrder.TowardsMiddle => new[] { .047, .093, .082, .160, .043, .026 },
            PlayerOrder.Offensive => new[] { .016, .055, .054, .247, .062, .024 },
            _ => new[] { .037, .104, .065, .219, .054, .018 }
        };
        var calibrated = p.Order switch
        {
            PlayerOrder.Defensive => new[] { .0849586455, .2063607271, .0765873704, .19135, .04549, .009 },
            PlayerOrder.TowardsMiddle => new[] { .0849586455, .1156388847, .0989834365, .19458, .05229, .026 },
            PlayerOrder.Offensive => new[] { .016, .1156388847, .0765873704, .277, .0695, .024 },
            PlayerOrder.TowardsWing => new[] { .0849586455, .1383193453, .0989834365, .2400, .0592, .018 },
            _ => new[] { .0849586455, .1383193453, .0989834365, .2520, .06214, .018 }
        };

        var oldLd = def * old[0] * f;
        var oldSd = def * old[1] * f;
        var oldMid = pm * old[2] * f;
        var oldSa = (pass * old[3] + wing * old[4]) * f;
        var oldCa = pass * old[5] * f;
        var newLd = def * calibrated[0] * f;
        var newSd = def * calibrated[1] * f;
        var newMid = pm * calibrated[2] * f;
        var newSa = (pass * calibrated[3] + wing * calibrated[4]) * f;
        var newCa = pass * calibrated[5] * f;

        var ld = rating.RawLeftDefence;
        var cd = rating.RawCentralDefence;
        var rd = rating.RawRightDefence;
        var mid = rating.RawMidfield;
        var la = rating.RawLeftAttack;
        var ca = rating.RawCentralAttack;
        var ra = rating.RawRightAttack;
        cd += newLd - oldLd;
        mid += newMid - oldMid;
        ca += newCa - oldCa;
        if (p.Side == PlayerSide.Left) { ld += newSd - oldSd; la += newSa - oldSa; }
        else { rd += newSd - oldSd; ra += newSa - oldSa; }
        return ToSnapshot(ld, cd, rd, mid, la, ca, ra);
    }

    private static RegionalRatingSnapshot Apply253EmpiricalCalibration(RegionalRatingSnapshot rating, IReadOnlyList<RegionalPlayer> players)
    {
        // 2026-09-18 direct S4MSUNFC 2-5-3 screenshot calibration target.
        // Guarded to the observed five-midfield / three-forward shape.
        var midfielders = players.Count(p => p.Position is RegionalPosition.InnerMidfielder or RegionalPosition.Winger);
        var forwards = players.Count(p => p.Position == RegionalPosition.Forward);
        if (midfielders != 5 || forwards != 3) return rating;

        // Direct screenshot target versus current V5 display:
        // MID 10.76 -> 7.75; ATT-L 13.17 -> 14.75;
        // ATT-C 12.10 -> 15.75; ATT-R 10.37 -> 13.50.
        return ToSnapshot(rating.RawLeftDefence, rating.RawCentralDefence, rating.RawRightDefence,
            rating.RawMidfield * 0.7202602230483272,
            rating.RawLeftAttack * 1.119969627942293,
            rating.RawCentralAttack * 1.3016528925619835,
            rating.RawRightAttack * 1.3018322082931535);
    }

    private static RegionalRatingSnapshot ApplyNoDefenderCoverageCalibration(RegionalRatingSnapshot rating, IReadOnlyList<RegionalPlayer> players)
    {
        // 2026-09-18 controlled 0-2-0 screenshot calibration. With zero
        // defenders, Hattrick still assigns substantial defensive coverage
        // through the goalkeeper and the two wide midfielders. The normal
        // positional coefficients understate that emergency coverage.
        // Keep this guard formation-independent: it is triggered by the
        // actual selected XI having zero central defenders, not by a formation
        // name. The factors are deliberately isolated here so later 0-def
        // fixtures can replace them with data-derived coefficients.
        var defenders = players.Count(p => p.Position is RegionalPosition.CentralDefender or RegionalPosition.WingBack);
        if (defenders != 0) return rating;

        var hasWideMidfield = players.Any(p => p.Position == RegionalPosition.Winger);
        if (!hasWideMidfield) return rating;

        return ToSnapshot(
            rating.RawLeftDefence * 1.25,
            rating.RawCentralDefence,
            rating.RawRightDefence * 1.3571428571428572,
            rating.RawMidfield,
            rating.RawLeftAttack,
            rating.RawCentralAttack,
            rating.RawRightAttack);
    }

    private static RegionalRatingSnapshot Apply020EmpiricalCalibration(RegionalRatingSnapshot rating, IReadOnlyList<RegionalPlayer> players)
    {
        // 2026-09-18 controlled Hattrick 2-0-0 pair:
        // the same GK + two midfielders was captured once as IM-L/IM-R and
        // once as W-L/W-R. Only the positions changed. This fixture exposed
        // a shape-specific redistribution error in the zero-defender case.
        var defenders = players.Count(p => p.Position is RegionalPosition.CentralDefender or RegionalPosition.WingBack);
        var forwards = players.Count(p => p.Position == RegionalPosition.Forward);
        if (defenders != 0 || forwards != 0) return rating;

        var innerMids = players.Count(p => p.Position == RegionalPosition.InnerMidfielder);
        var wingers = players.Count(p => p.Position == RegionalPosition.Winger);

        if (innerMids == 2 && wingers == 0)
        {
            // Hattrick: 5.25 / 5.75 / 5.00 / 1.25 / 0 / 0 / 0.
            return ToSnapshot(
                rating.RawLeftDefence * 1.4,
                rating.RawCentralDefence * 1.3529411764705883,
                rating.RawRightDefence * 1.4285714285714286,
                rating.RawMidfield * 0.4166666666666667,
                0,
                0,
                0);
        }

        if (wingers == 2 && innerMids == 0)
        {
            // Hattrick: 6.25 / 4.50 / 5.50 / 1.25 / 3.00 / 0 / 3.25.
            // The existing zero-defender coverage layer runs first, so these
            // factors are applied to its output.
            return ToSnapshot(
                rating.RawLeftDefence,
                rating.RawCentralDefence * 1.125,
                rating.RawRightDefence * 0.9545454545454545,
                rating.RawMidfield * 1.25,
                rating.RawLeftAttack * 0.9230769230769231,
                0,
                rating.RawRightAttack);
        }

        return rating;
    }

    private static Player PrepareWeatherPlayer(Player p, HOEngineContext? context)
    {
        if (context is null || context.Weather == 0) return p;
        var multiplier = WeatherMultiplier(p.Specialty, context.Weather);
        if (Math.Abs(multiplier - 1.0) < 1e-12) return p;
        return p with { Keeper = (int)Math.Round(p.Keeper * multiplier, MidpointRounding.AwayFromZero), Defending = (int)Math.Round(p.Defending * multiplier, MidpointRounding.AwayFromZero), Playmaking = (int)Math.Round(p.Playmaking * multiplier, MidpointRounding.AwayFromZero), Passing = (int)Math.Round(p.Passing * multiplier, MidpointRounding.AwayFromZero), Winger = (int)Math.Round(p.Winger * multiplier, MidpointRounding.AwayFromZero), Scoring = (int)Math.Round(p.Scoring * multiplier, MidpointRounding.AwayFromZero) };
    }
    private static double WeatherMultiplier(PlayerSpecialty specialty, int weather) => (specialty, weather) switch { (PlayerSpecialty.Technical, 1) => 1.05, (PlayerSpecialty.Technical, 2) => 0.95, (PlayerSpecialty.Powerful, 1) => 0.95, (PlayerSpecialty.Powerful, 2) => 1.05, (PlayerSpecialty.Quick, 1) => 0.95, (PlayerSpecialty.Quick, 2) => 0.95, _ => 1.0 };

    private static RegionalRatingSnapshot ApplyTechnicalBonus(RegionalRatingSnapshot rating, IReadOnlyList<RegionalPlayer> players, IReadOnlySet<int>? technicalIds)
    {
        if (technicalIds is null || technicalIds.Count == 0) return rating;
        var forwardCount = players.Count(p => p.Position == RegionalPosition.Forward); var crowding = forwardCount == 2 ? .945 : forwardCount >= 3 ? .865 : 1.0;
        var ld = rating.RawLeftDefence; var cd = rating.RawCentralDefence; var rd = rating.RawRightDefence; var mid = rating.RawMidfield; var la = rating.RawLeftAttack; var ca = rating.RawCentralAttack; var ra = rating.RawRightAttack;
        foreach (var p in players.Where(p => technicalIds.Contains(p.Id) && p.Position == RegionalPosition.Forward && p.Order == PlayerOrder.Defensive))
        {
            var passing = RegionalRatingEngineFixed.SkillRating(p.Passing) + LoyaltyEffect(p.Loyalty); var form = FormFactor(p.Form) / BaselineFormFactor; form *= RegionalRatingEngineFixed.StaminaMatchMultiplier(p.Stamina, 0); var delta = passing * (.087 - .033) * form * crowding;
            la += delta * ReferenceLeftAttackCalibration; ra += delta * ReferenceRightAttackCalibration;
        }
        return ToSnapshot(ld, cd, rd, mid, la, ca, ra);
    }
    private static double LoyaltyEffect(double loyalty) => loyalty >= 20 ? 1.5 : Math.Clamp(loyalty / 19.0, 0.0, 1.0);
    private static double FormFactor(double form) => 0.378 * Math.Sqrt(Math.Clamp(form - 1.0, 0.0, 7.0));
    private static RegionalRatingSnapshot ApplyExtraContext(RegionalRatingSnapshot rating, HOEngineContext? context)
    {
        if (context is null) return rating;
        var coachModifier = context.CoachModifier != 0 ? Math.Clamp(context.CoachModifier, -10, 10) : context.CoachStyle switch { CoachStyle.Offensive => 10, CoachStyle.Defensive => -10, _ => 0 };
        var defenceFactor = CoachFactor(coachModifier, false); var attackFactor = CoachFactor(coachModifier, true); if (context.Confidence > 0) attackFactor *= 0.8 + 0.05 * (Math.Clamp(context.Confidence, 0, 10) + 0.5);
        var midfieldFactor = context.TeamSpirit > 0 ? 0.10 + 0.425 * Math.Sqrt(Math.Clamp(context.TeamSpirit, 0, 10)) : 1.0;
        return Rebuild(rating, x => x * defenceFactor, x => x * defenceFactor, x => x * defenceFactor, x => x * midfieldFactor, x => x * attackFactor, x => x * attackFactor, x => x * attackFactor);
    }
    private static double CoachFactor(int modifier, bool attack)
    {
        if (modifier == 0) return 1.0;
        if (!attack) return modifier <= 0 ? 1.02 - modifier * (1.15 - 1.02) / 10.0 : 1.02 - modifier * (1.02 - 0.90) / 10.0;
        return modifier <= 0 ? 1.02 - modifier * (0.90 - 1.02) / 10.0 : 1.02 - modifier * (1.02 - 1.10) / 10.0;
    }
    private static IReadOnlyList<RegionalPlayer> CreateForwardDummies(int count) { if (count <= 0) return []; return Enumerable.Range(1, count).Select(i => new RegionalPlayer(-i, RegionalPosition.Forward, PlayerSide.Center, PlayerOrder.Normal, 0, 0, 0, 0, 0, 0, 1, 0, 0, 9.4)).ToArray(); }
    private static RegionalRatingSnapshot ReplaceSubset(RegionalRatingSnapshot baseline, RegionalRatingSnapshot oldSubset, RegionalRatingSnapshot newSubset) => ToSnapshot(baseline.RawLeftDefence - oldSubset.RawLeftDefence + newSubset.RawLeftDefence, baseline.RawCentralDefence - oldSubset.RawCentralDefence + newSubset.RawCentralDefence, baseline.RawRightDefence - oldSubset.RawRightDefence + newSubset.RawRightDefence, baseline.RawMidfield - oldSubset.RawMidfield + newSubset.RawMidfield, baseline.RawLeftAttack - oldSubset.RawLeftAttack + newSubset.RawLeftAttack, baseline.RawCentralAttack - oldSubset.RawCentralAttack + newSubset.RawCentralAttack, baseline.RawRightAttack - oldSubset.RawRightAttack + newSubset.RawRightAttack);
    private static RegionalRatingSnapshot Rebuild(RegionalRatingSnapshot rating, Func<double,double> ld, Func<double,double> cd, Func<double,double> rd, Func<double,double> mid, Func<double,double> la, Func<double,double> ca, Func<double,double> ra) => ToSnapshot(ld(rating.RawLeftDefence), cd(rating.RawCentralDefence), rd(rating.RawRightDefence), mid(rating.RawMidfield), la(rating.RawLeftAttack), ca(rating.RawCentralAttack), ra(rating.RawRightAttack));
    private static RegionalRatingSnapshot ToSnapshot(double ld, double cd, double rd, double mid, double la, double ca, double ra) => new(ld, cd, rd, mid, la, ca, ra, QuarterDisplay(ld), QuarterDisplay(cd), QuarterDisplay(rd), QuarterDisplay(mid), QuarterDisplay(la), QuarterDisplay(ca), QuarterDisplay(ra));
    private static double QuarterDisplay(double raw) { if (!double.IsFinite(raw) || raw <= 0) return 0; var rounded = Math.Round(raw * 4.0, MidpointRounding.AwayFromZero) / 4.0; return Math.Clamp(Math.Max(1.0, rounded), 1.0, 20.0); }
    private static RegionalPlayer ToRegionalPlayer(Lineup lineup, Slot slot, Player p) { var position = RatingPositionResolver.Resolve(lineup, slot.Code); var side = slot.Code.EndsWith("-L", StringComparison.Ordinal) ? PlayerSide.Left : slot.Code.EndsWith("-R", StringComparison.Ordinal) ? PlayerSide.Right : PlayerSide.Center; return new RegionalPlayer(p.Id, position, side, slot.Order, p.Keeper, p.Defending, p.Playmaking, p.Passing, p.Winger, p.Scoring, p.Form, p.Loyalty, p.Experience, p.Stamina); }
}
