namespace HattrickAI.V5.Core;

public sealed record RatingEngineComparisonRow(
    RatingEngineKind Engine,
    string Name,
    RegionalRatingSnapshot Rating,
    double? HatStats,
    double? LoddarStats,
    double MidfieldDeltaVsBaseline,
    double LeftDefenceDeltaVsBaseline,
    double CentralDefenceDeltaVsBaseline,
    double RightDefenceDeltaVsBaseline,
    double LeftAttackDeltaVsBaseline,
    double CentralAttackDeltaVsBaseline,
    double RightAttackDeltaVsBaseline);

public sealed record RatingEngineComparison(
    RatingEngineKind Baseline,
    RatingEngineKind Selected,
    IReadOnlyList<RatingEngineComparisonRow> Rows);

public sealed class RatingEngineComparisonService
{
    private readonly RatingEngineRegistry _registry;

    public RatingEngineComparisonService(RatingEngineRegistry? registry = null)
        => _registry = registry ?? new RatingEngineRegistry();

    public IReadOnlyList<RatingEngineResult> CalculateAll(RatingEngineRequest request)
        => _registry.All.Select(x => x.Calculate(request)).ToArray();

    public RatingEngineComparison Compare(RatingEngineRequest request, RatingEngineKind selected = RatingEngineKind.HatFor)
    {
        var results = CalculateAll(request);
        var baseline = results.Single(x => x.Engine == RatingEngineKind.HatFor).Rating;
        var rows = results.Select(result => new RatingEngineComparisonRow(
            result.Engine,
            _registry.Get(result.Engine).Name,
            result.Rating,
            result.HatStats,
            result.LoddarStats,
            result.Rating.Midfield - baseline.Midfield,
            result.Rating.LeftDefence - baseline.LeftDefence,
            result.Rating.CentralDefence - baseline.CentralDefence,
            result.Rating.RightDefence - baseline.RightDefence,
            result.Rating.LeftAttack - baseline.LeftAttack,
            result.Rating.CentralAttack - baseline.CentralAttack,
            result.Rating.RightAttack - baseline.RightAttack)).ToArray();
        return new RatingEngineComparison(RatingEngineKind.HatFor, selected, rows);
    }
}
