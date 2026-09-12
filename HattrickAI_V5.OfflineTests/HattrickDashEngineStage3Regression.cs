using System;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Deterministic regression for the HattrickDash lineup calculation.
/// The fixture intentionally uses equal skills so the expected aggregates are
/// unambiguous: every selected position estimates to 10.0.
/// </summary>
public static class HattrickDashEngineStage3Regression
{
    public static int Run()
    {
        var players = new[]
        {
            P(1), P(2), P(3), P(4), P(5), P(6), P(7), P(8), P(9), P(10), P(11)
        };

        var codes = new[]
        {
            "GK", "DEF-L", "DEF-CL", "DEF-CR", "DEF-R",
            "IM-L", "IM-C", "IM-R", "W-R", "FW-L", "FW-R"
        };

        var slots = new Slot[codes.Length];
        for (var i = 0; i < codes.Length; i++)
            slots[i] = new Slot(codes[i], codes[i], "dash fixture", $"P{i + 1}", i + 1, 0, i, 0);

        var request = new RatingEngineRequest(
            new Lineup("DashFixture", "4-4-2", slots),
            players,
            RatingContext.Default);

        var engine = new HattrickDashEngine();
        if (engine.Kind != RatingEngineKind.HattrickDash)
            throw new InvalidOperationException("HattrickDash engine kind drifted.");

        var result = engine.Calculate(request);
        AssertClose(result.Rating.Midfield, 10.0, "midfield");
        AssertClose(result.Rating.LeftDefence, 10.0, "left defence");
        AssertClose(result.Rating.CentralDefence, 10.0, "central defence");
        AssertClose(result.Rating.RightDefence, 10.0, "right defence");
        AssertClose(result.Rating.LeftAttack, 10.0, "left attack");
        AssertClose(result.Rating.CentralAttack, 10.0, "central attack");
        AssertClose(result.Rating.RightAttack, 10.0, "right attack");
        AssertClose(result.HatStats ?? double.NaN, 90.0, "HatStats");
        AssertClose(result.LoddarStats ?? double.NaN, 20.0, "LoddarStats");

        // The source Dash lineup calculation is independent of V5 match context.
        var changedContext = request with
        {
            Context = new RatingContext(MatchLocation.Home, TeamAttitude.MatchOfTheSeason, TeamTactic.AttackWings)
            {
                MatchMinute = 75,
                GoalDifference = -2
            }
        };
        var changed = engine.Calculate(changedContext);
        AssertClose(changed.Rating.Midfield, result.Rating.Midfield, "context-independent midfield");
        AssertClose(changed.HatStats ?? double.NaN, result.HatStats ?? double.NaN, "context-independent HatStats");

        return 0;
    }

    private static Player P(int id)
        => new(id, $"P{id}", 10, 10, 10, 10, 10, 10, 10, 10, 10);

    private static void AssertClose(double actual, double expected, string label)
    {
        if (double.IsNaN(actual) || Math.Abs(actual - expected) > 1e-9)
            throw new InvalidOperationException($"HattrickDash regression failed for {label}: actual={actual:G17}, expected={expected:G17}.");
    }
}
