namespace HattrickAI.V5.Core;

/// <summary>
/// Creative-specific suitability evaluation.
///
/// Source discipline:
/// - Official Hattrick material supports the 4x Passing weighting, 2x Unpredictable
///   contribution, the special-event ownership effect and the 7.5% defence reduction.
/// - The live tactical-level formula is not public, so V5 does not claim an exact formula.
/// - M9 already calculates the predicted own/opponent special-event goal layer. This
///   evaluator consumes that result instead of multiplying CreativeEventMultiplier again.
/// - Any bounded suitability weights below are V5 heuristics, not hidden-engine formulas.
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

        // M7.2 already preserves the documented Creative inputs. Use the resulting
        // tactical level once here rather than reconstructing another pseudo-formula.
        var tacticalLevel = Math.Clamp(tacticEvaluation.Advanced.Level.Value, 0, 10);
        var tacticalInput = Clamp01(tacticalLevel / 10.0);

        var portfolio = SpecialtyPortfolio(ownXi);
        var diversity = SpecialtyDiversity(ownXi);

        // M9 is the canonical forecast layer for the actual match. It already receives
        // the opponent XI and produces both own and opponent special-event goals. Using
        // those outputs here avoids bench contamination and avoids inventing a second
        // opponent-specialty ownership formula.
        var ownSpecialGoals = Math.Max(0.0, SpecialEventGoals(tacticEvaluation.Prediction.Prediction.EventGoals));
        var opponentSpecialGoals = Math.Max(0.0, SpecialEventGoals(tacticEvaluation.Prediction.Prediction.OpponentEventGoals));
        var totalSpecialGoals = ownSpecialGoals + opponentSpecialGoals;
        var opponentInteraction = totalSpecialGoals <= 1e-9
            ? 0.50
            : Clamp01(ownSpecialGoals / totalSpecialGoals);

        // Do not reuse CreativeEventMultiplier here: M9 already applies its Creative
        // event-volume mechanism. This is a composition signal from the resulting
        // forecast, not a second event multiplier.
        var eventBalance = opponentInteraction;
        var eventUpside = Clamp01(
            0.55 * eventBalance +
            0.25 * tacticalInput +
            0.20 * diversity);

        var ownGoalEventNegative = Math.Max(0.0, own.ExpectedGoalsConcededFromOwnGoalEvents);
        var negativeEventShare = totalSpecialGoals <= 1e-9
            ? 0.0
            : Clamp01(ownGoalEventNegative / totalSpecialGoals);
        var defensiveUnpredictableExposure = DefensiveNegativeEventExposure(ownXi);
        var negativeRisk = Clamp01(
            0.70 * negativeEventShare +
            0.20 * defensiveUnpredictableExposure +
            0.10 * Clamp01(portfolio.Total * 0.03));

        // The 7.5% defence reduction is a documented mechanic. Keep it visible as a
        // trade-off, but do not pretend it is an independent exact goal multiplier.
        const double documentedDefencePenalty = 0.075;
        var defenceQuality = Clamp01(Average(ownOutfield.Select(p => p.Defending)) / 10.0);
        var normalOpportunityLoss = RelativeLoss(baseline.OwnRegularChanceExpected, own.OwnRegularChanceExpected);
        var winProbabilityLoss = Math.Max(0.0,
            baselineNormal.Prediction.Prediction.WinProbability - tacticEvaluation.Prediction.Prediction.WinProbability);
        var tradeoff = Clamp01(
            0.60 * normalOpportunityLoss +
            0.25 * winProbabilityLoss +
            0.15 * documentedDefencePenalty * (1.0 - defenceQuality));

        // These weights are V5 suitability heuristics. The live Creative suitability
        // function is not public; final tactic choice must use the outcome layer.
        var suitability = Clamp01(
            0.35 * eventUpside +
            0.20 * tacticalInput +
            0.15 * opponentInteraction +
            0.10 * diversity +
            0.10 * defenceQuality -
            0.35 * negativeRisk -
            0.25 * tradeoff);

        var score = Clamp01(
            0.55 * suitability +
            0.45 * Clamp01(tacticEvaluation.Prediction.Prediction.WinProbability));

        var explanation =
            $"Creative: level {tacticalLevel:0.##}; M9 own SE goals {ownSpecialGoals:0.###}; " +
            $"opponent SE goals {opponentSpecialGoals:0.###}; event edge {opponentInteraction:P0}; " +
            $"tactical input {tacticalInput:P0}; specialty diversity {diversity:P0}; " +
            $"negative-event share {negativeEventShare:P0}; 7.5% defence penalty; " +
            $"Normal opportunity loss {normalOpportunityLoss:P0}.";

        return new TacticFitResult(
            TeamTactic.Creative,
            score,
            eventUpside,
            tradeoff,
            Clamp01(0.65 * tacticalInput + 0.35 * diversity),
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

    private static double DefensiveNegativeEventExposure(IEnumerable<Player> players)
    {
        var list = players.ToArray();
        if (list.Length == 0) return 0;
        var vulnerable = list.Count(p =>
            p.Specialty == PlayerSpecialty.Unpredictable &&
            p.Defending > 0 &&
            p.Defending < Math.Max(1, Math.Max(p.Experience, p.Stamina)));
        return Clamp01(vulnerable / 3.0);
    }

    private static double SpecialEventGoals(M9EventGoalBreakdown e)
        => Math.Max(0.0, e.PlayerBasedSpecialEventGoals +
                         e.TeamBasedSpecialEventGoals +
                         e.PowerfulNormalForwardGoals);

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
