namespace HattrickAI.V5.Core;

/// <summary>
/// Bridges the V5 internal tactical-strength scale (0-10) to the tactic-rating
/// scale used by the 2026 Hattrick paper's Equation B.2.
///
/// The paper does not publish a mapping from V5's compact 0-10 scale to its RT
/// scale. V5 therefore uses tactic-specific calibration anchors where the paper
/// publishes a bounded conversion range. These anchors are explicitly V5 bridges,
/// not hidden official live-engine formulas.
/// </summary>
public static class TacticPaperMappingEngine
{
    public const double V5InternalMax = 10.0;

    // LS keeps the explicit V5 -> paper RT bridge used by the existing linear
    // paper curve. The paper's broader empirical 6%-43% range is not claimed to
    // be the endpoint of this linear regression over V5's compact scale.
    public const double PaperRtMax = 20.0;
    public const double PaperRtPerV5Level = PaperRtMax / V5InternalMax;

    // Source-derived Pressing suppression endpoints from the 2026 paper.
    public const double PressingPaperRtAtMinSuppression = 2.56395572;
    public const double PressingPaperRtAtMaxSuppression = 3.41147624;
    public const double PressingMinSuppression = 0.05;
    public const double PressingMaxSuppression = 0.41;

    // The paper's B.2 curves reach these published tactic-conversion ranges at
    // the following RT values. V5 linearly bridges its 0-10 internal level between
    // those anchors so V5's documented acceptance envelope is not silently violated.
    public const double AiMPaperRtAtMinConversion = 6.694339785602811;
    public const double AiMPaperRtAtMaxConversion = 18.738397936525278;
    public const double AiMMinConversion = 0.20;
    public const double AiMMaxConversion = 0.35;

    public const double AoWPaperRtAtMinConversion = 9.59443018801709;
    public const double AoWPaperRtAtMaxConversion = 22.41552295010873;
    public const double AoWMinConversion = 0.34;
    public const double AoWMaxConversion = 0.52;

    public const double CounterAttackPaperRtAtMinConversion = 8.573130611600549;
    public const double CounterAttackPaperRtAtMaxConversion = 28.731396162869075;
    public const double CounterAttackMinConversion = 0.04;
    public const double CounterAttackMaxConversion = 0.45;

    public static double ToPaperRt(double v5TacticalLevel)
        => Math.Clamp(v5TacticalLevel, 0.0, V5InternalMax) * PaperRtPerV5Level;

    public static double ToPressingPaperRt(double v5TacticalLevel)
        => BridgeRt(v5TacticalLevel, PressingPaperRtAtMinSuppression, PressingPaperRtAtMaxSuppression);

    public static double ToAiMPaperRt(double v5TacticalLevel)
        => BridgeRt(v5TacticalLevel, AiMPaperRtAtMinConversion, AiMPaperRtAtMaxConversion);

    public static double ToAoWPaperRt(double v5TacticalLevel)
        => BridgeRt(v5TacticalLevel, AoWPaperRtAtMinConversion, AoWPaperRtAtMaxConversion);

    public static double ToCounterAttackPaperRt(double v5TacticalLevel)
        => BridgeRt(v5TacticalLevel, CounterAttackPaperRtAtMinConversion, CounterAttackPaperRtAtMaxConversion);

    public static double PaperTacticConversionRate(AdvancedTactic tactic, double v5TacticalLevel)
    {
        var paperRt = tactic switch
        {
            AdvancedTactic.Pressing => ToPressingPaperRt(v5TacticalLevel),
            AdvancedTactic.AttackMiddle => ToAiMPaperRt(v5TacticalLevel),
            AdvancedTactic.AttackWings => ToAoWPaperRt(v5TacticalLevel),
            AdvancedTactic.CounterAttack => ToCounterAttackPaperRt(v5TacticalLevel),
            _ => ToPaperRt(v5TacticalLevel)
        };

        var result = M8ChanceAllocationEngine.CalculateTacticConversionRateFromPaperRt(tactic, paperRt);
        return tactic switch
        {
            AdvancedTactic.Pressing => Math.Clamp(result, PressingMinSuppression, PressingMaxSuppression),
            AdvancedTactic.AttackMiddle => Math.Clamp(result, AiMMinConversion, AiMMaxConversion),
            AdvancedTactic.AttackWings => Math.Clamp(result, AoWMinConversion, AoWMaxConversion),
            AdvancedTactic.CounterAttack => Math.Clamp(result, CounterAttackMinConversion, CounterAttackMaxConversion),
            _ => result
        };
    }

    private static double BridgeRt(double v5TacticalLevel, double minRt, double maxRt)
    {
        var fraction = Math.Clamp(v5TacticalLevel, 0.0, V5InternalMax) / V5InternalMax;
        return minRt + ((maxRt - minRt) * fraction);
    }
}
