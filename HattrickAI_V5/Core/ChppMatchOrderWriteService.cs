using System.Globalization;

namespace HattrickAI.V5.Core;

/// <summary>
/// CHPP-WRITE-06 live write orchestration. Validation and permission guards
/// run immediately before the network write; the service does not silently
/// downgrade or replace an unsupported tactic.
/// </summary>
public sealed class ChppMatchOrderWriteService
{
    private readonly ChppV5 _chpp;

    public ChppMatchOrderWriteService(ChppV5 chpp) => _chpp = chpp;

    public async Task<ChppMatchOrderWriteResult> WriteAsync(
        ChppMatchOrderPayload payload,
        ChppUpcomingMatchSnapshot target,
        IEnumerable<int>? rosterPlayerIds,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(target);

        ChppMatchOrderPermissionGuard.EnsureCanWrite(_chpp);
        ChppMatchOrderValidator.EnsureValid(payload, target, rosterPlayerIds);

        var raw = await _chpp.SetMatchOrderAsync(target.MatchId, target.TeamId, payload, ct);
        var root = XmlV5.Root(raw);
        var matchData = root?.Descendants("MatchData").FirstOrDefault() ?? root;
        var reason = XmlV5.Text(matchData, "Reason");
        var ordersSet = string.Equals(XmlV5.Text(matchData, "OrdersSet"), "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals((string?)matchData?.Attribute("OrdersSet"), "true", StringComparison.OrdinalIgnoreCase);

        return new ChppMatchOrderWriteResult(
            target.MatchId,
            target.TeamId,
            ordersSet,
            payload.Tactic.ToString(),
            raw,
            reason);
    }
}

public sealed record ChppMatchOrderWriteResult(
    int MatchId,
    int TeamId,
    bool OrdersSet,
    string Tactic,
    string RawResponse,
    string Reason);
