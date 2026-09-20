using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Single source of truth for V5 Hattrick position overcrowding.
/// Crowding belongs to the player's own lineup role and is applied only
/// to the skill contribution. Experience remains outside the penalty.
/// </summary>
public static class RatingCrowding
{
    public const double NoPenalty = 1.0;

    public const double CentralDefenderTwo = .964;
    public const double CentralDefenderThree = .900;

    public const double InnerMidfielderTwo = .935;
    public const double InnerMidfielderThree = .825;

    public const double ForwardTwo = .945;
    public const double ForwardThree = .865;

    /// <summary>
    /// Returns the crowding multiplier for a player based on the number of
    /// players occupying the same crowded positional group.
    /// WB and W positions are intentionally not crowded by CD/IM/FW counts.
    /// </summary>
    public static double GetMultiplier(string slot, IReadOnlyList<RegionalPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        var canonical = CanonicalGroup(slot);
        if (canonical is null)
            return NoPenalty;

        var count = players.Count(p => CanonicalGroup(RatingPositionMatrix.CanonicalSlot(p)) == canonical);
        return GetMultiplier(canonical, count);
    }

    /// <summary>
    /// Direct lookup used by regression tests and diagnostics.
    /// </summary>
    public static double GetMultiplier(string group, int count)
        => group switch
        {
            "CD" => count switch
            {
                2 => CentralDefenderTwo,
                3 => CentralDefenderThree,
                _ => NoPenalty
            },
            "IM" => count switch
            {
                2 => InnerMidfielderTwo,
                3 => InnerMidfielderThree,
                _ => NoPenalty
            },
            "FW" => count switch
            {
                2 => ForwardTwo,
                3 => ForwardThree,
                _ => NoPenalty
            },
            _ => NoPenalty
        };

    /// <summary>
    /// Maps a canonical 14-slot position to its crowding group.
    /// </summary>
    public static string? CanonicalGroup(string slot)
        => slot switch
        {
            "DEF-CL" or "DEF-C" or "DEF-CR" => "CD",
            "IM-L" or "IM-C" or "IM-R" => "IM",
            "FW-L" or "FW-C" or "FW-R" => "FW",
            _ => null
        };
}
