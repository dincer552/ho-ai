using System;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>Stage-1 guard for the common rating-engine contract, registry, and V5 adapter parity.</summary>
public static class RatingEngineContractsRegression
{
    public static int Run()
    {
        var kinds = Enum.GetValues<RatingEngineKind>();
        if (kinds.Length != 4)
            throw new InvalidOperationException($"Rating engine contract drift: expected 4 engines, found {kinds.Length}.");

        var expected = new[] { RatingEngineKind.V5, RatingEngineKind.HO, RatingEngineKind.HattrickDash, RatingEngineKind.Foxtrick };
        for (var i = 0; i < expected.Length; i++)
            if (kinds[i] != expected[i])
                throw new InvalidOperationException($"Rating engine enum order drift at index {i}: {kinds[i]}.");

        if (!typeof(IRatingEngine).IsAssignableFrom(typeof(ContractProbeEngine)))
            throw new InvalidOperationException("IRatingEngine contract cannot be implemented.");

        var registry = new RatingEngineRegistry();
        if (registry.All.Count != 4)
            throw new InvalidOperationException($"Rating engine registry count drift: {registry.All.Count}.");
        foreach (var kind in expected)
        {
            var engine = registry.Get(kind);
            if (engine.Kind != kind || string.IsNullOrWhiteSpace(engine.Name))
                throw new InvalidOperationException($"Registry entry invalid for {kind}.");
        }

        var players = Enumerable.Range(1, 11)
            .Select(i => new Player(i, $"P{i}", 1, 10, 10, 10, 10, 10, 7, 7, 5))
            .ToList();
        var codes = new[] { "GK", "DEF-L", "DEF-C", "DEF-R", "W-L", "IM-L", "IM-C", "IM-R", "W-R", "FW-L", "FW-R" };
        var slots = codes.Select((code, i) => new Slot(code, code, "contract", players[i].Name, players[i].Id, 0, 0, 0)).ToList();
        var request = new RatingEngineRequest(new Lineup("Contract", "3-5-2", slots), players, RatingContext.Default);
        var expectedV5 = new RegionalRatingEngineFixed().CalculateLineup(request.Lineup, request.Players, request.Context);
        var actualV5 = registry.Calculate(RatingEngineKind.V5, request).Rating;
        var actual = Values(actualV5).ToArray();
        var expectedValues = Values(expectedV5).ToArray();
        var labels = new[] { "LD", "CD", "RD", "MF", "LA", "CA", "RA" };
        for (var i = 0; i < labels.Length; i++)
            if (Math.Abs(actual[i] - expectedValues[i]) > 1e-12)
                throw new InvalidOperationException($"V5 adapter parity drift at {labels[i]}: expected {expectedValues[i]:R}, got {actual[i]:R}.");

        return 0;
    }

    private static IEnumerable<double> Values(RegionalRatingSnapshot s)
    {
        yield return s.LeftDefence; yield return s.CentralDefence; yield return s.RightDefence;
        yield return s.Midfield; yield return s.LeftAttack; yield return s.CentralAttack; yield return s.RightAttack;
    }

    private sealed class ContractProbeEngine : IRatingEngine
    {
        public RatingEngineKind Kind => RatingEngineKind.V5;
        public string Name => "ContractProbe";
        public RatingEngineResult Calculate(RatingEngineRequest request)
            => throw new NotSupportedException("Contract probe does not execute a production rating calculation.");
    }
}
