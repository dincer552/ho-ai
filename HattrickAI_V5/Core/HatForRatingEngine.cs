// =============================================================================
// HatForRatingEngine.cs
// Excel formation-specific regional rating engine (sole production engine)
// - Per-formation coefficient tables
// - PlayerOrder relative weight modifiers
// - Signature cache for M6 beam efficiency
// - CalculateLineupWithTrace for JSON download button (HatFor only)
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HattrickAI.V5.Core;

public sealed class HatForRatingEngine : IRatingEngine
{
    public RatingEngineKind Kind => RatingEngineKind.HatFor;
    public string Name => "HatFor (Excel)";

    private static readonly ConcurrentDictionary<string, RegionalRatingSnapshot> Cache = new(StringComparer.Ordinal);

    public RatingEngineResult Calculate(RatingEngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cacheKey = BuildCacheKey(request);
        if (Cache.TryGetValue(cacheKey, out var cached))
            return new RatingEngineResult(Kind, cached);

        var playersById = request.Players.ToDictionary(p => p.Id);
        var formation = NormalizeFormation(request.Lineup.Formation);
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

        Cache[cacheKey] = snap;
        return new RatingEngineResult(Kind, snap);
    }

    public static void ClearCache() => Cache.Clear();
    public static int CacheCount => Cache.Count;

    public RatingCalculationTraceResult CalculateLineupWithTrace(
        Lineup lineup,
        IReadOnlyList<Player> players,
        RatingContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(players);

        var playersById = players.ToDictionary(p => p.Id);
        var formation = NormalizeFormation(lineup.Formation);
        var coeffs = HatForCoefficientTables.ForFormation(formation);
        var coachLevel = 0;

        var slots = MapLineupSlots(lineup, playersById);

        var (mid, midHam, midContrib) = SectorWithContrib(slots, coeffs.Mid, coachLevel, defSector: false);
        var (cd,  cdHam,  cdContrib)  = SectorWithContrib(slots, coeffs.Cd,  coachLevel, defSector: true);
        var (ld,  ldHam,  ldContrib)  = SectorWithContrib(slots, coeffs.Ld,  coachLevel, defSector: true);
        var (rd,  rdHam,  rdContrib)  = SectorWithContrib(slots, coeffs.Rd,  coachLevel, defSector: true);
        var (ca,  caHam,  caContrib)  = SectorWithContrib(slots, coeffs.Ca,  coachLevel, defSector: false);
        var (la,  laHam,  laContrib)  = SectorWithContrib(slots, coeffs.La,  coachLevel, defSector: false);
        var (ra,  raHam,  raContrib)  = SectorWithContrib(slots, coeffs.Ra,  coachLevel, defSector: false);

        var snap = new RegionalRatingSnapshot(
            ld, cd, rd, mid, la, ca, ra,
            ld, cd, rd, mid, la, ca, ra);

        var playerTraces = new List<PlayerRatingCalculationTrace>();
        foreach (var slot in lineup.Slots)
        {
            if (slot.PlayerId <= 0 || !playersById.TryGetValue(slot.PlayerId, out var pl)) continue;
            var key = NormalizeSlotCode(slot.Code);
            if (key is null) continue;

            var order = slot.Order;
            var ss = SlotSkills.From(pl, order);
            var ff = FormF(ss.Form);
            var ek = ExpKat(ss.Experience);

            var direct = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            void AddContrib(string sector, SectorCoeffs sc)
            {
                if (!sc.Weights.TryGetValue(key, out var weights)) return;
                double c = ek;
                if (weights.TryGetValue("GK", out var wGk)) c += Etkin(ss.Keeper, ss.Loyalty) * ff * wGk * OrderMult(order, "GK");
                if (weights.TryGetValue("DEF", out var wDef)) c += Etkin(ss.Defending, ss.Loyalty) * ff * wDef * OrderMult(order, "DEF");
                if (weights.TryGetValue("PM", out var wPm)) c += Etkin(ss.Playmaking, ss.Loyalty) * ff * wPm * OrderMult(order, "PM");
                if (weights.TryGetValue("PAS", out var wPas)) c += Etkin(ss.Passing, ss.Loyalty) * ff * wPas * OrderMult(order, "PAS");
                if (weights.TryGetValue("WI", out var wWi)) c += Etkin(ss.Winger, ss.Loyalty) * ff * wWi * OrderMult(order, "WI");
                if (weights.TryGetValue("SC", out var wSc)) c += Etkin(ss.Scoring, ss.Loyalty) * ff * wSc * OrderMult(order, "SC");
                if (c != 0) direct[sector] = c;
            }
            AddContrib("LeftDefence", coeffs.Ld);
            AddContrib("CentralDefence", coeffs.Cd);
            AddContrib("RightDefence", coeffs.Rd);
            AddContrib("Midfield", coeffs.Mid);
            AddContrib("LeftAttack", coeffs.La);
            AddContrib("CentralAttack", coeffs.Ca);
            AddContrib("RightAttack", coeffs.Ra);

            var rawSkills = new Dictionary<string, double>
            {
                ["keeper"] = pl.Keeper, ["defending"] = pl.Defending,
                ["playmaking"] = pl.Playmaking, ["passing"] = pl.Passing,
                ["winger"] = pl.Winger, ["scoring"] = pl.Scoring
            };
            var effective = new Dictionary<string, double>
            {
                ["keeper"] = Etkin(pl.Keeper, pl.Loyalty),
                ["defending"] = Etkin(pl.Defending, pl.Loyalty),
                ["playmaking"] = Etkin(pl.Playmaking, pl.Loyalty),
                ["passing"] = Etkin(pl.Passing, pl.Loyalty),
                ["winger"] = Etkin(pl.Winger, pl.Loyalty),
                ["scoring"] = Etkin(pl.Scoring, pl.Loyalty)
            };

            playerTraces.Add(new PlayerRatingCalculationTrace(
                pl.Id, pl.Name ?? $"#{pl.Id}", key,
                side: key.Contains("-L") || key.EndsWith("L") ? "Left" : key.Contains("-R") || key.EndsWith("R") ? "Right" : "Center",
                order: order.ToString(),
                calculationRoute: $"HatForCoefficientTables.{formation} → {key} / {order}",
                formulaReference: "HatFor: Etkin=(skill-1)+loyalty; FormF=0.378*√min(7,form-1); ExpKat poly; order mult; formation scale.",
                rawSkills: rawSkills,
                effectiveSkills: effective,
                form: pl.Form,
                formMultiplier: ff,
                staminaMultiplier: 1.0,
                loyaltyBonus: pl.Loyalty,
                experienceBonus: ek,
                crowdingMultiplier: 1.0,
                directContributions: direct,
                skillContributionsBeforeExperience: direct,
                experienceContributions: new Dictionary<string, double>(),
                crowdingAdjustedContributions: direct));
        }

        var sectors = new List<RatingSectorCalculationTrace>
        {
            new("LeftDefence", ldHam, 1, 1, ld, ld, ldContrib),
            new("CentralDefence", cdHam, 1, 1, cd, cd, cdContrib),
            new("RightDefence", rdHam, 1, 1, rd, rd, rdContrib),
            new("Midfield", midHam, 1, 1, mid, mid, midContrib),
            new("LeftAttack", laHam, 1, 1, la, la, laContrib),
            new("CentralAttack", caHam, 1, 1, ca, ca, caContrib),
            new("RightAttack", raHam, 1, 1, ra, ra, raContrib),
        };

        int cdCount = slots.Keys.Count(k => k is "DEF-CL" or "DEF-C" or "DEF-CR");
        int imCount = slots.Keys.Count(k => k.StartsWith("IM-"));
        int fwCount = slots.Keys.Count(k => k.StartsWith("FW-"));

        return new RatingCalculationTraceResult(
            Engine: "HatFor",
            TeamName: lineup.TeamName ?? "Takım",
            Formation: formation,
            MatchLocation: "Home",
            Attitude: "Normal",
            Tactic: "Normal",
            MatchMinute: 0,
            GoalDifference: 0,
            BaselineFormFactor: 0.756,
            CentralDefenderCount: cdCount,
            CentralMidfielderCount: imCount,
            ForwardCount: fwCount,
            ConfidenceLevel: 0,
            ConfidenceAttackMultiplier: 1.0,
            EngineRatingBeforeConfidence: snap,
            FinalRating: snap,
            Players: playerTraces,
            Sectors: sectors);
    }

    private static (double rating, double ham, IReadOnlyDictionary<string, double> contrib)
        SectorWithContrib(
            IReadOnlyDictionary<string, SlotSkills> slots,
            SectorCoeffs sc,
            int coachLevel,
            bool defSector)
    {
        double ham = 0;
        var contrib = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var (slot, weights) in sc.Weights)
        {
            if (!slots.TryGetValue(slot, out var p) || p is null) continue;
            var ff = FormF(p.Form);
            var ek = ExpKat(p.Experience);
            double c = ek;
            if (weights.TryGetValue("GK", out var wGk)) c += Etkin(p.Keeper, p.Loyalty) * ff * wGk * OrderMult(p.Order, "GK");
            if (weights.TryGetValue("DEF", out var wDef)) c += Etkin(p.Defending, p.Loyalty) * ff * wDef * OrderMult(p.Order, "DEF");
            if (weights.TryGetValue("PM", out var wPm)) c += Etkin(p.Playmaking, p.Loyalty) * ff * wPm * OrderMult(p.Order, "PM");
            if (weights.TryGetValue("PAS", out var wPas)) c += Etkin(p.Passing, p.Loyalty) * ff * wPas * OrderMult(p.Order, "PAS");
            if (weights.TryGetValue("WI", out var wWi)) c += Etkin(p.Winger, p.Loyalty) * ff * wWi * OrderMult(p.Order, "WI");
            if (weights.TryGetValue("SC", out var wSc)) c += Etkin(p.Scoring, p.Loyalty) * ff * wSc * OrderMult(p.Order, "SC");
            ham += c;
            contrib[$"{slot}"] = c;
        }
        var rating = ToRating(ham, sc.Scale, CoachMod(coachLevel, defSector));
        return (rating, ham, contrib);
    }

    private static string BuildCacheKey(RatingEngineRequest request)
    {
        var sb = new StringBuilder(128);
        sb.Append(NormalizeFormation(request.Lineup.Formation)).Append('|');
        sb.Append(request.HOContext?.CoachModifier ?? 0).Append('|');
        foreach (var s in request.Lineup.Slots.OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase))
        {
            if (s.PlayerId <= 0) continue;
            sb.Append(s.Code).Append(':').Append(s.PlayerId).Append(':').Append((int)s.Order).Append(';');
        }
        return sb.ToString();
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

    private static double OrderMult(PlayerOrder order, string skillKey)
    {
        skillKey = skillKey.ToUpperInvariant();
        return order switch
        {
            PlayerOrder.Offensive => skillKey switch
            {
                "DEF" => 0.75, "SC" => 1.18, "WI" => 1.12, "PM" => 1.05, "PAS" => 1.08, "GK" => 1.0, _ => 1.0
            },
            PlayerOrder.Defensive => skillKey switch
            {
                "DEF" => 1.22, "SC" => 0.78, "WI" => 0.88, "PM" => 0.92, "PAS" => 0.95, "GK" => 1.0, _ => 1.0
            },
            PlayerOrder.TowardsWing => skillKey switch
            {
                "WI" => 1.28, "PM" => 0.88, "PAS" => 0.95, "SC" => 1.05, "DEF" => 0.95, _ => 1.0
            },
            PlayerOrder.TowardsMiddle => skillKey switch
            {
                "PM" => 1.18, "PAS" => 1.12, "WI" => 0.82, "DEF" => 1.05, "SC" => 0.95, _ => 1.0
            },
            _ => 1.0
        };
    }

    private static double Sector(
        IReadOnlyDictionary<string, SlotSkills> slots,
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
            if (weights.TryGetValue("GK", out var wGk))
                contrib += Etkin(p.Keeper, p.Loyalty) * ff * wGk * OrderMult(p.Order, "GK");
            if (weights.TryGetValue("DEF", out var wDef))
                contrib += Etkin(p.Defending, p.Loyalty) * ff * wDef * OrderMult(p.Order, "DEF");
            if (weights.TryGetValue("PM", out var wPm))
                contrib += Etkin(p.Playmaking, p.Loyalty) * ff * wPm * OrderMult(p.Order, "PM");
            if (weights.TryGetValue("PAS", out var wPas))
                contrib += Etkin(p.Passing, p.Loyalty) * ff * wPas * OrderMult(p.Order, "PAS");
            if (weights.TryGetValue("WI", out var wWi))
                contrib += Etkin(p.Winger, p.Loyalty) * ff * wWi * OrderMult(p.Order, "WI");
            if (weights.TryGetValue("SC", out var wSc))
                contrib += Etkin(p.Scoring, p.Loyalty) * ff * wSc * OrderMult(p.Order, "SC");
            ham += contrib;
        }
        return ToRating(ham, sc.Scale, CoachMod(coachLevel, defSector));
    }

    private static IReadOnlyDictionary<string, SlotSkills> MapLineupSlots(
        Lineup lineup, IReadOnlyDictionary<int, Player> byId)
    {
        var map = new Dictionary<string, SlotSkills>(StringComparer.OrdinalIgnoreCase);
        foreach (var slot in lineup.Slots)
        {
            if (slot.PlayerId <= 0 || !byId.TryGetValue(slot.PlayerId, out var pl))
                continue;
            var key = NormalizeSlotCode(slot.Code);
            if (key is null) continue;
            map[key] = SlotSkills.From(pl, slot.Order);
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
            "DEF-L" or "WB-L" or "WBL" or "LB" => "WB-L",
            "DEF-R" or "WB-R" or "WBR" or "RB" => "WB-R",
            "DEF-CL" or "LCD" or "CD-L" => "DEF-CL",
            "DEF-CR" or "RCD" or "CD-R" => "DEF-CR",
            "DEF-C" or "CD" or "CB" => "DEF-C",
            "W-L" or "WL" or "LW" => "W-L",
            "W-R" or "WR" or "RW" => "W-R",
            "IM-L" or "LIM" or "CM-L" => "IM-L",
            "IM-R" or "RIM" or "CM-R" => "IM-R",
            "IM-C" or "IM" or "CIM" or "CM" => "IM-C",
            "FW-L" or "LF" or "ST-L" => "FW-L",
            "FW-R" or "RF" or "ST-R" => "FW-R",
            "FW-C" or "FC" or "FW" or "ST" => "FW-C",
            _ => code
        };
    }

    public static string NormalizeFormation(string? formation)
    {
        if (string.IsNullOrWhiteSpace(formation)) return "4-4-2";
        var f = formation.Trim().ToUpperInvariant()
            .Replace(" ", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("_", "");

        if (f is "352" or "3-5-2") return "3-5-2";
        if (f is "442" or "4-4-2") return "4-4-2";
        if (f is "433" or "4-3-3") return "4-3-3";
        if (f is "451" or "4-5-1") return "4-5-1";
        if (f is "541" or "5-4-1") return "5-4-1";
        if (f is "532" or "5-3-2") return "5-3-2";
        if (f is "343" or "3-4-3") return "3-4-3";
        if (f is "3432B" or "3-4-32B" or "343-2B" or "3-4-3-2B" or "3-4-32B") return "3-4-3-2B";
        if (f is "550" or "5-5-0") return "5-5-0";
        if (f is "253" or "2-5-3") return "2-5-3";
        if (f is "2532B" or "2-5-32B" or "253-2B" or "2-5-3-2B") return "2-5-3-2B";

        if (f.Contains('-')) return formation.Trim();
        return formation.Trim();
    }

    private sealed record SlotSkills(
        double Keeper, double Defending, double Playmaking, double Passing,
        double Winger, double Scoring, double Form, double Experience, double Loyalty,
        PlayerOrder Order)
    {
        public static SlotSkills From(Player p, PlayerOrder order) => new(
            p.Keeper, p.Defending, p.Playmaking, p.Passing,
            p.Winger, p.Scoring, p.Form, p.Experience, p.Loyalty, order);
    }
}
