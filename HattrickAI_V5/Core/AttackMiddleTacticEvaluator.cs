namespace HattrickAI.V5.Core;

/// <summary>
/// Attack in the Middle (AiM) specific suitability evaluator.
///
/// Sources/mechanics used by this evaluator:
/// - Official Hattrick rules/manual: AiM trades wing attacks for centre attacks;
///   roughly 15-30% in the manual, with total outfield Passing determining tactic skill
///   and an experience bonus contributing to the live tactic level.
/// - 2026 Constantinou et al. match-engine study: AiM wing-to-centre conversion is
///   modelled at 20-35%, producing roughly 47-55% central share versus 36.15% Normal.
/// - The same paper's Eq. C.2 provides the AiM tactic-conversion curve; M8 already
///   applies that exact curve and this evaluator consumes the resulting value.
/// - The public sources do not publish a trustworthy exact defensive penalty coefficient.
///   Therefore the evaluator uses an explicit bounded risk proxy rather than inventing
///   a hidden-engine coefficient.
///
/// The evaluator is intentionally broad: Requirements, Benefit, Opportunity Cost,
/// Opponent Interaction, Defensive Risk, Possession Context, Suitability and Explanation
/// are all considered. It does not replace M8/M9 mechanics; it evaluates whether AiM is
/// a good decision for this XI and this opponent.
/// </summary>
public static class AttackMiddleTacticEvaluator
{
    private const double NormalCentreShare = M8ChanceAllocationEngine.PaperCentreAttackShare;
    private const double NormalWingShare = M8ChanceAllocationEngine.PaperLeftAttackShare + M8ChanceAllocationEngine.PaperRightAttackShare;
    private const double AiMMinCentreShare = 0.47;
    private const double AiMMaxCentreShare = 0.55;
    private const double AiMWingDefenseRiskProxy = 0.10;
    private const double StrongPossessionThreshold = 0.50;

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
        var outfield = xi.Where(p => SlotFor(lineup, p.Id) is { } slot && !IsGoalkeeperSlot(slot)).ToArray();

        // -----------------------------------------------------------------
        // 1. REQUIREMENTS: AiM tactical skill is primarily total outfield
        // Passing. Experience contributes to the live tactical level.
        // The exact experience-bonus formula is not public, so experience is
        // deliberately a soft fit factor rather than a fabricated equation.
        // -----------------------------------------------------------------
        var passingTotal = outfield.Sum(p => Math.Max(0, p.Passing));
        var averageExperience = Average(outfield.Select(p => p.Experience));
        var passingFit = Clamp01(passingTotal / 105.0);
        var experienceFit = Clamp01(averageExperience / 10.0);

        // M8 is the single source of truth for the paper-derived conversion.
        // For AiM this is the 20-35% wing -> centre conversion curve.
        var conversion = Clamp01(own.TacticConversionRate);
        var conversionFit = Clamp01(
            (conversion - M8ChanceAllocationEngine.AiMMinWingConversion) /
            (M8ChanceAllocationEngine.AiMMaxWingConversion - M8ChanceAllocationEngine.AiMMinWingConversion));

        // -----------------------------------------------------------------
        // 2. BENEFIT: how much of the team's attack has actually moved into
        // the centre, and how good is the centre matchup?
        // -----------------------------------------------------------------
        var centreShare = Clamp01(own.CentreChanceShare);
        var centreShareFit = Clamp01((centreShare - AiMMinCentreShare) / (AiMMaxCentreShare - AiMMinCentreShare));
        var centreQuality = Clamp01(own.CentreAttackVsCentreDefence);
        var leftQuality = Clamp01(own.LeftAttackVsRightDefence);
        var rightQuality = Clamp01(own.RightAttackVsLeftDefence);
        var wingQuality = WeightedWingQuality(leftQuality, rightQuality, baseline.LeftChanceShare, baseline.RightChanceShare);

        // AiM is valuable when the centre is materially better than the wings.
        var centreVsWingAdvantage = Clamp01(0.5 + (centreQuality - wingQuality) / 2.0);
        var convertedWingShare = Math.Max(0.0, centreShare - NormalCentreShare);
        var wingShareAfter = Math.Max(0.0, own.LeftChanceShare + own.RightChanceShare);

        // Net attack-quality change from the redirected volume. This is more
        // informative than rewarding centre quality alone: moving a chance from
        // a strong wing to a slightly better centre has little value.
        var expectedMovedAttack = Math.Max(0.0,
            own.OwnRegularChanceExpected * convertedWingShare);
        var expectedNetScoringGain = expectedMovedAttack *
            Math.Max(-1.0, centreQuality - wingQuality);

        // -----------------------------------------------------------------
        // 3. POSSESSION CONTEXT: AiM does not create more total chances.
        // If we have little possession, redirecting attacks cannot compensate
        // for the underlying shortage of opportunities and the defence penalty
        // becomes harder to justify.
        // -----------------------------------------------------------------
        var possession = Clamp01(own.MidfieldShare);
        var possessionFit = PossessionFit(possession);

        // -----------------------------------------------------------------
        // 4. OPPONENT INTERACTION: evaluate the two weakened wing-defence
        // sectors against the opponent's actual attack-vs-defence matchups.
        // The 2026 paper calls the defensive penalty small; public rules say
        // wing defence gets somewhat worse. We therefore model only bounded
        // decision risk, not a hidden exact rating multiplier.
        // -----------------------------------------------------------------
        var opponentLeftThreat = Clamp01(own.OpponentLeftAttackVsOwnRightDefence);
        var opponentRightThreat = Clamp01(own.OpponentRightAttackVsOwnLeftDefence);
        var opponentWingThreat = WeightedWingQuality(
            opponentLeftThreat,
            opponentRightThreat,
            own.OpponentLeftAttackVsOwnRightDefence,
            own.OpponentRightAttackVsOwnLeftDefence);
        var defencePenaltyRisk = Clamp01(AiMWingDefenseRiskProxy * opponentWingThreat);

        // Approximate incremental defensive danger from the part of the opponent's
        // regular attack that arrives on the two weakened wings. It is used only as
        // a relative opportunity-cost signal.
        var opponentWingChanceShare = Clamp01(
            own.LeftChanceShare + own.RightChanceShare);
        var expectedDefensiveDamage = Math.Max(0.0,
            own.OpponentRegularChanceExpected * opponentWingChanceShare *
            opponentWingThreat * AiMWingDefenseRiskProxy);

        // -----------------------------------------------------------------
        // 5. OPPORTUNITY COST: compare AiM against the Normal baseline.
        // We explicitly measure the wing volume sacrificed, own chance-volume
        // loss, and the resulting match-prediction change.
        // -----------------------------------------------------------------
        var baselineWingQuality = WeightedWingQuality(
            baseline.LeftAttackVsRightDefence,
            baseline.RightAttackVsLeftDefence,
            baseline.LeftChanceShare,
            baseline.RightChanceShare);
        var baselineCentreQuality = Clamp01(baseline.CentreAttackVsCentreDefence);
        var baselineWingVolume = Clamp01(baseline.LeftChanceShare + baseline.RightChanceShare);
        var redirectedVolume = Math.Max(0.0, baselineWingVolume - wingShareAfter);
        var wingOpportunityCost = redirectedVolume * baselineWingQuality;
        var ownChanceVolumeLoss = RelativeLoss(
            baseline.OwnRegularChanceExpected,
            own.OwnRegularChanceExpected);
        var winProbabilityLoss = Math.Max(0.0,
            baselineNormal.Prediction.Prediction.WinProbability -
            tacticEvaluation.Prediction.Prediction.WinProbability);

        // If AiM's centre is not better than the baseline centre route, the
        // tactic is paying its defensive price without a real directional gain.
        var directionalGain = Clamp01(
            0.60 * Math.Max(0.0, centreQuality - baselineCentreQuality) +
            0.40 * Math.Max(0.0, centreQuality - wingQuality));

        // -----------------------------------------------------------------
        // 6. COMPONENT SCORES
        // -----------------------------------------------------------------
        var primary = Clamp01(
            0.24 * centreVsWingAdvantage +
            0.20 * conversionFit +
            0.16 * centreShareFit +
            0.16 * Clamp01((expectedNetScoringGain + 0.25) / 0.50) +
            0.10 * passingFit +
            0.06 * experienceFit +
            0.08 * possessionFit);

        var matchup = Clamp01(
            0.35 * centreQuality +
            0.25 * centreVsWingAdvantage +
            0.20 * directionalGain +
            0.20 * possessionFit);

        var squadFit = Clamp01(
            0.55 * passingFit +
            0.20 * experienceFit +
            0.15 * Clamp01(inputs.TotalPassing / 105.0) +
            0.10 * Clamp01(inputs.TotalScoring / 105.0));

        var tradeoff = Clamp01(
            0.30 * Clamp01(wingOpportunityCost / 0.35) +
            0.25 * ownChanceVolumeLoss +
            0.20 * defencePenaltyRisk +
            0.15 * Clamp01(expectedDefensiveDamage / 0.50) +
            0.10 * winProbabilityLoss);

        // Main suitability: AiM must earn its defensive/volume cost through
        // a genuinely better central route. Prediction probability is included
        // as a final match-level guard, not as a replacement for tactic logic.
        var suitability = Clamp01(
            0.55 * primary +
            0.25 * matchup +
            0.20 * squadFit -
            0.45 * tradeoff);

        var score = Clamp01(
            0.75 * suitability +
            0.15 * directionalGain +
            0.10 * tacticEvaluation.Prediction.Prediction.WinProbability);

        var explanation =
            $"AiM: Passing {passingTotal:0}; Exp {averageExperience:0.0}; " +
            $"conversion {conversion:P1}; centre share {centreShare:P1}; " +
            $"centre matchup {centreQuality:P0}; wing quality {wingQuality:P0}; " +
            $"centre-vs-wing {centreVsWingAdvantage:P0}; moved attack {expectedMovedAttack:0.##}; " +
            $"net scoring gain {expectedNetScoringGain:0.##}; possession {possession:P0}; " +
            $"wing defence risk {defencePenaltyRisk:P0}; defensive damage {expectedDefensiveDamage:0.##}; " +
            $"wing opportunity cost {wingOpportunityCost:0.##}; trade-off {tradeoff:P0}.";

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

    private static double WeightedWingQuality(double left, double right, double leftWeight, double rightWeight)
    {
        var sum = Math.Max(1e-9, leftWeight + rightWeight);
        return Clamp01((left * leftWeight + right * rightWeight) / sum);
    }

    private static double PossessionFit(double possession)
    {
        // Hattrick's public tactical guidance recommends having possession for
        // attack-direction tactics because the total chance pool is unchanged.
        // This is a soft preference, not an eligibility threshold.
        if (possession >= StrongPossessionThreshold)
            return Clamp01(0.75 + (possession - StrongPossessionThreshold) * 0.50);
        return Clamp01(possession / StrongPossessionThreshold * 0.75);
    }

    private static double Average(IEnumerable<int> values)
    {
        var a = values.Where(v => v >= 0).ToArray();
        return a.Length == 0 ? 0.0 : a.Average();
    }

    private static double RelativeLoss(double baseline, double tactic)
        => baseline <= 1e-9 ? 0.0 : Clamp01((baseline - tactic) / baseline);

    private static double Clamp01(double value) => Math.Clamp(value, 0.0, 1.0);
}
