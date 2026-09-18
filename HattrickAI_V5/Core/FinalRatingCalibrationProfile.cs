namespace HattrickAI.V5.Core;

/// <summary>
/// Bootstrap calibration profile built from the first real S4MSUNFC 3-5-2
/// screenshot fixture. It is intentionally provisional: weights must be
/// recalculated as more real Hattrick fixtures are added.
/// </summary>
public sealed record FinalRatingCalibrationProfile(
    IReadOnlyDictionary<RatingSector, IReadOnlyDictionary<RatingEngineKind, double>> Weights)
{
    public static FinalRatingCalibrationProfile Bootstrap352 { get; } = Create();

    private static FinalRatingCalibrationProfile Create()
    {
        static IReadOnlyDictionary<RatingEngineKind, double> Pick(RatingEngineKind kind)
            => new Dictionary<RatingEngineKind, double>
            {
                [RatingEngineKind.V5] = kind == RatingEngineKind.V5 ? 1.0 : 0.0,
                [RatingEngineKind.HO] = kind == RatingEngineKind.HO ? 1.0 : 0.0,
                [RatingEngineKind.HattrickDash] = kind == RatingEngineKind.HattrickDash ? 1.0 : 0.0
            };

        return new FinalRatingCalibrationProfile(new Dictionary<RatingSector, IReadOnlyDictionary<RatingEngineKind, double>>
        {
            [RatingSector.LeftDefence] = Pick(RatingEngineKind.V5),
            [RatingSector.CentralDefence] = Pick(RatingEngineKind.V5),
            [RatingSector.RightDefence] = Pick(RatingEngineKind.V5),
            [RatingSector.Midfield] = Pick(RatingEngineKind.HO),
            [RatingSector.LeftAttack] = Pick(RatingEngineKind.HattrickDash),
            [RatingSector.CentralAttack] = Pick(RatingEngineKind.HattrickDash),
            [RatingSector.RightAttack] = Pick(RatingEngineKind.HattrickDash)
        });
    }
}
