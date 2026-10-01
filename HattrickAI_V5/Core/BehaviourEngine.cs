namespace HattrickAI.V5.Core;

/// <summary>
/// Legal individual behaviour options for each fixed Hattrick position.
/// Includes WB-* aliases used by HatFor slot codes.
/// </summary>
public sealed class BehaviourEngine
{
    public IReadOnlyList<PlayerOrder> GetAllowedOrders(string positionCode)
    {
        if (string.IsNullOrWhiteSpace(positionCode))
            return [PlayerOrder.Normal];

        var code = positionCode.Trim().ToUpperInvariant();
        code = code switch
        {
            "WB-L" or "WBL" => "DEF-L",
            "WB-R" or "WBR" => "DEF-R",
            "LCD" => "DEF-CL",
            "RCD" => "DEF-CR",
            "CD" => "DEF-C",
            "LIM" => "IM-L",
            "RIM" => "IM-R",
            "CIM" or "IM" => "IM-C",
            "WL" or "LW" => "W-L",
            "WR" or "RW" => "W-R",
            "LF" => "FW-L",
            "RF" => "FW-R",
            "FC" or "FW" => "FW-C",
            _ => code
        };

        return code switch
        {
            "GK" => [PlayerOrder.Normal],
            "DEF-L" or "DEF-R" =>
                [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsMiddle],
            "DEF-CL" or "DEF-CR" =>
                [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.TowardsWing],
            "DEF-C" =>
                [PlayerOrder.Normal, PlayerOrder.Offensive],
            "W-L" or "W-R" =>
                [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsMiddle],
            "IM-L" or "IM-R" =>
                [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsWing],
            "IM-C" =>
                [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive],
            "FW-L" or "FW-R" =>
                [PlayerOrder.Normal, PlayerOrder.Defensive, PlayerOrder.TowardsWing],
            "FW-C" =>
                [PlayerOrder.Normal, PlayerOrder.Defensive],
            _ => [PlayerOrder.Normal]
        };
    }

    public IReadOnlyList<BehaviourCandidate> EnumerateCandidates(Player player, string positionCode)
    {
        ArgumentNullException.ThrowIfNull(player);

        return GetAllowedOrders(positionCode)
            .Distinct()
            .Select(order => new BehaviourCandidate(player.Id, positionCode, order))
            .ToList();
    }
}

public sealed record BehaviourCandidate(
    int PlayerId,
    string PositionCode,
    PlayerOrder Order);
