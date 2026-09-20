using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Single source of truth for V5 Hattrick position overcrowding.
///
/// HO!/Schum applies overcrowding by LINEUP SECTOR, not by the individual
/// left/center/right slot. Therefore:
///   CD  = DEF-CL + DEF-C + DEF-CR
///   IM  = IM-L + IM-C + IM-R
///   FW  = FW-L + FW-C + FW-R
///
/// Only these three central sectors have a positional overcrowding penalty.
/// GK, WB-L/WB-R and W-L/W-R always use 1.0.
///
/// The factor is applied to the player's skill contribution only; experience
/// remains outside the penalty and is added after crowding.
/// </summary>
public static class RatingCrowding
{
    public const double NoPenalty = 1.0;

    // Exact HO!/Schum overcrowding map.
    public const double CentralDefenderTwo = .964;
    public const double CentralDefenderThree = .900;

    public const double InnerMidfielderTwo = .935;
    public const double InnerMidfielderThree = .825;

    public const double ForwardTwo = .945;
    public const double ForwardThree = .865;

    /// <summary>
    /// Calculates the complete crowding state once for the active lineup.
    /// Empty/placeholder players (Id <= 0) do not contribute to the count.
    /// </summary>
    public static RatingCrowdingState Evaluate(IReadOnlyList<RegionalPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        var centralDefenders = 0;
        var innerMidfielders = 0;
        var forwards = 0;

        foreach (var player in players)
        {
            if (player.Id <= 0)
                continue;

            switch (CanonicalGroup(RatingPositionMatrix.CanonicalSlot(player)))
            {
                case "CD":
                    centralDefenders++;
                    break;
                case "IM":
                    innerMidfielders++;
                    break;
                case "FW":
                    forwards++;
                    break;
            }
        }

        return new RatingCrowdingState(
            centralDefenders,
            innerMidfielders,
            forwards);
    }

    /// <summary>
    /// Convenience API: calculates the state and returns the factor for one slot.
    /// </summary>
    public static double GetMultiplier(string slot, IReadOnlyList<RegionalPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);
        return Evaluate(players).ForSlot(slot);
    }

    /// <summary>
    /// Direct count lookup used by regression tests/diagnostics.
    /// For the HO! map every other count (0, 1, 4+) is exactly 1.0.
    /// </summary>
    public static double GetMultiplier(string group, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        return group switch
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
    }

    /// <summary>
    /// Maps a canonical 14-slot position to the overcrowding sector.
    /// Uncrowded positions return null.
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

/// <summary>
/// Immutable crowding state for one active lineup.
/// Counts are sector-wide, so L/C/R variants in a central sector share one
/// multiplier.
/// </summary>
public sealed record RatingCrowdingState(
    int CentralDefenders,
    int InnerMidfielders,
    int Forwards)
{
    public double ForSlot(string slot)
    {
        var group = RatingCrowding.CanonicalGroup(slot);
        return group is null
            ? RatingCrowding.NoPenalty
            : RatingCrowding.GetMultiplier(group, GroupCount(group));
    }

    public int GroupCount(string group)
        => group switch
        {
            "CD" => CentralDefenders,
            "IM" => InnerMidfielders,
            "FW" => Forwards,
            _ => 0
        };

    /// <summary>
    /// Returns factors for all 14 canonical field slots.
    /// </summary>
    public IReadOnlyDictionary<string, double> AllSlotMultipliers()
    {
        var result = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var slot in RatingPositionMatrix.CanonicalSlots)
            result[slot] = ForSlot(slot);
        return result;
    }
}
