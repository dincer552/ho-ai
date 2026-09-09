namespace HattrickAI.V5.Core;

/// <summary>
/// Scores a tactic against the same XI using the tactic's own objective.
/// It deliberately does not collapse all tactics into TacticalScore.
/// </summary>
public static class TacticObjectiveEngine
{
    public static TacticFitResult Evaluate(Lineup lineup, TeamTactic tactic, ComparisonEvaluationView baselineNormal, ComparisonEvaluationView tacticEvaluation, IReadOnlyList<Player> players, IReadOnlyList<Player>? opponentPlayers = null, Lineup? opponentLineup = null)
    {
        ArgumentNullException.ThrowIfNull(lineup); ArgumentNullException.ThrowIfNull(baselineNormal); ArgumentNullException.ThrowIfNull(tacticEvaluation); ArgumentNullException.ThrowIfNull(players);

        if (tactic == TeamTactic.Pressing)
            return PressingTacticEvaluator.Evaluate(lineup, baselineNormal, tacticEvaluation, players, opponentPlayers);
        if (tactic == TeamTactic.Creative)
            return CreativeTacticEvaluator.Evaluate(lineup, baselineNormal, tacticEvaluation, players, opponentPlayers, opponentLineup);
        if (tactic == TeamTactic.CounterAttack)
            return CounterAttackTacticEvaluator.Evaluate(lineup, baselineNormal, tacticEvaluation, players, opponentPlayers, opponentLineup);
        if (tactic == TeamTactic.AttackMiddle)
            return AttackMiddleTacticEvaluator.Evaluate(lineup, baselineNormal, tacticEvaluation, players, opponentPlayers);
        if (tactic == TeamTactic.AttackWings)
            return AttackWingsTacticEvaluator.Evaluate(lineup, baselineNormal, tacticEvaluation, players, opponentPlayers);
        if (tactic == TeamTactic.LongShots)
            return LongShotsTacticEvaluator.Evaluate(lineup, baselineNormal, tacticEvaluation, players, opponentPlayers);

        var own = tacticEvaluation.Chance; var baseline = baselineNormal.Chance; var inputs = tacticEvaluation.Advanced.Inputs;
        var outfieldCount = Math.Max(1, lineup.Slots.Count(s => s.Code != "GK"));
        var fit = SquadFit(tactic, inputs, outfieldCount);
        var matchup = MatchupFit(tactic, own, tacticEvaluation.Prediction.Prediction, baseline, opponentPlayers);
        var tradeoff = Tradeoff(tactic, baseline, own, baselineNormal.Prediction.Prediction, tacticEvaluation.Prediction.Prediction);
        var primary = PrimaryMetric(tactic, own, tacticEvaluation.Advanced, tacticEvaluation.Prediction.Prediction, baseline, opponentPlayers);
        var eligible = tactic != TeamTactic.CounterAttack || own.CounterAttackEligible;
        if (!eligible) return new TacticFitResult(tactic, 0, primary, tradeoff, fit, matchup, false, "CA eligibility failed: own midfield is not below opponent midfield.");
        var mechanic = MechanicValue(tactic, own, tacticEvaluation.Advanced);
        var score = Math.Clamp((0.45 * primary) + (0.25 * fit) + (0.20 * matchup) + (0.10 * mechanic) - (0.20 * tradeoff), 0, 1);
        return new TacticFitResult(tactic, score, primary, tradeoff, fit, matchup, true, Explain(tactic, own, tacticEvaluation.Advanced, baseline));
    }

    private static double PrimaryMetric(TeamTactic tactic, M8ChanceResult c, AdvancedTacticalScenarioResult a, MatchPrediction p, M8ChanceResult baseline, IReadOnlyList<Player>? opponentPlayers)
        => tactic switch
        {
            TeamTactic.Pressing => Math.Clamp(Suppression(c, baseline) * 0.65 + NormPerPlayer(a.Inputs.TotalDefending, 10) * 0.20 + NormPerPlayer(a.Inputs.TotalStamina, 10) * 0.15, 0, 1),
            TeamTactic.CounterAttack => Math.Clamp(Clamp01(c.CounterAttackChanceExpected / 2.5) * 0.60 + Clamp01((a.CounterAttack?.RelativeCounterInput ?? 0) / 200.0) * 0.20 + PossessionDisadvantage(c) * 0.20, 0, 1),
            TeamTactic.AttackWings => Math.Clamp(WingMatchup(c) * 0.55 + (c.RightChanceShare + c.LeftChanceShare) * 0.25 + Clamp01(c.TacticConversionRate / 0.52) * 0.20, 0, 1),
            TeamTactic.AttackMiddle => Math.Clamp(CentreMatchup(c) * 0.55 + c.CentreChanceShare * 0.25 + Clamp01(c.TacticConversionRate / 0.35) * 0.20, 0, 1),
            TeamTactic.LongShots => Math.Clamp(Clamp01(c.LongShotChanceExpected / 2.0) * 0.45 + Clamp01((a.LongShots?.ShooterInput ?? 0) / 150.0) * 0.25 + KeeperMatchup(opponentPlayers) * 0.30, 0, 1),
            _ => Math.Clamp(0.50 * c.StructuralChanceIndex + 0.50 * p.WinProbability, 0, 1)
        };

    private static double SquadFit(TeamTactic tactic, TacticalInputTotals i, int count)
    { var passing=Average(i.TotalPassing,count);var defending=Average(i.TotalDefending,count);var scoring=Average(i.TotalScoring,count);var stamina=Average(i.TotalStamina,count);var experience=Average(i.TotalExperience,count);return tactic switch{TeamTactic.Pressing=>Norm(defending)*.55+Norm(stamina)*.45,TeamTactic.CounterAttack=>Norm(passing)*.55+Norm(defending)*.35+Norm(stamina)*.10,TeamTactic.AttackWings=>Norm(i.TotalWinger/count)*.50+Norm(passing)*.30+Norm(scoring)*.20,TeamTactic.AttackMiddle=>Norm(passing)*.45+Norm(i.TotalPlaymaking/count)*.35+Norm(scoring)*.20,TeamTactic.LongShots=>Norm(scoring)*.60+Norm(passing)*.25+Norm(experience)*.15,_=>.5}; }
    private static double MatchupFit(TeamTactic tactic,M8ChanceResult c,MatchPrediction p,M8ChanceResult baseline,IReadOnlyList<Player>? opponentPlayers)=>tactic switch{TeamTactic.Pressing=>Clamp01(Suppression(c,baseline)*.45+(1-c.OpponentRegularQuality)*.30+p.WinProbability*.25),TeamTactic.CounterAttack=>Clamp01(PossessionDisadvantage(c)*.40+Clamp01(c.CounterAttackChanceExpected/2.5)*.35+p.WinProbability*.25),TeamTactic.AttackWings=>WingMatchup(c),TeamTactic.AttackMiddle=>CentreMatchup(c),TeamTactic.LongShots=>Clamp01(KeeperMatchup(opponentPlayers)*.45+Clamp01(c.LongShotChanceExpected/2.0)*.30+p.WinProbability*.25),_=>p.WinProbability};
    private static double Tradeoff(TeamTactic tactic,M8ChanceResult baseline,M8ChanceResult c,MatchPrediction baselinePrediction,MatchPrediction prediction){var ownLoss=RelativeLoss(baseline.OwnRegularChanceExpected,c.OwnRegularChanceExpected);var opponentGain=RelativeLoss(baseline.OpponentRegularChanceExpected,c.OpponentRegularChanceExpected);return tactic switch{TeamTactic.Pressing=>Clamp01(.70*ownLoss+.30*Math.Max(0,-opponentGain)),TeamTactic.CounterAttack=>Clamp01(.55*ownLoss+.45*Math.Max(0,baselinePrediction.WinProbability-prediction.WinProbability)),TeamTactic.AttackWings or TeamTactic.AttackMiddle or TeamTactic.LongShots=>Clamp01(.70*ownLoss+.30*Math.Max(0,baselinePrediction.WinProbability-prediction.WinProbability)),_=>0};}
    private static double MechanicValue(TeamTactic tactic,M8ChanceResult c,AdvancedTacticalScenarioResult a)=>tactic switch{TeamTactic.Pressing=>Clamp01(c.PressingSuppression/.41)*.75+NormPerPlayer(a.Inputs.TotalDefending,10)*.25,TeamTactic.CounterAttack=>Clamp01(c.CounterAttackChanceExpected/2.5)*.75+NormPerPlayer(a.Inputs.TotalPassing,10)*.25,TeamTactic.AttackWings or TeamTactic.AttackMiddle=>Clamp01(c.TacticConversionRate/.52),TeamTactic.LongShots=>Clamp01(c.LongShotChanceExpected/2.0)*.75+NormPerPlayer(a.Inputs.TotalScoring,10)*.25,_=>.5};
    private static double Suppression(M8ChanceResult c,M8ChanceResult baseline)=>baseline.OpponentRegularChanceExpected<=1e-9?0:Clamp01(1-c.OpponentRegularChanceExpected/baseline.OpponentRegularChanceExpected);private static double PossessionDisadvantage(M8ChanceResult c)=>Clamp01((.50-c.MidfieldShare)/.50);private static double WingMatchup(M8ChanceResult c)=>Clamp01((c.LeftAttackVsRightDefence+c.RightAttackVsLeftDefence)/2.0);private static double CentreMatchup(M8ChanceResult c)=>Clamp01(c.CentreAttackVsCentreDefence);private static double KeeperMatchup(IReadOnlyList<Player>? players)=>players is null?.5:Clamp01(1-players.Where(p=>p.Keeper>0).Select(p=>p.Keeper).DefaultIfEmpty(10).Average()/20.0);private static double RelativeLoss(double baseline,double tactic)=>baseline<=1e-9?0:Clamp01((baseline-tactic)/baseline);private static double Average(double total,int count)=>total/Math.Max(1,count);private static double Norm(double value)=>Clamp01(value/10.0);private static double NormPerPlayer(double total,int count)=>Norm(Average(total,count));private static double Clamp01(double value)=>Math.Clamp(value,0,1);
    private static string Explain(TeamTactic tactic,M8ChanceResult c,AdvancedTacticalScenarioResult a,M8ChanceResult baseline)=>tactic switch{TeamTactic.Pressing=>$"Opponent suppression {Suppression(c,baseline):P1}; own chance loss {RelativeLoss(baseline.OwnRegularChanceExpected,c.OwnRegularChanceExpected):P1}; DEF/STAM fit evaluated.",TeamTactic.CounterAttack=>$"CA eligibility {c.CounterAttackEligible}; tactical CA chances {c.CounterAttackChanceExpected:0.##}; midfield share {c.MidfieldShare:P1}.",TeamTactic.AttackWings=>$"Wing matchup {WingMatchup(c):P1}; wing conversion {c.TacticConversionRate:P1}; centre trade-off measured.",TeamTactic.AttackMiddle=>$"Centre matchup {CentreMatchup(c):P1}; centre conversion {c.TacticConversionRate:P1}; wing trade-off measured.",TeamTactic.LongShots=>$"Long-shot chances {c.LongShotChanceExpected:0.##}; dedicated shooter/SP fit, keeper matchup and regular-attack opportunity cost measured.",_=>"Normal: balanced baseline objective."};
}

public sealed record TacticFitResult(TeamTactic Tactic,double FitScore,double PrimaryMetric,double TradeoffCost,double SquadFit,double MatchupFit,bool Eligible,string Explanation);
public sealed record ComparisonEvaluationView(M8ChanceResult Chance,AdvancedTacticalScenarioResult Advanced,M9PredictionResult Prediction);
