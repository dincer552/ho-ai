using System;

namespace HattrickAI.V5.Core;

/// <summary>
/// Resolves a lineup slot into its tactical position. The slot assignment is
/// authoritative; formation text is only a fallback for legacy callers.
/// This allows V5 to calculate arbitrary XI selections such as 3-2-3.
/// </summary>
public static class RatingPositionResolver
{
    public static RegionalPosition Resolve(string? formation, string slotCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(slotCode);

        return slotCode switch
        {
            "GK" => RegionalPosition.Goalkeeper,
            "DEF-CL" or "DEF-C" or "DEF-CR" => RegionalPosition.CentralDefender,
            "DEF-L" or "DEF-R" => IsCentralDefenderFormation(formation)
                ? RegionalPosition.CentralDefender
                : RegionalPosition.WingBack,
            "W-L" or "W-R" => RegionalPosition.Winger,
            "IM-L" or "IM-C" or "IM-R" => RegionalPosition.InnerMidfielder,
            "FW-L" or "FW-C" or "FW-R" => RegionalPosition.Forward,
            _ => RegionalPosition.InnerMidfielder
        };
    }

    /// <summary>
    /// Resolves positions from the actual selected XI rather than from a
    /// predefined formation list. DEF-L/DEF-R are central when the XI has
    /// three defensive slots, wing-backs when it has two/four/five.
    /// </summary>
    public static RegionalPosition Resolve(Lineup lineup, string slotCode)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentException.ThrowIfNullOrEmpty(slotCode);

        if (slotCode is not ("DEF-L" or "DEF-R"))
            return Resolve(lineup.Formation, slotCode);

        var defenderCount = lineup.Slots.Count(s => s.Code is "DEF-L" or "DEF-C" or "DEF-CL" or "DEF-CR" or "DEF-R");
        return defenderCount is 1 or 3
            ? RegionalPosition.CentralDefender
            : RegionalPosition.WingBack;
    }

    private static bool IsCentralDefenderFormation(string? formation)
    {
        var normalized = formation?.Trim();
        if (string.IsNullOrEmpty(normalized))
            return false;

        var separator = normalized.IndexOf('-');
        if (separator <= 0 || !int.TryParse(normalized[..separator], out var defenders))
            return normalized.StartsWith("3-", StringComparison.Ordinal);

        return defenders is 1 or 3;
    }
}
