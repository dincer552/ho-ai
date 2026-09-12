using System;
using System.Collections.Generic;
using System.Linq;
using LegacyHO = HattrickAI.HOEngine;

namespace HattrickAI.V5.Core;

/// <summary>
/// Stage-2 adapter for the independent Hattrick Organizer rating implementation.
/// The legacy HO implementation is kept isolated behind this adapter; V5 rating
/// coefficients and the existing V5 calculation pipeline are not touched.
/// </summary>
public sealed class HOEngineAdapter : IRatingEngine
{
    private readonly LegacyHO.LineupRatingEngine _engine = new();

    public RatingEngineKind Kind => RatingEngineKind.HO;
    public string Name => "Hattrick Organizer";

    public RatingEngineResult Calculate(RatingEngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Lineup.Slots.Count != 11)
            throw new ArgumentException("HO engine requires exactly eleven lineup slots.", nameof(request));

        var playersById = request.Players.ToDictionary(x => x.Id);
        var orderedSlots = OrderSlots(request.Lineup);
        var lineup = orderedSlots
            .Select(slot =>
            {
                if (slot.PlayerId <= 0 || !playersById.TryGetValue(slot.PlayerId, out var player))
                    throw new ArgumentException($"HO engine cannot resolve player {slot.PlayerId} for slot {slot.Code}.", nameof(request));

                return ToLegacyPlayer(player);
            })
            .ToList();

        var context = ToLegacyContext(request.Context, orderedSlots);
        var ratings = _engine.Calculate(lineup, request.Lineup.Formation, context);

        var snapshot = new RegionalRatingSnapshot(
            ratings.LeftDefence, ratings.CentralDefence, ratings.RightDefence,
            ratings.Midfield, ratings.LeftAttack, ratings.CentralAttack, ratings.RightAttack,
            ratings.LeftDefence, ratings.CentralDefence, ratings.RightDefence,
            ratings.Midfield, ratings.LeftAttack, ratings.CentralAttack, ratings.RightAttack);

        var hatStats = CalculateHatStats(ratings);
        var loddarStats = CalculateLoddarStats(ratings, request.Context.Tactic, request.Context);

        return new RatingEngineResult(Kind, snapshot, hatStats, loddarStats);
    }

    private static LegacyHO.PlayerData ToLegacyPlayer(Player player)
        => new()
        {
            PlayerId = player.Id,
            Name = player.Name,
            Form = player.Form,
            Stamina = player.Stamina,
            Experience = player.Experience,
            Loyalty = player.Loyalty,
            Keeper = player.Keeper,
            Defending = player.Defending,
            Playmaking = player.Playmaking,
            Passing = player.Passing,
            Winger = player.Winger,
            Scoring = player.Scoring,
            SetPieces = player.SetPiecesSkill,
            Specialty = player.Specialty.ToString(),
            Injured = player.InjuryLevel >= 0,
            Suspended = false
        };

    private static LegacyHO.TeamMatchContext ToLegacyContext(
        RatingContext context,
        IReadOnlyList<Slot> slots)
    {
        var behaviour = new Dictionary<int, LegacyHO.PlayerBehaviour>();
        for (var i = 0; i < slots.Count; i++)
            behaviour[i] = ToLegacyBehaviour(slots[i].Order);

        return new LegacyHO.TeamMatchContext
        {
            TacticType = ToLegacyTactic(context.Tactic),
            TacticLevel = 1,
            Attitude = context.Attitude switch
            {
                TeamAttitude.PlayItCool => LegacyHO.TeamAttitude.PIC,
                TeamAttitude.MatchOfTheSeason => LegacyHO.TeamAttitude.MOTS,
                _ => LegacyHO.TeamAttitude.Normal
            },
            IsHome = context.MatchLocation == MatchLocation.Home,
            Minute = Math.Clamp(context.MatchMinute, 0, 120),
            SlotBehaviours = behaviour,
            CoachModifier = 0,
            TeamSpirit = 0,
            Confidence = 0,
            Weather = LegacyHO.MatchWeather.Normal
        };
    }

    private static LegacyHO.PlayerBehaviour ToLegacyBehaviour(PlayerOrder order) => order switch
    {
        PlayerOrder.Defensive => LegacyHO.PlayerBehaviour.Defensive,
        PlayerOrder.Offensive => LegacyHO.PlayerBehaviour.Offensive,
        PlayerOrder.TowardsWing => LegacyHO.PlayerBehaviour.TowardsWing,
        PlayerOrder.TowardsMiddle => LegacyHO.PlayerBehaviour.TowardsMiddle,
        _ => LegacyHO.PlayerBehaviour.Normal
    };

    private static int ToLegacyTactic(TeamTactic tactic) => tactic switch
    {
        TeamTactic.CounterAttack => 2,
        TeamTactic.AttackMiddle => 3,
        TeamTactic.AttackWings => 4,
        TeamTactic.Creative => 7,
        _ => 0
    };

    private static double CalculateHatStats(LegacyHO.TeamRatings ratings)
        => 4.0 * (3.0 * ratings.Midfield
            + ratings.LeftDefence + ratings.CentralDefence + ratings.RightDefence
            + ratings.LeftAttack + ratings.CentralAttack + ratings.RightAttack);

    private static double CalculateLoddarStats(
        LegacyHO.TeamRatings r,
        TeamTactic tactic,
        RatingContext context)
    {
        const double defenceWeight = 0.47;
        const double attackWeight = 0.53;
        const double centralWeight = 0.37;
        const double counterAttackWeight = 0.25;

        var tacticLevel = 1.0;
        var correctedCentralWeight = centralWeight;
        var counterCorrection = 0.0;

        if (tactic == TeamTactic.AttackMiddle)
            correctedCentralWeight += ((0.2 * (tacticLevel - 1.0) / 19.0) + 0.2);
        else if (tactic == TeamTactic.AttackWings)
            correctedCentralWeight -= ((0.2 * (tacticLevel - 1.0) / 19.0) + 0.2);

        if (tactic == TeamTactic.CounterAttack)
            counterCorrection = (counterAttackWeight * 2.0 * tacticLevel) / (tacticLevel + 20.0);

        var wingerWeight = (1.0 - correctedCentralWeight) / 2.0;
        var attackStrength = (attackWeight + counterCorrection)
            * (correctedCentralWeight * Hq(r.CentralAttack)
                + wingerWeight * (Hq(r.LeftAttack) + Hq(r.RightAttack)));
        var defenseStrength = defenceWeight
            * (centralWeight * Hq(r.CentralDefence)
                + ((1.0 - centralWeight) / 2.0) * (Hq(r.LeftDefence) + Hq(r.RightDefence)));
        var midfieldFactor = Hq(r.Midfield);

        return 80.0 * midfieldFactor * (defenseStrength + attackStrength);
    }

    private static double Hq(double value)
    {
        var x = (int)(((value - 1.0) * 4.0) + 1.0);
        return (2.0 * x) / (x + 80.0);
    }

    private static IReadOnlyList<Slot> OrderSlots(Lineup lineup)
    {
        var buckets = lineup.Slots
            .GroupBy(GetRoleBucket)
            .ToDictionary(x => x.Key, x => new Queue<Slot>(x));

        var roles = LegacyHO.LineupRatingEngine.GetRoles(lineup.Formation);
        var result = new List<Slot>(11);
        foreach (var role in roles)
        {
            var bucket = BucketFor(role);
            if (!TryDequeue(buckets, bucket, out var slot))
                throw new ArgumentException($"No lineup slot available for HO role {role}.", nameof(lineup));
            result.Add(slot);
        }
        return result;
    }

    private static bool TryDequeue(
        Dictionary<string, Queue<Slot>> buckets,
        string bucket,
        out Slot slot)
    {
        if (buckets.TryGetValue(bucket, out var direct) && direct.Count > 0)
        {
            slot = direct.Dequeue();
            return true;
        }

        // Hattrick's 3-5-2 / 4-5-1 style formations expose wing-side midfielder
        // slots in canonical input, while legacy HO models them as central midfield
        // roles in GetRoles(). Treat the two representations as equivalent only as
        // a role-mapping concern; the legacy calculation still receives the HO role.
        if (bucket == "IM-C")
        {
            if (buckets.TryGetValue("IM-L", out var left) && left.Count > 0)
            {
                slot = left.Dequeue();
                return true;
            }
            if (buckets.TryGetValue("IM-R", out var right) && right.Count > 0)
            {
                slot = right.Dequeue();
                return true;
            }
        }

        slot = default!;
        return false;
    }

    private static string GetRoleBucket(Slot slot) => slot.Code switch
    {
        "GK" or "GK-C" => "GK",
        "DEF-L" => "DEF-L",
        "DEF-R" => "DEF-R",
        "DEF-C" or "DEF-CL" or "DEF-CR" => "DEF-C",
        "IM-L" => "IM-L",
        "IM-R" => "IM-R",
        "IM-C" => "IM-C",
        "W-L" => "W-L",
        "W-R" => "W-R",
        "FW-L" => "FW-L",
        "FW-R" => "FW-R",
        "FW-C" => "FW-C",
        _ => throw new ArgumentException($"Unsupported HO lineup slot code: {slot.Code}.", nameof(slot))
    };

    private static string BucketFor(LegacyHO.PlayerRole role) => role switch
    {
        LegacyHO.PlayerRole.Goalkeeper => "GK",
        LegacyHO.PlayerRole.LeftDefender => "DEF-L",
        LegacyHO.PlayerRole.RightDefender => "DEF-R",
        LegacyHO.PlayerRole.CentralDefender => "DEF-C",
        LegacyHO.PlayerRole.LeftMidfielder => "IM-L",
        LegacyHO.PlayerRole.RightMidfielder => "IM-R",
        LegacyHO.PlayerRole.CentralMidfielder => "IM-C",
        LegacyHO.PlayerRole.LeftWinger => "W-L",
        LegacyHO.PlayerRole.RightWinger => "W-R",
        LegacyHO.PlayerRole.LeftForward => "FW-L",
        LegacyHO.PlayerRole.RightForward => "FW-R",
        LegacyHO.PlayerRole.CentralForward => "FW-C",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}
