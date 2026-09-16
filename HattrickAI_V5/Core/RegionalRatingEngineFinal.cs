using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Empirical final V5 regional-rating wrapper.
///
/// The researched Fixed engine remains the calculation source for established
/// position/order/coefficient layers. Normal forwards keep the empirically
/// verified L/C/R slot symmetry. Match-level factors that were previously
/// present only in the separate HO path are applied here when HOEngineContext
/// is available: weather, team spirit, confidence and coach style.
/// </summary>
public sealed class RegionalRatingEngineFinal
{
    private readonly RegionalRatingEngineFixed _inner = new();

    public RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context = null)
        => Calculate(players, context, null);

    public RegionalRatingSnapshot Calculate(
        IReadOnlyList<RegionalPlayer> players,
        RatingContext? context,
        HOEngineContext? engineContext)
    {
        context ??= RatingContext.Default;
        var baseline = ApplyExtraContext(_inner.Calculate(players, context), engineContext);

        var normalForwards = players
            .Where(p => p.Position == RegionalPosition.Forward && p.Order == PlayerOrder.Normal)
            .ToList();
        if (normalForwards.Count == 0)
            return baseline;

        var totalForwards = players.Count(p => p.Position == RegionalPosition.Forward);
        var dummies = CreateForwardDummies(totalForwards - normalForwards.Count);

        // Keep the same forward-sector crowding count while isolating only the
        // normal-forward side-routing contribution.
        var currentNormal = ApplyExtraContext(
            _inner.Calculate(normalForwards.Concat(dummies).ToList(), context),
            engineContext);

        // Empirical invariant from the supplied singleton screenshots:
        // normal FW-L/FW-C/FW-R produce the same seven-sector attack profile.
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
        var mapped = lineup.Slots
            .Where(s => s.PlayerId > 0 && byId.ContainsKey(s.PlayerId))
            .Select(s => ToRegionalPlayer(lineup.Formation, s, PrepareWeatherPlayer(byId[s.PlayerId], engineContext)))
            .ToList();
        return Calculate(mapped, context, engineContext);
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
            Keeper = p.Keeper * multiplier,
            Defending = p.Defending * multiplier,
            Playmaking = p.Playmaking * multiplier,
            Passing = p.Passing * multiplier,
            Winger = p.Winger * multiplier,
            Scoring = p.Scoring * multiplier
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
            .Select(i => new RegionalPlayer(
                -i,
                RegionalPosition.Forward,
                PlayerSide.Center,
                PlayerOrder.Normal,
                0, 0, 0, 0, 0, 0,
                1, 0, 0, 9.4))
            .ToArray();
    }

    private static RegionalRatingSnapshot ReplaceSubset(
        RegionalRatingSnapshot baseline,
        RegionalRatingSnapshot oldSubset,
        RegionalRatingSnapshot newSubset)
    {
        var ld = baseline.RawLeftDefence - oldSubset.RawLeftDefence + newSubset.RawLeftDefence;
        var cd = baseline.RawCentralDefence - oldSubset.RawCentralDefence + newSubset.RawCentralDefence;
        var rd = baseline.RawRightDefence - oldSubset.RawRightDefence + newSubset.RawRightDefence;
        var mid = baseline.RawMidfield - oldSubset.RawMidfield + newSubset.RawMidfield;
        var la = baseline.RawLeftAttack - oldSubset.RawLeftAttack + newSubset.RawLeftAttack;
        var ca = baseline.RawCentralAttack - oldSubset.RawCentralAttack + newSubset.RawCentralAttack;
        var ra = baseline.RawRightAttack - oldSubset.RawRightAttack + newSubset.RawRightAttack;
        return ToSnapshot(ld, cd, rd, mid, la, ca, ra);
    }

    private static RegionalRatingSnapshot Rebuild(
        RegionalRatingSnapshot rating,
        Func<double, double> ld,
        Func<double, double> cd,
        Func<double, double> rd,
        Func<double, double> mid,
        Func<double, double> la,
        Func<double, double> ca,
        Func<double, double> ra)
        => ToSnapshot(
            ld(rating.RawLeftDefence), cd(rating.RawCentralDefence), rd(rating.RawRightDefence),
            mid(rating.RawMidfield), la(rating.RawLeftAttack), ca(rating.RawCentralAttack), ra(rating.RawRightAttack));

    private static RegionalRatingSnapshot ToSnapshot(
        double ld, double cd, double rd, double mid, double la, double ca, double ra)
        => new(
            ld, cd, rd, mid, la, ca, ra,
            RegionalRatingEngine.Display(ld),
            RegionalRatingEngine.Display(cd),
            RegionalRatingEngine.Display(rd),
            RegionalRatingEngine.Display(mid),
            RegionalRatingEngine.Display(la),
            RegionalRatingEngine.Display(ca),
            RegionalRatingEngine.Display(ra));

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
            p.Form, p.Loyalty, p.Experience, p.Stamina);
    }
}
