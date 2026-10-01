// =============================================================================
// HatForRatingEngine.cs
// Excel (HatFor) reverse-engineered regional rating engine for HattrickAI V5
//
// Does NOT replace V5. Registered as RatingEngineKind.HatFor.
// Formula pipeline (Excel TOPLAM parity):
//   etkin = max(0, skill - 1) + loyalty
//   formF = 0.378 * sqrt(min(7, max(0, form - 1)))
//   expKat = poly(exp-1) * 0.345
//   ham   = sum(etkin * formF * coeff + expKat)
//   rating = ROUND(((ham * coachMod * sectorScale)^1.2 / 4 + 1) * 4, 0) / 4
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

public sealed record SectorCoeffs(
    double Scale,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Weights);

public sealed record FormationCoeffs(
    SectorCoeffs Mid, SectorCoeffs Cd, SectorCoeffs Ld, SectorCoeffs Rd,
    SectorCoeffs Ca, SectorCoeffs La, SectorCoeffs Ra);

public static class HatForCoefficientTables
{
    public static FormationCoeffs ForFormation(string formation)
    {
        formation = formation.Trim().ToUpperInvariant()
            .Replace(" ", "").Replace("(", "").Replace(")", "");
        if (formation is "352" or "3-5-2") return F352();
        if (formation is "442" or "4-4-2") return F442();
        if (formation is "433" or "4-3-3") return F433();
        if (formation is "451" or "4-5-1") return F451();
        if (formation is "541" or "5-4-1") return F541();
        if (formation is "532" or "5-3-2") return F532();
        if (formation is "343" or "3-4-3") return F343();
        if (formation is "3432B" or "3-4-32B" or "343-2B") return F343_2B();
        if (formation is "550" or "5-5-0") return F550();
        if (formation is "253" or "2-5-3") return F253();
        if (formation is "2532B" or "2-5-32B" or "253-2B") return F253_2B();
        return F442();
    }

    private static IReadOnlyDictionary<string, double> W(params (string k, double v)[] pairs)
        => pairs.ToDictionary(x => x.k, x => x.v);

    private static SectorCoeffs S(double scale, params (string slot, IReadOnlyDictionary<string, double> w)[] items)
        => new(scale, items.ToDictionary(x => x.slot, x => x.w));

    private static FormationCoeffs F442() => new(
        Mid: S(0.62,
            ("IM-L", W(("PM", 0.55), ("PAS", 0.15))),
            ("IM-R", W(("PM", 0.55), ("PAS", 0.15))),
            ("W-L", W(("PM", 0.30))),
            ("W-R", W(("PM", 0.30))),
            ("DEF-CL", W(("PM", 0.12))), ("DEF-CR", W(("PM", 0.12))),
            ("WB-L", W(("PM", 0.12))), ("WB-R", W(("PM", 0.12))),
            ("FW-L", W(("PM", 0.10))), ("FW-R", W(("PM", 0.10)))),
        Cd: S(0.60,
            ("GK", W(("GK", 0.70), ("DEF", 0.15))),
            ("DEF-CL", W(("DEF", 0.90))), ("DEF-CR", W(("DEF", 0.90))),
            ("WB-L", W(("DEF", 0.50))), ("WB-R", W(("DEF", 0.50))),
            ("IM-L", W(("DEF", 0.25))), ("IM-R", W(("DEF", 0.25)))),
        Ld: S(0.85,
            ("GK", W(("GK", 0.45), ("DEF", 0.12))),
            ("WB-L", W(("DEF", 1.00))), ("DEF-CL", W(("DEF", 0.70))),
            ("W-L", W(("DEF", 0.25))), ("IM-L", W(("DEF", 0.15)))),
        Rd: S(0.85,
            ("GK", W(("GK", 0.45), ("DEF", 0.12))),
            ("WB-R", W(("DEF", 1.00))), ("DEF-CR", W(("DEF", 0.70))),
            ("W-R", W(("DEF", 0.25))), ("IM-R", W(("DEF", 0.15)))),
        Ca: S(0.60,
            ("FW-L", W(("SC", 0.90), ("PAS", 0.30))),
            ("FW-R", W(("SC", 0.90), ("PAS", 0.30))),
            ("IM-L", W(("PAS", 0.25), ("SC", 0.10))),
            ("IM-R", W(("PAS", 0.25), ("SC", 0.10))),
            ("W-L", W(("PAS", 0.12))), ("W-R", W(("PAS", 0.12)))),
        La: S(0.70,
            ("W-L", W(("WI", 0.80), ("PAS", 0.20))),
            ("FW-L", W(("SC", 0.50), ("PAS", 0.20))),
            ("IM-L", W(("WI", 0.30), ("PAS", 0.15))),
            ("WB-L", W(("WI", 0.20)))),
        Ra: S(0.70,
            ("W-R", W(("WI", 0.80), ("PAS", 0.20))),
            ("FW-R", W(("SC", 0.50), ("PAS", 0.20))),
            ("IM-R", W(("WI", 0.30), ("PAS", 0.15))),
            ("WB-R", W(("WI", 0.20))))
    );

    private static FormationCoeffs F352() => new(
        Mid: S(0.62,
            ("IM-C", W(("PM", 0.60), ("PAS", 0.18))),
            ("IM-L", W(("PM", 0.50), ("PAS", 0.15))),
            ("IM-R", W(("PM", 0.50), ("PAS", 0.15))),
            ("W-L", W(("PM", 0.30))), ("W-R", W(("PM", 0.30))),
            ("DEF-CL", W(("PM", 0.12))), ("DEF-C", W(("PM", 0.12))), ("DEF-CR", W(("PM", 0.12))),
            ("FW-L", W(("PM", 0.10))), ("FW-R", W(("PM", 0.10)))),
        Cd: S(0.60,
            ("GK", W(("GK", 0.70), ("DEF", 0.15))),
            ("DEF-CL", W(("DEF", 0.85))), ("DEF-C", W(("DEF", 0.90))), ("DEF-CR", W(("DEF", 0.85))),
            ("IM-C", W(("DEF", 0.30))), ("IM-L", W(("DEF", 0.25))), ("IM-R", W(("DEF", 0.25)))),
        Ld: S(0.85,
            ("GK", W(("GK", 0.40), ("DEF", 0.12))),
            ("DEF-CL", W(("DEF", 0.80))), ("DEF-C", W(("DEF", 0.40))),
            ("W-L", W(("DEF", 0.35))), ("IM-L", W(("DEF", 0.20)))),
        Rd: S(0.85,
            ("GK", W(("GK", 0.40), ("DEF", 0.12))),
            ("DEF-CR", W(("DEF", 0.80))), ("DEF-C", W(("DEF", 0.40))),
            ("W-R", W(("DEF", 0.35))), ("IM-R", W(("DEF", 0.20)))),
        Ca: S(0.60,
            ("FW-L", W(("SC", 0.95), ("PAS", 0.35))),
            ("FW-R", W(("SC", 0.95), ("PAS", 0.35))),
            ("IM-C", W(("PAS", 0.30), ("SC", 0.10))),
            ("IM-L", W(("PAS", 0.25))), ("IM-R", W(("PAS", 0.25)))),
        La: S(0.70,
            ("W-L", W(("WI", 0.85), ("PAS", 0.25))),
            ("FW-L", W(("SC", 0.55), ("PAS", 0.20))),
            ("IM-L", W(("WI", 0.35), ("PAS", 0.15)))),
        Ra: S(0.70,
            ("W-R", W(("WI", 0.85), ("PAS", 0.25))),
            ("FW-R", W(("SC", 0.55), ("PAS", 0.20))),
            ("IM-R", W(("WI", 0.35), ("PAS", 0.15))))
    );

    private static FormationCoeffs F541() => new(
        Mid: S(0.62,
            ("IM-L", W(("PM", 0.50), ("PAS", 0.18))),
            ("IM-R", W(("PM", 0.50), ("PAS", 0.18))),
            ("W-L", W(("PM", 0.35))), ("W-R", W(("PM", 0.35))),
            ("WB-L", W(("PM", 0.12))), ("WB-R", W(("PM", 0.12))),
            ("DEF-CL", W(("PM", 0.12))), ("DEF-C", W(("PM", 0.12))), ("DEF-CR", W(("PM", 0.12))),
            ("FW-C", W(("PM", 0.10)))),
        Cd: S(0.62,
            ("GK", W(("GK", 0.70), ("DEF", 0.15))),
            ("DEF-C", W(("DEF", 1.00))),
            ("DEF-CL", W(("DEF", 0.90))), ("DEF-CR", W(("DEF", 0.90))),
            ("WB-L", W(("DEF", 0.60))), ("WB-R", W(("DEF", 0.60))),
            ("IM-L", W(("DEF", 0.20))), ("IM-R", W(("DEF", 0.20)))),
        Ld: S(0.85,
            ("GK", W(("GK", 0.45), ("DEF", 0.12))),
            ("WB-L", W(("DEF", 1.10))), ("DEF-CL", W(("DEF", 0.70))),
            ("DEF-C", W(("DEF", 0.50))),
            ("W-L", W(("DEF", 0.20))), ("IM-L", W(("DEF", 0.12)))),
        Rd: S(0.85,
            ("GK", W(("GK", 0.45), ("DEF", 0.12))),
            ("WB-R", W(("DEF", 1.10))), ("DEF-CR", W(("DEF", 0.70))),
            ("DEF-C", W(("DEF", 0.50))),
            ("W-R", W(("DEF", 0.20))), ("IM-R", W(("DEF", 0.12)))),
        Ca: S(0.60,
            ("FW-C", W(("SC", 1.10), ("PAS", 0.50))),
            ("IM-L", W(("PAS", 0.30), ("SC", 0.10))),
            ("IM-R", W(("PAS", 0.30), ("SC", 0.10))),
            ("W-L", W(("PAS", 0.12))), ("W-R", W(("PAS", 0.12)))),
        La: S(0.70,
            ("W-L", W(("WI", 0.90), ("PAS", 0.20))),
            ("IM-L", W(("WI", 0.20), ("PAS", 0.15))),
            ("FW-C", W(("SC", 0.60))),
            ("WB-L", W(("WI", 0.20)))),
        Ra: S(0.70,
            ("W-R", W(("WI", 0.90), ("PAS", 0.20))),
            ("IM-R", W(("WI", 0.20), ("PAS", 0.15))),
            ("FW-C", W(("SC", 0.60))),
            ("WB-R", W(("WI", 0.20))))
    );

    private static FormationCoeffs F253() => new(
        Mid: S(0.62,
            ("IM-C", W(("PM", 0.45), ("PAS", 0.18))),
            ("IM-L", W(("PM", 0.45), ("PAS", 0.126))),
            ("IM-R", W(("PM", 0.45), ("PAS", 0.126))),
            ("W-L", W(("PM", 0.25))), ("W-R", W(("PM", 0.25))),
            ("DEF-CL", W(("PM", 0.12))), ("DEF-CR", W(("PM", 0.12))),
            ("FW-L", W(("PM", 0.10))), ("FW-C", W(("PM", 0.10))), ("FW-R", W(("PM", 0.10)))),
        Cd: S(0.65,
            ("GK", W(("GK", 0.80), ("DEF", 0.15))),
            ("DEF-CL", W(("DEF", 0.90))), ("DEF-CR", W(("DEF", 0.90))),
            ("IM-C", W(("DEF", 0.30))), ("IM-L", W(("DEF", 0.30))), ("IM-R", W(("DEF", 0.30))),
            ("W-L", W(("DEF", 0.20))), ("W-R", W(("DEF", 0.20)))),
        Ld: S(0.85,
            ("GK", W(("GK", 0.40), ("DEF", 0.12))),
            ("DEF-CL", W(("DEF", 0.70))), ("DEF-CR", W(("DEF", 0.15))),
            ("W-L", W(("DEF", 0.30))), ("IM-L", W(("DEF", 0.25)))),
        Rd: S(0.85,
            ("GK", W(("GK", 0.40), ("DEF", 0.12))),
            ("DEF-CR", W(("DEF", 0.70))), ("DEF-CL", W(("DEF", 0.15))),
            ("W-R", W(("DEF", 0.30))), ("IM-R", W(("DEF", 0.25)))),
        Ca: S(0.62,
            ("FW-L", W(("SC", 0.90), ("PAS", 0.30))),
            ("FW-C", W(("SC", 0.90), ("PAS", 0.30))),
            ("FW-R", W(("SC", 0.90), ("PAS", 0.30))),
            ("IM-C", W(("PAS", 0.30), ("SC", 0.10))),
            ("IM-L", W(("PAS", 0.30), ("SC", 0.10))),
            ("IM-R", W(("PAS", 0.30), ("SC", 0.10))),
            ("W-L", W(("PAS", 0.12))), ("W-R", W(("PAS", 0.12)))),
        La: S(0.70,
            ("W-L", W(("WI", 0.70), ("PAS", 0.25))),
            ("IM-L", W(("WI", 0.50), ("PAS", 0.20))),
            ("FW-L", W(("SC", 0.50), ("PAS", 0.25))),
            ("FW-C", W(("SC", 0.30))),
            ("DEF-CL", W(("WI", 0.10)))),
        Ra: S(0.70,
            ("W-R", W(("WI", 0.70), ("PAS", 0.25))),
            ("IM-R", W(("WI", 0.50), ("PAS", 0.20))),
            ("FW-R", W(("SC", 0.50), ("PAS", 0.25))),
            ("FW-C", W(("SC", 0.30))),
            ("DEF-CR", W(("WI", 0.10))))
    );

    private static FormationCoeffs F433() => F442();
    private static FormationCoeffs F451() => F352();
    private static FormationCoeffs F532() => F541();
    private static FormationCoeffs F343() => F352();
    private static FormationCoeffs F343_2B() => F442();
    private static FormationCoeffs F550() => F541();
    private static FormationCoeffs F253_2B() => F253();
}
