using System;
using System.Collections.Generic;
using System.Linq;

namespace HattrickAI.V5.Core;

public sealed record SectorCoeffs(
    double Scale,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Weights);

public sealed record FormationCoeffs(
    SectorCoeffs Mid, SectorCoeffs Cd, SectorCoeffs Ld, SectorCoeffs Rd,
    SectorCoeffs Ca, SectorCoeffs La, SectorCoeffs Ra);

/// <summary>
/// Per-formation Excel TOPLAM coefficient tables. Every formation has distinct
/// slot weights and sector scales — no cross-formation stubs.
/// </summary>
public static partial class HatForCoefficientTables
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
        if (formation is "3432B" or "3-4-32B" or "343-2B" or "3-4-3-2B" or "3-4-32B") return F343_2B();
        if (formation is "550" or "5-5-0") return F550();
        if (formation is "253" or "2-5-3") return F253();
        if (formation is "2532B" or "2-5-32B" or "253-2B" or "2-5-3-2B") return F253_2B();
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
            ("W-L", W(("PM", 0.30))), ("W-R", W(("PM", 0.30))),
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

    private static FormationCoeffs F433() => new(
        Mid: S(0.60,
            ("IM-C", W(("PM", 0.55), ("PAS", 0.18))),
            ("IM-L", W(("PM", 0.48), ("PAS", 0.15))),
            ("IM-R", W(("PM", 0.48), ("PAS", 0.15))),
            ("WB-L", W(("PM", 0.12))), ("WB-R", W(("PM", 0.12))),
            ("DEF-CL", W(("PM", 0.12))), ("DEF-CR", W(("PM", 0.12))),
            ("FW-L", W(("PM", 0.12))), ("FW-C", W(("PM", 0.10))), ("FW-R", W(("PM", 0.12)))),
        Cd: S(0.60,
            ("GK", W(("GK", 0.70), ("DEF", 0.15))),
            ("DEF-CL", W(("DEF", 0.90))), ("DEF-CR", W(("DEF", 0.90))),
            ("WB-L", W(("DEF", 0.50))), ("WB-R", W(("DEF", 0.50))),
            ("IM-C", W(("DEF", 0.28))), ("IM-L", W(("DEF", 0.22))), ("IM-R", W(("DEF", 0.22)))),
        Ld: S(0.85,
            ("GK", W(("GK", 0.45), ("DEF", 0.12))),
            ("WB-L", W(("DEF", 1.00))), ("DEF-CL", W(("DEF", 0.70))),
            ("IM-L", W(("DEF", 0.20))), ("FW-L", W(("DEF", 0.08)))),
        Rd: S(0.85,
            ("GK", W(("GK", 0.45), ("DEF", 0.12))),
            ("WB-R", W(("DEF", 1.00))), ("DEF-CR", W(("DEF", 0.70))),
            ("IM-R", W(("DEF", 0.20))), ("FW-R", W(("DEF", 0.08)))),
        Ca: S(0.62,
            ("FW-C", W(("SC", 1.00), ("PAS", 0.35))),
            ("FW-L", W(("SC", 0.75), ("PAS", 0.25))),
            ("FW-R", W(("SC", 0.75), ("PAS", 0.25))),
            ("IM-C", W(("PAS", 0.28), ("SC", 0.10))),
            ("IM-L", W(("PAS", 0.22))), ("IM-R", W(("PAS", 0.22)))),
        La: S(0.70,
            ("FW-L", W(("SC", 0.70), ("WI", 0.40), ("PAS", 0.25))),
            ("IM-L", W(("WI", 0.35), ("PAS", 0.18))),
            ("FW-C", W(("SC", 0.35))),
            ("WB-L", W(("WI", 0.25)))),
        Ra: S(0.70,
            ("FW-R", W(("SC", 0.70), ("WI", 0.40), ("PAS", 0.25))),
            ("IM-R", W(("WI", 0.35), ("PAS", 0.18))),
            ("FW-C", W(("SC", 0.35))),
            ("WB-R", W(("WI", 0.25))))
    );

    private static FormationCoeffs F451() => new(
        Mid: S(0.62,
            ("IM-C", W(("PM", 0.58), ("PAS", 0.18))),
            ("IM-L", W(("PM", 0.48), ("PAS", 0.15))),
            ("IM-R", W(("PM", 0.48), ("PAS", 0.15))),
            ("W-L", W(("PM", 0.32))), ("W-R", W(("PM", 0.32))),
            ("WB-L", W(("PM", 0.12))), ("WB-R", W(("PM", 0.12))),
            ("DEF-CL", W(("PM", 0.12))), ("DEF-CR", W(("PM", 0.12))),
            ("FW-C", W(("PM", 0.10)))),
        Cd: S(0.60,
            ("GK", W(("GK", 0.70), ("DEF", 0.15))),
            ("DEF-CL", W(("DEF", 0.90))), ("DEF-CR", W(("DEF", 0.90))),
            ("WB-L", W(("DEF", 0.50))), ("WB-R", W(("DEF", 0.50))),
            ("IM-C", W(("DEF", 0.30))), ("IM-L", W(("DEF", 0.25))), ("IM-R", W(("DEF", 0.25)))),
        Ld: S(0.85,
            ("GK", W(("GK", 0.45), ("DEF", 0.12))),
            ("WB-L", W(("DEF", 1.00))), ("DEF-CL", W(("DEF", 0.70))),
            ("W-L", W(("DEF", 0.28))), ("IM-L", W(("DEF", 0.18)))),
        Rd: S(0.85,
            ("GK", W(("GK", 0.45), ("DEF", 0.12))),
            ("WB-R", W(("DEF", 1.00))), ("DEF-CR", W(("DEF", 0.70))),
            ("W-R", W(("DEF", 0.28))), ("IM-R", W(("DEF", 0.18)))),
        Ca: S(0.58,
            ("FW-C", W(("SC", 1.15), ("PAS", 0.50))),
            ("IM-C", W(("PAS", 0.30), ("SC", 0.12))),
            ("IM-L", W(("PAS", 0.25))), ("IM-R", W(("PAS", 0.25))),
            ("W-L", W(("PAS", 0.14))), ("W-R", W(("PAS", 0.14)))),
        La: S(0.68,
            ("W-L", W(("WI", 0.90), ("PAS", 0.25))),
            ("IM-L", W(("WI", 0.30), ("PAS", 0.15))),
            ("FW-C", W(("SC", 0.50))),
            ("WB-L", W(("WI", 0.22)))),
        Ra: S(0.68,
            ("W-R", W(("WI", 0.90), ("PAS", 0.25))),
            ("IM-R", W(("WI", 0.30), ("PAS", 0.15))),
            ("FW-C", W(("SC", 0.50))),
            ("WB-R", W(("WI", 0.22))))
    );
}
