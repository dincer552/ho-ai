namespace HattrickAI.V5.Core;

/// <summary>
/// CounterAttack-specific suitability evaluation.
/// Uses the 2026 Hattrick research model as the behavioural baseline:
/// lower pre-penalty midfield is required, 7% midfield reduction is paid,
/// opponent missed Normal chances create CA opportunities, and CA conversion
/// is bounded by the research-derived tactic curve. The evaluator then judges
/// whether the XI has enough defence and finishing power to make those chances useful.
/// </summary>
public static class CounterAttackTacticEvaluator
{
    private const double PaperNormalNonTacticalCaRate = 0.038;
    private const double PaperNonTacticalCaTwoDefenders = 0.0175;
    private const double PaperNonTacticalCaThreeDefenders = 0.0363;
    private const double PaperNonTacticalCaFourDefenders = 0.0604;
    private const double PaperNonTacticalCaFiveDefenders = 0.0803;
    private const double PaperTechnicalCaTwoDefenders = 0.0084;
    private const double PaperTechnicalCaThreeDefenders = 0.0100;
    private const double PaperTechnicalCaFourPlusDefenders = 0.0311;

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
        var xi = LineupPlayers(lineup, players);
        var opponent = opponentPlayers ?? Array.Empty<Player>();

        var ownMidfield = OwnMidfield(tacticEvaluation);
        var opponentMidfield = OpponentMidfield(tacticEvaluation);
        var eligible = own.CounterAttackEligible && ownMidfield < opponentMidfield;
        var conversion = Math.Clamp(own.CounterAttackConversionRate, 0.0, 1.0);

        var averageDefending = Average(xi.Select(p => p.Defending));
        var averagePassing = Average(xi.Select(p => p.Passing));
        var averageScoring = Average(xi.Select(p => p.Scoring));
        var defenseFit = Clamp01(averageDefending / 10.0);
        var passingFit = Clamp01(averagePassing / 10.0);
        var scoringFit = Clamp01(averageScoring / 10.0);

        var defenders = DefensivePlayers(lineup, xi).Count;
        var quickOwn = RelevantQuickAttackers(lineup, xi);
        var quickOpponentDefenders = RelevantQuickDefenders(opponent);
        var quickBoost = SpecialtyInteractionEngine.CounterAttackSpecialtyBoostPercent(quickOwn, quickOpponentDefenders);
        var technicalDefenders = DefensivePlayers(lineup, xi).Count(p => p.Specialty == PlayerSpecialty.Technical);
        var technicalCaRate = TechnicalCounterAttackRate(defenders, technicalDefenders);

        var missedNormal = Math.Max(0.0, own.MissedOpponentNormalChanceExpected);
        var tacticalCaExpected = eligible ? Math.Max(0.0, missedNormal * conversion) : 0.0;
        var attackFinish = CounterAttackFinishQuality(own, averageScoring, averagePassing);
        var conversionValue = Clamp01(conversion / M8ChanceAllocationEngine.CounterAttackMaxConversion);
        var opportunityValue = Clamp01(missedNormal / 8.745);
        var specialtyValue = Clamp01(0.70 * Clamp01(quickBoost / 0.14) + 0.30 * Clamp01(technicalCaRate / 0.0311));

        // The core CA upside comes from converting the opponent's missed Normal chances.
        // Defence is already represented in missedNormal, but remains explicit here because
        // a CA lineup must also survive long enough to benefit from the generated chances.
        var primary = Clamp01(
            0.35 * opportunityValue +
            0.30 * conversionValue +
            0.20 * attackFinish +
            0.10 * defenseFit +
            0.05 * specialtyValue);

        var midfieldOpportunity = Clamp01((opponentMidfield - ownMidfield) / Math.Max(1.0, opponentMidfield));
        var matchup = Clamp01(
            0.30 * midfieldOpportunity +
            0.25 * Clamp01(1.0 - own.MidfieldShare) +
            0.25 * Clamp01(opponent.OpponentRegularQuality <= 0 ? own.OpponentRegularQuality : own.OpponentRegularQuality) +
            0.20 * attackFinish);

        var ownChanceLoss = RelativeLoss(baseline.OwnRegularChanceExpected, own.OwnRegularChanceExpected);
        var winProbabilityLoss = Math.Max(0, baselineNormal.Prediction.Prediction.WinProbability - tacticEvaluation.Prediction.Prediction.WinProbability);
        var midfieldPenaltyCost = Clamp01((ownMidfield - own.EffectiveOwnMidfield()) / Math.Max(1.0, ownMidfield));
        var tradeoff = Clamp01(
            0.45 * ownChanceLoss +
            0.30 * midfieldPenaltyCost +
            0.25 * winProbabilityLoss);

        // CA is intentionally penalised when the tactic creates little usable volume,
        // when the XI has weak finishing, or when the 7% midfield sacrifice buys little.
        var suitability = Clamp01(
            0.55 * primary +
            0.20 * defenseFit +
            0.15 * passingFit +
            0.10 * specialtyValue -
            0.35 * tradeoff);

        if (!eligible)
        {
            return new TacticFitResult(
                TeamTactic.CounterAttack,
                0.0,
                0.0,
                1.0,
                Clamp01(0.55 * defenseFit + 0.25 * passingFit + 0.20 * scoringFit),
                0.0,
                false,
                $"CA uygun değil: 7% midfield penalty öncesi own MF {ownMidfield:0.##} >= opponent MF {opponentMidfield:0.##}.");
        }

        var score = Clamp01(0.70 * suitability + 0.30 * Clamp01(tacticEvaluation.Prediction.Prediction.WinProbability));
        var explanation =
            $"CA: pre-penalty MF {ownMidfield:0.##} vs {opponentMidfield:0.##}; 7% MF cost paid; " +
            $"missed opponent Normal {missedNormal:0.##}; tactical CA rate {conversion:P1}; expected CA {tacticalCaExpected:0.##}; " +
            $"DEF fit {defenseFit:P0}; finishing fit {attackFinish:P0}; Quick CA boost {quickBoost:P1}; " +
            $"Technical CA support {technicalCaRate:P1}; opportunity cost {tradeoff:P0}.";

        return new TacticFitResult(
            TeamTactic.CounterAttack,
            score,
            primary,
            tradeoff,
            Clamp01(0.55 * defenseFit + 0.25 * passingFit + 0.20 * scoringFit),
            matchup,
            true,
            explanation);
    }

    private static double OwnMidfield(ComparisonEvaluationView evaluation)
        => evaluation.Chance.Allocation.EffectiveOwnMidfield > 0
            ? evaluation.Chance.Allocation.EffectiveOwnMidfield / 0.93
            : 0.0;

    private static double OpponentMidfield(ComparisonEvaluationView evaluation)
        => evaluation.Chance.Allocation.EffectiveOwnMidfield > 0
            ? evaluation.Chance.Allocation.EffectiveOwnMidfield / Math.Max(0.01, 1.0 - M8ChanceAllocationEngine.CounterAttackMidfieldPenalty)
            : InferOpponentMidfieldFromPossession(evaluation.Chance.MidfieldShare);

    private static double InferOpponentMidfieldFromPossession(double possession)
    {
        if (possession <= 0.0 || possession >= 1.0) return 0.0;
        // Only a fallback when direct opponent midfield is unavailable from the comparison view.
        // The primary eligibility path uses M8's CounterAttackEligible flag.
        return Math.Max(0.0, possession < 0.5 ? possession * 20.0 : possession * 10.0);
    }

    private static double CounterAttackFinishQuality(M8ChanceResult chance, double averageScoring, double averagePassing)
    {
        var sector = Clamp01(
            0.45 * chance.LeftAttackVsRightDefence +
            0.10 * chance.CentreAttackVsCentreDefence +
            0.45 * chance.RightAttackVsLeftDefence);
        var player = Clamp01(0.65 * (averageScoring / 10.0) + 0.35 * (averagePassing / 10.0));
        return Clamp01(0.70 * sector + 0.30 * player);
    }

    private static double TechnicalCounterAttackRate(int defenders, int technicalDefenders)
    {
        if (technicalDefenders <= 0) return 0.0;
        return defenders switch
        {
            <= 2 => PaperTechnicalCaTwoDefenders,
            3 => PaperTechnicalCaThreeDefenders,
            _ => PaperTechnicalCaFourPlusDefenders
        };
    }

    private static double NonTacticalCounterAttackRate(int defenders)
        => defenders switch
        {
            <= 2 => PaperNonTacticalCaTwoDefenders,
            3 => PaperNonTacticalCaThreeDefenders,
            4 => PaperNonTacticalCaFourDefenders,
            _ => PaperNonTacticalCaFiveDefenders
        };

    private static int RelevantQuickAttackers(Lineup lineup, IReadOnlyList<Player> players)
        => players.Count(p => p.Specialty == PlayerSpecialty.Quick && SlotFor(lineup, p.Id) is not null && !IsDefensiveSlot(SlotFor(lineup, p.Id)!));

    private static int RelevantQuickDefenders(IReadOnlyList<Player> players)
        => players.Count(p => p.Specialty == PlayerSpecialty.Quick && p.Defending >= p.Scoring && p.Defending >= p.Playmaking);

    private static IReadOnlyList<Player> DefensivePlayers(Lineup lineup, IReadOnlyList<Player> players)
        => players.Where(p => SlotFor(lineup, p.Id) is { } slot && IsDefensiveSlot(slot)).ToArray();

    private static bool IsDefensiveSlot(string code)
        => code.StartsWith("DEF", StringComparison.Ordinal) || code.StartsWith("GK", StringComparison.Ordinal);

    private static string? SlotFor(Lineup lineup, int playerId)
        => lineup.Slots.FirstOrDefault(s => s.PlayerId == playerId)?.Code;

    private static IReadOnlyList<Player> LineupPlayers(Lineup lineup, IReadOnlyList<Player> players)
    {
        var ids = lineup.Slots.Where(s => s.PlayerId > 0).Select(s => s.PlayerId).ToHashSet();
        return players.Where(p => ids.Contains(p.Id)).ToArray();
    }

    private static double RelativeLoss(double baseline, double tactic)
        => baseline <= 1e-9 ? 0.0 : Clamp01((baseline - tactic) / baseline);

    private static double Average(IEnumerable<int> values)
    {
        var a = values.Where(v => v > 0).ToArray();
        return a.Length == 0 ? 0.0 : a.Average();
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0.0, 1.0);
}
