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
        if (formation is "3432B" or "3-4-32B" or "343-2B" or "3-4-3-2B" or "3-4-32B") return F343_2B();
        if (formation is "550" or "5-5-0") return F550();
        if (formation is "253" or "2-5-3") return F253();
        if (formation is "2532B" or "2-5-32B" or "253-2B" or "2-5-3-2B") return F253_2B();
        return F442();
    }
