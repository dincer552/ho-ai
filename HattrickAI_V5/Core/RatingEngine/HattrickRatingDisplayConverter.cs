using System;

namespace HattrickAI.V5.Core;

/// <summary>
/// Converts a sector raw contribution value into the Hattrick/HO-style
/// displayed regional rating. Stage 2 deliberately isolates this layer from
/// player contribution coefficients so the raw ledger and display conversion
/// can be validated independently.
///
/// Reference structure: sector scale followed by the nonlinear
/// pow(x, 1.2) / 4 + 1 conversion used by the researched HO Organizer model.
/// This is a community/reverse-engineered reference, not claimed as official
/// Hattrick source code.
/// </summary>
public static class HattrickRatingDisplayConverter
{
    private const double Exponent = 1.2;
    private const double Divisor = 4.0;
    private const double Offset = 1.0;

    public static double ToDisplay(RatingSector sector, double raw)
    {
        if (!double.IsFinite(raw) || raw <= 0)
            return Offset;

        var scaled = raw * SectorScale(sector);
        if (scaled <= 0)
            return Offset;

        var displayed = Math.Pow(scaled, Exponent) / Divisor + Offset;
        return Math.Clamp(Math.Round(displayed, 2, MidpointRounding.AwayFromZero), 1.0, 20.0);
    }

    public static double SectorScale(RatingSector sector) => sector switch
    {
        RatingSector.Midfield => .312,
        RatingSector.CentralDefence => .501,
        RatingSector.LeftDefence => .834,
        RatingSector.RightDefence => .834,
        RatingSector.CentralAttack => .513,
        RatingSector.LeftAttack => .615,
        RatingSector.RightAttack => .615,
        _ => throw new ArgumentOutOfRangeException(nameof(sector), sector, null)
    };

    public static double Nonlinear(double scaledRaw)
    {
        if (!double.IsFinite(scaledRaw) || scaledRaw <= 0)
            return Offset;
        return Math.Pow(scaledRaw, Exponent) / Divisor + Offset;
    }
}
