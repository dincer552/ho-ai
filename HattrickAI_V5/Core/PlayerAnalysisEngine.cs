using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

/// <summary>
/// Motor 3: Oyuncu Analiz Motoru.
/// Oyuncunun her yasal pozisyon ailesindeki Foxtrick-normalized katkısını üretir.
/// XI seçmez, diziliş seçmez, rakip skoru kullanmaz ve takım ratingi üretmez.
///
/// The previous hand-written skill-weight formula has been removed. Player
/// suitability now comes from the Foxtrick 20-position contribution model.
/// </summary>
public sealed class PlayerAnalysisEngine : IPlayerAnalysisEngine
{
    private static readonly string[] PositionCodes =
    [
        "GK",
        "DEF-L", "DEF-CL", "DEF-C", "DEF-CR", "DEF-R",
        "W-L", "IM-L", "IM-C", "IM-R", "W-R",
        "FW-L", "FW-C", "FW-R"
    ];

    private readonly FoxtrickPositionContributionEngine _foxtrick = new();

    public PlayerAnalysisResult Analyze(IReadOnlyList<Player> players)
    {
        ArgumentNullException.ThrowIfNull(players);
        return new PlayerAnalysisResult(players.Select(AnalyzePlayer).ToList());
    }

    public PlayerAnalysisProfile AnalyzePlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var eligible = IsEligible(player);
        var foxtrick = eligible ? _foxtrick.Evaluate(player) :
            new FoxtrickPositionResult(player.Id, player.Name, new Dictionary<string, double>(), null, 0.0);

        var candidates = PositionCodes
            .Select(code => new PlayerPositionCandidate(
                code,
                eligible ? _foxtrick.BestForFamily(player, code) : double.NegativeInfinity))
            .Where(x => !double.IsNegativeInfinity(x.Score))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => PositionOrder(x.PositionCode))
            .ToList();

        var mappedFoxtrickPositions = foxtrick.Contributions
            .OrderByDescending(x => x.Value)
            .ThenBy(x => FoxtrickPositionOrder(x.Key))
            .ToList();

        var baseOrder = mappedFoxtrickPositions
            .Select(x => MapFoxtrickToBasePosition(x.Key))
            .Where(x => x is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var primary = baseOrder.FirstOrDefault() ?? candidates.FirstOrDefault()?.PositionCode;
        var secondary = baseOrder.Skip(1).FirstOrDefault() ?? candidates.Skip(1).FirstOrDefault()?.PositionCode;

        return new PlayerAnalysisProfile(
            player.Id,
            player.Name,
            eligible,
            player.InjuryLevel,
            player.Specialty,
            BuildSpecialtyProfile(player.Specialty),
            candidates,
            primary,
            secondary)
        {
            FoxtrickPositions = foxtrick.Contributions,
            FoxtrickBestPosition = foxtrick.BestPositionCode,
            FoxtrickBestPositionValue = foxtrick.BestPositionValue
        };
    }

    /// <summary>
    /// Returns the strongest Foxtrick position/order inside the requested
    /// positional family. This lets Motor 5 choose players using their best
    /// usage type without pretending the final individual order has already
    /// been selected.
    /// </summary>
    public double Score(Player player, string positionCode)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!IsEligible(player)) return double.NegativeInfinity;
        return _foxtrick.BestForFamily(player, positionCode);
    }

    private static string? MapFoxtrickToBasePosition(string code) => code switch
    {
        "kp" => "GK",
        "cd" or "cdo" or "cdtw" => "DEF-C",
        "wb" or "wbd" or "wbo" or "wbtm" => "DEF-L",
        "w" or "wd" or "wo" or "wtm" => "W-L",
        "im" or "imd" or "imo" or "imtw" => "IM-C",
        "fw" or "fwd" or "tdf" or "fwtw" => "FW-C",
        _ => null
    };

    private static PlayerSpecialtyProfile BuildSpecialtyProfile(PlayerSpecialty specialty)
        => specialty switch
        {
            PlayerSpecialty.Technical => new(
                specialty,
                HasSpecialEventContext: true,
                HasWeatherInteraction: true,
                HasCounterAttackInteraction: false,
                HasPressingInteraction: false,
                HasQuickEventInteraction: false,
                HasHeaderInteraction: true,
                HasPlayCreativelyInteraction: false,
                Notes: "Technical specialty; weather and Technical-vs-Head interactions are retained for later event resolution."),

            PlayerSpecialty.Quick => new(
                specialty,
                HasSpecialEventContext: true,
                HasWeatherInteraction: false,
                HasCounterAttackInteraction: true,
                HasPressingInteraction: false,
                HasQuickEventInteraction: true,
                HasHeaderInteraction: false,
                HasPlayCreativelyInteraction: false,
                Notes: "Quick specialty; quick events and counter-attack interaction are retained for later tactical/event resolution."),

            PlayerSpecialty.Powerful => new(
                specialty,
                HasSpecialEventContext: true,
                HasWeatherInteraction: true,
                HasCounterAttackInteraction: false,
                HasPressingInteraction: true,
                HasQuickEventInteraction: false,
                HasHeaderInteraction: false,
                HasPlayCreativelyInteraction: false,
                Notes: "Powerful specialty; weather and pressing interactions are retained for later tactical/event resolution."),

            PlayerSpecialty.Unpredictable => new(
                specialty,
                HasSpecialEventContext: true,
                HasWeatherInteraction: false,
                HasCounterAttackInteraction: false,
                HasPressingInteraction: false,
                HasQuickEventInteraction: false,
                HasHeaderInteraction: false,
                HasPlayCreativelyInteraction: true,
                Notes: "Unpredictable specialty; unexpected actions and Play Creatively interaction are retained for later event resolution."),

            PlayerSpecialty.Head => new(
                specialty,
                HasSpecialEventContext: true,
                HasWeatherInteraction: false,
                HasCounterAttackInteraction: false,
                HasPressingInteraction: false,
                HasQuickEventInteraction: false,
                HasHeaderInteraction: true,
                HasPlayCreativelyInteraction: false,
                Notes: "Head specialty; own header and opponent anti-header interaction are retained for later event resolution."),

            _ => new(
                PlayerSpecialty.None,
                HasSpecialEventContext: false,
                HasWeatherInteraction: false,
                HasCounterAttackInteraction: false,
                HasPressingInteraction: false,
                HasQuickEventInteraction: false,
                HasHeaderInteraction: false,
                HasPlayCreativelyInteraction: false,
                Notes: "No specialty; no specialty-specific event context."),
        };

    private static bool IsEligible(Player player)
        => player.Id > 0 && player.InjuryLevel != 999;

    private static int PositionOrder(string code) => code switch
    {
        "GK" => 0,
        "DEF-L" => 10,
        "DEF-CL" => 11,
        "DEF-C" => 12,
        "DEF-CR" => 13,
        "DEF-R" => 14,
        "W-L" => 20,
        "IM-L" => 21,
        "IM-C" => 22,
        "IM-R" => 23,
        "W-R" => 24,
        "FW-L" => 30,
        "FW-C" => 31,
        "FW-R" => 32,
        _ => 99
    };

    private static int FoxtrickPositionOrder(string code) => code switch
    {
        "kp" => 0,
        "cd" => 10, "cdo" => 11, "cdtw" => 12,
        "wb" => 20, "wbd" => 21, "wbo" => 22, "wbtm" => 23,
        "w" => 30, "wd" => 31, "wo" => 32, "wtm" => 33,
        "im" => 40, "imd" => 41, "imo" => 42, "imtw" => 43,
        "fw" => 50, "fwd" => 51, "tdf" => 52, "fwtw" => 53,
        _ => 99
    };
}

public sealed record PlayerPositionCandidate(string PositionCode, double Score);

public sealed record PlayerSpecialtyProfile(
    PlayerSpecialty Specialty,
    bool HasSpecialEventContext,
    bool HasWeatherInteraction,
    bool HasCounterAttackInteraction,
    bool HasPressingInteraction,
    bool HasQuickEventInteraction,
    bool HasHeaderInteraction,
    bool HasPlayCreativelyInteraction,
    string Notes);

public sealed record PlayerAnalysisProfile(
    int PlayerId,
    string PlayerName,
    bool IsEligible,
    int InjuryLevel,
    PlayerSpecialty Specialty,
    PlayerSpecialtyProfile SpecialtyProfile,
    IReadOnlyList<PlayerPositionCandidate> Positions,
    string? PrimaryPosition,
    string? SecondaryPosition)
{
    public double PrimaryScore => Positions.Count == 0 ? double.NegativeInfinity : Positions[0].Score;
    public double SecondaryScore => Positions.Count < 2 ? double.NegativeInfinity : Positions[1].Score;

    public IReadOnlyDictionary<string, double> FoxtrickPositions { get; init; }
        = new Dictionary<string, double>(StringComparer.Ordinal);

    public string? FoxtrickBestPosition { get; init; }
    public double FoxtrickBestPositionValue { get; init; }
}
