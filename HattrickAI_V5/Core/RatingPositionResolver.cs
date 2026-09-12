using System;

namespace HattrickAI.V5.Core;

/// <summary>
/// Resolves the tactical meaning of a lineup slot before rating contributions are calculated.
/// Slot codes alone are not sufficient: DEF-L/DEF-R are central defenders in three-defender
/// formations but wing-backs in four-, five- and the locked two-defender variant.
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
            "DEF-L" or "DEF-R" => IsThreeDefenderFormation(formation)
                ? RegionalPosition.CentralDefender
                : RegionalPosition.WingBack,
            "W-L" or "W-R" => RegionalPosition.Winger,
            "IM-L" or "IM-C" or "IM-R" => RegionalPosition.InnerMidfielder,
            "FW-L" or "FW-C" or "FW-R" => RegionalPosition.Forward,
            _ => RegionalPosition.InnerMidfielder
        };
    }

    private static bool IsThreeDefenderFormation(string? formation)
    {
        var normalized = formation?.Trim();
        return !string.IsNullOrEmpty(normalized)
            && normalized.StartsWith("3-", StringComparison.Ordinal);
    }
}
