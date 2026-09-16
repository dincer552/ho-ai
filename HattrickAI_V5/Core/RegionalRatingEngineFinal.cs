using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Empirical final V5 regional-rating wrapper.
///
/// The researched Fixed engine remains the calculation source for all
/// established position/order/coefficient layers. The 2026-09-14 normal-FW
/// singleton screenshots add one high-confidence invariant: FW-L, FW-C and
/// FW-R produced the same seven-sector attack profile for the same player.
/// Therefore normal forwards are normalized to center-side for their own
/// contribution only; defensive/towards-wing forwards keep the existing
/// side-aware behavior until more screenshots isolate those orders.
/// </summary>
public sealed class RegionalRatingEngineFinal
{
    private readonly RegionalRatingEngineFixed _inner = new();

    public RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context = null)
    {
        context ??= RatingContext.Default;
        var baseline = _inner.Calculate(players, context);

        var normalForwards = players
            .Where(p => p.Position == RegionalPosition.Forward && p.Order == PlayerOrder.Normal)
            .ToList();
        if (normalForwards.Count == 0)
            return baseline;

        var totalForwards = players.Count(p => p.Position == RegionalPosition.Forward);
        var dummies = CreateForwardDummies(totalForwards - normalForwards.Count);

        // Current Fixed contribution for the normal-forward subset, with dummy
        // zero-skill forwards so overcrowding count stays identical to the full XI.
        var currentNormal = _inner.Calculate(
            normalForwards.Concat(dummies).ToList(), context);

        // Empirical invariant: normal FW is slot-symmetric across L/C/R.
        // Center-side normalization preserves all skill/form/stamina/XP/context
        // handling of the established engine while changing only the side routing.
        var empiricalNormal = _inner.Calculate(
            normalForwards.Select(p => p with { Side = PlayerSide.Center })
                .Concat(dummies)
                .ToList(), context);

        return ReplaceSubset(baseline, currentNormal, empiricalNormal);
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
        Lineup ownLineup,
        IReadOnlyList<Player> ownPlayers,
        Lineup opponentLineup,
        IReadOnlyList<Player> opponentPlayers,
        RatingContext? ownContext = null,
        RatingContext? opponentContext = null)
        => new(
            CalculateLineup(ownLineup, ownPlayers, ownContext),
            CalculateLineup(opponentLineup, opponentPlayers, opponentContext));

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
        var leftDefence = baseline.RawLeftDefence - oldSubset.RawLeftDefence + newSubset.RawLeftDefence;
        var centralDefence = baseline.RawCentralDefence - oldSubset.RawCentralDefence + newSubset.RawCentralDefence;
        var rightDefence = baseline.RawRightDefence - oldSubset.RawRightDefence + newSubset.RawRightDefence;
        var midfield = baseline.RawMidfield - oldSubset.RawMidfield + newSubset.RawMidfield;
        var leftAttack = baseline.RawLeftAttack - oldSubset.RawLeftAttack + newSubset.RawLeftAttack;
        var centralAttack = baseline.RawCentralAttack - oldSubset.RawCentralAttack + newSubset.RawCentralAttack;
        var rightAttack = baseline.RawRightAttack - oldSubset.RawRightAttack + newSubset.RawRightAttack;

        return new RegionalRatingSnapshot(
            leftDefence,
            centralDefence,
            rightDefence,
            midfield,
            leftAttack,
            centralAttack,
            rightAttack,
            RegionalRatingEngine.Display(leftDefence),
            RegionalRatingEngine.Display(centralDefence),
            RegionalRatingEngine.Display(rightDefence),
            RegionalRatingEngine.Display(midfield),
            RegionalRatingEngine.Display(leftAttack),
            RegionalRatingEngine.Display(centralAttack),
            RegionalRatingEngine.Display(rightAttack));
    }

    private static RegionalPlayer ToRegionalPlayer(string formation, Slot slot, Player p)
    {
        var position = RatingPositionResolver.Resolve(formation, slot.Code);
        var side = slot.Code.EndsWith("-L", StringComparison.Ordinal)
            ? PlayerSide.Left
            : slot.Code.EndsWith("-R", StringComparison.Ordinal)
                ? PlayerSide.Right
                : PlayerSide.Center;

        return new RegionalPlayer(
            p.Id,
            position,
            side,
            slot.Order,
            p.Keeper,
            p.Defending,
            p.Playmaking,
            p.Passing,
            p.Winger,
            p.Scoring,
            p.Form,
            p.Loyalty,
            p.Experience,
            p.Stamina);
    }
}
