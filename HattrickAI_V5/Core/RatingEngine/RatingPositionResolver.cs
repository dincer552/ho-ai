using System;

namespace HattrickAI.V5.Core;

/// <summary>
/// Resolves a lineup slot into its broad tactical role. The exact 14-slot
/// identity is preserved separately by Slot.Code and carried into V5 rating
/// calculation through RegionalPlayer.SlotCode.
/// </summary>
public static class RatingPositionResolver
{
    public static RegionalPosition Resolve(string? formation, string slotCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(slotCode);

        return slotCode switch
        {
            "GK" => RegionalPosition.Goalkeeper,
            "WB-L" or "WB-R" => RegionalPosition.WingBack,
            "DEF-CL" or "DEF-C" or "DEF-CR" => RegionalPosition.CentralDefender,
            // Legacy aliases are central-defender side slots. New lineups should
            // use WB-L/WB-R when a wing-back is intended.
            "DEF-L" or "DEF-R" => RegionalPosition.CentralDefender,
            "W-L" or "W-R" => RegionalPosition.Winger,
            "IM-L" or "IM-C" or "IM-R" => RegionalPosition.InnerMidfielder,
            "FW-L" or "FW-C" or "FW-R" => RegionalPosition.Forward,
            _ => throw new ArgumentException($"Unknown V5 slot code: {slotCode}", nameof(slotCode))
        };
    }

    public static RegionalPosition Resolve(Lineup lineup, string slotCode)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        return Resolve(lineup.Formation, slotCode);
    }
}
