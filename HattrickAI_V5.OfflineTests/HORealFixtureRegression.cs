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
/// Expected values are fixed to detect algorithm drift.
/// </summary>
public static class HORealFixtureRegression
{
    private const string FixturePath = "HattrickAI_V5.OfflineTests/Fixtures/HO_Real_CHPP_Fixture_2026-09-01.json";

    private static readonly double[] ExpectedSectors =
    {
        8.9278407632371284,
        14.241616934311249,
        8.8845667124997263,
        5.437522215250981,
        8.1350636338080307,
        9.5241231738830496,
        7.647351119659394
    };

    private const double ExpectedHatStats = 294.69251593260606;
    private const double ExpectedLoddarStats = 17.640091980027538;

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

        var lineup = new Lineup(
            root.GetProperty("teamName").GetString() ?? "RealFixture",
            root.GetProperty("formation").GetString() ?? "3-5-2",
            slots);

        var engine = new HOEngineAdapter();
        var baseRequest = new RatingEngineRequest(lineup, players, RatingContext.Default);
        var result = engine.Calculate(baseRequest);
        AssertExpected(result, "base");

        // Canonical context parity: the legacy HO minute/stamina path must remain active.
        var minute60 = engine.Calculate(baseRequest with
        {
            Context = RatingContext.Default with { MatchMinute = 60 }
        });
        if (minute60.Rating.Midfield >= result.Rating.Midfield)
            throw new InvalidOperationException("HO minute/stamina parity regression: midfield did not decay at minute 60.");

        var home = engine.Calculate(baseRequest with
        {
            Context = RatingContext.Default with { MatchLocation = MatchLocation.Home }
        });
        if (home.Rating.Midfield <= result.Rating.Midfield)
            throw new InvalidOperationException("HO home-context parity regression: home midfield modifier did not apply.");

        var pic = engine.Calculate(baseRequest with
        {
            Context = RatingContext.Default with { Attitude = TeamAttitude.PlayItCool }
        });
        var mots = engine.Calculate(baseRequest with
        {
            Context = RatingContext.Default with { Attitude = TeamAttitude.MatchOfTheSeason }
        });
        if (!(pic.Rating.Midfield < result.Rating.Midfield && mots.Rating.Midfield > result.Rating.Midfield))
            throw new InvalidOperationException("HO attitude-context parity regression: PIC/MOTS midfield modifiers did not apply.");

        return 0;
    }

    private static void AssertExpected(RatingEngineResult result, string label)
    {
        var actual = Values(result.Rating).ToArray();
        for (var i = 0; i < ExpectedSectors.Length; i++)
        {
            if (Math.Abs(actual[i] - ExpectedSectors[i]) > 1e-9)
                throw new InvalidOperationException(
                    $"HO real-fixture {label} sector drift at index {i}: expected={ExpectedSectors[i]:G17}, actual={actual[i]:G17}.");
        }

        if (!result.HatStats.HasValue || Math.Abs(result.HatStats.Value - ExpectedHatStats) > 1e-9)
            throw new InvalidOperationException(
                $"HO real-fixture {label} HatStats drift: expected={ExpectedHatStats:G17}, actual={result.HatStats.GetValueOrDefault():G17}.");

        if (!result.LoddarStats.HasValue || Math.Abs(result.LoddarStats.Value - ExpectedLoddarStats) > 1e-9)
            throw new InvalidOperationException(
                $"HO real-fixture {label} LoddarStats drift: expected={ExpectedLoddarStats:G17}, actual={result.LoddarStats.GetValueOrDefault():G17}.");
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
