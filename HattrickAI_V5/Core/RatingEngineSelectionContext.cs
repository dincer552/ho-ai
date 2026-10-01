using System.Threading;

namespace HattrickAI.V5.Core;

/// <summary>
/// Carries the rating-engine choice through the analysis pipeline.
/// Production default is HatFor (Excel formation-specific formulas).
/// </summary>
public static class RatingEngineSelectionContext
{
    private static readonly AsyncLocal<RatingEngineKind?> Current = new();

    public static RatingEngineKind Selected => Current.Value ?? RatingEngineKind.HatFor;

    public static IDisposable Push(RatingEngineKind selected)
    {
        var previous = Current.Value;
        Current.Value = selected;
        return new Scope(previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly RatingEngineKind? _previous;
        private bool _disposed;

        public Scope(RatingEngineKind? previous) => _previous = previous;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Current.Value = _previous;
        }
    }
}
