// =============================================================================
// HatForRatingEngine.cs
// Excel formation-specific regional rating engine (sole production engine)
// Coefficient tables: HatForCoefficientTables.cs
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

public sealed class HatForRatingEngine : IRatingEngine
{
    public RatingEngineKind Kind => RatingEngineKind.HatFor;
    public string Name => "HatFor (Excel)";

    public RatingEngineResult Calculate(RatingEngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var playersById = request.Players.ToDictionary(p => p.Id);
        var formation = NormalizeFormation(request.Lineup);
        var coeffs = HatForCoefficientTables.ForFormation(formation);
        var coachLevel = request.HOContext?.CoachModifier ?? 0;

        var slots = MapLineupSlots(request.Lineup, playersById);

        double mid = Sector(slots, coeffs.Mid, coachLevel, defSector: false);
        double cd  = Sector(slots, coeffs.Cd,  coachLevel, defSector: true);
        double ld  = Sector(slots, coeffs.Ld,  coachLevel, defSector: true);
        double rd  = Sector(slots, coeffs.Rd,  coachLevel, defSector: true);
        double ca  = Sector(slots, coeffs.Ca,  coachLevel, defSector: false);
        double la  = Sector(slots, coeffs.La,  coachLevel, defSector: false);
        double ra  = Sector(slots, coeffs.Ra,  coachLevel, defSector: false);

        var snap = new RegionalRatingSnapshot(
            ld, cd, rd, mid, la, ca, ra,
            ld, cd, rd, mid, la, ca, ra);

        return new RatingEngineResult(Kind, snap);
    }

    private static double FormF(double form)
        => 0.378 * Math.Sqrt(Math.Min(7.0, Math.Max(0.0, form - 1.0)));

    private static double Etkin(double skill, double loyalty)
        => Math.Max(0.0, skill - 1.0) + loyalty;

    private static double ExpKat(double exp)
    {
        var x = Math.Max(0.0, exp - 1.0);
        return (-0.00000725 * Math.Pow(x, 4)
                + 0.0005 * Math.Pow(x, 3)
                - 0.01336 * Math.Pow(x, 2)
                + 0.176 * x) * 0.345;
    }

    private static double CoachMod(int coachModifier, bool defSector)
    {
        double adj = coachModifier <= 0
            ? coachModifier * (defSector ? 0.13 : 0.08) / 10.0
            : coachModifier * 0.12 / 10.0;
        return 1.02 - adj;
    }

    private static double ToRating(double ham, double scale, double coachMod)
    {
        if (ham <= 0) return 0;
        var inner = Math.Pow(ham * coachMod * scale, 1.2) / 4.0 + 1.0;
        return Math.Round(inner * 4.0, MidpointRounding.AwayFromZero) / 4.0;
    }

    private static double Sector(
        IReadOnlyDictionary<string, PlayerSkills> slots,
        SectorCoeffs sc,
        int coachLevel,
        bool defSector)
    {
        double ham = 0;
        foreach (var (slot, weights) in sc.Weights)
        {
            if (!slots.TryGetValue(slot, out var p) || p is null) continue;
            var ff = FormF(p.Form);
            var ek = ExpKat(p.Experience);
            double contrib = ek;
            if (weights.TryGetValue("GK", out var wGk)) contrib += Etkin(p.Keeper, p.Loyalty) * ff * wGk;
            if (weights.TryGetValue("DEF", out var wDef)) contrib += Etkin(p.Defending, p.Loyalty) * ff * wDef;
            if (weights.TryGetValue("PM", out var wPm)) contrib += Etkin(p.Playmaking, p.Loyalty) * ff * wPm;
            if (weights.TryGetValue("PAS", out var wPas)) contrib += Etkin(p.Passing, p.Loyalty) * ff * wPas;
            if (weights.TryGetValue("WI", out var wWi)) contrib += Etkin(p.Winger, p.Loyalty) * ff * wWi;
            if (weights.TryGetValue("SC", out var wSc)) contrib += Etkin(p.Scoring, p.Loyalty) * ff * wSc;
            ham += contrib;
        }
        return ToRating(ham, sc.Scale, CoachMod(coachLevel, defSector));
    }

    private static IReadOnlyDictionary<string, PlayerSkills> MapLineupSlots(
        Lineup lineup, IReadOnlyDictionary<int, Player> byId)
    {
        var map = new Dictionary<string, PlayerSkills>(StringComparer.OrdinalIgnoreCase);
        foreach (var slot in lineup.Slots)
        {
            if (slot.PlayerId <= 0 || !byId.TryGetValue(slot.PlayerId, out var pl))
                continue;
            var key = NormalizeSlotCode(slot.Code);
            if (key is null) continue;
            map[key] = PlayerSkills.From(pl);
        }
        return map;
    }

    private static string? NormalizeSlotCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        code = code.Trim().ToUpperInvariant();
        return code switch
        {
            "GK" => "GK",
            "DEF-L" or "WB-L" or "WBL" => "WB-L",
            "DEF-R" or "WB-R" or "WBR" => "WB-R",
            "DEF-CL" or "LCD" => "DEF-CL",
            "DEF-CR" or "RCD" => "DEF-CR",
            "DEF-C" or "CD" => "DEF-C",
            "W-L" or "WL" or "LW" => "W-L",
            "W-R" or "WR" or "RW" => "W-R",
            "IM-L" or "LIM" => "IM-L",
            "IM-R" or "RIM" => "IM-R",
            "IM-C" or "IM" or "CIM" => "IM-C",
            "FW-L" or "LF" => "FW-L",
            "FW-R" or "RF" => "FW-R",
            "FW-C" or "FC" or "FW" => "FW-C",
            _ => code
        };
    }

    private static string NormalizeFormation(Lineup lineup)
    {
        if (!string.IsNullOrWhiteSpace(lineup.Formation))
            return lineup.Formation.Trim();
        return "4-4-2";
    }

    private sealed record PlayerSkills(
        double Keeper, double Defending, double Playmaking, double Passing,
        double Winger, double Scoring, double Form, double Experience, double Loyalty)
    {
        public static PlayerSkills From(Player p) => new(
            p.Keeper, p.Defending, p.Playmaking, p.Passing,
            p.Winger, p.Scoring, p.Form, p.Experience, p.Loyalty);
    }
}
