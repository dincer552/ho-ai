namespace HattrickAI.V5.Core;

/// <summary>
/// Common contract for independently implemented rating engines.
/// This layer is intentionally passive: it does not replace or modify the
/// existing V5 pipeline. Each future engine may consume canonical lineup/player
/// data and, when available, engine-specific match context.
/// </summary>
public enum RatingEngineKind
{
    V5,
    HO,
    HattrickDash,
    Foxtrick
}

public sealed record HOEngineContext(
    double TeamSpirit,
    double Confidence,
    CoachStyle CoachStyle,
    int TacticLevel = 1,
    int CoachModifier = 0,
    int Weather = 0);

public sealed record RatingEngineRequest(
    Lineup Lineup,
    IReadOnlyList<Player> Players,
    RatingContext Context,
    RegionalRatingSnapshot? CanonicalRating = null,
    HOEngineContext? HOContext = null);

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
