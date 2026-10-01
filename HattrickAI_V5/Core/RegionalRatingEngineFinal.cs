using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Production rating entry point. Delegates to HatFor (Excel formation-specific
/// coefficient tables). Old RegionalRatingEngineFixed path is no longer used.
/// </summary>
public sealed class RegionalRatingEngineFinal
{
    private readonly HatForRatingEngine _hatFor = new();

    public RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context = null)
        => CalculateLineupFromRegional(players, context);

    public RegionalRatingSnapshot Calculate(
        IReadOnlyList<RegionalPlayer> players,
        RatingContext? context,
        HOEngineContext? engineContext)
        => CalculateLineupFromRegional(players, context, engineContext);

    /// <summary>
    /// RegionalPlayer list path: rebuild a synthetic Lineup so HatFor formation
    /// tables still apply when only RegionalPlayer data is available.
    /// </summary>
    private RegionalRatingSnapshot CalculateLineupFromRegional(
        IReadOnlyList<RegionalPlayer> players,
        RatingContext? context,
        HOEngineContext? engineContext = null)
    {
        context ??= RatingContext.Default;
        var slots = players
            .Where(p => p.Id > 0)
            .Select(p => new Slot(
                string.IsNullOrWhiteSpace(p.SlotCode) ? GuessCode(p) : p.SlotCode!,
                p.SlotCode ?? "",
                "",
                null,
                p.Id,
                0,
                0,
                0,
                p.Order))
            .ToList();

        var lineupPlayers = players.Select(ToPlayer).ToList();
        var formation = InferFormation(slots);
        var lineup = new Lineup("team", formation, slots);
        var request = new RatingEngineRequest(lineup, lineupPlayers, context, null, engineContext);
        return _hatFor.Calculate(request).Rating;
    }

    public RegionalRatingSnapshot CalculateLineup(
        Lineup lineup,
        IReadOnlyList<Player> players,
        RatingContext? context = null,
        HOEngineContext? engineContext = null)
    {
        context ??= RatingContext.Default;
        var request = new RatingEngineRequest(lineup, players, context, null, engineContext);
        return _hatFor.Calculate(request).Rating;
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

    private static Player ToPlayer(RegionalPlayer p) => new(
        p.Id,
        $"P{p.Id}",
        (int)Math.Round(p.Keeper),
        (int)Math.Round(p.Defending),
        (int)Math.Round(p.Playmaking),
        (int)Math.Round(p.Passing),
        (int)Math.Round(p.Winger),
        (int)Math.Round(p.Scoring),
        (int)Math.Round(p.Stamina),
        (int)Math.Round(p.Form),
        (int)Math.Round(p.Experience),
        (int)Math.Round(p.Loyalty));

    private static string GuessCode(RegionalPlayer p)
    {
        var side = p.Side switch
        {
            PlayerSide.Left => "L",
            PlayerSide.Right => "R",
            _ => "C"
        };
        return p.Position switch
        {
            RegionalPosition.Goalkeeper => "GK",
            RegionalPosition.CentralDefender => side == "C" ? "DEF-C" : (side == "L" ? "DEF-CL" : "DEF-CR"),
            RegionalPosition.WingBack => side == "L" ? "WB-L" : "WB-R",
            RegionalPosition.InnerMidfielder => side == "C" ? "IM-C" : $"IM-{side}",
            RegionalPosition.Winger => side == "L" ? "W-L" : "W-R",
            RegionalPosition.Forward => side == "C" ? "FW-C" : $"FW-{side}",
            _ => "IM-C"
        };
    }

    private static string InferFormation(IReadOnlyList<Slot> slots)
    {
        int def = slots.Count(s => s.Code.StartsWith("DEF", StringComparison.OrdinalIgnoreCase)
                                || s.Code.StartsWith("WB", StringComparison.OrdinalIgnoreCase));
        int mid = slots.Count(s => s.Code.StartsWith("IM", StringComparison.OrdinalIgnoreCase)
                                || s.Code.StartsWith("W-", StringComparison.OrdinalIgnoreCase));
        int fw = slots.Count(s => s.Code.StartsWith("FW", StringComparison.OrdinalIgnoreCase));
        if (def + mid + fw >= 10)
            return $"{def}-{mid}-{fw}";
        return "4-4-2";
    }
}
