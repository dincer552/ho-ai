namespace HattrickAI.V5.Core;

/// <summary>
/// Applies the team's current confidence to attack ratings without crossing
/// the independent rating-engine display boundary.
///
/// Production engine results expose a raw ledger plus HO-style display fields.
/// This layer changes the raw attack ledger and then rebuilds the displayed
/// fields through the same sector conversion used by the production engine.
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
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftDefence, ld),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.CentralDefence, cd),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.RightDefence, rd),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.Midfield, mid),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.LeftAttack, la),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.CentralAttack, ca),
            HattrickRatingDisplayConverter.ToDisplay(RatingSector.RightAttack, ra));
}
