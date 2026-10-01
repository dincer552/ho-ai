using System;
using System.Linq;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>Guards the HatFor-only production contract and legacy-name routing.</summary>
public static class RatingEngineContractsRegression
{
    public static int Run()
    {
        var kinds = Enum.GetValues<RatingEngineKind>();
        if (kinds.Length != 1 || kinds[0] != RatingEngineKind.HatFor)
            throw new InvalidOperationException($"Production rating engines must contain HatFor only; found {string.Join(", ", kinds)}.");

        var registry = new RatingEngineRegistry();
        if (registry.All.Count != 1 || registry.All[0].Kind != RatingEngineKind.HatFor)
            throw new InvalidOperationException("Production registry must expose HatFor only.");

        foreach (var legacyName in new[] { "V5", "HO", "HattrickDash", "Foxtrick" })
        {
            if (!RatingEngineKindParse.TryParse(legacyName, out var parsed) || parsed != RatingEngineKind.HatFor)
                throw new InvalidOperationException($"Legacy engine name {legacyName} must resolve to HatFor.");
            if (registry.Get(parsed).Kind != RatingEngineKind.HatFor)
                throw new InvalidOperationException($"Legacy engine name {legacyName} did not route to HatFor.");
        }
        if (RatingEngineKindParse.TryParse("unknown-engine", out _))
            throw new InvalidOperationException("Unknown rating-engine names must be rejected.");

        if (!typeof(IRatingEngine).IsAssignableFrom(typeof(ContractProbeEngine)))
            throw new InvalidOperationException("IRatingEngine contract cannot be implemented.");

        var players = Enumerable.Range(1, 11)
            .Select(i => new Player(i, $"P{i}", 1, 10, 10, 10, 10, 10, 7, 7, 5))
            .ToList();
        var codes = new[] { "GK", "DEF-L", "DEF-C", "DEF-R", "W-L", "IM-L", "IM-C", "IM-R", "W-R", "FW-L", "FW-R" };
        var slots = codes.Select((code, i) => new Slot(code, code, "contract", players[i].Name, players[i].Id, 0, 0, 0)).ToList();
        var request = new RatingEngineRequest(new Lineup("Contract", "3-5-2", slots), players, RatingContext.Default);
        var direct = new HatForRatingEngine().Calculate(request).Rating;
        var routed = registry.Calculate(RatingEngineKind.HatFor, request).Rating;
        var actual = Values(routed).ToArray();
        var expected = Values(direct).ToArray();
        for (var i = 0; i < actual.Length; i++)
            if (Math.Abs(actual[i] - expected[i]) > 1e-12)
                throw new InvalidOperationException($"HatFor registry parity drift at sector {i}: expected {expected[i]:R}, got {actual[i]:R}.");

        return 0;
    }

    private static double[] Values(RegionalRatingSnapshot s) => new[]
    {
        s.LeftDefence, s.CentralDefence, s.RightDefence, s.Midfield,
        s.LeftAttack, s.CentralAttack, s.RightAttack
    };

    private sealed class ContractProbeEngine : IRatingEngine
    {
        public RatingEngineKind Kind => RatingEngineKind.HatFor;
        public string Name => "ContractProbe";
        public RatingEngineResult Calculate(RatingEngineRequest request)
            => throw new NotSupportedException("Contract probe does not execute a production rating calculation.");
    }
}