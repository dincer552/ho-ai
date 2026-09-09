using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Locks the exact legal individual-order matrix for all 14 field slots.
/// </summary>
public static class BehaviourPositionOrderRegression
{
    public static int Run()
    {
        var engine = new BehaviourEngine();
        var expected = new Dictionary<string, PlayerOrder[]>
        {
            ["GK"] = [PlayerOrder.Normal],
            ["DEF-L"] = [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsMiddle],
            ["DEF-CL"] = [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.TowardsWing],
            ["DEF-C"] = [PlayerOrder.Normal, PlayerOrder.Offensive],
            ["DEF-CR"] = [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.TowardsWing],
            ["DEF-R"] = [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsMiddle],
            ["W-L"] = [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsMiddle],
            ["IM-L"] = [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsWing],
            ["IM-C"] = [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive],
            ["IM-R"] = [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsWing],
            ["W-R"] = [PlayerOrder.Normal, PlayerOrder.Offensive, PlayerOrder.Defensive, PlayerOrder.TowardsMiddle],
            ["FW-L"] = [PlayerOrder.Normal, PlayerOrder.Defensive, PlayerOrder.TowardsWing],
            ["FW-C"] = [PlayerOrder.Normal, PlayerOrder.Defensive],
            ["FW-R"] = [PlayerOrder.Normal, PlayerOrder.Defensive, PlayerOrder.TowardsWing]
        };

        var failures = new List<string>();
        foreach (var (position, expectedOrders) in expected)
        {
            var actual = engine.GetAllowedOrders(position).ToArray();
            if (!actual.SequenceEqual(expectedOrders))
            {
                failures.Add($"{position}: expected [{string.Join(", ", expectedOrders)}], got [{string.Join(", ", actual)}]");
            }
        }

        if (failures.Count > 0)
        {
            Console.Error.WriteLine("BehaviourPositionOrderRegression FAILED");
            foreach (var failure in failures) Console.Error.WriteLine($" - {failure}");
            return 1;
        }

        Console.WriteLine("BehaviourPositionOrderRegression PASS");
        return 0;
    }
}
