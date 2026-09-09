namespace HattrickAI.V5.Core;

/// <summary>
/// Counter-Attack-specific suitability evaluation.
/// Opponent specialty interactions use the opponent's actual lineup; player pool alone
/// is not sufficient to decide who is a relevant defender.
/// </summary>
public static class CounterAttackTacticEvaluator
{
    private const double PaperNonTacticalCaTwoDefenders = 0.0175;
    private const double PaperNonTacticalCaThreeDefenders = 0.0363;
    private const double PaperNonTacticalCaFourDefenders = 0.0604;
    private const double PaperNonTacticalCaFiveDefenders = 0.0803;
    private const double PaperTechnicalCaTwoDefenders = 0.017;
    private const double PaperTechnicalCaThreeDefenders = 0.020;
    private const double PaperTechnicalCaFourPlusDefenders = 0.031;

    public static TacticFitResult Evaluate(Lineup lineup, ComparisonEvaluationView baselineNormal, ComparisonEvaluationView tacticEvaluation, IReadOnlyList<Player> players, IReadOnlyList<Player>? opponentPlayers = null, Lineup? opponentLineup = null)
    {
        ArgumentNullException.ThrowIfNull(lineup); ArgumentNullException.ThrowIfNull(baselineNormal); ArgumentNullException.ThrowIfNull(tacticEvaluation); ArgumentNullException.ThrowIfNull(players);
        var own = tacticEvaluation.Chance; var baseline = baselineNormal.Chance; var xi = LineupPlayers(lineup, players); var opponent = opponentPlayers ?? Array.Empty<Player>();
        var eligible = own.CounterAttackEligible; var defenders = CaDefenders(lineup, xi); var defenderCount = defenders.Count;
        var averageDefending = Average(defenders.Select(p => p.Defending)); var averagePassing = Average(defenders.Select(p => p.Passing)); var averageExperience = Average(defenders.Select(p => p.Experience));
        var averageScoring = Average(xi.Select(p => p.Scoring)); var averagePassingAll = Average(xi.Select(p => p.Passing));
        var defenderCaInput = (averageDefending + (2.0 * averagePassing)) / 3.0; var defenderCaFit = Clamp01(defenderCaInput / 10.0); var experienceFit = Clamp01(averageExperience / 10.0);
        var attackFit = CounterAttackAttackQuality(own, averageScoring, averagePassingAll); var defenceResistance = Clamp01(1.0 - own.OpponentRegularQuality);
        var quickOwn = RelevantQuickAttackers(lineup, xi); var quickOpponentDefenders = RelevantQuickDefenders(opponentLineup, opponent); var quickBoost = SpecialtyInteractionEngine.CounterAttackSpecialtyBoostPercent(quickOwn, quickOpponentDefenders);
        var technicalDefenders = defenders.Count(p => p.Specialty == PlayerSpecialty.Technical); var technicalCaRate = TechnicalCounterAttackRate(defenderCount, technicalDefenders); var nonTacticalCaRate = NonTacticalCounterAttackRate(defenderCount);
        var specialtyFit = Clamp01(0.65 * (quickBoost / SpecialtyInteractionEngine.QuickCounterAttackEightPlayerBoost) + 0.35 * (technicalCaRate / PaperTechnicalCaFourPlusDefenders));
        var missedNormal = Math.Max(0.0, own.MissedOpponentNormalChanceExpected); var conversion = Math.Clamp(own.CounterAttackConversionRate, 0.0, 1.0); var tacticalCaExpected = eligible ? missedNormal * conversion : 0.0;
        var conversionFit = Clamp01(conversion / M8ChanceAllocationEngine.CounterAttackMaxConversion); var opportunityFit = Clamp01(missedNormal / M8ChanceAllocationEngine.PaperExpectedRegularSectorChances);
        var primary = Clamp01(0.25 * opportunityFit + 0.25 * conversionFit + 0.20 * defenceResistance + 0.15 * attackFit + 0.10 * defenderCaFit + 0.05 * specialtyFit);
        var midfieldDisadvantage = eligible ? Clamp01((0.50 - own.MidfieldShare) / 0.25) : 0.0;
        var matchup = Clamp01(0.35 * midfieldDisadvantage + 0.25 * defenceResistance + 0.20 * opportunityFit + 0.15 * attackFit + 0.05 * specialtyFit);
        var ownChanceLoss = RelativeLoss(baseline.OwnRegularChanceExpected, own.OwnRegularChanceExpected); var winProbabilityLoss = Math.Max(0.0, baselineNormal.Prediction.Prediction.WinProbability - tacticEvaluation.Prediction.Prediction.WinProbability);
        var tradeoff = Clamp01(0.45 * ownChanceLoss + 0.30 * M8ChanceAllocationEngine.CounterAttackMidfieldPenalty + 0.25 * winProbabilityLoss);
        var squadFit = Clamp01(0.45 * defenderCaFit + 0.15 * experienceFit + 0.25 * attackFit + 0.15 * specialtyFit);
        if (!eligible) return new TacticFitResult(TeamTactic.CounterAttack, 0.0, primary, 1.0, squadFit, 0.0, false, "CA uygun değil: pre-penalty midfield şartı sağlanmıyor.");
        var suitability = Clamp01(0.50 * primary + 0.20 * squadFit + 0.20 * matchup + 0.10 * experienceFit - 0.35 * tradeoff);
        var score = Clamp01(0.70 * suitability + 0.30 * tacticEvaluation.Prediction.Prediction.WinProbability);
        var explanation = $"CA: pre-penalty eligibility OK; 7% MF penalty; defenders {defenderCount}; CA input Def+2xPass {defenderCaInput:0.##}; defender experience {averageExperience:0.##}; missed opponent Normal {missedNormal:0.##}; tactical CA rate {conversion:P1}; expected tactical CA {tacticalCaExpected:0.##}; defence resistance {defenceResistance:P0}; attack finish fit {attackFit:P0}; Quick CA boost {quickBoost:P1}; Technical defender CA {technicalCaRate:P1}; non-tactical CA baseline {nonTacticalCaRate:P1}; opponent lineup {(opponentLineup is null ? "missing" : "used")}; opportunity cost {tradeoff:P0}.";
        return new TacticFitResult(TeamTactic.CounterAttack, score, primary, tradeoff, squadFit, matchup, true, explanation);
    }

    private static double CounterAttackAttackQuality(M8ChanceResult chance, double averageScoring, double averagePassing)
    { var sector=Clamp01(0.40*chance.LeftAttackVsRightDefence+0.20*chance.CentreAttackVsCentreDefence+0.40*chance.RightAttackVsLeftDefence); var player=Clamp01(0.70*(averageScoring/10.0)+0.30*(averagePassing/10.0)); return Clamp01(0.70*sector+0.30*player); }
    private static double TechnicalCounterAttackRate(int defenders,int technicalDefenders){if(technicalDefenders<=0)return 0.0;return defenders switch{<=2=>PaperTechnicalCaTwoDefenders,3=>PaperTechnicalCaThreeDefenders,_=>PaperTechnicalCaFourPlusDefenders};}
    private static double NonTacticalCounterAttackRate(int defenders)=>defenders switch{<=2=>PaperNonTacticalCaTwoDefenders,3=>PaperNonTacticalCaThreeDefenders,4=>PaperNonTacticalCaFourDefenders,_=>PaperNonTacticalCaFiveDefenders};
    private static int RelevantQuickAttackers(Lineup lineup,IReadOnlyList<Player> players)=>players.Count(p=>p.Specialty==PlayerSpecialty.Quick&&SlotFor(lineup,p.Id) is { } slot&&!IsCaDefenderSlot(slot));
    private static int RelevantQuickDefenders(Lineup? opponentLineup,IReadOnlyList<Player> players)=>opponentLineup is null?0:players.Count(p=>p.Specialty==PlayerSpecialty.Quick&&SlotFor(opponentLineup,p.Id) is { } slot&&IsCaDefenderSlot(slot));
    private static IReadOnlyList<Player> CaDefenders(Lineup lineup,IReadOnlyList<Player> players)=>players.Where(p=>SlotFor(lineup,p.Id) is { } slot&&IsCaDefenderSlot(slot)).ToArray();
    private static bool IsCaDefenderSlot(string code)=>code.StartsWith("DEF",StringComparison.Ordinal);
    private static string? SlotFor(Lineup lineup,int playerId)=>lineup.Slots.FirstOrDefault(s=>s.PlayerId==playerId)?.Code;
    private static IReadOnlyList<Player> LineupPlayers(Lineup lineup,IReadOnlyList<Player> players){var ids=lineup.Slots.Where(s=>s.PlayerId>0).Select(s=>s.PlayerId).ToHashSet();return players.Where(p=>ids.Contains(p.Id)).ToArray();}
    private static double RelativeLoss(double baseline,double tactic)=>baseline<=1e-9?0.0:Clamp01((baseline-tactic)/baseline);
    private static double Average(IEnumerable<int> values){var a=values.Where(v=>v>0).ToArray();return a.Length==0?0.0:a.Average();}
    private static double Clamp01(double value)=>Math.Clamp(value,0.0,1.0);
}
