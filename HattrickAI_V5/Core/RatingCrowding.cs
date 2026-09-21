using System;
using System.Collections.Generic;

namespace HattrickAI.V5.Core;

/// <summary>
/// Single source of truth for the Hattrick central-line overcrowding layer.
///
/// The 14 canonical slots are represented as a 14-bit occupancy mask.
/// All 2^14 = 16,384 possible slot combinations are precomputed once.
/// Crowding itself only depends on the number of central defenders,
/// inner midfielders and forwards in the active lineup.
///
/// HO!/Schum factors:
///   CD: 2=.964, 3=.900, otherwise 1.0
///   IM: 2=.935, 3=.825, otherwise 1.0
///   FW: 2=.945, 3=.865, otherwise 1.0
///
/// The factor is applied to the player's skill contribution only.
/// Experience, when/if added as a separate layer, is not crowded.
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

    public const int CombinationCount = 1 << 14;

    private static readonly IReadOnlyDictionary<string, int> SlotBits =
        BuildSlotBits();

    private static readonly RatingCrowdingState[] CombinationTable =
        BuildCombinationTable();

    /// <summary>
    /// Read-only snapshot of the complete 14-slot crowding matrix.
    /// Index = 14-bit canonical-slot occupancy mask.
    /// </summary>
    public static IReadOnlyList<RatingCrowdingState> AllCombinations => CombinationTable;

    public static RatingCrowdingState Evaluate(IReadOnlyList<RegionalPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);
        return ForMask(GetCombinationMask(players));
    }

    public static int GetCombinationMask(IReadOnlyList<RegionalPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        var mask = 0;
        foreach (var player in players)
        {
            if (player is null || player.Id <= 0)
                continue;

            var slot = RatingPositionMatrix.CanonicalSlot(player);
            if (SlotBits.TryGetValue(slot, out var bit))
                mask |= 1 << bit;
        }

        return mask;
    }

    public static RatingCrowdingState ForMask(int mask)
    {
        if ((uint)mask >= CombinationCount)
            throw new ArgumentOutOfRangeException(nameof(mask));

        return CombinationTable[mask];
    }

    public static double GetMultiplier(string slot, IReadOnlyList<RegionalPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);
        return Evaluate(players).ForSlot(slot);
    }

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

    public static string? CanonicalGroup(string slot)
        => slot switch
        {
            "DEF-CL" or "DEF-C" or "DEF-CR" => "CD",
            "IM-L" or "IM-C" or "IM-R" => "IM",
            "FW-L" or "FW-C" or "FW-R" => "FW",
            _ => null
        };

    private static IReadOnlyDictionary<string, int> BuildSlotBits()
    {
        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < RatingPositionMatrix.CanonicalSlots.Length; i++)
            result[RatingPositionMatrix.CanonicalSlots[i]] = i;
        return result;
    }

    private static RatingCrowdingState[] BuildCombinationTable()
    {
        var table = new RatingCrowdingState[CombinationCount];

        for (var mask = 0; mask < CombinationCount; mask++)
        {
            var cd = 0;
            var im = 0;
            var fw = 0;

            foreach (var slot in RatingPositionMatrix.CanonicalSlots)
            {
                var bit = SlotBits[slot];
                if ((mask & (1 << bit)) == 0)
                    continue;

                switch (CanonicalGroup(slot))
                {
                    case "CD":
                        cd++;
                        break;
                    case "IM":
                        im++;
                        break;
                    case "FW":
                        fw++;
                        break;
                }
            }

            table[mask] = new RatingCrowdingState(cd, im, fw, mask);
        }

        return table;
    }
}

public sealed record RatingCrowdingState(
    int CentralDefenders,
    int InnerMidfielders,
    int Forwards,
    int Mask = 0)
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

    public IReadOnlyDictionary<string, double> AllSlotMultipliers()
    {
        var result = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var slot in RatingPositionMatrix.CanonicalSlots)
            result[slot] = ForSlot(slot);
        return result;
    }
}
