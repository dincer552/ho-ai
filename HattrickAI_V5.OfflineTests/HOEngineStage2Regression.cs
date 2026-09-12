using System;
using System.Collections.Generic;
using System.Linq;
using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

/// <summary>
/// Stage-2 regression guard for the independent HO engine adapter.
/// It checks determinism and verifies that core HO-sensitive skill changes
/// move the expected sectors without invoking the V5 calculation pipeline.
/// </summary>
public static class HOEngineStage2Regression
{
    public static int Run()
    {
        var players = FixturePlayers();
        var lineup = FixtureLineup(players);
        var request = new RatingEngineRequest(lineup, players, RatingContext.Default with
        {
            MatchLocation = MatchLocation.Home,
            Tactic = TeamTactic.Normal,
            Attitude = TeamAttitude.Normal
        });

        var engine = new HOEngineAdapter();
        if (engine.Kind != RatingEngineKind.HO)
            throw new InvalidOperationException("HO adapter kind drifted.");
        if (!string.Equals(engine.Name, "Hattrick Organizer", StringComparison.Ordinal))
            throw new InvalidOperationException("HO adapter name drifted.");

        var first = engine.Calculate(request);
        var second = engine.Calculate(request);
        AssertSnapshotEqual(first.Rating, second.Rating, "HO engine is not deterministic.");

        if (first.HatStats is null || first.HatStats <= 0)
            throw new InvalidOperationException("HO HatStats output is missing or non-positive.");
        if (first.LoddarStats is null || first.LoddarStats <= 0)
            throw new InvalidOperationException("HO LoddarStats output is missing or non-positive.");

        var strongerMidfield = players
            .Select(p => p.Id == 6 ? p with { Playmaking = p.Playmaking + 3 } : p)
            .ToList();
        var strongerMidRequest = request with { Players = strongerMidfield };
        var strongerMid = engine.Calculate(strongerMidRequest);
        if (strongerMid.Rating.Midfield <= first.Rating.Midfield)
            throw new InvalidOperationException("HO midfield response regression: higher playmaking did not raise midfield rating.");

        var strongerAttack = players
            .Select(p => p.Id == 10 ? p with { Scoring = p.Scoring + 3 } : p)
            .ToList();
        var strongerAttackRequest = request with { Players = strongerAttack };
        var strongerAttackResult = engine.Calculate(strongerAttackRequest);
        if (strongerAttackResult.Rating.CentralAttack <= first.Rating.CentralAttack)
            throw new InvalidOperationException("HO attack response regression: higher scoring did not raise central attack rating.");

        return 0;
    }

    private static IReadOnlyList<Player> FixturePlayers()
        => new[]
        {
            new Player(1, "GK", 10, 9, 4, 5, 3, 3, 8, 7, 7),
            new Player(2, "LB", 1, 11, 5, 6, 6, 4, 8, 7, 6),
            new Player(3, "LCB", 1, 12, 6, 6, 3, 4, 8, 7, 7),
            new Player(4, "RCB", 1, 12, 6, 6, 3, 4, 8, 7, 7),
            new Player(5, "RB", 1, 11, 5, 6, 6, 4, 8, 7, 6),
            new Player(6, "IM1", 1, 6, 13, 9, 5, 6, 8, 7, 8),
            new Player(7, "IM2", 1, 6, 12, 9, 5, 6, 8, 7, 8),
            new Player(8, "IM3", 1, 6, 12, 9, 5, 6, 8, 7, 8),
            new Player(9, "LW", 1, 6, 10, 9, 12, 7, 8, 7, 7),
            new Player(10, "CF", 1, 5, 6, 10, 5, 13, 8, 7, 7),
            new Player(11, "RW", 1, 5, 6, 10, 12, 8, 8, 7, 7)
        };

    private static Lineup FixtureLineup(IReadOnlyList<Player> players)
    {
        var slots = new[]
        {
            ("GK-C", 1), ("DEF-L", 2), ("DEF-CL", 3), ("DEF-CR", 4), ("DEF-R", 5),
            ("IM-C", 6), ("IM-C", 7), ("IM-C", 8),
            ("FW-L", 9), ("FW-C", 10), ("FW-R", 11)
        };

        return new Lineup(
            "HO Regression Fixture",
            "4-3-3",
            slots.Select((x, i) => new Slot(
                x.Item1,
                x.Item1,
                "HO stage 2 regression",
                players.First(p => p.Id == x.Item2).Name,
                x.Item2,
                0,
                i,
                0,
                PlayerOrder.Normal)).ToList());
    }

    private static void AssertSnapshotEqual(RegionalRatingSnapshot a, RegionalRatingSnapshot b, string message)
    {
        var av = Values(a).ToArray();
        var bv = Values(b).ToArray();
        for (var i = 0; i < av.Length; i++)
            if (Math.Abs(av[i] - bv[i]) > 1e-12)
                throw new InvalidOperationException(message);
    }

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
