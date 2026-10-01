using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Production final rating: HatFor only (no multi-engine blend).
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
        return _registry.Calculate(RatingEngineKind.HatFor, request).Rating;
    }

    public FinalRatingExplanation Explain(
        RatingEngineRequest request,
        FinalRatingCalibrationProfile? profile = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rating = Calculate(request, profile);
        var weights = new Dictionary<RatingSector, IReadOnlyDictionary<RatingEngineKind, double>>();
        foreach (RatingSector sector in Enum.GetValues<RatingSector>())
            weights[sector] = new Dictionary<RatingEngineKind, double> { [RatingEngineKind.HatFor] = 1.0 };
        return new FinalRatingExplanation(rating, weights);
    }
}

public sealed record FinalRatingExplanation(
    RegionalRatingSnapshot Rating,
    IReadOnlyDictionary<RatingSector, IReadOnlyDictionary<RatingEngineKind, double>> Weights);
