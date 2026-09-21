using HattrickAI.V5.Core;

namespace HattrickAI.V5.OfflineTests;

public static class Stage6OvercrowdingRegression
{
    public static int Run()
    {
        var failures = new List<string>();
        var engine = new RegionalRatingEngineFixed();

        // Exhaustive crowding-layer regression: every possible occupancy
        // combination of the 14 canonical slots must resolve to the same
        // CD/IM/FW counts and multipliers as the direct count law.
        if (RatingCrowding.AllCombinations.Count != RatingCrowding.CombinationCount)
            failures.Add($"Crowding table size expected {RatingCrowding.CombinationCount}, got {RatingCrowding.AllCombinations.Count}");

        for (var mask = 0; mask < RatingCrowding.CombinationCount; mask++)
        {
            var state = RatingCrowding.ForMask(mask);
            var expectedCd = 0;
            var expectedIm = 0;
            var expectedFw = 0;

            for (var bit = 0; bit < RatingPositionMatrix.CanonicalSlots.Length; bit++)
            {
                if ((mask & (1 << bit)) == 0)
                    continue;

                switch (RatingCrowding.CanonicalGroup(RatingPositionMatrix.CanonicalSlots[bit]))
                {
                    case "CD": expectedCd++; break;
                    case "IM": expectedIm++; break;
                    case "FW": expectedFw++; break;
                }
            }

            if (state.CentralDefenders != expectedCd ||
                state.InnerMidfielders != expectedIm ||
                state.Forwards != expectedFw)
            {
                failures.Add($"mask {mask}: crowding counts mismatch");
                continue;
            }

            var expectedCdFactor = RatingCrowding.GetMultiplier("CD", expectedCd);
            var expectedImFactor = RatingCrowding.GetMultiplier("IM", expectedIm);
            var expectedFwFactor = RatingCrowding.GetMultiplier("FW", expectedFw);

            if (Math.Abs(state.ForSlot("DEF-C") - expectedCdFactor) > 1e-12 ||
                Math.Abs(state.ForSlot("IM-C") - expectedImFactor) > 1e-12 ||
                Math.Abs(state.ForSlot("FW-C") - expectedFwFactor) > 1e-12)
                failures.Add($"mask {mask}: crowding multiplier mismatch");
        }


        // Exact HO!/Schum crowding law:
        // CD sector: 2=.964, 3=.900; otherwise 1.0
        // IM sector: 2=.935, 3=.825; otherwise 1.0
        // FW sector: 2=.945, 3=.865; otherwise 1.0
        // The factor belongs to each player's own lineup sector and is applied
        // to every sector contribution of that player. It is NOT applied to the
        // accumulated rating sector and must NOT compound as players are added.

        var cd = new RegionalPlayer(
            1, RegionalPosition.CentralDefender, PlayerSide.Center, PlayerOrder.Normal,
            1, 12, 10, 8, 3, 6, 7, 7, 0, 0);

        var imLeft = new RegionalPlayer(
            10, RegionalPosition.InnerMidfielder, PlayerSide.Left, PlayerOrder.Normal,
            1, 8, 14, 10, 8, 6, 7, 7, 0, 0, "IM-L");

        var imCenter = imLeft with { Id = 11, Side = PlayerSide.Center, SlotCode = "IM-C" };
        var imRight = imLeft with { Id = 12, Side = PlayerSide.Right, SlotCode = "IM-R" };

        var fw = new RegionalPlayer(
            20, RegionalPosition.Forward, PlayerSide.Center, PlayerOrder.Normal,
            1, 4, 5, 6, 9, 13, 7, 7, 0, 0);

        var wb = new RegionalPlayer(
            30, RegionalPosition.WingBack, PlayerSide.Left, PlayerOrder.Normal,
            1, 10, 6, 8, 4, 6, 7, 7, 0, 0, "WB-L");

        var winger = new RegionalPlayer(
            40, RegionalPosition.Winger, PlayerSide.Left, PlayerOrder.Normal,
            1, 6, 7, 8, 13, 6, 7, 7, 0, 0, "W-L");

        CheckSingleGroup(engine, cd, 2, .964, "2 central defenders", failures);
        CheckSingleGroup(engine, cd, 3, .900, "3 central defenders", failures);
        CheckSingleGroup(engine, cd, 4, 1.000, "4 central defenders (HO! map has no 4-player penalty)", failures);

        CheckSingleGroup(engine, imLeft, 2, .935, "2 inner midfielders", failures);
        CheckSingleGroup(engine, imLeft, 3, .825, "3 inner midfielders", failures);
        CheckSingleGroup(engine, imLeft, 4, 1.000, "4 inner midfielders (HO! map has no 4-player penalty)", failures);

        CheckSingleGroup(engine, fw, 2, .945, "2 forwards", failures);
        CheckSingleGroup(engine, fw, 3, .865, "3 forwards", failures);
        CheckSingleGroup(engine, fw, 4, 1.000, "4 forwards (HO! map has no 4-player penalty)", failures);

        // Side combinations are NOT different crowding regimes: L+C, C+R and L+R
        // are all two players in the same InnerMidfield sector.
        CheckPair(engine, new[] { imLeft, imCenter }, .935, "IM-L + IM-C", failures);
        CheckPair(engine, new[] { imCenter, imRight }, .935, "IM-C + IM-R", failures);
        CheckPair(engine, new[] { imLeft, imRight }, .935, "IM-L + IM-R", failures);
        CheckTriple(engine, new[] { imLeft, imCenter, imRight }, .825, "IM-L + IM-C + IM-R", failures);

        // Mixed central lines: 5 "defenders" means 3 crowded CDs + 2 uncrowded WBs.
        // Their midfield/attack/defence contributions must retain their own factors.
        var mixed = engine.CalculateWithTrace(new[]
        {
            cd,
            cd with { Id = 2 },
            cd with { Id = 3 },
            wb,
            wb with { Id = 31 },
            imLeft,
            imCenter,
            imRight,
            fw,
            fw with { Id = 21 },
            fw with { Id = 22 },
            winger,
            winger with { Id = 41 }
        });

        CheckPlayerCrowding(mixed, 1, "CD-1", .900, failures);
        CheckPlayerCrowding(mixed, 2, "CD-2", .900, failures);
        CheckPlayerCrowding(mixed, 3, "CD-3", .900, failures);
        CheckPlayerCrowding(mixed, 30, "WB-1", 1.000, failures);
        CheckPlayerCrowding(mixed, 31, "WB-2", 1.000, failures);
        CheckPlayerCrowding(mixed, 10, "IM-L", .825, failures);
        CheckPlayerCrowding(mixed, 11, "IM-C", .825, failures);
        CheckPlayerCrowding(mixed, 12, "IM-R", .825, failures);
        CheckPlayerCrowding(mixed, 20, "FW-1", .865, failures);
        CheckPlayerCrowding(mixed, 21, "FW-2", .865, failures);
        CheckPlayerCrowding(mixed, 22, "FW-3", .865, failures);
        CheckPlayerCrowding(mixed, 40, "W-1", 1.000, failures);
        CheckPlayerCrowding(mixed, 41, "W-2", 1.000, failures);

        // The final sector subtotal must be exactly the sum of already-crowded
        // per-player contributions. This catches the old bug where the whole
        // accumulated sector was multiplied again for every new player.
        foreach (var sector in mixed.Sectors)
        {
            var sum = sector.PlayerContributions.Values.Sum();
            if (Math.Abs(sum - sector.MatrixSubtotal) > 1e-10)
                failures.Add($"{sector.Sector}: subtotal {sector.MatrixSubtotal:F10} != player contribution sum {sum:F10}");
        }


        if (failures.Count == 0)
        {
            Console.WriteLine("PASS: Stage 6 exact HO! overcrowding model; no cross-position contamination or cumulative compounding");
            return 0;
        }

        foreach (var failure in failures)
            Console.WriteLine("FAIL: " + failure);

        Console.WriteLine($"FAIL: Stage 6 ({failures.Count} assertion(s))");
        return 1;
    }


    private static void CheckSingleGroup(
        RegionalRatingEngineFixed engine,
        RegionalPlayer player,
        int count,
        double expectedFactor,
        string name,
        List<string> failures)
    {
        var players = Enumerable.Range(0, count)
            .Select(i => player with { Id = player.Id + i + 1000 })
            .ToArray();

        var trace = engine.CalculateWithTrace(players);
        foreach (var p in trace.Players)
        {
            CheckPlayerTrace(p, expectedFactor, name, failures);
        }
    }

    private static void CheckPair(
        RegionalRatingEngineFixed engine,
        IReadOnlyList<RegionalPlayer> players,
        double expectedFactor,
        string name,
        List<string> failures)
    {
        var trace = engine.CalculateWithTrace(players);
        foreach (var p in trace.Players)
            CheckPlayerTrace(p, expectedFactor, name, failures);
    }

    private static void CheckTriple(
        RegionalRatingEngineFixed engine,
        IReadOnlyList<RegionalPlayer> players,
        double expectedFactor,
        string name,
        List<string> failures)
    {
        var trace = engine.CalculateWithTrace(players);
        foreach (var p in trace.Players)
            CheckPlayerTrace(p, expectedFactor, name, failures);
    }

    private static void CheckPlayerCrowding(
        RatingCalculationTraceResult trace,
        int playerId,
        string name,
        double expectedFactor,
        List<string> failures)
    {
        var player = trace.Players.Single(p => p.PlayerId == playerId);
        CheckPlayerTrace(player, expectedFactor, name, failures);
    }

    private static void CheckPlayerTrace(
        PlayerRatingCalculationTrace player,
        double expectedFactor,
        string name,
        List<string> failures)
    {
        if (Math.Abs(player.CrowdingMultiplier - expectedFactor) > 1e-12)
            failures.Add($"{name}: crowding expected {expectedFactor:F3}, got {player.CrowdingMultiplier:F3}");

        foreach (var kv in player.SkillContributionsBeforeExperience)
        {
            player.CrowdingAdjustedContributions.TryGetValue(kv.Key, out var actual);
            player.ExperienceContributions.TryGetValue(kv.Key, out var experience);
            var expected = kv.Value * expectedFactor + experience;
            if (Math.Abs(actual - expected) > 1e-10)
                failures.Add($"{name} {kv.Key}: expected skill×crowding+XP={expected:F10}, got {actual:F10}");
        }

        // Explicitly prove that experience itself is not crowded.
        foreach (var kv in player.ExperienceContributions)
        {
            player.CrowdingAdjustedContributions.TryGetValue(kv.Key, out var actual);
            player.SkillContributionsBeforeExperience.TryGetValue(kv.Key, out var skill);
            var expected = skill * expectedFactor + kv.Value;
            if (Math.Abs(actual - expected) > 1e-10)
                failures.Add($"{name} {kv.Key}: experience was crowded or lost; expected {expected:F10}, got {actual:F10}");
        }
    }
}
