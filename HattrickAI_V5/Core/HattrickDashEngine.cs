using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Stage-3 implementation of the rating logic used by HattrickDash's lineup
/// service. HattrickDash currently exposes a simplified position estimate:
/// primary positional skill 70%, form 20%, stamina 10%, then averages those
/// estimates into midfield/defence/attack team scores.
///
/// The source project documents this as a local lineup/analytics calculation;
/// it is deliberately kept separate from V5 and HO.
/// </summary>
public sealed class HattrickDashEngine : IRatingEngine
{
    public RatingEngineKind Kind => RatingEngineKind.HattrickDash;
    public string Name => "HattrickDash";

    public RatingEngineResult Calculate(RatingEngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Lineup.Slots.Count != 11)
            throw new ArgumentException("HattrickDash engine requires exactly eleven lineup slots.", nameof(request));

        var byId = request.Players.ToDictionary(p => p.Id);
        var placements = request.Lineup.Slots.Select(slot =>
        {
            if (slot.PlayerId <= 0 || !byId.TryGetValue(slot.PlayerId, out var player))
                throw new ArgumentException($"HattrickDash cannot resolve player {slot.PlayerId} for slot {slot.Code}.", nameof(request));
            return new Placement(slot.Code, Estimate(player, slot.Code));
        }).ToList();

        var midfield = Average(placements.Where(x => IsMidfield(x.Role)).Select(x => x.Rating));
        var defence = Average(placements.Where(x => IsDefence(x.Role)).Select(x => x.Rating));
        var attack = Average(placements.Where(x => IsAttack(x.Role)).Select(x => x.Rating));

        // HattrickDash itself exposes aggregate mid/def/att values. The common
        // contract requires seven sectors, so the aggregate is normalized into
        // each corresponding sector without inventing side-specific coefficients.
        var snapshot = new RegionalRatingSnapshot(
            defence, defence, defence, midfield,
            attack, attack, attack,
            defence, defence, defence, midfield,
            attack, attack, attack);

        var hatStats = 3.0 * (midfield + attack + defence);
        var loddarStats = midfield + ((attack + defence) / 2.0);

        return new RatingEngineResult(Kind, snapshot, hatStats, loddarStats);
    }

    private static double Estimate(Player p, string role)
    {
        var primary = role switch
        {
            "GK" => p.Keeper,
            "DEF-L" or "DEF-CL" or "DEF-C" or "DEF-CR" or "DEF-R" => p.Defending,
            "IM-L" or "IM-C" or "IM-R" => p.Playmaking,
            "W-L" or "W-R" => p.Winger,
            "FW-L" or "FW-C" or "FW-R" => p.Scoring,
            _ => throw new ArgumentException($"Unsupported HattrickDash slot code: {role}", nameof(role))
        };

        return (primary * 0.70) + (p.Form * 0.20) + (p.Stamina * 0.10);
    }

    private static bool IsDefence(string role)
        => role is "DEF-L" or "DEF-CL" or "DEF-C" or "DEF-CR" or "DEF-R";

    private static bool IsMidfield(string role)
        => role is "W-L" or "IM-L" or "IM-C" or "IM-R" or "W-R";

    private static bool IsAttack(string role)
        => role is "FW-L" or "FW-C" or "FW-R";

    private static double Average(IEnumerable<double> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? 0 : list.Average();
    }

    private sealed record Placement(string Role, double Rating);
}
