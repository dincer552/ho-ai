using System;

namespace HattrickAI.V5.Core;

/// <summary>
/// Resolves the tactical meaning of a lineup slot before rating contributions are calculated.
/// DEF-L/DEF-R are not always wing-backs: in a one-defender setup the lone defender is a
/// central defender, while in three-defender setups the side defenders are central defenders.
/// In two-, four- and five-defender setups the side defender slots are wing-backs.
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

    private static bool IsCentralDefenderFormation(string? formation)
    {
        var normalized = formation?.Trim();
        if (string.IsNullOrEmpty(normalized))
            return false;

        var separator = normalized.IndexOf('-');
        if (separator <= 0 || !int.TryParse(normalized[..separator], out var defenders))
            return normalized.StartsWith("3-", StringComparison.Ordinal);

        // A lone defender is the central defender. Three-defender formations use
        // three central defenders. This also keeps DEF-L/DEF-R + TowardsWing valid
        // for the 1-0-0 empirical order screenshots.
        return defenders is 1 or 3;
    }
}
