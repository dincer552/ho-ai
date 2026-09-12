namespace HattrickAI.V5.Core;

public sealed record RatingEngineComparisonRow(
    RatingEngineKind Engine,
    string Name,
    RegionalRatingSnapshot Rating,
    double? HatStats,
    double? LoddarStats,
    double MidfieldDeltaVsV5,
    double LeftDefenceDeltaVsV5,
    double CentralDefenceDeltaVsV5,
    double RightDefenceDeltaVsV5,
    double LeftAttackDeltaVsV5,
    double CentralAttackDeltaVsV5,
    double RightAttackDeltaVsV5);

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

    public RatingEngineComparison Compare(RatingEngineRequest request, RatingEngineKind selected = RatingEngineKind.V5)
    {
        var results = CalculateAll(request);
        var baseline = results.Single(x => x.Engine == RatingEngineKind.V5).Rating;
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
        return new RatingEngineComparison(RatingEngineKind.V5, selected, rows);
    }
}
