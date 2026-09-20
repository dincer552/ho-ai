using System.Collections.Generic;

namespace HattrickAI.V5.Core;

/// <summary>
/// Stage 2 adapter: preserves the existing raw contribution engine and
/// replaces only the raw -> displayed-rating conversion with the researched
/// sector-specific nonlinear conversion.
/// </summary>
public sealed class Stage2RegionalRatingEngine
{
    private readonly RegionalRatingEngine _inner = new();

    public RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context = null)
        => Convert(_inner.Calculate(players, context));

    public RegionalRatingSnapshot CalculateLineup(Lineup lineup, IReadOnlyList<Player> players, RatingContext? context = null)
        => Convert(_inner.CalculateLineup(lineup, players, context));

    public RegionalRatingPair CalculatePair(
        Lineup ownLineup,
        IReadOnlyList<Player> ownPlayers,
        Lineup opponentLineup,
        IReadOnlyList<Player> opponentPlayers,
        RatingContext? ownContext = null,
        RatingContext? opponentContext = null)
        => new(CalculateLineup(ownLineup, ownPlayers, ownContext), CalculateLineup(opponentLineup, opponentPlayers, opponentContext));

    public static RegionalRatingSnapshot Convert(RegionalRatingSnapshot raw) => new(
        raw.RawLeftDefence, raw.RawCentralDefence, raw.RawRightDefence,
        raw.RawMidfield, raw.RawLeftAttack, raw.RawCentralAttack, raw.RawRightAttack,
        HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftDefence, raw.RawLeftDefence),
        HattrickRatingDisplayConverter.ToDisplay(RatingSector.CentralDefence, raw.RawCentralDefence),
        HattrickRatingDisplayConverter.ToDisplay(RatingSector.RightDefence, raw.RawRightDefence),
        HattrickRatingDisplayConverter.ToDisplay(RatingSector.Midfield, raw.RawMidfield),
        HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftAttack, raw.RawLeftAttack),
        HattrickRatingDisplayConverter.ToDisplay(RatingSector.CentralAttack, raw.RawCentralAttack),
        HattrickRatingDisplayConverter.ToDisplay(RatingSector.RightAttack, raw.RawRightAttack));
}

/// <summary>Stage 2 display-conversion adapter for the current Fixed engine.</summary>
public sealed class Stage2RegionalRatingEngineFixed
{
    private readonly RegionalRatingEngineFixed _inner = new();

    public RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context = null)
        => Stage2RegionalRatingEngine.Convert(_inner.Calculate(players, context));

    public RegionalRatingSnapshot CalculateLineup(Lineup lineup, IReadOnlyList<Player> players, RatingContext? context = null)
        => Stage2RegionalRatingEngine.Convert(_inner.CalculateLineup(lineup, players, context));

    public RegionalRatingPair CalculatePair(
        Lineup ownLineup,
        IReadOnlyList<Player> ownPlayers,
        Lineup opponentLineup,
        IReadOnlyList<Player> opponentPlayers,
        RatingContext? ownContext = null,
        RatingContext? opponentContext = null)
        => new(CalculateLineup(ownLineup, ownPlayers, ownContext), CalculateLineup(opponentLineup, opponentPlayers, opponentContext));
}
