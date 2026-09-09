using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// MATCHUP-01 acceptance: exercise multiple opponent profiles through the real
/// M3-M11 pipeline and verify every legal formation is paired with all seven tactics.
/// This validates matrix coverage and canonical outcome fields; it does not calibrate
/// hidden engine coefficients.
/// </summary>
public static class C21TacticalMatchupMatrixRegression
{
    public static async Task<int> RunAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) return Fail($"fixture bulunamadı: {path}");

        await using var stream = File.OpenRead(path);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;
        var normalized = root.GetProperty("normalized");
        var analysis = root.GetProperty("v5Analysis");
        var players = normalized.GetProperty("ownPlayers").EnumerateArray().Select(ReadPlayer).ToList();
        var ownTeamName = GetString(analysis.GetProperty("ownLineup"), "teamName", "Fixture");
        var baseOpponentRating = ReadRating(analysis.GetProperty("opponentRating"));
        var scenarios = new[]
        {
            ("balanced", 1.00, 1.00, 1.00),
            ("strong-mid-attack", 1.25, 1.00, 1.25),
            ("strong-defence", 1.00, 1.30, 1.00),
            ("strong-wings", 1.05, 1.05, 1.30),
            ("low-stamina", 0.92, 0.92, 0.92)
        };

        var allTactics = Enum.GetValues<TeamTactic>();
        var observedScenarioResults = new List<string>();
        Console.WriteLine("=== C21 MATCHUP-01 TACTIC MATRIX REGRESSION ===");

        foreach (var scenario in scenarios)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var opponentRating = ScaleRating(baseOpponentRating, scenario.Item2, scenario.Item3, scenario.Item4);
            var opponent = new OpponentMatchProfile(
                "Opponent-" + scenario.Item1,
                GetString(analysis, "opponentFormation", "4-4-2"),
                opponentRating,
                new OpponentThreatEngine().Analyze(opponentRating));
            var context = new MatchDataContext(
                players,
                0,
                ownTeamName,
                opponent,
                RatingContext.Default,
                MatchQuestionnaire.Default);

            var runId = MotorRunLogStore.Start("offline-c21-" + scenario.Item1);
            try
            {
                var result = await new MotorPipelineService().RunAsync(context, players, cancellationToken, runId);
                var legalFormations = result.M4.Candidates
                    .Select(x => x.Formation)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                Check(legalFormations.Length >= 3, $"{scenario.Item1}: fewer than three legal formations");

                var matrix = result.TacticComparisons;
                Check(matrix.Count > 0, $"{scenario.Item1}: tactic matrix is empty");
                foreach (var formation in legalFormations)
                {
                    foreach (var tactic in allTactics)
                    {
                        Check(
                            matrix.Any(x => x.CandidateId.Length > 0 && x.Tactic == tactic && CandidateFormation(result, x.CandidateId) == formation),
                            $"{scenario.Item1}: missing {formation} × {tactic}");
                    }
                }

                Check(matrix.All(x => double.IsFinite(x.WinProbability) && x.WinProbability is >= 0 and <= 1), $"{scenario.Item1}: invalid win probability");
                Check(matrix.All(x => double.IsFinite(x.ExpectedPoints) && x.ExpectedPoints is >= 0 and <= 3), $"{scenario.Item1}: invalid expected points");
                Check(matrix.All(x => Math.Abs(x.ExpectedPoints - (3.0 * x.WinProbability + x.DrawProbability)) < 1e-12), $"{scenario.Item1}: non-canonical expected points");
                var finalSignature = Signature(result.FinalPlan.Lineup);
                var finalRows = matrix.Where(x => x.CandidateId == finalSignature).ToList();
                Check(finalRows.Count == allTactics.Length, $"{scenario.Item1}: final XI has {finalRows.Count}/{allTactics.Length} tactic rows");
                Check(result.M11 is not null && result.M11.FormationCount == legalFormations.Length, $"{scenario.Item1}: M11 formation count mismatch");
                Check(result.M11 is not null && result.M11.Prediction is not null, $"{scenario.Item1}: M11 prediction missing");
                MotorRunLogStore.Finish(runId, true, "C21 matchup matrix passed");
                observedScenarioResults.Add($"{scenario.Item1}: F={legalFormations.Length}; rows={matrix.Count}");
            }
            catch (Exception ex)
            {
                MotorRunLogStore.Finish(runId, false, ex.Message);
                throw;
            }
        }

        Console.WriteLine(string.Join(" | ", observedScenarioResults));
        Console.WriteLine("PASS: C21 MATCHUP-01 multi-opponent × multi-formation × seven-tactic matrix");
        return 0;
    }

    private static string CandidateFormation(MotorPipelineResult result, string candidateId)
    {
        var match = result.CandidateDatabase2.FirstOrDefault(x => x.CandidateId.Equals(candidateId, StringComparison.Ordinal));
        return match?.Formation ?? string.Empty;
    }

    private static RegionalRatingSnapshot ScaleRating(RegionalRatingSnapshot r, double midfieldScale, double defenceScale, double wingScale)
        => new(
            r.LeftDefence * defenceScale, r.CentralDefence * defenceScale, r.RightDefence * wingScale,
            r.Midfield * midfieldScale,
            r.LeftAttack * wingScale, r.CentralAttack * midfieldScale, r.RightAttack * wingScale,
            r.OpponentLeftDefence * defenceScale, r.OpponentCentralDefence * defenceScale, r.OpponentRightDefence * wingScale,
            r.OpponentMidfield * midfieldScale,
            r.OpponentLeftAttack * wingScale, r.OpponentCentralAttack * midfieldScale, r.OpponentRightAttack * wingScale);

    private static Player ReadPlayer(JsonElement e)
        => new(e.GetProperty("id").GetInt32(), e.GetProperty("name").GetString() ?? "Player", e.GetProperty("keeper").GetInt32(), e.GetProperty("defending").GetInt32(), e.GetProperty("playmaking").GetInt32(), e.GetProperty("passing").GetInt32(), e.GetProperty("winger").GetInt32(), e.GetProperty("scoring").GetInt32(), e.GetProperty("stamina").GetInt32(), e.GetProperty("form").GetInt32(), e.GetProperty("experience").GetInt32(), GetInt(e, "loyalty", 0), GetInt(e, "injuryLevel", -1));

    private static RegionalRatingSnapshot ReadRating(JsonElement e)
    {
        var ld = GetDouble(e, "leftDefence"); var cd = GetDouble(e, "centralDefence"); var rd = GetDouble(e, "rightDefence");
        var mid = GetDouble(e, "midfield"); var la = GetDouble(e, "leftAttack"); var ca = GetDouble(e, "centralAttack"); var ra = GetDouble(e, "rightAttack");
        return new RegionalRatingSnapshot(ld, cd, rd, mid, la, ca, ra, ld, cd, rd, mid, la, ca, ra);
    }

    private static string Signature(Lineup lineup)
        => string.Join(";", lineup.Slots.OrderBy(s => s.Code, StringComparer.Ordinal).ThenBy(s => s.PlayerId).Select(s => $"{s.Code}:{s.PlayerId}:{(int)s.Order}"));

    private static string GetString(JsonElement e, string name, string fallback)
        => e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;

    private static int GetInt(JsonElement e, string name, int fallback)
        => e.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? parsed : fallback;

    private static double GetDouble(JsonElement e, string name) => e.GetProperty(name).GetDouble();

    private static int Fail(string message) { Console.WriteLine("FAIL: " + message); return 1; }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
