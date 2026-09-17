using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Empirical final V5 regional-rating wrapper.
///
/// The researched Fixed engine remains the calculation source for established
/// position/order/coefficient layers. Normal forwards keep the empirically
/// verified L/C/R slot symmetry. Match-level factors are applied when
/// HOEngineContext is available: weather, team spirit, confidence and coach
/// style. The documented Technical defensive-forward side-passing bonus is
/// added as a separate correction so the central-attack coefficient is not
/// accidentally inflated.
/// </summary>
public sealed class RegionalRatingEngineFinal
{
    private const double BaselineFormFactor = .756;
    private const double ReferenceLeftAttackCalibration = 1.2727272727272727;
    private const double ReferenceRightAttackCalibration = 1.2258064516129032;

    private readonly RegionalRatingEngineFixed _inner = new();

    public RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context = null)
        => Calculate(players, context, null, null);

    public RegionalRatingSnapshot Calculate(
        IReadOnlyList<RegionalPlayer> players,
        RatingContext? context,
        HOEngineContext? engineContext)
        => Calculate(players, context, engineContext, null);

    private RegionalRatingSnapshot Calculate(
        IReadOnlyList<RegionalPlayer> players,
        RatingContext? context,
        HOEngineContext? engineContext,
        IReadOnlySet<int>? technicalDefensiveForwardIds)
    {
        context ??= RatingContext.Default;
        var baseline = ApplyExtraContext(
            ApplyTechnicalBonus(_inner.Calculate(players, context), players, technicalDefensiveForwardIds),
            engineContext);

        var normalForwards = players
            .Where(p => p.Position == RegionalPosition.Forward && p.Order == PlayerOrder.Normal)
            .ToList();
        if (normalForwards.Count == 0)
            return baseline;

        var totalForwards = players.Count(p => p.Position == RegionalPosition.Forward);
        var dummies = CreateForwardDummies(totalForwards - normalForwards.Count);

        var currentNormal = ApplyExtraContext(
            _inner.Calculate(normalForwards.Concat(dummies).ToList(), context),
            engineContext);

        var empiricalNormal = ApplyExtraContext(
            _inner.Calculate(
                normalForwards.Select(p => p with { Side = PlayerSide.Center })
                    .Concat(dummies)
                    .ToList(),
                context),
            engineContext);

        return ReplaceSubset(baseline, currentNormal, empiricalNormal);
    }

    public RegionalRatingSnapshot CalculateLineup(
        Lineup lineup,
        IReadOnlyList<Player> players,
        RatingContext? context = null,
        HOEngineContext? engineContext = null)
    {
        var byId = players.ToDictionary(p => p.Id);
        var selected = lineup.Slots
            .Where(s => s.PlayerId > 0 && byId.ContainsKey(s.PlayerId))
            .Select(s => (slot: s, player: PrepareWeatherPlayer(byId[s.PlayerId], engineContext)))
            .ToList();

        var mapped = selected
            .Select(s => ToRegionalPlayer(lineup.Formation, s.slot, s.player))
            .ToList();

        var technicalDefensiveForwardIds = selected
            .Where(s => s.slot.Order == PlayerOrder.Defensive
                        && s.player.Specialty == PlayerSpecialty.Technical
                        && RatingPositionResolver.Resolve(lineup.Formation, s.slot.Code) == RegionalPosition.Forward)
            .Select(s => s.player.Id)
            .ToHashSet();

        return Calculate(mapped, context, engineContext, technicalDefensiveForwardIds);
    }

    public RegionalRatingPair CalculatePair(
        Lineup ownLineup,
        IReadOnlyList<Player> ownPlayers,
        Lineup opponentLineup,
        IReadOnlyList<Player> opponentPlayers,
        RatingContext? ownContext = null,
        RatingContext? opponentContext = null)
        => new(
            CalculateLineup(ownLineup, ownPlayers, ownContext),
            CalculateLineup(opponentLineup, opponentPlayers, opponentContext));

    private static Player PrepareWeatherPlayer(Player p, HOEngineContext? context)
    {
        if (context is null || context.Weather == 0)
            return p;

        var multiplier = WeatherMultiplier(p.Specialty, context.Weather);
        if (Math.Abs(multiplier - 1.0) < 1e-12)
            return p;

        return p with
        {
            Keeper = (int)Math.Round(p.Keeper * multiplier, MidpointRounding.AwayFromZero),
            Defending = (int)Math.Round(p.Defending * multiplier, MidpointRounding.AwayFromZero),
            Playmaking = (int)Math.Round(p.Playmaking * multiplier, MidpointRounding.AwayFromZero),
            Passing = (int)Math.Round(p.Passing * multiplier, MidpointRounding.AwayFromZero),
            Winger = (int)Math.Round(p.Winger * multiplier, MidpointRounding.AwayFromZero),
            Scoring = (int)Math.Round(p.Scoring * multiplier, MidpointRounding.AwayFromZero)
        };
    }

    private static double WeatherMultiplier(PlayerSpecialty specialty, int weather)
        => (specialty, weather) switch
        {
            (PlayerSpecialty.Technical, 1) => 1.05,
            (PlayerSpecialty.Technical, 2) => 0.95,
            (PlayerSpecialty.Powerful, 1) => 0.95,
            (PlayerSpecialty.Powerful, 2) => 1.05,
            (PlayerSpecialty.Quick, 1) => 0.95,
            (PlayerSpecialty.Quick, 2) => 0.95,
            _ => 1.0
        };

    private static RegionalRatingSnapshot ApplyTechnicalBonus(
        RegionalRatingSnapshot rating,
        IReadOnlyList<RegionalPlayer> players,
        IReadOnlySet<int>? technicalIds)
    {
        if (technicalIds is null || technicalIds.Count == 0)
            return rating;

        var forwardCount = players.Count(p => p.Position == RegionalPosition.Forward);
        var crowding = forwardCount == 2 ? .945 : forwardCount >= 3 ? .865 : 1.0;
        var ld = rating.RawLeftDefence;
        var cd = rating.RawCentralDefence;
        var rd = rating.RawRightDefence;
        var mid = rating.RawMidfield;
        var la = rating.RawLeftAttack;
        var ca = rating.RawCentralAttack;
        var ra = rating.RawRightAttack;

        foreach (var p in players.Where(p => technicalIds.Contains(p.Id) && p.Position == RegionalPosition.Forward && p.Order == PlayerOrder.Defensive))
        {
            var passing = RegionalRatingEngineFixed.SkillRating(p.Passing) + LoyaltyEffect(p.Loyalty);
            var form = FormFactor(p.Form) / BaselineFormFactor;
            form *= RegionalRatingEngineFixed.StaminaMatchMultiplier(p.Stamina, 0);
            var delta = passing * (.087 - .033) * form * crowding;
            la += delta * ReferenceLeftAttackCalibration;
            ra += delta * ReferenceRightAttackCalibration;
        }

        return ToSnapshot(ld, cd, rd, mid, la, ca, ra);
    }

    private static double LoyaltyEffect(double loyalty) => loyalty >= 20 ? 1.5 : Math.Clamp(loyalty / 19.0, 0.0, 1.0);
    private static double FormFactor(double form) => 0.378 * Math.Sqrt(Math.Clamp(form - 1.0, 0.0, 7.0));

    private static RegionalRatingSnapshot ApplyExtraContext(
        RegionalRatingSnapshot rating,
        HOEngineContext? context)
    {
        if (context is null)
            return rating;

        var coachModifier = context.CoachModifier != 0
            ? Math.Clamp(context.CoachModifier, -10, 10)
            : context.CoachStyle switch
            {
                CoachStyle.Offensive => 10,
                CoachStyle.Defensive => -10,
                _ => 0
            };

        var defenceFactor = CoachFactor(coachModifier, false);
        var attackFactor = CoachFactor(coachModifier, true);
        if (context.Confidence > 0)
            attackFactor *= 0.8 + 0.05 * (Math.Clamp(context.Confidence, 0, 10) + 0.5);

        var midfieldFactor = context.TeamSpirit > 0
            ? 0.10 + 0.425 * Math.Sqrt(Math.Clamp(context.TeamSpirit, 0, 10))
            : 1.0;

        return Rebuild(
            rating,
            x => x * defenceFactor,
            x => x * defenceFactor,
            x => x * defenceFactor,
            x => x * midfieldFactor,
            x => x * attackFactor,
            x => x * attackFactor,
            x => x * attackFactor);
    }

    private static double CoachFactor(int modifier, bool attack)
    {
        if (modifier == 0)
            return 1.0;

        if (!attack)
        {
            return modifier <= 0
                ? 1.02 - modifier * (1.15 - 1.02) / 10.0
                : 1.02 - modifier * (1.02 - 0.90) / 10.0;
        }

        return modifier <= 0
            ? 1.02 - modifier * (0.90 - 1.02) / 10.0
            : 1.02 - modifier * (1.02 - 1.10) / 10.0;
    }

    private static IReadOnlyList<RegionalPlayer> CreateForwardDummies(int count)
    {
        if (count <= 0) return [];
        return Enumerable.Range(1, count)
            .Select(i => new RegionalPlayer(-i, RegionalPosition.Forward, PlayerSide.Center, PlayerOrder.Normal,
                0, 0, 0, 0, 0, 0, 1, 0, 0, 9.4))
            .ToArray();
    }

    private static RegionalRatingSnapshot ReplaceSubset(
        RegionalRatingSnapshot baseline,
        RegionalRatingSnapshot oldSubset,
        RegionalRatingSnapshot newSubset)
    {
        return ToSnapshot(
            baseline.RawLeftDefence - oldSubset.RawLeftDefence + newSubset.RawLeftDefence,
            baseline.RawCentralDefence - oldSubset.RawCentralDefence + newSubset.RawCentralDefence,
            baseline.RawRightDefence - oldSubset.RawRightDefence + newSubset.RawRightDefence,
            baseline.RawMidfield - oldSubset.RawMidfield + newSubset.RawMidfield,
            baseline.RawLeftAttack - oldSubset.RawLeftAttack + newSubset.RawLeftAttack,
            baseline.RawCentralAttack - oldSubset.RawCentralAttack + newSubset.RawCentralAttack,
            baseline.RawRightAttack - oldSubset.RawRightAttack + newSubset.RawRightAttack);
    }

    private static RegionalRatingSnapshot Rebuild(
        RegionalRatingSnapshot rating,
        Func<double, double> ld, Func<double, double> cd, Func<double, double> rd,
        Func<double, double> mid, Func<double, double> la, Func<double, double> ca, Func<double, double> ra)
        => ToSnapshot(ld(rating.RawLeftDefence), cd(rating.RawCentralDefence), rd(rating.RawRightDefence),
            mid(rating.RawMidfield), la(rating.RawLeftAttack), ca(rating.RawCentralAttack), ra(rating.RawRightAttack));

    private static RegionalRatingSnapshot ToSnapshot(double ld, double cd, double rd, double mid, double la, double ca, double ra)
        => new(ld, cd, rd, mid, la, ca, ra,
            QuarterDisplay(ld), QuarterDisplay(cd), QuarterDisplay(rd), QuarterDisplay(mid),
            QuarterDisplay(la), QuarterDisplay(ca), QuarterDisplay(ra));

    private static double QuarterDisplay(double raw)
    {
        if (!double.IsFinite(raw) || raw <= 0) return 0;
        var rounded = Math.Round(raw * 4.0, MidpointRounding.AwayFromZero) / 4.0;
        return Math.Clamp(Math.Max(1.0, rounded), 1.0, 20.0);
    }

    private static RegionalPlayer ToRegionalPlayer(string formation, Slot slot, Player p)
    {
        var position = RatingPositionResolver.Resolve(formation, slot.Code);
        var side = slot.Code.EndsWith("-L", StringComparison.Ordinal) ? PlayerSide.Left
            : slot.Code.EndsWith("-R", StringComparison.Ordinal) ? PlayerSide.Right : PlayerSide.Center;
        return new RegionalPlayer(p.Id, position, side, slot.Order,
            p.Keeper, p.Defending, p.Playmaking, p.Passing, p.Winger, p.Scoring,
            p.Form, p.Loyalty, p.Experience, p.Stamina);
    }
}
