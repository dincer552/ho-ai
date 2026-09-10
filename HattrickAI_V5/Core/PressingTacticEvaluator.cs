namespace HattrickAI.V5.Core;

/// <summary>
/// Dedicated Pressing suitability model.
/// The evaluator keeps Pressing's own objective separate from the generic tactic score:
/// suppress normal chances, verify XI-wide DEF/STAM/EXP support, account for Powerful
/// defenders, compare the Normal opportunity cost, and penalize poor stamina resilience.
///
/// Exact historical event/level distributions remain calibration data; this class does
/// not invent a hidden official formula.
/// </summary>
public static class PressingTacticEvaluator
{
    // V5 heuristic weights. These are implementation coefficients, not official Hattrick formulas.
    public const double SquadDefenceWeight = 0.42;
    public const double SquadStaminaWeight = 0.33;
    public const double SquadExperienceWeight = 0.15;
    public const double SquadPowerfulWeight = 0.10;
    public const double NetSuppressionWeight = 0.60;
    public const double NetSuppressionExcessWeight = 0.25;
    public const double NetSuppressionAttackWeight = 0.15;
    public const double OpportunityOwnChanceLossWeight = 0.48;
    public const double OpportunityStaminaRiskWeight = 0.22;
    public const double OpportunityWinProbabilityLossWeight = 0.18;
    public const double OpportunityMidfieldRiskWeight = 0.12;
    public const double MatchupAttackWeight = 0.45;
    public const double MatchupSuppressionWeight = 0.30;
    public const double MatchupLowOpponentQualityWeight = 0.15;
    public const double MatchupStaminaGapWeight = 0.10;
    public const double PrimaryNetSuppressionWeight = 0.55;
    public const double PrimaryTacticalSignalWeight = 0.25;
    public const double PrimaryDefenceWeight = 0.20;
    public const double ScorePrimaryWeight = 0.40;
    public const double ScoreSquadFitWeight = 0.25;
    public const double ScoreMatchupWeight = 0.20;
    public const double ScoreTacticalSignalWeight = 0.15;
    public const double ScoreOpportunityCostPenalty = 0.25;
    public const double StaminaAverageWeight = 0.75;
    public const double StaminaWeakestWeight = 0.25;

    public static TacticFitResult Evaluate(
        Lineup lineup,
        ComparisonEvaluationView baselineNormal,
        ComparisonEvaluationView pressingEvaluation,
        IReadOnlyList<Player> players,
        IReadOnlyList<Player>? opponentPlayers = null)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(baselineNormal);
        ArgumentNullException.ThrowIfNull(pressingEvaluation);
        ArgumentNullException.ThrowIfNull(players);

        var xi = LineupPlayers(lineup, players);
        var outfield = xi.Where(p => !IsGoalkeeper(lineup, p.Id)).ToArray();
        if (outfield.Length == 0)
            return new TacticFitResult(TeamTactic.Pressing, 0, 0, 1, 0, 0, false, "Pressing suitability failed: no outfield players in XI.");

        var own = pressingEvaluation.Chance;
        var baseline = baselineNormal.Chance;

        var defenceSupport = DefenceSupport(outfield);
        var staminaSupport = StaminaSupport(outfield);
        var experienceSupport = ExperienceSupport(outfield);
        var powerfulBoost = PowerfulDefenceBoost(outfield);
        var squadFit = Math.Clamp(
            SquadDefenceWeight * defenceSupport +
            SquadStaminaWeight * staminaSupport +
            SquadExperienceWeight * experienceSupport +
            SquadPowerfulWeight * powerfulBoost,
            0, 1);

        var opponentSuppression = RelativeReduction(baseline.OpponentRegularChanceExpected, own.OpponentRegularChanceExpected);
        var ownChanceLoss = RelativeReduction(baseline.OwnRegularChanceExpected, own.OwnRegularChanceExpected);
        var opponentAttackValue = Clamp01(own.OpponentRegularQuality);
        var midfieldRisk = Clamp01((0.50 - own.MidfieldShare) / 0.50);

        var netSuppression = Clamp01(NetSuppressionWeight * opponentSuppression + NetSuppressionExcessWeight * Math.Max(0, opponentSuppression - ownChanceLoss) + NetSuppressionAttackWeight * opponentAttackValue);

        var staminaRisk = 1.0 - staminaSupport;
        var opportunityCost = Clamp01(
            OpportunityOwnChanceLossWeight * ownChanceLoss +
            OpportunityStaminaRiskWeight * staminaRisk +
            OpportunityWinProbabilityLossWeight * Math.Max(0, baselineNormal.Prediction.Prediction.WinProbability - pressingEvaluation.Prediction.Prediction.WinProbability) +
            OpportunityMidfieldRiskWeight * midfieldRisk);

        var matchup = Clamp01(
            MatchupAttackWeight * opponentAttackValue +
            MatchupSuppressionWeight * opponentSuppression +
            MatchupLowOpponentQualityWeight * (1.0 - Clamp01(own.OpponentRegularQuality)) +
            MatchupStaminaGapWeight * StaminaGap(outfield, opponentPlayers));

        var tacticalSignal = Clamp01(pressingEvaluation.Advanced.Level.Value / 10.0);
        var primary = Clamp01(PrimaryNetSuppressionWeight * netSuppression + PrimaryTacticalSignalWeight * tacticalSignal + PrimaryDefenceWeight * defenceSupport);

        var score = Math.Clamp(
            ScorePrimaryWeight * primary +
            ScoreSquadFitWeight * squadFit +
            ScoreMatchupWeight * matchup +
            ScoreTacticalSignalWeight * tacticalSignal -
            ScoreOpportunityCostPenalty * opportunityCost,
            0, 1);

        var explanation =
            $"Pressing: suppression {opponentSuppression:P1}; own chance loss {ownChanceLoss:P1}; " +
            $"net suppression {Math.Max(0, opponentSuppression - ownChanceLoss):P1}; " +
            $"DEF fit {defenceSupport:P1}; STAM fit {staminaSupport:P1}; EXP fit {experienceSupport:P1}; " +
            $"Powerful DEF boost {powerfulBoost:P1}; stamina risk {staminaRisk:P1}; " +
            $"opponent attack value {opponentAttackValue:P1}.";

        return new TacticFitResult(
            TeamTactic.Pressing,
            score,
            primary,
            opportunityCost,
            squadFit,
            matchup,
            true,
            explanation);
    }

    private static IReadOnlyList<Player> LineupPlayers(Lineup lineup, IReadOnlyList<Player> players)
    {
        var byId = players.ToDictionary(p => p.Id);
        return lineup.Slots
            .Where(s => s.PlayerId > 0 && byId.ContainsKey(s.PlayerId))
            .Select(s => byId[s.PlayerId])
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToArray();
    }

    private static bool IsGoalkeeper(Lineup lineup, int playerId)
        => lineup.Slots.Any(s => s.PlayerId == playerId && s.Code == "GK");

    private static double DefenceSupport(IReadOnlyList<Player> outfield)
        => Clamp01(outfield.Average(p => p.Defending) / 10.0);

    private static double StaminaSupport(IReadOnlyList<Player> outfield)
    {
        var average = Clamp01(outfield.Average(p => p.Stamina) / 10.0);
        var weakest = Clamp01(outfield.Min(p => p.Stamina) / 10.0);
        return Clamp01(StaminaAverageWeight * average + StaminaWeakestWeight * weakest);
    }

    private static double ExperienceSupport(IReadOnlyList<Player> outfield)
        => Clamp01(outfield.Average(p => p.Experience) / 10.0);

    private static double PowerfulDefenceBoost(IReadOnlyList<Player> outfield)
    {
        var normalDefence = outfield.Sum(p => Math.Max(0, p.Defending));
        if (normalDefence <= 0) return 0;
        var weightedDefence = outfield.Sum(p => p.Specialty == PlayerSpecialty.Powerful
            ? 2.0 * Math.Max(0, p.Defending)
            : Math.Max(0, p.Defending));
        return Clamp01((weightedDefence / normalDefence - 1.0) / 1.0);
    }

    private static double StaminaGap(IReadOnlyList<Player> ownOutfield, IReadOnlyList<Player>? opponentPlayers)
    {
        if (opponentPlayers is null || opponentPlayers.Count == 0) return 0.5;
        var own = ownOutfield.Average(p => p.Stamina);
        var opponent = opponentPlayers.Where(p => p.Keeper <= 0).Select(p => p.Stamina).DefaultIfEmpty(10).Average();
        return Clamp01(0.5 + ((own - opponent) / 20.0));
    }

    private static double RelativeReduction(double baseline, double tactic)
        => baseline <= 1e-9 ? 0 : Clamp01((baseline - tactic) / baseline);

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);
}
