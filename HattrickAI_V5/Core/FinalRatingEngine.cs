using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Sector-level ensemble engine. It never becomes the production default by
/// itself; callers explicitly opt into it with a calibration profile.
/// </summary>
public sealed class FinalRatingEngine
{
    private readonly RatingEngineRegistry _registry;

    public FinalRatingEngine(RatingEngineRegistry? registry = null)
        => _registry = registry ?? new RatingEngineRegistry();

    public RegionalRatingSnapshot Calculate(
        RatingEngineRequest request,
        FinalRatingCalibrationProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        profile ??= FinalRatingCalibrationProfile.Bootstrap352;

        var results = _registry.All
            .Where(x => x.Kind is RatingEngineKind.V5 or RatingEngineKind.HO or RatingEngineKind.HattrickDash)
            .Select(x => x.Calculate(request))
            .ToDictionary(x => x.Engine);

        double Blend(RatingSector sector)
        {
            var weights = profile.Weights[sector];
            var totalWeight = weights.Values.Sum();
            if (totalWeight <= 0)
                throw new InvalidOperationException($"Final rating sector has no calibration weight: {sector}");

            return weights.Sum(x => x.Value * Raw(results[x.Key].Rating, sector)) / totalWeight;
        }

        var rawLd = Blend(RatingSector.LeftDefence);
        var rawCd = Blend(RatingSector.CentralDefence);
        var rawRd = Blend(RatingSector.RightDefence);
        var rawMid = Blend(RatingSector.Midfield);
        var rawLa = Blend(RatingSector.LeftAttack);
        var rawCa = Blend(RatingSector.CentralAttack);
        var rawRa = Blend(RatingSector.RightAttack);

        return new RegionalRatingSnapshot(
            rawLd, rawCd, rawRd, rawMid, rawLa, rawCa, rawRa,
            Display(rawLd), Display(rawCd), Display(rawRd), Display(rawMid),
            Display(rawLa), Display(rawCa), Display(rawRa));
    }

    public FinalRatingExplanation Explain(
        RatingEngineRequest request,
        FinalRatingCalibrationProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        profile ??= FinalRatingCalibrationProfile.Bootstrap352;

        var rating = Calculate(request, profile);
        return new FinalRatingExplanation(rating, profile.Weights);
    }

    private static double Raw(RegionalRatingSnapshot rating, RatingSector sector) => sector switch
    {
        RatingSector.LeftDefence => rating.RawLeftDefence,
        RatingSector.CentralDefence => rating.RawCentralDefence,
        RatingSector.RightDefence => rating.RawRightDefence,
        RatingSector.Midfield => rating.RawMidfield,
        RatingSector.LeftAttack => rating.RawLeftAttack,
        RatingSector.CentralAttack => rating.RawCentralAttack,
        RatingSector.RightAttack => rating.RawRightAttack,
        _ => throw new ArgumentOutOfRangeException(nameof(sector))
    };

    private static double Display(double raw)
        => Math.Clamp(Math.Round(raw * 4.0, MidpointRounding.AwayFromZero) / 4.0, 1.0, 20.0);
}

public sealed record FinalRatingExplanation(
    RegionalRatingSnapshot Rating,
    IReadOnlyDictionary<RatingSector, IReadOnlyDictionary<RatingEngineKind, double>> Weights);
