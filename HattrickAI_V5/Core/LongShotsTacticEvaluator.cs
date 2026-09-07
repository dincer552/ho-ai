using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Long Shots-specific suitability objective.
/// The evaluator separates the documented LS requirements (Scoring + Set Pieces),
/// the long-shot opportunity conversion, shooter-vs-keeper quality, possession,
/// opponent interaction and the opportunity cost of replacing normal attacks.
/// It does not invent a hidden live-engine coefficient.
/// </summary>
public static class LongShotsTacticEvaluator
{
    private const double PaperMinConversion = M8ChanceAllocationEngine.LongShotsMinConversion;
    private const double PaperMaxConversion = M8ChanceAllocationEngine.LongShotsMaxConversion;
    private const double PaperMaxRegularConversion = 1.0 / 3.0;
    private const double DocumentedAttackPenalty = 0.027;
    private const double DocumentedMidfieldPenalty = 0.05;

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
        var outfield = xi.Where(p => SlotFor(lineup, p.Id) is { } slot && !IsGoalkeeperSlot(slot)).ToArray();
        if (outfield.Length == 0)
            return new TacticFitResult(TeamTactic.LongShots, 0, 0, 1, 0, 0, false, "Long Shots: no outfield XI players available.");

        // REQUIREMENTS: official/manual mechanics use Scoring 3x Set Pieces for tactic skill.
        var scoringAverage = Average(outfield.Select(p => Math.Max(0, p.Scoring)));
        var setPiecesAverage = Average(outfield.Select(p => Math.Max(0, p.SetPiecesSkill)));
        var passingAverage = Average(outfield.Select(p => Math.Max(0, p.Passing)));
        var playmakingAverage = Average(outfield.Select(p => Math.Max(0, p.Playmaking)));
        var defendingAverage = Average(outfield.Select(p => Math.Max(0, p.Defending)));
        var experienceAverage = Average(outfield.Select(p => Math.Max(0, p.Experience)));

        var tacticalInput = (3.0 * scoringAverage + setPiecesAverage) / 4.0;
        var scoringFit = Clamp01(scoringAverage / 10.0);
        var setPiecesFit = Clamp01(setPiecesAverage / 10.0);
        var tacticalSkillFit = Clamp01(tacticalInput / 10.0);
        var conversion = Clamp01(own.LongShotConversionRate);
        var conversionFit = Clamp01((conversion - PaperMinConversion) / (PaperMaxConversion - PaperMinConversion));

        // BENEFIT: actual M8 LS opportunity volume, then shooter-vs-keeper quality.
        var expectedLongShots = Math.Max(0.0, own.LongShotChanceExpected);
        var conversionVolumeFit = Clamp01(expectedLongShots / Math.Max(0.1, own.OwnRegularChanceExpected * PaperMaxRegularConversion));
        var shooterQuality = WeightedShooterQuality(outfield, lineup);
        var keeperQuality = OpponentKeeperQuality(opponentPlayers);
        var longShotScoringProbability = ShotProbability(shooterQuality, keeperQuality);
        var keeperFit = 1.0 - keeperQuality;

        // POSSESSION: LS still needs attacks. The manual explicitly notes that possession
        // has almost the same importance as tactical level because it creates the chances.
        var possession = Clamp01(own.MidfieldShare);
        var possessionFit = PossessionFit(possession);

        // OPPONENT INTERACTION: LS bypasses normal sector defence, but the goalkeeper remains
        // the direct opponent. The 2026 paper also found a strong relationship with tactic
        // rating minus opponent average defence as a proxy for hidden goalkeeper skill.
        var opponentDefenceProxy = OpponentDefenceProxy(opponentPlayers);
        var tacticVsKeeperProxy = Clamp01(0.5 + (tacticalInput - opponentDefenceProxy) / 20.0);
        var keeperInteraction = Clamp01(0.65 * keeperFit + 0.35 * tacticVsKeeperProxy);

        // OPPORTUNITY COST: LS trades middle/wing attacks for long shots and carries documented
        // small penalties to attack and midfield. Use actual M8/M9 Normal-vs-LS loss rather than
        // inventing a hidden coefficient.
        var ownChanceVolumeLoss = RelativeLoss(baseline.OwnRegularChanceExpected, own.OwnRegularChanceExpected);
        var normalAttackQuality = Clamp01(
            0.50 * Clamp01((baseline.LeftAttackVsRightDefence + baseline.RightAttackVsLeftDefence) / 2.0) +
            0.50 * Clamp01(baseline.CentreAttackVsCentreDefence));
        var redirectedVolume = Math.Max(0.0, baseline.OwnRegularChanceExpected - own.OwnRegularChanceExpected);
        var regularAttackOpportunityCost = Clamp01(redirectedVolume / Math.Max(0.1, baseline.OwnRegularChanceExpected)) * normalAttackQuality;
        var ratingPenaltyProxy = Clamp01(0.60 * DocumentedAttackPenalty + 0.40 * DocumentedMidfieldPenalty);
        var winProbabilityLoss = Math.Max(0.0,
            baselineNormal.Prediction.Prediction.WinProbability - tacticEvaluation.Prediction.Prediction.WinProbability);

        var tradeoff = Clamp01(
            0.45 * regularAttackOpportunityCost +
            0.20 * ownChanceVolumeLoss +
            0.15 * ratingPenaltyProxy +
            0.20 * winProbabilityLoss);

        // Suitability is deliberately XI-specific: a team with a good tactic level but weak
        // shooters/keepers can still receive a poor LS fit.
        var primary = Clamp01(
            0.22 * scoringFit +
            0.18 * setPiecesFit +
            0.18 * tacticalSkillFit +
            0.12 * conversionFit +
            0.12 * conversionVolumeFit +
            0.10 * shooterQuality +
            0.08 * longShotScoringProbability);

        var matchup = Clamp01(
            0.50 * longShotScoringProbability +
            0.25 * keeperInteraction +
            0.15 * possessionFit +
            0.10 * tacticVsKeeperProxy);

        var squadFit = Clamp01(
            0.35 * scoringFit +
            0.30 * setPiecesFit +
            0.15 * shooterQuality +
            0.10 * Clamp01(passingAverage / 10.0) +
            0.05 * Clamp01(playmakingAverage / 10.0) +
            0.05 * Clamp01(defendingAverage / 10.0));

        var suitability = Clamp01(
            0.55 * primary +
            0.25 * matchup +
            0.20 * squadFit -
            0.45 * tradeoff);

        var score = Clamp01(
            0.75 * suitability +
            0.15 * possessionFit +
            0.10 * tacticEvaluation.Prediction.Prediction.WinProbability);

        var explanation =
            $"LS: Scoring avg {scoringAverage:0.0}; SP avg {setPiecesAverage:0.0}; " +
            $"tactical input {(3.0 * scoringAverage + setPiecesAverage) / 4.0:0.0}; " +
            $"M8 conversion {conversion:P1}; LS chances {expectedLongShots:0.##}; " +
            $"shooter quality {shooterQuality:P0}; estimated LS score probability {longShotScoringProbability:P0}; " +
            $"opponent GK quality {keeperQuality:P0}; possession {possession:P0}; " +
            $"regular-attack opportunity cost {regularAttackOpportunityCost:P0}; trade-off {tradeoff:P0}.";

        return new TacticFitResult(
            TeamTactic.LongShots,
            score,
            primary,
            tradeoff,
            squadFit,
            matchup,
            true,
            explanation);
    }

    private static Player[] LineupPlayers(Lineup lineup, IReadOnlyList<Player> players)
    {
        var byId = players.ToDictionary(p => p.Id);
        return lineup.Slots
            .Where(s => s.PlayerId > 0 && byId.ContainsKey(s.PlayerId))
            .Select(s => byId[s.PlayerId])
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .ToArray();
    }

    private static Slot? SlotFor(Lineup lineup, int playerId)
        => lineup.Slots.FirstOrDefault(s => s.PlayerId == playerId);

    private static bool IsGoalkeeperSlot(Slot slot) => string.Equals(slot.Code, "GK", StringComparison.OrdinalIgnoreCase);

    private static double WeightedShooterQuality(IReadOnlyList<Player> outfield, Lineup lineup)
    {
        double weighted = 0;
        double weight = 0;
        foreach (var player in outfield)
        {
            var slot = SlotFor(lineup, player.Id);
            if (slot is null) continue;
            var positionWeight = slot.Code.StartsWith("IM-", StringComparison.OrdinalIgnoreCase) ||
                                 slot.Code.StartsWith("W-", StringComparison.OrdinalIgnoreCase) ? 2.0 : 1.0;
            var quality = Math.Sqrt(Math.Max(0, player.Scoring) * Math.Max(0, player.SetPiecesSkill)) / 10.0;
            weighted += positionWeight * Clamp01(quality);
            weight += positionWeight;
        }
        return weight <= 0 ? 0 : Clamp01(weighted / weight);
    }

    private static double OpponentKeeperQuality(IReadOnlyList<Player>? opponentPlayers)
    {
        if (opponentPlayers is null) return 0.5;
        var keepers = opponentPlayers.Where(p => p.Keeper > 0).ToArray();
        if (keepers.Length == 0) return 0.5;
        return Clamp01(keepers.Average(p => KeeperStrength(p.Keeper, p.SetPiecesSkill)));
    }

    private static double OpponentDefenceProxy(IReadOnlyList<Player>? opponentPlayers)
    {
        if (opponentPlayers is null) return 5.0;
        var outfield = opponentPlayers.Where(p => p.Keeper <= 0).ToArray();
        return outfield.Length == 0 ? 5.0 : Math.Clamp(outfield.Average(p => p.Defending), 0, 20);
    }

    private static double ShotProbability(double shooterQuality, double keeperQuality)
    {
        // The public paper reports an 11%-100% score-rate relationship against the
        // tactic-rating minus opponent-defence proxy. We keep the evaluator bounded
        // and use the published shooter/keeper principle rather than claiming the
        // paper's hidden goalkeeper equation is the live-engine formula.
        var relative = Clamp01(shooterQuality * (1.0 - 0.70 * keeperQuality));
        return Clamp01(0.11 + 0.89 * relative);
    }

    private static double KeeperStrength(double keeper, double setPieces)
    {
        var gk = Math.Max(0, keeper);
        var sp = Math.Max(0, setPieces);
        var gkTerm = 1977.4524 * Math.Pow(gk, 0.9);
        var spTerm = 31.4827 * Math.Pow(sp, 2.3262);
        // Normalize against a strong but plausible keeper so the result is a fit score.
        var reference = 1977.4524 * Math.Pow(10, 0.9) + 31.4827 * Math.Pow(10, 2.3262);
        return Clamp01((gkTerm + spTerm) / reference);
    }

    private static double PossessionFit(double possession)
        => Clamp01(0.25 + 0.75 * possession);

    private static double RelativeLoss(double baseline, double tactic)
        => baseline <= 1e-9 ? 0 : Clamp01((baseline - tactic) / baseline);

    private static double Average(IEnumerable<int> values)
    {
        var array = values.ToArray();
        return array.Length == 0 ? 0 : array.Average();
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);
}
