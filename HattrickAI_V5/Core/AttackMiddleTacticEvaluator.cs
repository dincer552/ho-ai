namespace HattrickAI.V5.Core;

/// <summary>
/// Attack in the Middle (AiM) specific suitability evaluator.
/// Research-backed mechanics:
/// - AiM converts 20-35% of wing attacks to the middle depending on tactic skill.
/// - The 2026 match-engine study models the resulting central share at roughly 47-55%.
/// - Tactical skill is driven by the passing of all outfield players; experience is also
///   part of the live-game tactical-level calculation.
/// - AiM carries a wing-defence penalty. The public 2026 study calls it a small penalty;
///   V5 uses a bounded 10% defensive-risk proxy rather than pretending an unpublished
///   exact coefficient is known.
/// </summary>
public static class AttackMiddleTacticEvaluator
{
    private const double WingDefensePenaltyProxy = 0.10;

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
        var inputs = tacticEvaluation.Advanced.Inputs;
        var xi = LineupPlayers(lineup, players);
        var opponent = opponentPlayers ?? Array.Empty<Player>();
        var outfield = xi.Where(p => SlotFor(lineup, p.Id) is { } slot && !IsGoalkeeperSlot(slot)).ToArray();

        var passingTotal = outfield.Sum(p => Math.Max(0, p.Passing));
        var passingFit = Clamp01(passingTotal / 105.0);
        var experienceFit = Clamp01(Average(outfield.Select(p => p.Experience)) / 10.0);

        // M8 already contains the paper-derived tactic conversion and resulting chance
        // distribution. Keep that value as the source of truth rather than re-deriving it.
        var conversion = Clamp01(own.TacticConversionRate);
        var convertedWingShare = Math.Max(0.0, own.CentreChanceShare - baseline.CentreChanceShare);
        var centralShareFit = Clamp01((own.CentreChanceShare - 0.3615) / (0.55 - 0.3615));
        var conversionFit = Clamp01(conversion / M8ChanceAllocationEngine.AiMMaxWingConversion);

        // Centre matchup is the direct upside: moved attacks only help if central attack
        // can beat the opponent's central defence. Wing quality is the opportunity-cost side.
        var centreQuality = Clamp01(own.CentreAttackVsCentreDefence);
        var wingQuality = Clamp01((own.LeftAttackVsRightDefence + own.RightAttackVsLeftDefence) / 2.0);
        var centralAdvantage = Clamp01(0.5 + (centreQuality - wingQuality) / 2.0);

        // Opponent wing pressure determines how expensive the defensive penalty is.
        // M8 exposes the opponent's sector scoring probabilities against our defence;
        // combine the two wings and apply the bounded 10% AiM defensive-risk proxy.
        var opponentWingThreat = Clamp01(
            0.50 * (own.OpponentLeftAttackVsOwnRightDefence + own.OpponentRightAttackVsOwnLeftDefence));
        var defensePenaltyRisk = Clamp01(WingDefensePenaltyProxy * opponentWingThreat);

        // A strong central attack plus a weak wing attack is the classic AiM sweet spot.
        // If wings are already the team's best scoring route, redirecting them is costly.
        var redirectionValue = Clamp01(
            0.45 * centralAdvantage +
            0.30 * conversionFit +
            0.15 * centralShareFit +
            0.10 * passingFit);

        var expectedCentralGain = Math.Max(0.0,
            own.OwnRegularChanceExpected * convertedWingShare * centreQuality);
        var expectedWingOpportunityCost = Math.Max(0.0,
            baseline.OwnRegularChanceExpected * Math.Max(0.0, baseline.CentreChanceShare - own.CentreChanceShare) * wingQuality);

        var primary = Clamp01(
            0.35 * centralAdvantage +
            0.25 * conversionFit +
            0.20 * Clamp01(expectedCentralGain / 1.5) +
            0.10 * passingFit +
            0.10 * experienceFit);

        var matchup = Clamp01(
            0.55 * centralAdvantage +
            0.25 * centreQuality +
            0.20 * Clamp01(expectedCentralGain / 1.5));

        var ownChanceLoss = RelativeLoss(baseline.OwnRegularChanceExpected, own.OwnRegularChanceExpected);
        var winProbabilityLoss = Math.Max(0.0,
            baselineNormal.Prediction.Prediction.WinProbability - tacticEvaluation.Prediction.Prediction.WinProbability);
        var tradeoff = Clamp01(
            0.45 * Clamp01(expectedWingOpportunityCost / 1.5) +
            0.25 * ownChanceLoss +
            0.20 * defensePenaltyRisk +
            0.10 * winProbabilityLoss);

        var squadFit = Clamp01(
            0.55 * passingFit +
            0.20 * experienceFit +
            0.15 * Clamp01(inputs.TotalPassing / 105.0) +
            0.10 * Clamp01(inputs.TotalScoring / 105.0));

        var suitability = Clamp01(
            0.55 * primary +
            0.25 * matchup +
            0.20 * squadFit -
            0.40 * tradeoff -
            0.20 * defensePenaltyRisk);

        var score = Clamp01(
            0.70 * suitability +
            0.20 * tacticEvaluation.Prediction.Prediction.WinProbability +
            0.10 * redirectionValue);

        var explanation =
            $"AiM: passing {passingTotal:0}; conversion {conversion:P1}; " +
            $"centre share {own.CentreChanceShare:P1}; centre matchup {centreQuality:P0}; " +
            $"central-vs-wing advantage {centralAdvantage:P0}; expected central gain {expectedCentralGain:0.##}; " +
            $"wing opportunity cost {expectedWingOpportunityCost:0.##}; wing defence risk {defensePenaltyRisk:P0}; " +
            $"passing fit {passingFit:P0}; trade-off {tradeoff:P0}.";

        return new TacticFitResult(
            TeamTactic.AttackMiddle,
            score,
            primary,
            tradeoff,
            squadFit,
            matchup,
            true,
            explanation);
    }

    private static IReadOnlyList<Player> LineupPlayers(Lineup lineup, IReadOnlyList<Player> players)
    {
        var ids = lineup.Slots.Where(s => s.PlayerId > 0).Select(s => s.PlayerId).ToHashSet();
        return players.Where(p => ids.Contains(p.Id)).ToArray();
    }

    private static string? SlotFor(Lineup lineup, int playerId)
        => lineup.Slots.FirstOrDefault(s => s.PlayerId == playerId)?.Code;

    private static bool IsGoalkeeperSlot(string code)
        => code.StartsWith("GK", StringComparison.Ordinal);

    private static double Average(IEnumerable<int> values)
    {
        var a = values.Where(v => v >= 0).ToArray();
        return a.Length == 0 ? 0.0 : a.Average();
    }

    private static double RelativeLoss(double baseline, double tactic)
        => baseline <= 1e-9 ? 0.0 : Clamp01((baseline - tactic) / baseline);

    private static double Clamp01(double value) => Math.Clamp(value, 0.0, 1.0);
}
