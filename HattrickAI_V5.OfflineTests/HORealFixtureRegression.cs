using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Real CHPP fixture regression for the independent HO engine.
/// The fixture is a real 2026-09-01 CHPP export reduced to the starting XI.
/// Expected values are deliberately fixed to detect algorithm drift.
/// </summary>
public static class HORealFixtureRegression
{
    private const string FixturePath = "HattrickAI_V5.OfflineTests/Fixtures/HO_Real_CHPP_Fixture_2026-09-01.json";

    public static int Run()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(FixturePath));
        var root = document.RootElement;
        var players = root.GetProperty("players").EnumerateArray().Select(ToPlayer).ToList();
        var slots = root.GetProperty("players").EnumerateArray().Select((p, i) =>
        {
            var id = p.GetProperty("id").GetInt32();
            var name = p.GetProperty("name").GetString() ?? id.ToString();
            var code = p.GetProperty("slot").GetString() ?? throw new InvalidOperationException("Missing slot code.");
            var order = (PlayerOrder)p.GetProperty("order").GetInt32();
            return new Slot(code, code, "real CHPP fixture", name, id, 0, i, 0, order);
        }).ToList();

        var lineup = new Lineup(root.GetProperty("teamName").GetString() ?? "RealFixture", root.GetProperty("formation").GetString() ?? "3-5-2", slots);
        var request = new RatingEngineRequest(lineup, players, RatingContext.Default);
        var result = new HOEngineAdapter().Calculate(request);

        var expected = new[] { 0d, 0d, 0d, 0d, 0d, 0d, 0d };
        var actual = Values(result.Rating).ToArray();
        var actualText = string.Join(", ", actual.Select(x => x.ToString("G17")));
        for (var i = 0; i < expected.Length; i++)
        {
            if (Math.Abs(actual[i] - expected[i]) > 1e-9)
                throw new InvalidOperationException($"HO real-fixture expected values are not locked yet. Actual sectors: [{actualText}]. HatStats={result.HatStats:G17}; LoddarStats={result.LoddarStats:G17}");
        }

        return 0;
    }

    private static Player ToPlayer(JsonElement p)
        => new(
            p.GetProperty("id").GetInt32(),
            p.GetProperty("name").GetString() ?? "",
            p.GetProperty("keeper").GetInt32(),
            p.GetProperty("defending").GetInt32(),
            p.GetProperty("playmaking").GetInt32(),
            p.GetProperty("passing").GetInt32(),
            p.GetProperty("winger").GetInt32(),
            p.GetProperty("scoring").GetInt32(),
            p.GetProperty("stamina").GetInt32(),
            p.GetProperty("form").GetInt32(),
            p.GetProperty("experience").GetInt32(),
            p.GetProperty("loyalty").GetInt32(),
            -1,
            (PlayerSpecialty)p.GetProperty("specialty").GetInt32(),
            0);

    private static IEnumerable<double> Values(RegionalRatingSnapshot s)
    {
        yield return s.LeftDefence;
        yield return s.CentralDefence;
        yield return s.RightDefence;
        yield return s.Midfield;
        yield return s.LeftAttack;
        yield return s.CentralAttack;
        yield return s.RightAttack;
    }
}
