namespace HattrickAI.V5.Core;

/// <summary>
/// Production adapter: all former "V5" pipeline callers now use HatFor.
/// Formation-specific Excel coefficients; not the old RegionalRatingEngineFixed path.
/// </summary>
public sealed class V5RatingEngine : IRatingEngine
{
    private readonly HatForRatingEngine _engine = new();
    public RatingEngineKind Kind => RatingEngineKind.HatFor;
    public string Name => "HatFor (Excel)";

    public RatingEngineResult Calculate(RatingEngineRequest request)
        => _engine.Calculate(request);
}

/// <summary>Single-engine registry: HatFor only.</summary>
public sealed class RatingEngineRegistry
{
    private readonly IReadOnlyDictionary<RatingEngineKind, IRatingEngine> _engines;

    public RatingEngineRegistry(IEnumerable<IRatingEngine>? engines = null)
    {
        var list = (engines ?? new IRatingEngine[]
        {
            new HatForRatingEngine()
        }).ToList();

        _engines = list.ToDictionary(x => x.Kind);
        if (!_engines.ContainsKey(RatingEngineKind.HatFor))
            throw new InvalidOperationException("HatFor registry'de zorunlu.");
    }

    public IReadOnlyList<IRatingEngine> All => _engines.Values.OrderBy(x => x.Kind).ToArray();
    public IRatingEngine Get(RatingEngineKind kind)
    {
        // Any legacy kind request resolves to HatFor
        if (_engines.TryGetValue(RatingEngineKind.HatFor, out var engine))
            return engine;
        throw new KeyNotFoundException($"Rating engine bulunamadı: {kind}");
    }

    public RatingEngineResult Calculate(RatingEngineKind kind, RatingEngineRequest request)
        => Get(kind).Calculate(request);
}
