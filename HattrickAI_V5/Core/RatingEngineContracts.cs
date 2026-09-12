namespace HattrickAI.V5.Core;

/// <summary>
/// Common contract for independently implemented rating engines.
/// This layer is intentionally passive: it does not replace or modify the
/// existing V5 pipeline. Each future engine must consume canonical lineup/
/// player data and return the same normalized rating shape.
/// </summary>
public enum RatingEngineKind
{
    V5,
    HO,
    HattrickDash,
    Foxtrick
}

public sealed record RatingEngineRequest(
    Lineup Lineup,
    IReadOnlyList<Player> Players,
    RatingContext Context);

public sealed record RatingEngineResult(
    RatingEngineKind Engine,
    RegionalRatingSnapshot Rating,
    double? HatStats = null,
    double? LoddarStats = null);

public interface IRatingEngine
{
    RatingEngineKind Kind { get; }
    string Name { get; }
    RatingEngineResult Calculate(RatingEngineRequest request);
}
