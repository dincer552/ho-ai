using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class PressingWeightRegression
{
    public static int Run()
    {
        AssertNear(0.42, PressingTacticEvaluator.SquadDefenceWeight, "DEF squad weight");
        AssertNear(0.33, PressingTacticEvaluator.SquadStaminaWeight, "STAM squad weight");
        AssertNear(0.15, PressingTacticEvaluator.SquadExperienceWeight, "EXP squad weight");
        AssertNear(0.10, PressingTacticEvaluator.SquadPowerfulWeight, "Powerful squad weight");
        AssertNear(1.0, PressingTacticEvaluator.SquadDefenceWeight + PressingTacticEvaluator.SquadStaminaWeight + PressingTacticEvaluator.SquadExperienceWeight + PressingTacticEvaluator.SquadPowerfulWeight, "squad-fit weights sum");

        AssertNear(0.60, PressingTacticEvaluator.NetSuppressionWeight, "suppression weight");
        AssertNear(0.25, PressingTacticEvaluator.NetSuppressionExcessWeight, "net-suppression excess weight");
        AssertNear(0.15, PressingTacticEvaluator.NetSuppressionAttackWeight, "opponent-attack suppression weight");
        AssertNear(1.0, PressingTacticEvaluator.NetSuppressionWeight + PressingTacticEvaluator.NetSuppressionExcessWeight + PressingTacticEvaluator.NetSuppressionAttackWeight, "net-suppression weights sum");

        AssertNear(0.48, PressingTacticEvaluator.OpportunityOwnChanceLossWeight, "own chance-loss opportunity weight");
        AssertNear(0.22, PressingTacticEvaluator.OpportunityStaminaRiskWeight, "stamina-risk opportunity weight");
        AssertNear(0.18, PressingTacticEvaluator.OpportunityWinProbabilityLossWeight, "win-probability opportunity weight");
        AssertNear(0.12, PressingTacticEvaluator.OpportunityMidfieldRiskWeight, "midfield-risk opportunity weight");
        AssertNear(1.0, PressingTacticEvaluator.OpportunityOwnChanceLossWeight + PressingTacticEvaluator.OpportunityStaminaRiskWeight + PressingTacticEvaluator.OpportunityWinProbabilityLossWeight + PressingTacticEvaluator.OpportunityMidfieldRiskWeight, "opportunity-cost weights sum");

        AssertNear(0.45, PressingTacticEvaluator.MatchupAttackWeight, "matchup attack weight");
        AssertNear(0.30, PressingTacticEvaluator.MatchupSuppressionWeight, "matchup suppression weight");
        AssertNear(0.15, PressingTacticEvaluator.MatchupLowOpponentQualityWeight, "matchup low-quality weight");
        AssertNear(0.10, PressingTacticEvaluator.MatchupStaminaGapWeight, "matchup stamina-gap weight");
        AssertNear(1.0, PressingTacticEvaluator.MatchupAttackWeight + PressingTacticEvaluator.MatchupSuppressionWeight + PressingTacticEvaluator.MatchupLowOpponentQualityWeight + PressingTacticEvaluator.MatchupStaminaGapWeight, "matchup weights sum");

        AssertNear(0.55, PressingTacticEvaluator.PrimaryNetSuppressionWeight, "primary suppression weight");
        AssertNear(0.25, PressingTacticEvaluator.PrimaryTacticalSignalWeight, "primary tactical weight");
        AssertNear(0.20, PressingTacticEvaluator.PrimaryDefenceWeight, "primary defence weight");
        AssertNear(1.0, PressingTacticEvaluator.PrimaryNetSuppressionWeight + PressingTacticEvaluator.PrimaryTacticalSignalWeight + PressingTacticEvaluator.PrimaryDefenceWeight, "primary weights sum");

        AssertNear(0.75, PressingTacticEvaluator.StaminaAverageWeight, "stamina average weight");
        AssertNear(0.25, PressingTacticEvaluator.StaminaWeakestWeight, "stamina weakest weight");
        AssertNear(1.0, PressingTacticEvaluator.StaminaAverageWeight + PressingTacticEvaluator.StaminaWeakestWeight, "stamina weights sum");

        AssertNear(0.40, PressingTacticEvaluator.ScorePrimaryWeight, "score primary weight");
        AssertNear(0.25, PressingTacticEvaluator.ScoreSquadFitWeight, "score squad-fit weight");
        AssertNear(0.20, PressingTacticEvaluator.ScoreMatchupWeight, "score matchup weight");
        AssertNear(0.15, PressingTacticEvaluator.ScoreTacticalSignalWeight, "score tactical weight");
        AssertNear(1.0, PressingTacticEvaluator.ScorePrimaryWeight + PressingTacticEvaluator.ScoreSquadFitWeight + PressingTacticEvaluator.ScoreMatchupWeight + PressingTacticEvaluator.ScoreTacticalSignalWeight, "positive score weights sum");
        if (PressingTacticEvaluator.ScoreOpportunityCostPenalty <= 0 || PressingTacticEvaluator.ScoreOpportunityCostPenalty >= 1)
            throw new InvalidOperationException("PressingWeightRegression failed: opportunity-cost penalty must be in (0,1)");

        Console.WriteLine("PASS: PRESS-01 Pressing evaluator heuristic weights locked by regression");
        return 0;
    }

    private static void AssertNear(double expected, double actual, string message)
    {
        if (Math.Abs(expected - actual) > 1e-12)
            throw new InvalidOperationException($"PressingWeightRegression failed: {message}; expected {expected}, actual {actual}");
    }
}
