namespace HattrickAI.V5.Core;

/// <summary>
/// Attack on Wings (AoW) specific suitability evaluator.
///
/// The evaluator separates AoW suitability from the generic TacticalScore path.
/// It uses the public Hattrick mechanics plus the 2026 Constantinou et al.
/// match-engine study. M8 remains the source of truth for the actual tactic
/// conversion and chance distribution; this class decides whether the XI and
/// opponent make that conversion worth using.
///
/// AoW mechanics used here:
/// - total outfield Passing determines tactical skill;
/// - Experience contributes to the live tactical level;
/// - official guidance: roughly 20-40% of centre attacks can be converted;
/// - 2026 paper: 34-52% centre -> wing conversion, with wing share rising
///   from 51.3% Normal to roughly 63-70% under AoW;
/// - the price is a weaker central defence;
/// - the tactic trades chance direction rather than creating a larger normal
///   chance pool.
///
/// No hidden exact central-defence penalty coefficient is invented here. The
/// defensive cost is represented as a bounded decision-risk proxy and the
/// actual M8/M9 mechanics remain authoritative.
/// </summary>
public static class AttackWingsTacticEvaluator
{
    private const double NormalCentreShare = M8ChanceAllocationEngine.PaperCentreAttackShare;
    private const double NormalWingShare =
        M8ChanceAllocationEngine.PaperLeftAttackShare + M8ChanceAllocationEngine.PaperRightAttackShare;
    private const double AoWMinWingShare = 0.63;
    private const double AoWMaxWingShare = 0.70;
    private const double AoWCentralDefenseRiskProxy = 0.10;
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
        // 1. REQUIREMENTS
        // -----------------------------------------------------------------
        // AoW tactical skill is driven by total outfield Passing. Experience
        // contributes to the live level, but its exact public formula is not
        // sufficiently documented to justify fabricating one here.
        var passingTotal = outfield.Sum(p => Math.Max(0, p.Passing));
        var averageExperience = Average(outfield.Select(p => p.Experience));
        var passingFit = Clamp01(passingTotal / 110.0);
        var experienceFit = Clamp01(averageExperience / 10.0);

        // M8 already applies Constantinou et al. Eq. C.2 through the explicit
        // V5->paper RT bridge. Consume that result instead of creating another
        // tactic-strength formula here.
        var conversion = Clamp01(own.TacticConversionRate);
        var conversionFit = Clamp01(
            (conversion - M8ChanceAllocationEngine.AoWMinCentreConversion) /
            (M8ChanceAllocationEngine.AoWMaxCentreConversion - M8ChanceAllocationEngine.AoWMinCentreConversion));

        // -----------------------------------------------------------------
        // 2. BENEFIT: did we move enough centre volume, and are the wings
        // actually better scoring routes for this XI and opponent?
        // -----------------------------------------------------------------
        var wingShare = Clamp01(own.LeftChanceShare + own.RightChanceShare);
        var wingShareFit = Clamp01((wingShare - AoWMinWingShare) / (AoWMaxWingShare - AoWMinWingShare));
        var centreShare = Clamp01(own.CentreChanceShare);

        var leftQuality = Clamp01(own.LeftAttackVsRightDefence);
        var rightQuality = Clamp01(own.RightAttackVsLeftDefence);
        var wingQuality = WeightedWingQuality(
            leftQuality,
            rightQuality,
            own.LeftChanceShare,
            own.RightChanceShare);
        var centreQuality = Clamp01(own.CentreAttackVsCentreDefence);

        // The tactic is useful when the two wing routes collectively beat the
        // central route that they are replacing. Keep this continuous so one
        // weak wing does not disappear behind a simple average.
        var wingVsCentreAdvantage = Clamp01(0.5 + (wingQuality - centreQuality) / 2.0);
        var convertedCentreShare = Math.Max(0.0, NormalCentreShare - centreShare);
        var expectedMovedAttack = Math.Max(0.0,
            own.OwnRegularChanceExpected * convertedCentreShare);
        var expectedNetScoringGain = expectedMovedAttack *
            Math.Max(-1.0, wingQuality - centreQuality);

        // Directional gain against the Normal baseline. AoW is not rewarded
        // merely for having high wing ratings; the redirected volume must have
        // a better route than the centre route it replaces.
        var baselineWingQuality = WeightedWingQuality(
            baseline.LeftAttackVsRightDefence,
            baseline.RightAttackVsLeftDefence,
            baseline.LeftChanceShare,
            baseline.RightChanceShare);
        var baselineCentreQuality = Clamp01(baseline.CentreAttackVsCentreDefence);
        var directionalGain = Clamp01(
            0.60 * Math.Max(0.0, wingQuality - baselineCentreQuality) +
            0.40 * Math.Max(0.0, wingQuality - baselineWingQuality));

        // -----------------------------------------------------------------
        // 3. POSSESSION CONTEXT
        // -----------------------------------------------------------------
        // AoW does not create a new normal-chance pool. Possession therefore
        // matters because the tactic is most valuable when we expect enough of
        // the team's normal opportunities to exploit the better direction.
        var possession = Clamp01(own.MidfieldShare);
        var possessionFit = PossessionFit(possession);

        // -----------------------------------------------------------------
        // 4. OPPONENT INTERACTION / DEFENSIVE PRICE
        // -----------------------------------------------------------------
        // Public sources state that AoW weakens central defence. The opponent's
        // central attack-vs-our-central-defence matchup therefore determines how
        // expensive that weakness is. We use a bounded 10% risk proxy rather
        // than claiming an undocumented engine multiplier.
        var opponentCentreThreat = Clamp01(own.OpponentCentreAttackVsOwnCentreDefence);
        var centralDefenseRisk = Clamp01(AoWCentralDefenseRiskProxy * opponentCentreThreat);
        var expectedDefensiveDamage = Math.Max(0.0,
            own.OpponentRegularChanceExpected *
            Clamp01(own.CentreChanceShare) *
            opponentCentreThreat *
            AoWCentralDefenseRiskProxy);

        // If the opponent already attacks mainly through the centre, weakening
        // that sector is more dangerous. Conversely, a weak opponent centre
        // reduces the cost of AoW.
        var centralThreatFit = 1.0 - opponentCentreThreat;

        // -----------------------------------------------------------------
        // 5. OPPORTUNITY COST AGAINST NORMAL
        // -----------------------------------------------------------------
        // 1:1 directional exchange means we should explicitly charge AoW for
        // the central attacks it gives up. M8's chance pool remains the source
        // of truth for actual volume changes.
        var redirectedVolume = Math.Max(0.0, wingShare - NormalWingShare);
        var centreOpportunityCost = redirectedVolume * Math.Max(0.0, baselineCentreQuality);
        var ownChanceVolumeLoss = RelativeLoss(
            baseline.OwnRegularChanceExpected,
            own.OwnRegularChanceExpected);
        var winProbabilityLoss = Math.Max(0.0,
            baselineNormal.Prediction.Prediction.WinProbability -
            tacticEvaluation.Prediction.Prediction.WinProbability);

        var tradeoff = Clamp01(
            0.30 * Clamp01(centreOpportunityCost / 0.35) +
            0.20 * ownChanceVolumeLoss +
            0.25 * centralDefenseRisk +
            0.15 * Clamp01(expectedDefensiveDamage / 0.50) +
            0.10 * winProbabilityLoss);

        // -----------------------------------------------------------------
        // 6. COMPONENT SCORES
        // -----------------------------------------------------------------
        var primary = Clamp01(
            0.24 * wingVsCentreAdvantage +
            0.18 * conversionFit +
            0.14 * wingShareFit +
            0.16 * Clamp01((expectedNetScoringGain + 0.25) / 0.50) +
            0.10 * passingFit +
            0.05 * experienceFit +
            0.08 * possessionFit +
            0.05 * centralThreatFit);

        var matchup = Clamp01(
            0.35 * wingQuality +
            0.25 * wingVsCentreAdvantage +
            0.20 * directionalGain +
            0.10 * possessionFit +
            0.10 * centralThreatFit);

        // Passing is the documented tactical input. Winger/Scoring totals are
        // soft squad-context signals only; M8's sector matchup remains primary.
        var squadFit = Clamp01(
            0.60 * passingFit +
            0.15 * experienceFit +
            0.15 * Clamp01(inputs.TotalWinger / 40.0) +
            0.10 * Clamp01(inputs.TotalScoring / 105.0));

        // Suitability deliberately makes the directional gain earn the central
        // defence price. This prevents high tactical skill from automatically
        // selecting AoW against a strong central attack.
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
            $"AoW: Passing {passingTotal:0}; Exp {averageExperience:0.0}; " +
            $"conversion {conversion:P1}; wing share {wingShare:P1}; centre share {centreShare:P1}; " +
            $"wing matchup {wingQuality:P0}; centre matchup {centreQuality:P0}; " +
            $"wing-vs-centre {wingVsCentreAdvantage:P0}; moved attack {expectedMovedAttack:0.##}; " +
            $"net scoring gain {expectedNetScoringGain:0.##}; possession {possession:P0}; " +
            $"central-defence risk {centralDefenseRisk:P0}; defensive damage {expectedDefensiveDamage:0.##}; " +
            $"centre opportunity cost {centreOpportunityCost:0.##}; trade-off {tradeoff:P0}.";

        return new TacticFitResult(
            TeamTactic.AttackWings,
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
