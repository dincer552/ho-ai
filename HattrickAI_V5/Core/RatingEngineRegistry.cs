namespace HattrickAI.V5.Core;

/// <summary>Production adapter that exposes the current V5 rating calculation without altering its pipeline.</summary>
public sealed class V5RatingEngine : IRatingEngine
{
    private readonly Stage2RegionalRatingEngineFixed _engine = new();
    public RatingEngineKind Kind => RatingEngineKind.V5;
    public string Name => "V5";

    public RatingEngineResult Calculate(RatingEngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var rating = _engine.CalculateLineup(request.Lineup, request.Players, request.Context);
        return new RatingEngineResult(Kind, rating);
    }
}

/// <summary>Single registry/factory for the four independent rating engines.</summary>
public sealed class RatingEngineRegistry
{
    private readonly IReadOnlyDictionary<RatingEngineKind, IRatingEngine> _engines;

    public RatingEngineRegistry(IEnumerable<IRatingEngine>? engines = null)
    {
        var list = (engines ?? new IRatingEngine[]
        {
            new V5RatingEngine(),
            new HOEngineAdapter(),
            new HattrickDashEngine(),
            new FoxtrickEngine()
        }).ToList();

        if (list.Count != Enum.GetValues<RatingEngineKind>().Length)
            throw new InvalidOperationException("Rating engine registry eksik veya fazla motor içeriyor.");
        _engines = list.ToDictionary(x => x.Kind);
        if (!_engines.ContainsKey(RatingEngineKind.V5))
            throw new InvalidOperationException("V5 registry'de zorunlu.");
    }

    public IReadOnlyList<IRatingEngine> All => _engines.Values.OrderBy(x => x.Kind).ToArray();
    public IRatingEngine Get(RatingEngineKind kind) => _engines.TryGetValue(kind, out var engine)
        ? engine
        : throw new KeyNotFoundException($"Rating engine bulunamadı: {kind}");

    public RatingEngineResult Calculate(RatingEngineKind kind, RatingEngineRequest request) => Get(kind).Calculate(request);
}
