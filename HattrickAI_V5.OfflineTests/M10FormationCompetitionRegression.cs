using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class M10FormationCompetitionRegression
{
    public static async Task<int> RunAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) return Fail($"fixture bulunamadı: {path}");
        var runId = MotorRunLogStore.Start("offline-acceptance-c10");
        string? repeatRunId = null;
        try
        {
            await using var stream = File.OpenRead(path);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement; var normalized = root.GetProperty("normalized"); var analysis = root.GetProperty("v5Analysis");
            var players = normalized.GetProperty("ownPlayers").EnumerateArray().Select(ReadPlayer).ToList();
            var opponentRating = ReadRating(analysis.GetProperty("opponentRating"));
            var opponent = new OpponentMatchProfile(GetString(analysis, "opponentName", "Opponent"), GetString(analysis, "opponentFormation", ""), opponentRating, new OpponentThreatEngine().Analyze(opponentRating));
            var context = new MatchDataContext(players, 0, GetString(analysis.GetProperty("ownLineup"), "teamName", "Fixture"), opponent, RatingContext.Default, MatchQuestionnaire.Default);
            Console.WriteLine("=== C10 M10 FORMATION COMPETITION REGRESSION ===");
            var result = await new MotorPipelineService().RunAsync(context, players, cancellationToken, runId);
            var legal = result.M4.Candidates.Select(x => x.Formation).Distinct(StringComparer.Ordinal).ToList();
            var competition = result.M10.FormationCompetition ?? [];
            var telemetry = MotorRunLogStore.Get(runId);
            Check(telemetry is not null, "M10 run telemetry exists");
            Check(telemetry!.Stages.Any(x => x.Motor == "M10" && x.Status == "completed"), "M10 completed telemetry exists");
            Check(competition.Count == legal.Count, $"M10 compares {competition.Count} formations; expected {legal.Count}");
            Check(competition.Select(x => x.Formation).Distinct(StringComparer.Ordinal).Count() == competition.Count, "M10 formation competition contains duplicates");
            Check(competition.Select(x => x.Formation).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(legal.OrderBy(x => x, StringComparer.Ordinal)), "M10 formation competition does not cover the legal M4 set");
            Check(competition.Select(x => x.Rank).OrderBy(x => x).SequenceEqual(Enumerable.Range(1, competition.Count)), "M10 ranks are not contiguous");
            Check(competition.All(x => double.IsFinite(x.ExpectedPoints) && double.IsFinite(x.WinProbability)), "M10 competition outcome scores are finite");
            Check(competition.All(x => x.CandidateCount > 0), "M10 has no candidate for a competing formation");
            Check(!string.IsNullOrWhiteSpace(result.M10.BestPlan.Formation), "M10 selected formation is empty");
            Check(competition.Any(x => x.Formation == result.M10.BestPlan.Formation && x.Rank == 1), "M10 BestPlan is not the rank-1 formation");
            var ranked = competition.OrderBy(x => x.Rank).ToList();
            for (var i = 0; i < ranked.Count - 1; i++)
            {
                Check(ranked[i].ExpectedPoints >= ranked[i + 1].ExpectedPoints - 1e-12, "M10 formation ranking is not ExpectedPoints-first");
                if (Math.Abs(ranked[i].ExpectedPoints - ranked[i + 1].ExpectedPoints) < 1e-12)
                    Check(ranked[i].WinProbability >= ranked[i + 1].WinProbability - 1e-12, "M10 equal-EP tie-break is not win-probability-first");
            }
            Check(Math.Abs(ranked[0].ExpectedPoints - ranked.Max(x => x.ExpectedPoints)) < 1e-12, "M10 rank #1 is not the maximum ExpectedPoints formation");
            Check(Math.Abs(result.M10.BestPlan.Formation == ranked[0].Formation ? 0 : 1) < 0.5, "M10 BestPlan does not match EP-first rank #1");
            repeatRunId = MotorRunLogStore.Start("offline-acceptance-c10-repeat");
            var repeat = await new MotorPipelineService().RunAsync(context, players, cancellationToken, repeatRunId);
            Check(repeat.M10.BestPlan.Formation == result.M10.BestPlan.Formation, "M10 winner changed on deterministic rerun");
            Check(repeat.M10.FormationCompetition is not null && repeat.M10.FormationCompetition.Count == competition.Count, "M10 competition depth changed on deterministic rerun");
            var repeatRanked = repeat.M10.FormationCompetition!.OrderBy(x => x.Rank).ToList();
            Check(ranked.Select(x => x.Formation).SequenceEqual(repeatRanked.Select(x => x.Formation)), "M10 formation ordering changed on deterministic rerun");
            Check(ranked.Select(x => x.ExpectedPoints).Zip(repeatRanked.Select(x => x.ExpectedPoints), (a, b) => Math.Abs(a - b) < 1e-12).All(x => x), "M10 ExpectedPoints changed on deterministic rerun");
            MotorRunLogStore.Finish(runId, true, "C10 M10 formation competition continuity passed");
            MotorRunLogStore.Finish(repeatRunId, true, "C10 deterministic rerun passed");
            Console.WriteLine($"M10 formations={competition.Count} | winner={result.M10.BestPlan.Formation} | rank1 EP={ranked[0].ExpectedPoints:0.####}");
            Console.WriteLine("PASS: C10 M10 formation competition continuity"); return 0;
        }
        catch (Exception ex)
        {
            MotorRunLogStore.Finish(runId, false, ex.Message);
            if (repeatRunId is not null) MotorRunLogStore.Finish(repeatRunId, false, ex.Message);
            return Fail("C10 exception: " + ex.Message);
        }
    }
    private static Player ReadPlayer(JsonElement e) => new(e.GetProperty("id").GetInt32(), e.GetProperty("name").GetString() ?? "Player", e.GetProperty("keeper").GetInt32(), e.GetProperty("defending").GetInt32(), e.GetProperty("playmaking").GetInt32(), e.GetProperty("passing").GetInt32(), e.GetProperty("winger").GetInt32(), e.GetProperty("scoring").GetInt32(), e.GetProperty("stamina").GetInt32(), e.GetProperty("form").GetInt32(), e.GetProperty("experience").GetInt32(), GetInt(e, "loyalty", 0), GetInt(e, "injuryLevel", -1));
    private static RegionalRatingSnapshot ReadRating(JsonElement e) { var ld = GetDouble(e, "leftDefence"); var cd = GetDouble(e, "centralDefence"); var rd = GetDouble(e, "rightDefence"); var mid = GetDouble(e, "midfield"); var la = GetDouble(e, "leftAttack"); var ca = GetDouble(e, "centralAttack"); var ra = GetDouble(e, "rightAttack"); return new RegionalRatingSnapshot(ld, cd, rd, mid, la, ca, ra, ld, cd, rd, mid, la, ca, ra); }
    private static string GetString(JsonElement e, string n, string f) => e.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? f : f;
    private static int GetInt(JsonElement e, string n, int f) => e.TryGetProperty(n, out var v) && v.TryGetInt32(out var x) ? x : f;
    private static double GetDouble(JsonElement e, string n) => e.GetProperty(n).GetDouble();
    private static void Check(bool ok, string m) { if (!ok) throw new InvalidOperationException(m); }
    private static int Fail(string m) { Console.WriteLine("FAIL: " + m); return 1; }
}
