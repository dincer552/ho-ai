using System;
using System.Collections.Generic;

namespace HattrickAI.V5.Core;

/// <summary>
/// V5.1 M7 scenario layer.
/// Uses the Stage 2 regional-rating engine so raw contribution and displayed
/// rating conversion remain separate and deterministic.
/// </summary>
public sealed class RegionalRatingScenarioEngine
{
    private readonly Stage2RegionalRatingEngine _baseEngine = new();

    public RatingScenarioResult Calculate(
        IReadOnlyList<RegionalPlayer> players,
        MatchState state)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(state);

        var context = new RatingContext(state.MatchLocation, state.TeamAttitude, state.TeamTactic)
        {
            MatchMinute = state.MatchMinute,
            GoalDifference = state.GoalDifference,
            IgnoreLeadRetreat = state.IgnoreLeadRetreat
        };

        var baseRating = _baseEngine.Calculate(players, context);
        var adjusted = ApplyQuestionnaireContext(baseRating, state);

        return new RatingScenarioResult(
            adjusted,
            state,
            RatingConfidence.High,
            BuildModifiers(state));
    }

    public RatingScenarioResult CalculateLineup(
        Lineup lineup,
        IReadOnlyList<Player> players,
        MatchState state)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(state);

        var context = new RatingContext(state.MatchLocation, state.TeamAttitude, state.TeamTactic)
        {
            MatchMinute = state.MatchMinute,
            GoalDifference = state.GoalDifference,
            IgnoreLeadRetreat = state.IgnoreLeadRetreat
        };

        var baseRating = _baseEngine.CalculateLineup(lineup, players, context);
        var adjusted = ApplyQuestionnaireContext(baseRating, state);

        return new RatingScenarioResult(
            adjusted,
            state,
            RatingConfidence.High,
            BuildModifiers(state));
    }

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

    private static RegionalRatingSnapshot ApplyQuestionnaireContext(
        RegionalRatingSnapshot rating,
        MatchState state)
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

    private static RegionalRatingSnapshot ApplyTeamSpirit(
        RegionalRatingSnapshot rating,
        double teamSpirit)
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

        return new RegionalRatingSnapshot(
            rawLd, rawCd, rawRd, rawMid, rawLa, rawCa, rawRa,
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftDefence, rawLd),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.CentralDefence, rawCd),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.RightDefence, rawRd),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.Midfield, rawMid),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftAttack, rawLa),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.CentralAttack, rawCa),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.RightAttack, rawRa));
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

public enum RatingConfidence
{
    Unknown,
    Low,
    Medium,
    High
}

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
