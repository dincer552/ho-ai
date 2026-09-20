using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// V5 regional rating engine. Skill contribution is calculated from the
/// canonical 14 Hattrick field slots independently; context, loyalty,
/// stamina and central-position crowding are applied as separate layers afterwards.
/// </summary>
public sealed class RegionalRatingEngineFixed
{
    private const double BaselineFormFactor = .756;
    private const double ReferenceMidfieldCalibration = .8285714285714286;
    private const double ReferenceLeftAttackCalibration = 1.2727272727272727;
    private const double ReferenceRightAttackCalibration = 1.2258064516129032;

    public RegionalRatingSnapshot Calculate(IReadOnlyList<RegionalPlayer> players, RatingContext? context = null)
        => CalculateCore(players, context, null, null, null).Rating;

    public RatingCalculationTraceResult CalculateWithTrace(
        IReadOnlyList<RegionalPlayer> players,
        RatingContext? context = null,
        string? teamName = null,
        string? formation = null)
        => CalculateCore(players, context, teamName, formation, players.ToDictionary(x => x.Id, _ => string.Empty)).Trace!;

    private sealed record CoreResult(RegionalRatingSnapshot Rating, RatingCalculationTraceResult? Trace);

    private CoreResult CalculateCore(
        IReadOnlyList<RegionalPlayer> players,
        RatingContext? context,
        string? teamName,
        string? formation,
        IReadOnlyDictionary<int, string>? playerNames)
    {
        context ??= RatingContext.Default;
        var sectors = Empty();
        var playerTraces = new List<PlayerRatingCalculationTrace>(players.Count);
        var crowdingState = RatingCrowding.Evaluate(players);

        foreach (var p in players)
        {
            var formFactor = FormFactor(p.Form);
            var staminaMultiplier = StaminaMatchMultiplier(p.Stamina, context.MatchMinute);
            var formMultiplier = formFactor / BaselineFormFactor * staminaMultiplier;
            var loyalty = LoyaltyEffect(p.Loyalty);
            var experienceBonus = ExperienceBonus(p.Experience);

            var k = new EffectiveSkillsFixed(
                SkillRating(p.Keeper) + loyalty + experienceBonus,
                SkillRating(p.Defending) + loyalty + experienceBonus,
                SkillRating(p.Playmaking) + loyalty + experienceBonus,
                SkillRating(p.Passing) + loyalty + experienceBonus,
                SkillRating(p.Winger) + loyalty + experienceBonus,
                SkillRating(p.Scoring) + loyalty + experienceBonus,
                formMultiplier);

            // Schum/HO! ordering is critical:
            //   1) skill + loyalty
            //   2) form
            //   3) contribution coefficient
            //   4) central-position crowding
            //   5) experience is added AFTER crowding
            //
            // The old V5 path folded ExperienceBonus into every effective skill
            // before crowding, which incorrectly reduced experience whenever a
            // central line had 2/3 players. That is the main crowding bug.
            var direct = Empty();
            var skillOnly = Empty();
            string route = RatingCalculationFormulaCatalog.RouteFor(RatingPositionMatrix.CanonicalSlot(p), p.Order, p.Side);

            RatingPositionMatrix.AddContribution(
                direct, p, k.Keeper, k.Defending, k.Playmaking, k.Passing,
                k.Winger, k.Scoring, k.FormMultiplier, experienceBonus,
                (_, routed, delta) =>
                {
                    route = routed;
                    direct = delta.ToDictionary(x => x.Key, x => x.Value);
                });

            var skillK = new EffectiveSkillsFixed(
                SkillRating(p.Keeper) + loyalty,
                SkillRating(p.Defending) + loyalty,
                SkillRating(p.Playmaking) + loyalty,
                SkillRating(p.Passing) + loyalty,
                SkillRating(p.Winger) + loyalty,
                SkillRating(p.Scoring) + loyalty,
                formMultiplier);

            RatingPositionMatrix.AddContribution(
                skillOnly, p, skillK.Keeper, skillK.Defending, skillK.Playmaking,
                skillK.Passing, skillK.Winger, skillK.Scoring, skillK.FormMultiplier, 0.0);

            var experienceContributions = direct.ToDictionary(
                x => x.Key,
                x => x.Value - skillOnly[x.Key]);

            var slot = RatingPositionMatrix.CanonicalSlot(p);
            var crowding = crowdingState.ForSlot(slot);

            // Only skill contribution is crowded. Experience is a post-crowding
            // flat contribution and therefore remains fully intact.
            var crowdingAdjusted = skillOnly.ToDictionary(
                x => x.Key,
                x => x.Value * crowding + experienceContributions[x.Key]);

            foreach (var contribution in crowdingAdjusted)
                sectors[contribution.Key] += contribution.Value;

            var name = playerNames is not null && playerNames.TryGetValue(p.Id, out var playerName)
                ? playerName
                : string.Empty;

            playerTraces.Add(new PlayerRatingCalculationTrace(
                p.Id,
                name,
                slot,
                p.Side.ToString(),
                p.Order.ToString(),
                route,
                RatingCalculationFormulaCatalog.FormulaFor(slot, p.Order, p.Side),
                new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["keeper"] = p.Keeper,
                    ["defending"] = p.Defending,
                    ["playmaking"] = p.Playmaking,
                    ["passing"] = p.Passing,
                    ["winger"] = p.Winger,
                    ["scoring"] = p.Scoring
                },
                new Dictionary<string, double>(StringComparer.Ordinal)
                {
                    ["keeper"] = k.Keeper,
                    ["defending"] = k.Defending,
                    ["playmaking"] = k.Playmaking,
                    ["passing"] = k.Passing,
                    ["winger"] = k.Winger,
                    ["scoring"] = k.Scoring
                },
                p.Form,
                k.FormMultiplier,
                staminaMultiplier,
                loyalty,
                experienceBonus,
                crowding,
                direct
                    .Where(x => Math.Abs(x.Value) > 1e-12)
                    .ToDictionary(x => x.Key.ToString(), x => x.Value, StringComparer.Ordinal),
                skillOnly
                    .Where(x => Math.Abs(x.Value) > 1e-12)
                    .ToDictionary(x => x.Key.ToString(), x => x.Value, StringComparer.Ordinal),
                experienceContributions
                    .Where(x => Math.Abs(x.Value) > 1e-12)
                    .ToDictionary(x => x.Key.ToString(), x => x.Value, StringComparer.Ordinal),
                crowdingAdjusted
                    .Where(x => Math.Abs(x.Value) > 1e-12)
                    .ToDictionary(x => x.Key.ToString(), x => x.Value, StringComparer.Ordinal)));
        }

        var matrixSubtotal = new Dictionary<RatingSector, double>(sectors);
        ApplyContext(sectors, context);
        var contextAdjusted = new Dictionary<RatingSector, double>(sectors);

        var contextMultipliers = Enum.GetValues<RatingSector>()
            .ToDictionary(
                sector => sector,
                sector => Math.Abs(matrixSubtotal[sector]) < 1e-12
                    ? 1.0
                    : contextAdjusted[sector] / matrixSubtotal[sector]);

        ApplyReferenceCalibration(sectors);
        var sectorTraces = Enum.GetValues<RatingSector>()
            .Select(sector =>
            {
                var key = sector.ToString();
                var playerContributions = playerTraces
                    .Where(x => x.CrowdingAdjustedContributions.ContainsKey(key))
                    .ToDictionary(
                        x => $"{x.PlayerId} • {x.PlayerName}".TrimEnd(' ', '•'),
                        x => x.CrowdingAdjustedContributions[key],
                        StringComparer.Ordinal);

                var reference = ReferenceCalibrationFor(sector);
                return new RatingSectorCalculationTrace(
                    key,
                    matrixSubtotal[sector],
                    contextMultipliers[sector],
                    reference,
                    sectors[sector],
                    RegionalRatingEngine.Display(sectors[sector]),
                    playerContributions);
            })
            .ToList();

        var rating = ToSnapshot(sectors);
        var trace = new RatingCalculationTraceResult(
            "V5",
            teamName ?? string.Empty,
            formation ?? string.Empty,
            context.MatchLocation.ToString(),
            context.Attitude.ToString(),
            context.Tactic.ToString(),
            context.MatchMinute,
            context.GoalDifference,
            BaselineFormFactor,
            crowdingState.CentralDefenders,
            crowdingState.InnerMidfielders,
            crowdingState.Forwards,
            4,
            1.0,
            rating,
            rating,
            playerTraces,
            sectorTraces);

        return new CoreResult(rating, trace);
    }

    public RatingCalculationTraceResult CalculateLineupWithTrace(
        Lineup lineup,
        IReadOnlyList<Player> players,
        RatingContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(players);

        var byId = players
            .Where(p => p is not null)
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First());

        var sourceSlots = lineup.Slots ?? Array.Empty<Slot>();
        var mapped = sourceSlots
            .Where(s => s is not null && s.PlayerId > 0 && !string.IsNullOrWhiteSpace(s.Code) && byId.ContainsKey(s.PlayerId))
            .Select(s => ToRegionalPlayer(lineup.Formation ?? string.Empty, s, byId[s.PlayerId]))
            .ToList();

        if (mapped.Count == 0)
            throw new InvalidOperationException("V5 rating trace için geçerli oyuncu/pozisyon eşleşmesi bulunamadı.");

        var names = players
            .Where(p => p is not null)
            .GroupBy(p => p.Id)
            .ToDictionary(g => g.Key, g => g.First().Name ?? string.Empty);

        return CalculateCore(mapped, context, lineup.TeamName ?? string.Empty, lineup.Formation ?? string.Empty, names).Trace!;
    }

    private static double ReferenceCalibrationFor(RatingSector sector)
        => sector switch
        {
            RatingSector.Midfield => ReferenceMidfieldCalibration,
            RatingSector.LeftAttack => ReferenceLeftAttackCalibration,
            RatingSector.RightAttack => ReferenceRightAttackCalibration,
            _ => 1.0
        };

    public RegionalRatingSnapshot CalculateLineup(Lineup lineup, IReadOnlyList<Player> players, RatingContext? context = null)
    {
        var byId = players.ToDictionary(p => p.Id);
        var mapped = lineup.Slots
            .Where(s => s.PlayerId > 0 && byId.ContainsKey(s.PlayerId))
            .Select(s => ToRegionalPlayer(lineup.Formation, s, byId[s.PlayerId]))
            .ToList();

        return Calculate(mapped, context);
    }

    public RegionalRatingPair CalculatePair(
        Lineup ownLineup, IReadOnlyList<Player> ownPlayers,
        Lineup opponentLineup, IReadOnlyList<Player> opponentPlayers,
        RatingContext? ownContext = null, RatingContext? opponentContext = null)
        => new(
            CalculateLineup(ownLineup, ownPlayers, ownContext),
            CalculateLineup(opponentLineup, opponentPlayers, opponentContext));

    internal static double SkillRating(double skill) => Math.Max(0.0, skill - 1.0);

    internal static double StaminaMatchMultiplier(double stamina, int minute)
    {
        if (minute <= 0) return 1.0;

        var m = Math.Clamp(minute, 0, 120);
        var factor45 = StaminaAtPoint(stamina, 45);
        var factor90 = StaminaAtPoint(stamina, 90);
        var factor120 = StaminaAtPoint(stamina, 120);

        if (m <= 45) return Lerp(1.0, factor45, m / 45.0);
        if (m <= 90) return Lerp(factor45, factor90, (m - 45) / 45.0);
        return Lerp(factor90, factor120, (m - 90) / 30.0);
    }

    private static double StaminaAtPoint(double stamina, int minute)
    {
        var levels = new[] { 1.7, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.4 };
        var at45 = new[] { .5886, .608, .640, .671, .703, .735, .767, .799, .831, .863, .894, .926, .958, .990, 1.000, 1.000, 1.000 };
        var at90 = new[] { .265, .294, .344, .393, .442, .491, .541, .590, .639, .688, .737, .787, .836, .885, .956, 1.000, 1.000 };
        var at120 = new[] { .100, .100, .100, .100, .156, .218, .281, .344, .406, .469, .532, .595, .657, .720, .791, .863, .920 };
        var table = minute <= 45 ? at45 : minute <= 90 ? at90 : at120;
        var x = Math.Clamp(stamina, levels[0], levels[^1]);

        for (var i = 1; i < levels.Length; i++)
        {
            if (x <= levels[i])
            {
                var t = (x - levels[i - 1]) / (levels[i] - levels[i - 1]);
                return Lerp(table[i - 1], table[i], t);
            }
        }

        return table[^1];
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static Dictionary<RatingSector, double> Empty()
        => Enum.GetValues<RatingSector>().ToDictionary(x => x, _ => 0d);

    private static void ApplyContext(Dictionary<RatingSector, double> s, RatingContext c)
    {
        // Match location must not alter midfield rating.
        // Regional rating is driven by player contributions; no Home/Derby midfield bonus.
        var midfield = 1.0;

        midfield *= c.Attitude switch
        {
            TeamAttitude.MatchOfTheSeason => 1.1149,
            TeamAttitude.PlayItCool => .83945,
            _ => 1.0
        };

        if (c.Tactic == TeamTactic.CounterAttack)
            midfield *= .93;

        switch (c.Tactic)
        {
            case TeamTactic.AttackMiddle:
                s[RatingSector.LeftDefence] *= .85;
                s[RatingSector.RightDefence] *= .85;
                break;
            case TeamTactic.AttackWings:
                s[RatingSector.CentralDefence] *= .85;
                break;
            case TeamTactic.Creative:
                s[RatingSector.LeftDefence] *= .93;
                s[RatingSector.CentralDefence] *= .93;
                s[RatingSector.RightDefence] *= .93;
                break;
            case TeamTactic.LongShots:
                s[RatingSector.LeftAttack] *= .96;
                s[RatingSector.CentralAttack] *= .96;
                s[RatingSector.RightAttack] *= .96;
                break;
        }

        if (c.MatchMinute > 0)
            s[RatingSector.Midfield] *= 1.0 - .10 * Math.Clamp(c.MatchMinute / 90.0, 0, 1);

        s[RatingSector.Midfield] *= midfield;

        if (c.GoalDifference >= 2 && !c.IgnoreLeadRetreat)
        {
            var steps = Math.Min(c.GoalDifference - 1, 7);
            var protection = 1.0 + steps * .075;
            var attack = 1.0 - steps * .09;

            s[RatingSector.LeftDefence] *= protection;
            s[RatingSector.CentralDefence] *= protection;
            s[RatingSector.RightDefence] *= protection;
            s[RatingSector.LeftAttack] *= attack;
            s[RatingSector.CentralAttack] *= attack;
            s[RatingSector.RightAttack] *= attack;
        }
    }

    private static void ApplyReferenceCalibration(Dictionary<RatingSector, double> s)
    {
        s[RatingSector.Midfield] *= ReferenceMidfieldCalibration;
        s[RatingSector.LeftAttack] *= ReferenceLeftAttackCalibration;
        s[RatingSector.RightAttack] *= ReferenceRightAttackCalibration;
    }

    private static double LoyaltyEffect(double loyalty)
        => loyalty >= 20 ? 1.5 : Math.Clamp(loyalty / 19.0, 0.0, 1.0);

    private static double ExperienceBonus(double experience)
    {
        var values = new[]
        {
            0.00, 0.00, .40, .64, .80, .93, 1.04, 1.13, 1.20, 1.27, 1.33,
            1.39, 1.44, 1.49, 1.53, 1.57, 1.61, 1.64, 1.67, 1.71, 1.73
        };

        return values[Math.Clamp((int)Math.Round(experience), 1, 20)];
    }

    private static double FormFactor(double form)
        => .378 * Math.Sqrt(Math.Clamp(form - 1.0, 0.0, 7.0));

    private static RegionalRatingSnapshot ToSnapshot(Dictionary<RatingSector, double> s)
        => new(
            s[RatingSector.LeftDefence],
            s[RatingSector.CentralDefence],
            s[RatingSector.RightDefence],
            s[RatingSector.Midfield],
            s[RatingSector.LeftAttack],
            s[RatingSector.CentralAttack],
            s[RatingSector.RightAttack],
            s[RatingSector.LeftDefence],
            s[RatingSector.CentralDefence],
            s[RatingSector.RightDefence],
            s[RatingSector.Midfield],
            s[RatingSector.LeftAttack],
            s[RatingSector.CentralAttack],
            s[RatingSector.RightAttack]);

    private static RegionalPlayer ToRegionalPlayer(string formation, Slot slot, Player p)
    {
        var position = RatingPositionResolver.Resolve(formation, slot.Code);
        var side = slot.Code.EndsWith("-L", StringComparison.Ordinal)
            ? PlayerSide.Left
            : slot.Code.EndsWith("-R", StringComparison.Ordinal)
                ? PlayerSide.Right
                : PlayerSide.Center;

        return new RegionalPlayer(
            p.Id, position, side, slot.Order,
            p.Keeper, p.Defending, p.Playmaking, p.Passing, p.Winger, p.Scoring,
            p.Form, p.Loyalty, p.Experience, p.Stamina, slot.Code);
    }

    private readonly record struct EffectiveSkillsFixed(
        double Keeper, double Defending, double Playmaking, double Passing,
        double Winger, double Scoring, double FormMultiplier);
}
