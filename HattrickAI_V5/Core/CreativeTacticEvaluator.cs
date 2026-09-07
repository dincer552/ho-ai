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
        IReadOnlyList<Player>? opponentPlayers = null)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(baselineNormal);
        ArgumentNullException.ThrowIfNull(tacticEvaluation);
        ArgumentNullException.ThrowIfNull(players);

        var own = tacticEvaluation.Chance;
        var baseline = baselineNormal.Chance;
        var ownXi = LineupPlayers(lineup, players);
        var opponent = opponentPlayers ?? Array.Empty<Player>();

        var tacticalLevel = Math.Clamp(tacticEvaluation.Advanced.Level.Value, 0, 10);
        var passing = Average(ownXi.Select(p => p.Passing));
        var experience = Average(ownXi.Select(p => p.Experience));
        var tacticalInput = Clamp01(((4.0 * passing) + experience) / 50.0);

        var portfolio = SpecialtyPortfolio(ownXi);
        var opponentPortfolio = SpecialtyPortfolio(opponent);
        var diversity = SpecialtyDiversity(ownXi);
        var opponentInteraction = OpponentSpecialtyInteraction(portfolio, opponentPortfolio);

        var eventUpside = Clamp01(
            0.40 * Clamp01((own.CreativeEventMultiplier - 1.0) / 2.8) +
            0.25 * tacticalInput +
            0.20 * diversity +
            0.15 * Clamp01(SpecialEventGoals(tacticEvaluation.Prediction.Prediction.EventGoals));

        var negativeRisk = Clamp01(
            0.45 * UnpredictableRisk(ownXi) +
            0.25 * DefensiveNegativeEventRisk(ownXi) +
            0.15 * WeatherNeutralRisk(ownXi) +
            0.15 * Clamp01(portfolio.Total * 0.03));

        var defencePenalty = 0.075;
        var defenceQuality = Clamp01(Average(ownXi.Select(p => p.Defending)) / 10.0);
        var tradeoff = Clamp01(
            0.55 * RelativeLoss(baseline.OwnRegularChanceExpected, own.OwnRegularChanceExpected) +
            0.30 * Math.Max(0, baselineNormal.Prediction.Prediction.WinProbability - tacticEvaluation.Prediction.Prediction.WinProbability) +
            0.15 * defencePenalty * (1.0 - defenceQuality));

        var suitability = Clamp01(
            0.32 * eventUpside +
            0.20 * tacticalInput +
            0.18 * opponentInteraction +
            0.15 * diversity +
            0.15 * Clamp01(defenceQuality + tacticalLevel / 20.0) -
            0.35 * negativeRisk -
            0.25 * tradeoff);

        // Creative is a tactic, not a blanket bonus. If the event upside does not
        // compensate for its risks and Normal is stronger, the fit must fall.
        var score = Clamp01(0.65 * suitability + 0.35 * Clamp01(tacticEvaluation.Prediction.Prediction.WinProbability));
        var explanation =
            $"Creative: level {tacticalLevel:0.##}; 4x Passing+Experience input {tacticalInput:P0}; " +
            $"specialty diversity {diversity:P0}; opponent specialty interaction {opponentInteraction:P0}; " +
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
        if (own.Total == 0) return 0.15;

        // Technical/Quick/Powerful/Head contribute to different event families;
        // Unpredictable adds upside but also negative-event exposure. The opponent
        // portfolio therefore modifies, rather than decides, Creative suitability.
        var ownPositive = own.Technical + own.Quick + own.Powerful + own.Head;
        var ownRisk = own.Unpredictable;
        var opponentAmplifier = opponent.Technical + opponent.Quick + opponent.Powerful + opponent.Head;
        var value = 0.50 + 0.08 * Math.Min(5, ownPositive) - 0.07 * Math.Min(5, ownRisk) + 0.025 * Math.Min(8, opponentAmplifier);
        return Clamp01(value);
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
