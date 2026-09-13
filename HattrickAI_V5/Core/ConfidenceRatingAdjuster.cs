namespace HattrickAI.V5.Core;

/// <summary>
/// Applies the team's current confidence to attack ratings without crossing
/// the independent rating-engine display boundary.
///
/// Production engine results already contain their own display-space values.
/// This layer changes the raw attack ledger and then exposes the adjusted raw
/// sector values directly. The experimental HattrickRatingDisplayConverter is
/// intentionally not used here; it remains isolated to Stage-2 display tests.
/// </summary>
public static class ConfidenceRatingAdjuster
{
    private const double NeutralConfidence = 4.0;
    private const double AttackPerLevel = 0.05;

    public static RegionalRatingSnapshot Apply(RegionalRatingSnapshot rating, int confidenceLevel)
    {
        ArgumentNullException.ThrowIfNull(rating);

        var level = Math.Clamp(confidenceLevel, 0, 9);
        var multiplier = 1.0 + (level - NeutralConfidence) * AttackPerLevel;
        multiplier = Math.Clamp(multiplier, 0.80, 1.25);

        return Rebuild(
            rating,
            rating.RawLeftDefence,
            rating.RawCentralDefence,
            rating.RawRightDefence,
            rating.RawMidfield,
            rating.RawLeftAttack * multiplier,
            rating.RawCentralAttack * multiplier,
            rating.RawRightAttack * multiplier);
    }

    private static RegionalRatingSnapshot Rebuild(
        RegionalRatingSnapshot r,
        double ld, double cd, double rd, double mid,
        double la, double ca, double ra)
        => new(
            ld, cd, rd, mid, la, ca, ra,
            ld, cd, rd, mid, la, ca, ra);
}
