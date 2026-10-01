namespace HattrickAI.V5.Core;

/// <summary>
/// Production rating engine is HatFor only (Excel formation-specific formulas).
/// </summary>
public enum RatingEngineKind
{
    HatFor = 0
}

public static class RatingEngineKindParse
{
    /// <summary>Accepts legacy names (V5, HO, HattrickDash, Foxtrick) and maps them to HatFor.</summary>
    public static bool TryParse(string? value, out RatingEngineKind kind)
    {
        kind = RatingEngineKind.HatFor;
        if (string.IsNullOrWhiteSpace(value)) return true;
        var v = value.Trim();
        if (v.Equals("HatFor", StringComparison.OrdinalIgnoreCase)) return true;
        if (v.Equals("V5", StringComparison.OrdinalIgnoreCase)) return true;
        if (v.Equals("HO", StringComparison.OrdinalIgnoreCase)) return true;
        if (v.Equals("HattrickDash", StringComparison.OrdinalIgnoreCase)) return true;
        if (v.Equals("Foxtrick", StringComparison.OrdinalIgnoreCase)) return true;
        return Enum.TryParse(v, true, out kind);
    }
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
