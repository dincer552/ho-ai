namespace HattrickAI.V5.Core;

/// <summary>
/// Cheap post-M11 order (emir) neighborhood search using HatFor ratings.
/// Starts from all-Normal, tries single-slot order flips, then optional second pass.
/// Does not expand full Cartesian product of behaviours.
/// </summary>
public sealed class OrderNeighborhoodSearch
{
    private readonly HatForRatingEngine _engine = new();
    private readonly BehaviourEngine _behaviours = new();

    public sealed record Result(
        Lineup Lineup,
        RegionalRatingSnapshot Rating,
        double Score,
        int Evaluated,
        IReadOnlyList<string> Changes);

    public Result Optimize(
        Lineup lineup,
        IReadOnlyList<Player> players,
        RegionalRatingSnapshot? opponentRating = null,
        int maxSingleFlips = 40)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(players);

        var current = ApplyAllNormal(lineup);
        var baselineReq = new RatingEngineRequest(current, players, RatingContext.Default);
        var baselineRating = _engine.Calculate(baselineReq).Rating;
        var bestScore = Score(baselineRating, opponentRating);
        var best = current;
        var bestRating = baselineRating;
        var changes = new List<string>();
        var evaluated = 1;

        var candidates = new List<(Lineup Lineup, string Change)>();
        foreach (var slot in current.Slots.Where(s => s.PlayerId > 0))
        {
            var allowed = _behaviours.GetAllowedOrders(NormalizeCode(slot.Code));
            foreach (var order in allowed)
            {
                if (order == slot.Order) continue;
                var flipped = WithOrder(current, slot.PlayerId, order);
                candidates.Add((flipped, $"{slot.Code}:{slot.Order}->{order}"));
            }
        }

        foreach (var (cand, change) in candidates.Take(maxSingleFlips))
        {
            var req = new RatingEngineRequest(cand, players, RatingContext.Default);
            var rating = _engine.Calculate(req).Rating;
            evaluated++;
            var score = Score(rating, opponentRating);
            if (score > bestScore + 1e-9)
            {
                bestScore = score;
                best = cand;
                bestRating = rating;
                changes.Add(change);
            }
        }

        var secondPass = 0;
        foreach (var slot in best.Slots.Where(s => s.PlayerId > 0))
        {
            if (secondPass >= 12) break;
            var allowed = _behaviours.GetAllowedOrders(NormalizeCode(slot.Code));
            foreach (var order in allowed)
            {
                if (order == slot.Order) continue;
                var flipped = WithOrder(best, slot.PlayerId, order);
                var req = new RatingEngineRequest(flipped, players, RatingContext.Default);
                var rating = _engine.Calculate(req).Rating;
                evaluated++;
                secondPass++;
                var score = Score(rating, opponentRating);
                if (score > bestScore + 1e-9)
                {
                    bestScore = score;
                    best = flipped;
                    bestRating = rating;
                    changes.Add($"{slot.Code}:{slot.Order}->{order}");
                }
            }
        }

        return new Result(best, bestRating, bestScore, evaluated, changes);
    }

    private static double Score(RegionalRatingSnapshot own, RegionalRatingSnapshot? opp)
    {
        double s =
            1.35 * own.Midfield +
            1.00 * own.CentralAttack +
            0.90 * own.LeftAttack +
            0.90 * own.RightAttack +
            1.00 * own.CentralDefence +
            0.85 * own.LeftDefence +
            0.85 * own.RightDefence;

        if (opp is null) return s;

        s += 0.35 * (own.CentralAttack - opp.CentralDefence);
        s += 0.25 * (own.LeftAttack - opp.RightDefence);
        s += 0.25 * (own.RightAttack - opp.LeftDefence);
        s += 0.30 * (own.Midfield - opp.Midfield);
        s += 0.20 * (own.CentralDefence - opp.CentralAttack);
        return s;
    }

    private static Lineup ApplyAllNormal(Lineup lineup)
    {
        var slots = lineup.Slots.Select(s =>
            s.PlayerId <= 0 ? s : s with { Order = PlayerOrder.Normal }).ToList();
        return lineup with {Slots = slots};
    }

    private static Lineup WithOrder(Lineup lineup, int playerId, PlayerOrder order)
    {
        var slots = lineup.Slots.Select(s =>
            s.PlayerId == playerId ? s with { Order = order } : s).ToList();
        return lineup with {Slots = slots};
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return code;
        code = code.Trim().ToUpperInvariant();
        return code switch
        {
            "WB-L" or "WBL" => "DEF-L",
            "WB-R" or "WBR" => "DEF-R",
            _ => code
        };
    }
}
