namespace HattrickAI.V5.Core;

/// <summary>
/// Production calibration: single-engine HatFor (100% weight on every sector).
/// Legacy multi-engine blend removed.
/// </summary>
public sealed record FinalRatingCalibrationProfile(
    IReadOnlyDictionary<RatingSector, IReadOnlyDictionary<RatingEngineKind, double>> Weights)
{
    public static FinalRatingCalibrationProfile Bootstrap352 { get; } = Create();

    private static FinalRatingCalibrationProfile Create()
    {
        static IReadOnlyDictionary<RatingEngineKind, double> HatForOnly()
            => new Dictionary<RatingEngineKind, double>
            {
                [RatingEngineKind.HatFor] = 1.0
            };

        var w = HatForOnly();
        return new FinalRatingCalibrationProfile(new Dictionary<RatingSector, IReadOnlyDictionary<RatingEngineKind, double>>
        {
            [RatingSector.LeftDefence] = w,
            [RatingSector.CentralDefence] = w,
            [RatingSector.RightDefence] = w,
            [RatingSector.Midfield] = w,
            [RatingSector.LeftAttack] = w,
            [RatingSector.CentralAttack] = w,
            [RatingSector.RightAttack] = w
        });
    }
}
