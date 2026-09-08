using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Foxtrick-compatible player-position contribution model.
///
/// This is a direct C# implementation of Foxtrick's PlayerPositionsEvaluations
/// calculation: the published 20-position coefficient map, effective skill
/// calculation, weighted coefficients and optional normalization.
///
/// V5 deliberately uses the same effective-skill options visible in the
/// Bertalan reference screenshot: form + stamina enabled; experience and
/// loyalty disabled. Those options can be changed explicitly later without
/// changing the position coefficient table.
/// </summary>
public sealed class FoxtrickPositionContributionEngine
{
    public const double DefaultCtrVsWinger = 35.0 / 25.0;
    public const double DefaultWingBackDefenceVsCentralDefender = 1.7;
    public const double DefaultWingerOffenceVsForward = 1.3;
    public const double DefaultMidfieldVsAttack = 3.0;
    public const double DefaultDefenceVsAttack = 1.1;
    public const double DefaultInnerMidfielderVsCentralDefender = 0.6;

    public IReadOnlyDictionary<string, double> Calculate(Player player)
        => Calculate(player, FoxtrickContributionOptions.Default);

    public IReadOnlyDictionary<string, double> Calculate(Player player, FoxtrickContributionOptions options)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(options);

        if (player.Id <= 0 || player.InjuryLevel == 999)
            return new Dictionary<string, double>();

        var skills = EffectiveSkills(player, options);
        var result = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var position in Factors.Keys)
        {
            var score = 0.0;
            var coefficientSum = 0.0;

            foreach (var skill in SkillNames)
            {
                if (!Factors[position].TryGetValue(skill, out var definition))
                    continue;

                var coefficient = GetCoefficient(definition, skill, options);
                coefficientSum += coefficient;
                score += coefficient * skills[skill];
            }

            var value = options.Normalise && coefficientSum > 0
                ? score / coefficientSum
                : score;

            result[position] = Math.Round(value, 2, MidpointRounding.ToEven);
        }

        // Foxtrick only allows Technical forwards to use the TDF position.
        // Non-Technical players get zero for TDF; Technical players get zero
        // for the ordinary defensive-forward position.
        if (player.Specialty == PlayerSpecialty.Technical)
            result["fwd"] = 0.0;
        else
            result["tdf"] = 0.0;

        return result;
    }

    public FoxtrickPositionResult Evaluate(Player player)
        => Evaluate(player, FoxtrickContributionOptions.Default);

    public FoxtrickPositionResult Evaluate(Player player, FoxtrickContributionOptions options)
    {
        var contributions = Calculate(player, options);
        if (contributions.Count == 0)
            return new FoxtrickPositionResult(player.Id, player.Name, contributions, null, 0.0);

        var best = contributions
            .OrderByDescending(x => x.Value)
            .ThenBy(x => PositionOrder(x.Key))
            .First();

        return new FoxtrickPositionResult(player.Id, player.Name, contributions, best.Key, best.Value);
    }

    public double BestForFamily(Player player, string basePositionCode)
    {
        var contributions = Calculate(player);
        return basePositionCode switch
        {
            "GK" => Get(contributions, "kp"),
            "DEF-L" or "DEF-R" or "DEF-CL" or "DEF-C" or "DEF-CR" => Max(contributions, "cd", "cdo", "cdtw"),
            "W-L" or "W-R" => Max(contributions, "w", "wd", "wo", "wtm"),
            "IM-L" or "IM-C" or "IM-R" => Max(contributions, "im", "imd", "imo", "imtw"),
            "FW-L" or "FW-C" or "FW-R" => Max(contributions, "fw", "fwd", "tdf", "fwtw"),
            _ => 0.0
        };
    }

    public string? BestBasePosition(Player player)
    {
        var result = Evaluate(player);
        if (result.BestPositionCode is null) return null;
        return result.BestPositionCode switch
        {
            "kp" => "GK",
            "cd" or "cdo" or "cdtw" => "DEF-C",
            "wb" or "wbd" or "wbo" or "wbtm" => "DEF-L",
            "w" or "wd" or "wo" or "wtm" => "W-L",
            "im" or "imd" or "imo" or "imtw" => "IM-C",
            "fw" or "fwd" or "tdf" or "fwtw" => "FW-C",
            _ => null
        };
    }

    private static double Get(IReadOnlyDictionary<string, double> values, string key)
        => values.TryGetValue(key, out var value) ? value : 0.0;

    private static double Max(IReadOnlyDictionary<string, double> values, params string[] keys)
        => keys.Select(k => Get(values, k)).DefaultIfEmpty(0).Max();

    private static Dictionary<SkillName, double> EffectiveSkills(Player player, FoxtrickContributionOptions options)
    {
        var values = new Dictionary<SkillName, double>
        {
            [SkillName.Keeper] = player.Keeper,
            [SkillName.Defending] = player.Defending,
            [SkillName.Playmaking] = player.Playmaking,
            [SkillName.Winger] = player.Winger,
            [SkillName.Passing] = player.Passing,
            [SkillName.Scoring] = player.Scoring
        };

        if (options.Experience)
        {
            var bonus = Math.Log10(Math.Max(player.Experience, 0)) * 4.0 / 3.0;
            if (double.IsFinite(bonus))
                foreach (var skill in SkillNames) values[skill] += bonus;
        }

        if (options.Loyalty)
        {
            var bonus = Math.Max(0.0, player.Loyalty - 1) / 19.0;
            foreach (var skill in SkillNames) values[skill] += bonus;
        }

        if (options.Stamina)
        {
            var energy = AverageEnergy90(player.Stamina);
            foreach (var skill in SkillNames) values[skill] *= energy;
        }

        if (options.Form)
        {
            var form = Math.Clamp(player.Form, 0, 8);
            var formInflation = new[] { 0.0, 0.305, 0.5, 0.629, 0.732, 0.82, 0.897, 0.967, 1.0 }[form];
            foreach (var skill in SkillNames) values[skill] *= formInflation;
        }

        return values;
    }

    private static double AverageEnergy90(double stamina)
    {
        if (stamina >= 8.63) return 1.0;

        var total = 0.0;
        var initial = 1.0 + (0.0292 * stamina + 0.05);
        if (stamina > 8) initial += 0.15 * (stamina - 8);
        var decay = Math.Max(0.0325, -0.0039 * stamina + 0.0634);

        for (var checkpoint = 1; checkpoint <= 18; checkpoint++)
        {
            var energy = initial - checkpoint * decay;
            if (checkpoint > 9) energy += 0.1875;
            total += Math.Min(1.0, energy);
        }

        return total / 18.0;
    }

    private static double GetCoefficient(ContributionDefinition definition, SkillName skill, FoxtrickContributionOptions options)
    {
        if (definition.IsScalar)
            return definition.Center * options.InnerMidfielderVsCentralDefender * options.MidfieldVsAttack;

        var sideMultiplier = IsDefensiveSkill(skill)
            ? options.WingBackDefenceVsCentralDefender
            : options.WingerOffenceVsForward;
        var side = definition.Side * sideMultiplier;
        var farSide = definition.FarSide * sideMultiplier;
        var wings = side + farSide;
        var factor = (definition.Center + wings / options.CtrVsWinger)
            / (1.0 + 2.0 / options.CtrVsWinger);

        if (IsDefensiveSkill(skill))
            factor *= options.DefenceVsAttack;

        return factor;
    }

    private static bool IsDefensiveSkill(SkillName skill)
        => skill is SkillName.Keeper or SkillName.Defending;

    private static int PositionOrder(string position) => position switch
    {
        "kp" => 0,
        "cd" => 10, "cdo" => 11, "cdtw" => 12,
        "wb" => 20, "wbd" => 21, "wbo" => 22, "wbtm" => 23,
        "w" => 30, "wd" => 31, "wo" => 32, "wtm" => 33,
        "im" => 40, "imd" => 41, "imo" => 42, "imtw" => 43,
        "fw" => 50, "fwd" => 51, "tdf" => 52, "fwtw" => 53,
        _ => 99
    };

    private static readonly SkillName[] SkillNames =
    [
        SkillName.Keeper,
        SkillName.Defending,
        SkillName.Playmaking,
        SkillName.Winger,
        SkillName.Passing,
        SkillName.Scoring
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<SkillName, ContributionDefinition>> Factors =
        new Dictionary<string, IReadOnlyDictionary<SkillName, ContributionDefinition>>(StringComparer.Ordinal)
        {
            ["kp"] = D((SkillName.Keeper, C(.87,.61,.61)), (SkillName.Defending, C(.35,.25,.25))),
            ["cd"] = D((SkillName.Defending, C(1,.26,.26)), (SkillName.Playmaking, S(.25))),
            ["cdo"] = D((SkillName.Defending, C(.73,.20,.20)), (SkillName.Playmaking, S(.40))),
            ["cdtw"] = D((SkillName.Defending, C(.67,.81)), (SkillName.Playmaking, S(.15)), (SkillName.Winger, C(0,.26))),
            ["wb"] = D((SkillName.Defending, C(.38,.92)), (SkillName.Playmaking, S(.15)), (SkillName.Winger, C(0,.59))),
            ["wbd"] = D((SkillName.Defending, C(.43,1)), (SkillName.Playmaking, S(.10)), (SkillName.Winger, C(0,.45))),
            ["wbo"] = D((SkillName.Defending, C(.35,.74)), (SkillName.Playmaking, S(.20)), (SkillName.Winger, C(0,.69))),
            ["wbtm"] = D((SkillName.Defending, C(.70,.75)), (SkillName.Playmaking, S(.20)), (SkillName.Winger, C(0,.35))),
            ["w"] = D((SkillName.Defending, C(.20,.35)), (SkillName.Playmaking, S(.45)), (SkillName.Passing, C(.11,.26)), (SkillName.Winger, C(0,.86))),
            ["wd"] = D((SkillName.Defending, C(.25,.61)), (SkillName.Playmaking, S(.30)), (SkillName.Passing, C(.05,.21)), (SkillName.Winger, C(0,.69))),
            ["wo"] = D((SkillName.Defending, C(.13,.22)), (SkillName.Playmaking, S(.30)), (SkillName.Passing, C(.13,.29)), (SkillName.Winger, C(0,1))),
            ["wtm"] = D((SkillName.Defending, C(.25,.29)), (SkillName.Playmaking, S(.55)), (SkillName.Passing, C(.16,.15)), (SkillName.Winger, C(0,.74))),
            ["im"] = D((SkillName.Defending, C(.40,.09,.09)), (SkillName.Playmaking, S(1)), (SkillName.Passing, C(.33,.13,.13)), (SkillName.Scoring, C(.22,0))),
            ["imd"] = D((SkillName.Defending, C(.58,.14,.14)), (SkillName.Playmaking, S(.95)), (SkillName.Passing, C(.18,.07,.07)), (SkillName.Scoring, C(.13,0))),
            ["imo"] = D((SkillName.Defending, C(.16,.04,.04)), (SkillName.Playmaking, S(.95)), (SkillName.Passing, C(.49,.18,.18)), (SkillName.Scoring, C(.31,0))),
            ["imtw"] = D((SkillName.Defending, C(.33,.24)), (SkillName.Playmaking, S(.90)), (SkillName.Passing, C(.23,.31)), (SkillName.Winger, C(0,.59))),
            ["fw"] = D((SkillName.Playmaking, S(.25)), (SkillName.Passing, C(.33,.14,.14)), (SkillName.Scoring, C(1,.27,.27)), (SkillName.Winger, C(0,.24,.24))),
            ["fwd"] = D((SkillName.Playmaking, S(.35)), (SkillName.Passing, C(.53,.31,.31)), (SkillName.Scoring, C(.56,.13,.13)), (SkillName.Winger, C(0,.13,.13))),
            ["tdf"] = D((SkillName.Playmaking, S(.35)), (SkillName.Passing, C(.53,.41,.41)), (SkillName.Scoring, C(.56,.13,.13)), (SkillName.Winger, C(0,.13,.13))),
            ["fwtw"] = D((SkillName.Playmaking, S(.15)), (SkillName.Passing, C(.23,.21,.06)), (SkillName.Scoring, C(.66,.51,.19)), (SkillName.Winger, C(0,.64,.21)))
        };

    private static IReadOnlyDictionary<SkillName, ContributionDefinition> D(params (SkillName Skill, ContributionDefinition Definition)[] items)
        => items.ToDictionary(x => x.Skill, x => x.Definition);

    private static ContributionDefinition C(double center, double side, double farSide = 0.0)
        => new(center, side, farSide, false);

    private static ContributionDefinition S(double value)
        => new(value, 0, 0, true);

    private enum SkillName { Keeper, Defending, Playmaking, Winger, Passing, Scoring }

    private readonly record struct ContributionDefinition(double Center, double Side, double FarSide, bool IsScalar);
}

public sealed record FoxtrickContributionOptions(
    bool Form,
    bool Stamina,
    bool Experience,
    bool Loyalty,
    bool Bruised,
    bool Normalise,
    double CtrVsWinger,
    double WingBackDefenceVsCentralDefender,
    double WingerOffenceVsForward,
    double MidfieldVsAttack,
    double DefenceVsAttack,
    double InnerMidfielderVsCentralDefender)
{
    public static FoxtrickContributionOptions Default { get; } = new(
        Form: true,
        Stamina: true,
        Experience: false,
        Loyalty: false,
        Bruised: false,
        Normalise: true,
        CtrVsWinger: FoxtrickPositionContributionEngine.DefaultCtrVsWinger,
        WingBackDefenceVsCentralDefender: FoxtrickPositionContributionEngine.DefaultWingBackDefenceVsCentralDefender,
        WingerOffenceVsForward: FoxtrickPositionContributionEngine.DefaultWingerOffenceVsForward,
        MidfieldVsAttack: FoxtrickPositionContributionEngine.DefaultMidfieldVsAttack,
        DefenceVsAttack: FoxtrickPositionContributionEngine.DefaultDefenceVsAttack,
        InnerMidfielderVsCentralDefender: FoxtrickPositionContributionEngine.DefaultInnerMidfielderVsCentralDefender);
}

public sealed record FoxtrickPositionResult(
    int PlayerId,
    string PlayerName,
    IReadOnlyDictionary<string, double> Contributions,
    string? BestPositionCode,
    double BestPositionValue);
