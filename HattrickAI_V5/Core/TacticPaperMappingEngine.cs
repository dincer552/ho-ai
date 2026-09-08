namespace HattrickAI.V5.Core;

/// <summary>
/// Bridges the V5 internal tactical-strength scale (0-10) to the tactic-rating
/// scale used by the 2026 Hattrick paper's Equation B.2.
///
/// V5 keeps a compact 0-10 internal scale. The paper's regression uses its own
/// tactic-rating RT. For pressing, the paper reports an empirical 5%-41%
/// suppression range; the quadratic in B.2 reaches those endpoints at RT values
/// 2.56395572 and 3.41147624. We therefore use a dedicated pressing RT bridge
/// so V5 does not extrapolate the pressing curve to an impossible 100% suppression.
/// Other tactics retain the explicit RT = V5 * 2 bridge.
/// </summary>
public static class TacticPaperMappingEngine
{
    public const double V5InternalMax = 10.0;
    public const double PaperRtMax = 20.0;
    public const double PaperRtPerV5Level = PaperRtMax / V5InternalMax;

    public const double PressingPaperRtAtMinSuppression = 2.56395572;
    public const double PressingPaperRtAtMaxSuppression = 3.41147624;
    public const double PressingMinSuppression = 0.05;
    public const double PressingMaxSuppression = 0.41;

    public static double ToPaperRt(double v5TacticalLevel)
        => Math.Clamp(v5TacticalLevel, 0.0, V5InternalMax) * PaperRtPerV5Level;

    public static double ToPressingPaperRt(double v5TacticalLevel)
    {
        var level = Math.Clamp(v5TacticalLevel, 0.0, V5InternalMax);
        var fraction = level / V5InternalMax;
        return PressingPaperRtAtMinSuppression +
               (PressingPaperRtAtMaxSuppression - PressingPaperRtAtMinSuppression) * fraction;
    }

    public static double PaperTacticConversionRate(AdvancedTactic tactic, double v5TacticalLevel)
    {
        var paperRt = tactic == AdvancedTactic.Pressing
            ? ToPressingPaperRt(v5TacticalLevel)
            : ToPaperRt(v5TacticalLevel);
        var result = M8ChanceAllocationEngine.CalculateTacticConversionRateFromPaperRt(tactic, paperRt);
        return tactic == AdvancedTactic.Pressing
            ? Math.Clamp(result, PressingMinSuppression, PressingMaxSuppression)
            : result;
    }
}
