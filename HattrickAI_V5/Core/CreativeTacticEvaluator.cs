namespace HattrickAI.V5.Core;

/// <summary>
/// Creative-specific suitability evaluation. This is deliberately separate from the
/// generic tactic objective because Creative depends on the specialty portfolio,
/// opponent specialty interaction, event upside/risk and the defence trade-off.
/// Exact live-engine Creative formula is not public, so the evaluator uses documented
/// mechanics and bounded suitability heuristics rather than inventing an exact formula.
/// </summary>
public static class CreativeTacticEvaluator
{
    public static TacticFitResult Evaluate(
        Lineup lineup,
        ComparisonEvaluationView baselineNormal,
        ComparisonEvaluationView tacticEvaluation,
        IReadOnlyList<Player> players,
        IReadOnlyList<Player>? opponentPlayers = null,
        Lineup? opponentLineup = null)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(baselineNormal);
        ArgumentNullException.ThrowIfNull(tacticEvaluation);
        ArgumentNullException.ThrowIfNull(players);

        var own = tacticEvaluation.Chance;
        var baseline = baselineNormal.Chance;
        var ownXi = LineupPlayers(lineup, players);
        var ownOutfield = LineupOutfieldPlayers(lineup, players);
        var opponentXi = opponentLineup is not null && opponentPlayers is not null
            ? LineupPlayers(opponentLineup, opponentPlayers)
            : Array.Empty<Player>();

        var tacticalLevel = Math.Clamp(tacticEvaluation.Advanced.Level.Value, 0, 10);

        // Official developer documentation: PC level uses the starting 10 outfield
        // players; Passing has 4x the weight of Experience; Unpredictable contributes 2x.
        // The goalkeeper is explicitly excluded from PC tactical-level calculation.
        var passing = WeightedAverage(ownOutfield, p => p.Passing, p => p.Specialty == PlayerSpecialty.Unpredictable ? 2.0 : 1.0);
        var experience = WeightedAverage(ownOutfield, p => p.Experience, p => p.Specialty == PlayerSpecialty.Unpredictable ? 2.0 : 1.0);
        var tacticalInput = Clamp01(((4.0 * passing) + experience) / 50.0);

        var portfolio = SpecialtyPortfolio(ownXi);
        var opponentPortfolio = SpecialtyPortfolio(opponentXi);
        var diversity = SpecialtyDiversity(ownXi);
        var opponentInteraction = OpponentSpecialtyInteraction(portfolio, opponentPortfolio);

        var eventUpside = Clamp01(
            0.35 * Clamp01((own.CreativeEventMultiplier - 1.0) / 2.8) +
            0.25 * tacticalInput +
            0.20 * diversity +
            0.20 * Clamp01(SpecialEventGoals(tacticEvaluation.Prediction.Prediction.EventGoals)));

        var negativeRisk = Clamp01(
            0.45 * UnpredictableRisk(ownXi) +
            0.25 * DefensiveNegativeEventRisk(ownXi) +
            0.15 * WeatherNeutralRisk(ownXi) +
            0.15 * Clamp01(portfolio.Total * 0.03));

        // The paper/official mechanics document a 7.5% defence reduction. It is a
        // real trade-off, not a generic bonus, so keep it explicit and bounded.
        var defencePenalty = 0.075;
        var defenceQuality = Clamp01(Average(ownOutfield.Select(p => p.Defending)) / 10.0);
        var tradeoff = Clamp01(
            0.55 * RelativeLoss(baseline.OwnRegularChanceExpected, own.OwnRegularChanceExpected) +
            0.30 * Math.Max(0, baselineNormal.Prediction.Prediction.WinProbability - tacticEvaluation.Prediction.Prediction.WinProbability) +
            0.15 * defencePenalty * (1.0 - defenceQuality));

        var suitability = Clamp01(
            0.28 * eventUpside +
            0.22 * tacticalInput +
            0.15 * opponentInteraction +
            0.12 * diversity +
            0.13 * Clamp01(defenceQuality + tacticalLevel / 20.0) -
            0.35 * negativeRisk -
            0.25 * tradeoff);

        // Keep Creative as one tactical option, not a blanket winner. Outcome
        // probability remains a substantial part of the fit score.
        var score = Clamp01(0.55 * suitability + 0.45 * Clamp01(tacticEvaluation.Prediction.Prediction.WinProbability));
        var explanation =
            $"Creative: level {tacticalLevel:0.##}; 4x Passing+Experience input {tacticalInput:P0}; " +
            $"specialty diversity {diversity:P0}; opponent XI specialty interaction {opponentInteraction:P0}; " +
            $"event upside {eventUpside:P0}; negative-event risk {negativeRisk:P0}; " +
            $"7.5% defence penalty applied; Normal opportunity cost {tradeoff:P0}.";

        return new TacticFitResult(
            TeamTactic.Creative,
            score,
            eventUpside,
            tradeoff,
            Clamp01(0.60 * tacticalInput + 0.40 * diversity),
            opponentInteraction,
            true,
            explanation);
    }

    private static IReadOnlyList<Player> LineupPlayers(Lineup lineup, IReadOnlyList<Player> players)
    {
        var ids = lineup.Slots.Where(s => s.PlayerId > 0).Select(s => s.PlayerId).ToHashSet();
        return players.Where(p => ids.Contains(p.Id)).ToArray();
    }

    private static IReadOnlyList<Player> LineupOutfieldPlayers(Lineup lineup, IReadOnlyList<Player> players)
    {
        var ids = lineup.Slots.Where(s => s.PlayerId > 0 && s.Code != "GK").Select(s => s.PlayerId).ToHashSet();
        return players.Where(p => ids.Contains(p.Id)).ToArray();
    }

    private static SpecialtyCounts SpecialtyPortfolio(IEnumerable<Player> players)
    {
        var list = players.ToArray();
        return new SpecialtyCounts(
            list.Count(p => p.Specialty == PlayerSpecialty.Technical),
            list.Count(p => p.Specialty == PlayerSpecialty.Quick),
            list.Count(p => p.Specialty == PlayerSpecialty.Powerful),
            list.Count(p => p.Specialty == PlayerSpecialty.Unpredictable),
            list.Count(p => p.Specialty == PlayerSpecialty.Head));
    }

    private static double SpecialtyDiversity(IReadOnlyList<Player> players)
    {
        if (players.Count == 0) return 0;
        var types = players.Where(p => p.Specialty != PlayerSpecialty.None).Select(p => p.Specialty).Distinct().Count();
        return Clamp01(types / 5.0);
    }

    private static double OpponentSpecialtyInteraction(SpecialtyCounts own, SpecialtyCounts opponent)
    {
        if (own.Total == 0 || opponent.Total == 0) return 0.50;

        // Hattrick allocates individual SEs using the number of relevant specialists
        // on each starting XI. We do not have the hidden event-allocation formula, so
        // this is intentionally a narrow 0.25..0.75 bounded ownership signal rather
        // than the previous unbounded additive heuristic that frequently hit 100%.
        var ownRelevant = own.Technical + own.Quick + own.Powerful + own.Unpredictable + own.Head;
        var opponentRelevant = opponent.Technical + opponent.Quick + opponent.Powerful + opponent.Unpredictable + opponent.Head;
        var share = ownRelevant / (double)(ownRelevant + opponentRelevant);
        return Clamp01(0.50 + 0.50 * (share - 0.50));
    }

    private static double UnpredictableRisk(IEnumerable<Player> players)
    {
        var list = players.ToArray();
        if (list.Length == 0) return 0;
        return Clamp01(list.Count(p => p.Specialty == PlayerSpecialty.Unpredictable) / 5.0);
    }

    private static double DefensiveNegativeEventRisk(IEnumerable<Player> players)
    {
        var list = players.ToArray();
        if (list.Length == 0) return 0;
        var relevant = list.Count(p => p.Specialty == PlayerSpecialty.Unpredictable && IsDefender(p));
        return Clamp01(relevant / 3.0);
    }

    private static bool IsDefender(Player p)
        => p.Defending >= p.Scoring && p.Defending >= p.Playmaking;

    private static double WeatherNeutralRisk(IEnumerable<Player> players)
        => 0.0; // Match weather is not currently carried into TacticObjectiveEngine.

    private static double SpecialEventGoals(M9EventGoalBreakdown e)
        => Math.Max(0, e.PlayerBasedSpecialEventGoals + e.TeamBasedSpecialEventGoals + e.PowerfulNormalForwardGoals);

    private static double RelativeLoss(double baseline, double tactic)
        => baseline <= 1e-9 ? 0 : Clamp01((baseline - tactic) / baseline);

    private static double WeightedAverage(IEnumerable<Player> players, Func<Player, int> selector, Func<Player, double> weightSelector)
    {
        var rows = players.Select(p => (Value: selector(p), Weight: weightSelector(p))).Where(x => x.Weight > 0).ToArray();
        var weight = rows.Sum(x => x.Weight);
        return weight <= 0 ? 0 : rows.Sum(x => x.Value * x.Weight) / weight;
    }

    private static double Average(IEnumerable<int> values)
    {
        var a = values.Where(v => v > 0).ToArray();
        return a.Length == 0 ? 0 : a.Average();
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);

    private sealed record SpecialtyCounts(int Technical, int Quick, int Powerful, int Unpredictable, int Head)
    {
        public int Total => Technical + Quick + Powerful + Unpredictable + Head;
    }
}
