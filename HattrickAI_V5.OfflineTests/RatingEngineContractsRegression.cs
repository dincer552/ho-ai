using System;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Stage-1 contract guard. It validates that the multi-engine boundary exists
/// without executing or modifying the production V5 rating calculation.
/// </summary>
public static class RatingEngineContractsRegression
{
    public static int Run()
    {
        var kinds = Enum.GetValues<RatingEngineKind>();
        if (kinds.Length != 4)
            throw new InvalidOperationException($"Rating engine contract drift: expected 4 engines, found {kinds.Length}.");

        var expected = new[]
        {
            RatingEngineKind.V5,
            RatingEngineKind.HO,
            RatingEngineKind.HattrickDash,
            RatingEngineKind.Foxtrick
        };

        for (var i = 0; i < expected.Length; i++)
        {
            if (kinds[i] != expected[i])
                throw new InvalidOperationException($"Rating engine enum order drift at index {i}: {kinds[i]}.");
        }

        if (!typeof(IRatingEngine).IsAssignableFrom(typeof(ContractProbeEngine)))
            throw new InvalidOperationException("IRatingEngine contract cannot be implemented.");

        return 0;
    }

    private sealed class ContractProbeEngine : IRatingEngine
    {
        public RatingEngineKind Kind => RatingEngineKind.V5;
        public string Name => "ContractProbe";
        public RatingEngineResult Calculate(RatingEngineRequest request)
            => throw new NotSupportedException("Contract probe does not execute a production rating calculation.");
    }
}
