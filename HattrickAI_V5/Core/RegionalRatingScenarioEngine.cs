using System;
using System.Collections.Generic;

namespace HattrickAI.V5.Core;

/// <summary>
/// M7 scenario layer. Production path uses HatFor for all lineup ratings.
/// </summary>
public sealed class RegionalRatingScenarioEngine
{
    private readonly RegionalRatingEngineFinal _hatForFinal = new();
    private readonly RatingEngineRegistry _ratingEngines = new();

    public RatingScenarioResult Calculate(IReadOnlyList<RegionalPlayer> players, MatchState state)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(state);

        var context = BuildRatingContext(state);
        // Always HatFor — no Stage2 path
        var baseRating = _hatForFinal.Calculate(players, context, BuildHOContext(state));
        var adjusted = ApplyQuestionnaireContext(baseRating, state);

        return new RatingScenarioResult(adjusted, state, RatingConfidence.High, BuildModifiers(state));
    }

    public RatingScenarioResult CalculateLineup(Lineup lineup, IReadOnlyList<Player> players, MatchState state)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(state);

        var context = BuildRatingContext(state);
        var selected = RatingEngineSelectionContext.Selected;
        var request = new RatingEngineRequest(
            lineup,
            players,
            context,
            HOContext: BuildHOContext(state));

        // Registry resolves every kind to HatFor
        var baseRating = _ratingEngines.Calculate(selected, request).Rating;

        return new RatingScenarioResult(baseRating, state, RatingConfidence.High, BuildModifiers(state));
    }

    private static RatingContext BuildRatingContext(MatchState state)
        => new(state.MatchLocation, state.TeamAttitude, state.TeamTactic)
        {
            MatchMinute = state.MatchMinute,
            GoalDifference = state.GoalDifference,
            IgnoreLeadRetreat = state.IgnoreLeadRetreat
        };

    private static HOEngineContext BuildHOContext(MatchState state)
        => new(
            state.TeamSpirit,
            state.Confidence,
            state.CoachStyle,
            TacticLevel: 1,
            CoachModifier: 0,
            Weather: 0);

    public static double TeamSpiritMultiplier(double teamSpirit)
    {
        if (teamSpirit <= 0) return 1.0;
        return 0.10 + 0.425 * Math.Sqrt(Math.Clamp(teamSpirit, 0.0, 10.0));
    }

    public static (double AttackMultiplier, double DefenceMultiplier) CoachStyleMultipliers(CoachStyle coach)
        => coach switch
        {
            CoachStyle.Offensive => (1.08, 0.89),
            CoachStyle.Defensive => (0.92, 1.14),
            _ => (1.0, 1.0)
        };

    private static RegionalRatingSnapshot ApplyQuestionnaireContext(RegionalRatingSnapshot rating, MatchState state)
    {
        var withSpirit = ApplyTeamSpirit(rating, state.TeamSpirit);
        var (attack, defence) = CoachStyleMultipliers(state.CoachStyle);

        return Rebuild(
            withSpirit,
            ld => ld * defence,
            cd => cd * defence,
            rd => rd * defence,
            _ => withSpirit.RawMidfield,
            la => la * attack,
            ca => ca * attack,
            ra => ra * attack);
    }

    private static RatingModifiers BuildModifiers(MatchState state)
    {
        var (attack, defence) = CoachStyleMultipliers(state.CoachStyle);
        return new RatingModifiers(
            TeamSpiritMultiplier(state.TeamSpirit),
            state.MatchLocation,
            state.TeamAttitude,
            state.TeamTactic,
            state.CoachStyle,
            attack,
            defence);
    }

    private static RegionalRatingSnapshot ApplyTeamSpirit(RegionalRatingSnapshot rating, double teamSpirit)
    {
        var factor = TeamSpiritMultiplier(teamSpirit);
        var rawMidfield = rating.RawMidfield * factor;

        return Rebuild(
            rating,
            ld => ld,
            cd => cd,
            rd => rd,
            _ => rawMidfield,
            la => la,
            ca => ca,
            ra => ra);
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
    {
        var rawLd = ld(rating.RawLeftDefence);
        var rawCd = cd(rating.RawCentralDefence);
        var rawRd = rd(rating.RawRightDefence);
        var rawMid = mid(rating.RawMidfield);
        var rawLa = la(rating.RawLeftAttack);
        var rawCa = ca(rating.RawCentralAttack);
        var rawRa = ra(rating.RawRightAttack);

        // HatFor already returns display-space quarter ratings; keep raw==display
        return new RegionalRatingSnapshot(
            rawLd, rawCd, rawRd, rawMid, rawLa, rawCa, rawRa,
            rawLd, rawCd, rawRd, rawMid, rawLa, rawCa, rawRa);
    }
}

public sealed record MatchState(
    string CandidateId,
    string FormationId,
    string LineupId,
    string BehaviourSetId,
    MatchLocation MatchLocation,
    TeamAttitude TeamAttitude,
    TeamTactic TeamTactic,
    double TeamSpirit,
    CoachStyle CoachStyle)
{
    public int MatchMinute { get; init; }
    public int GoalDifference { get; init; }
    public bool IgnoreLeadRetreat { get; init; }
    public double Confidence { get; init; } = 1.0;
}

public enum RatingConfidence { Unknown, Low, Medium, High }

public sealed record RatingModifiers(
    double TeamSpiritMultiplier,
    MatchLocation MatchLocation,
    TeamAttitude TeamAttitude,
    TeamTactic TeamTactic,
    CoachStyle CoachStyle,
    double CoachAttackMultiplier,
    double CoachDefenceMultiplier);

public sealed record RatingScenarioResult(
    RegionalRatingSnapshot Rating,
    MatchState State,
    RatingConfidence Confidence,
    RatingModifiers Modifiers);
