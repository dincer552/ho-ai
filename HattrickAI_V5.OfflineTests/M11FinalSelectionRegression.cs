using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class M11FinalSelectionRegression
{
    public static async Task<int> RunAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) return Fail($"fixture bulunamadı: {path}");
        var runId = MotorRunLogStore.Start("offline-c15-m11-selection");
        try
        {
            await using var stream = File.OpenRead(path);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;
            var normalized = root.GetProperty("normalized");
            var analysis = root.GetProperty("v5Analysis");
            var players = normalized.GetProperty("ownPlayers").EnumerateArray().Select(ReadPlayer).ToList();
            var opponentRating = ReadRating(analysis.GetProperty("opponentRating"));
            var opponent = new OpponentMatchProfile(GetString(analysis, "opponentName", "Opponent"), GetString(analysis, "opponentFormation", ""), opponentRating, new OpponentThreatEngine().Analyze(opponentRating));
            var context = new MatchDataContext(players, 0, GetString(analysis.GetProperty("ownLineup"), "teamName", "Fixture"), opponent, RatingContext.Default, MatchQuestionnaire.Default);
            Console.WriteLine("=== C15 M11 FINAL SELECTION REGRESSION ===");
            var result = await new MotorPipelineService().RunAsync(context, players, cancellationToken, runId);
            var legal = result.M4.Candidates.Select(x => x.Formation).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList();
            var db2 = result.CandidateDatabase2;
            var m11 = result.M11;
            Check(m11 is not null, "M11 result missing");
            Check(db2.Count > 0, "DB2 is empty");
            Check(m11!.CandidateCount > 0, "M11 selected from an empty finalist pool");
            Check(m11.Ranking.Count > 0 && m11.Ranking.Count <= m11.CandidateCount, "M11 ranking count is invalid");
            Check(m11.Ranking.Count == Math.Min(20, m11.CandidateCount), "M11 ranking top-N count is inconsistent");
            Check(m11.FormationCount == legal.Count, "M11 final selection lost a legal formation");
            Check(m11.Ranking.All(x => double.IsFinite(x.FinalScore)), "M11 ranking contains non-finite diagnostic score");
            Check(m11.Ranking.All(x => double.IsFinite(x.TacticalScore)), "M11 ranking contains non-finite tactical score");
            Check(m11.Ranking.All(x => x.WinProbability is >= 0 and <= 1), "M11 ranking contains invalid win probability");
            Check(m11.Ranking.Select(x => x.CandidateId).Distinct(StringComparer.Ordinal).Count() == m11.Ranking.Count, "M11 ranking contains duplicate candidate IDs");
            Check(m11.Ranking.Select(x => x.Formation).Distinct(StringComparer.Ordinal).Count() > 0 && m11.Ranking.Select(x => x.Formation).Distinct(StringComparer.Ordinal).Count() <= m11.FormationCount, "M11 ranking formation diversity is inconsistent");

            var winnerSignature = Signature(m11.BestPlan.Lineup);
            Check(m11.Ranking[0].CandidateId == winnerSignature, "M11 BestPlan is not ranking #1");
            Check(m11.Ranking[0].Formation == m11.BestPlan.Formation, "M11 BestPlan formation differs from ranking #1");
            Check(db2.Any(x => x.CandidateId == winnerSignature), "M11 winner is not sourced from DB2");
            var winnerDb2 = db2.First(x => x.CandidateId == winnerSignature);
            Check(winnerDb2.Stage == "M6-B", "M11 winner is not an M6-B DB2 candidate");
            Check(winnerDb2.Prediction is not null, "M11 winner lost M9 prediction continuity");
            Check(double.IsFinite(winnerDb2.RankingScore), "M11 winner DB2 ranking score is not finite");
            Check(m11.Prediction is not null, "M11 final prediction is missing");
            Check(m11.Prediction.WinProbability is >= 0 and <= 1, "M11 final prediction win probability is invalid");
            Check(double.IsFinite(m11.Prediction.ExpectedHomeGoals) && double.IsFinite(m11.Prediction.ExpectedAwayGoals), "M11 final xG is not finite");
            Check(Math.Abs((m11.Prediction.WinProbability * 3.0) + m11.Prediction.DrawProbability - m11.Prediction.ExpectedPoints) < 1e-12, "M11 winner expected-points formula is not canonical");

            // The entire seven-tactic chain must be present for the M11-selected XI.
            var tacticRows = result.TacticComparisons.Where(x => x.CandidateId == winnerSignature).ToList();
            var tactics = Enum.GetValues<TeamTactic>();
            Check(tacticRows.Count == tactics.Length, $"final XI has {tacticRows.Count}/{tactics.Length} tactic evaluations");
            Check(tacticRows.Select(x => x.Tactic).Distinct().Count() == tactics.Length, "final XI has duplicate/missing tactic evaluations");
            Check(tacticRows.All(x => double.IsFinite(x.WinProbability) && double.IsFinite(x.ExpectedPoints) && x.ExpectedPoints is >= 0 and <= 3), "tactic outcomes are not finite/bounded");
            Check(tacticRows.All(x => Math.Abs(x.ExpectedPoints - (3.0 * x.WinProbability + x.DrawProbability)) < 1e-12), "tactic Expected Points are not canonical 3W+D");
            Check(tacticRows.All(x => double.IsFinite(x.TacticFitScore) && double.IsFinite(x.TacticPrimaryMetric) && double.IsFinite(x.TacticTradeoffCost) && double.IsFinite(x.TacticSquadFit) && double.IsFinite(x.TacticMatchupFit)), "tactic objective components are not finite");

            var canonicalDb3 = TacticalMatchupDatabaseBuilder.Build(tacticRows);
            var expectedFirst = tacticRows.Where(x => x.TacticEligible).OrderByDescending(x => x.ExpectedPoints).ThenByDescending(x => x.WinProbability).ThenByDescending(x => x.TacticFitScore).ThenBy(x => x.Tactic).First();
            var db3Winner = canonicalDb3.BestEligible();
            Check(db3Winner.Tactic == expectedFirst.Tactic, "DB3 canonical tactic selector diverged from explicit ExpectedPoints ordering");
            Check(Math.Abs(db3Winner.ExpectedPoints - expectedFirst.ExpectedPoints) < 1e-12, "DB3 canonical tactic selector changed ExpectedPoints winner");

            var pressing = tacticRows.Single(x => x.Tactic == TeamTactic.Pressing);
            Check(pressing.TacticalLevel >= 0 && pressing.TacticalLevel <= 10, "Pressing tactical level out of V5 bounds");
            Check(pressing.OwnRegularChanceExpected >= 0 && pressing.OpponentRegularChanceExpected >= 0, "Pressing chance expectations invalid");
            var normal = tacticRows.Single(x => x.Tactic == TeamTactic.Normal);
            var pressingSuppression = normal.OpponentRegularChanceExpected <= 1e-9 ? 0 : (normal.OpponentRegularChanceExpected - pressing.OpponentRegularChanceExpected) / normal.OpponentRegularChanceExpected;
            Check(pressingSuppression >= -1e-12 && pressingSuppression <= 0.41 + 1e-9, $"Pressing suppression exceeded source bound: {pressingSuppression:P2}");
            var ca = tacticRows.Single(x => x.Tactic == TeamTactic.CounterAttack);
            Check(ca.TacticConversionRate >= 0 && ca.TacticConversionRate <= M8ChanceAllocationEngine.CounterAttackMaxConversion + 1e-9, "CA conversion outside bound");
            Check(ca.TacticConversionRate <= 0 || ca.TacticConversionRate + 1e-9 >= M8ChanceAllocationEngine.CounterAttackMinConversion, "CA conversion below source floor");
            var aim = tacticRows.Single(x => x.Tactic == TeamTactic.AttackMiddle);
            Check(aim.TacticConversionRate >= M8ChanceAllocationEngine.AiMMinWingConversion - 1e-9 && aim.TacticConversionRate <= M8ChanceAllocationEngine.AiMMaxWingConversion + 1e-9, "AiM conversion outside source bound");
            var aow = tacticRows.Single(x => x.Tactic == TeamTactic.AttackWings);
            Check(aow.TacticConversionRate >= M8ChanceAllocationEngine.AoWMinCentreConversion - 1e-9 && aow.TacticConversionRate <= M8ChanceAllocationEngine.AoWMaxCentreConversion + 1e-9, "AoW conversion outside source bound");
            var ls = tacticRows.Single(x => x.Tactic == TeamTactic.LongShots);
            Check(ls.TacticConversionRate >= M8ChanceAllocationEngine.LongShotsMinConversion - 1e-9 && ls.TacticConversionRate <= M8ChanceAllocationEngine.LongShotsMaxConversion + 1e-9, "Long Shots conversion outside source bound");
            Check(tacticRows.Single(x => x.Tactic == TeamTactic.Creative).TacticEligible, "Creative unexpectedly ineligible for final XI");

            // Explicit objective regression: high tactical score must not defeat a
            // lower-tactical candidate when its M9 Expected Points are higher.
            var synthetic = BuildSyntheticOutcomeRegression();
            var syntheticWinner = new M11FinalSelectorEngine().Select(synthetic).BestPlan;
            Check(syntheticWinner.Formation == "OUTCOME-WINNER", "M11 synthetic outcome-first selection regressed to tactical/composite preference");

            var log = MotorRunLogStore.Get(runId);
            Check(log is not null, "M11 telemetry missing");
            var m6bIndex = IndexOf(log!.Stages, x => x.Motor == "M6-B" && x.Status == "completed");
            var m11Index = IndexOf(log.Stages, x => x.Motor == "M11" && x.Status == "completed");
            Check(m6bIndex >= 0 && m11Index >= 0 && m6bIndex < m11Index, "M11 final selection did not execute after M6-B");
            var m11Stage = log.Stages[m11Index];
            Check(m11Stage.CandidateCount.GetValueOrDefault() == m11.CandidateCount, "M11 telemetry candidate count mismatch");
            Console.WriteLine($"M11 finalists={m11.CandidateCount} | formations={m11.FormationCount} | winner={m11.BestPlan.Formation} | expectedPoints={m11.Prediction.ExpectedPoints:0.####}");
            Console.WriteLine($"Tactic chain=7/7 | selected={tacticRows.OrderByDescending(x => x.ExpectedPoints).First().Tactic} | EP={tacticRows.Max(x => x.ExpectedPoints):0.####}");
            Console.WriteLine("PASS: C15 M11 final selection + seven-tactic chain");
            MotorRunLogStore.Finish(runId, true, "C15 M11 final selection and seven-tactic chain passed");
            return 0;
        }
        catch (Exception ex)
        {
            MotorRunLogStore.Finish(runId, false, ex.Message);
            return Fail("C15 exception: " + ex.Message);
        }
    }

    private static IReadOnlyList<M11CandidateEvaluation> BuildSyntheticOutcomeRegression()
    {
        var winnerLineup = SyntheticLineup("OUTCOME-WINNER");
        var fitLineup = SyntheticLineup("FIT-WINNER");
        var winnerPrediction = new MatchPrediction(0.60, 2.0, 1.0, 0.50, 0.10, 0.40);
        var fitPrediction = new MatchPrediction(0.45, 1.0, 1.0, 0.45, 0.10, 0.45);
        var rating = new RegionalRatingSnapshot(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
        var matchup = new MatchupEvaluation(0, 0, 0, 0, 0, 0, 0, 0);
        return
        [
            new M11CandidateEvaluation(new TacticalCandidate(winnerLineup, rating, matchup, 0.10), winnerPrediction, 0.10, 0.10),
            new M11CandidateEvaluation(new TacticalCandidate(fitLineup, rating, matchup, 9.00), fitPrediction, 0.99, 1.00)
        ];
    }

    private static Lineup SyntheticLineup(string formation) => new("Synthetic", formation, []);
    private static int IndexOf<T>(IReadOnlyList<T> source, Func<T, bool> predicate) { for (var i = 0; i < source.Count; i++) if (predicate(source[i])) return i; return -1; }
    private static Player ReadPlayer(JsonElement e) => new(e.GetProperty("id").GetInt32(), e.GetProperty("name").GetString() ?? "Player", e.GetProperty("keeper").GetInt32(), e.GetProperty("defending").GetInt32(), e.GetProperty("playmaking").GetInt32(), e.GetProperty("passing").GetInt32(), e.GetProperty("winger").GetInt32(), e.GetProperty("scoring").GetInt32(), e.GetProperty("stamina").GetInt32(), e.GetProperty("form").GetInt32(), e.GetProperty("experience").GetInt32(), GetInt(e, "loyalty", 0), GetInt(e, "injuryLevel", -1));
    private static RegionalRatingSnapshot ReadRating(JsonElement e) { var ld = GetDouble(e, "leftDefence"); var cd = GetDouble(e, "centralDefence"); var rd = GetDouble(e, "rightDefence"); var mid = GetDouble(e, "midfield"); var la = GetDouble(e, "leftAttack"); var ca = GetDouble(e, "centralAttack"); var ra = GetDouble(e, "rightAttack"); return new RegionalRatingSnapshot(ld, cd, rd, mid, la, ca, ra, ld, cd, rd, mid, la, ca, ra); }
    private static string Signature(Lineup lineup) => string.Join(";", lineup.Slots.OrderBy(s => s.Code, StringComparer.Ordinal).ThenBy(s => s.PlayerId).Select(s => $"{s.Code}:{s.PlayerId}:{(int)s.Order}"));
    private static string GetString(JsonElement e, string n, string f) => e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? f : f;
    private static int GetInt(JsonElement e, string n, int f) => e.TryGetProperty(n, out var v) && v.TryGetInt32(out var x) ? x : f;
    private static double GetDouble(JsonElement e, string n) => e.GetProperty(n).GetDouble();
    private static int Fail(string m) { Console.WriteLine("FAIL: " + m); return 1; }
    private static void Check(bool ok, string m) { if (!ok) throw new InvalidOperationException(m); }
}
