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
        var inputs = pressingEvaluation.Advanced.Inputs;

        var defenceSupport = DefenceSupport(outfield);
        var staminaSupport = StaminaSupport(outfield);
        var experienceSupport = ExperienceSupport(outfield);
        var powerfulBoost = PowerfulDefenceBoost(outfield);
        var squadFit = Math.Clamp(
            0.42 * defenceSupport +
            0.33 * staminaSupport +
            0.15 * experienceSupport +
            0.10 * powerfulBoost,
            0, 1);

        var opponentSuppression = RelativeReduction(baseline.OpponentRegularChanceExpected, own.OpponentRegularChanceExpected);
        var ownChanceLoss = RelativeReduction(baseline.OwnRegularChanceExpected, own.OwnRegularChanceExpected);
        var opponentAttackValue = Clamp01(own.OpponentRegularQuality);
        var midfieldRisk = Clamp01((0.50 - own.MidfieldShare) / 0.50);

        // Pressing is valuable when it removes opponent normal chances without
        // sacrificing a disproportionate share of our own normal chances.
        var netSuppression = Clamp01(0.60 * opponentSuppression + 0.25 * Math.Max(0, opponentSuppression - ownChanceLoss) + 0.15 * opponentAttackValue);

        // Low stamina is a specific Pressing failure mode because pressure is
        // maintained by every outfield player and fatigue can erode later match play.
        var staminaRisk = 1.0 - staminaSupport;
        var opportunityCost = Clamp01(
            0.48 * ownChanceLoss +
            0.22 * staminaRisk +
            0.18 * Math.Max(0, baselineNormal.Prediction.Prediction.WinProbability - pressingEvaluation.Prediction.Prediction.WinProbability) +
            0.12 * midfieldRisk);

        // A strong midfield/attack opponent is a meaningful Pressing target, while
        // a weak opponent attack gives Pressing less value because there is less to suppress.
        var matchup = Clamp01(
            0.45 * opponentAttackValue +
            0.30 * opponentSuppression +
            0.15 * (1.0 - Clamp01(own.OpponentRegularQuality)) +
            0.10 * StaminaGap(outfield, opponentPlayers));

        // The tactical level is already represented by M7.2/M8. Here it is used as
        // an objective signal, not as a replacement for the player-by-player checks.
        var tacticalSignal = Clamp01(pressingEvaluation.Advanced.Level.Value / 10.0);
        var primary = Clamp01(0.55 * netSuppression + 0.25 * tacticalSignal + 0.20 * defenceSupport);

        var score = Math.Clamp(
            0.40 * primary +
            0.25 * squadFit +
            0.20 * matchup +
            0.15 * tacticalSignal -
            0.25 * opportunityCost,
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
        // Pressing uses every outfield player's stamina, so avoid treating one weak
        // stamina link as harmless while still keeping the average dominant.
        return Clamp01(0.75 * average + 0.25 * weakest);
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
