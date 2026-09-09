namespace HattrickAI.V5.Core;

/// <summary>
/// M6 primitive: defines legal individual behaviour options for each fixed
/// Hattrick position. It does not inspect the opponent and does not choose
/// the winning behaviour.
/// </summary>
public sealed class BehaviourEngine
{
    public IReadOnlyList<PlayerOrder> GetAllowedOrders(string positionCode) => positionCode switch
    {
        "GK" => [PlayerOrder.Normal],

        // Wing backs: normal, offensive, defensive and towards middle.
        "DEF-L" or "DEF-R" =>
            [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsMiddle],

        // Wide central defenders: normal, offensive and towards wing.
        "DEF-CL" or "DEF-CR" =>
            [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.TowardsWing],

        // Central defender: normal and offensive only.
        "DEF-C" =>
            [PlayerOrder.Normal, PlayerOrder.Offensive],

        // Wingers: normal, offensive, defensive and towards middle.
        "W-L" or "W-R" =>
            [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsMiddle],

        // Wide inner midfielders: normal, offensive, defensive and towards wing.
        "IM-L" or "IM-R" =>
            [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsWing],

        // Central inner midfielder: normal, offensive and defensive only.
        "IM-C" =>
            [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive],

        // Wide forwards: normal, defensive and towards wing.
        "FW-L" or "FW-R" =>
            [PlayerOrder.Normal, PlayerOrder.Defensive, PlayerOrder.TowardsWing],

        // Central forward: normal and defensive only.
        "FW-C" =>
            [PlayerOrder.Normal, PlayerOrder.Defensive],

        _ => [PlayerOrder.Normal]
    };

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
