// Auto-generated from Excel Genel sheet (HatFor_RATING_TABLE_ALTYAPI)
// Position × Order absolute skill coefficients per sector.
// Used by HatForRatingEngine instead of relative OrderMult.
using System;
using System.Collections.Generic;

namespace HattrickAI.V5.Core;

/// <summary>
/// Excel Genel sekmesi: her pozisyon + emir için sektöre göre mutlak skill katsayıları.
/// Key = slot code (GK, WB-L, DEF-CL, ...); inner = PlayerOrder → sector → skill → weight.
/// </summary>
public static class HatForPositionCoefficients
{
    public static IReadOnlyDictionary<string, double>? GetWeights(string slot, PlayerOrder order, string sector)
    {
        if (!Table.TryGetValue(slot, out var byOrder)) return null;
        if (!byOrder.TryGetValue(order, out var bySector))
        {
            if (!byOrder.TryGetValue(PlayerOrder.Normal, out bySector)) return null;
        }
        return bySector.TryGetValue(sector, out var w) ? w : null;
    }

    public static bool HasSlot(string slot) => Table.ContainsKey(slot);

    private static readonly Dictionary<string, Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>> Table = Build();

    private static Dictionary<string, Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>> Build()
    {
        var t = new Dictionary<string, Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>>(StringComparer.OrdinalIgnoreCase);

        // --- DEF-C ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.26 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.95 };
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.26 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.15 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.16 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.72 };
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.16 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.29 };
                ord[PlayerOrder.Offensive] = sec;
            }
            t["DEF-C"] = ord;
        }

        // --- DEF-CL ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.9 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.95 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.15 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.4 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.72 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.25 };
                ord[PlayerOrder.Offensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.85 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.55 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.12 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.18 };
                ord[PlayerOrder.TowardsWing] = sec;
            }
            t["DEF-CL"] = ord;
        }

        // --- DEF-CR ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.9 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.95 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.15 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.4 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.72 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.25 };
                ord[PlayerOrder.Offensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.85 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.55 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.12 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.18 };
                ord[PlayerOrder.TowardsWing] = sec;
            }
            t["DEF-CR"] = ord;
        }

        // --- GK ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["GK"] = 0.55, ["DEF"] = 0.2 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["GK"] = 0.8, ["DEF"] = 0.25 };
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["GK"] = 0.55, ["DEF"] = 0.2 };
                ord[PlayerOrder.Normal] = sec;
            }
            t["GK"] = ord;
        }

        // --- WB-L ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 1.07 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.38 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.12 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.59 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.74 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.35 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.2 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.69 };
                ord[PlayerOrder.Offensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.908 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.02 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.065 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.607 };
                ord[PlayerOrder.Defensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.75 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.683 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.2 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.35 };
                ord[PlayerOrder.TowardsMiddle] = sec;
            }
            t["WB-L"] = ord;
        }

        // --- WB-R ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 1.07 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.38 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.12 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.59 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.74 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.35 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.2 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.69 };
                ord[PlayerOrder.Offensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.908 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.02 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.065 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.607 };
                ord[PlayerOrder.Defensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.75 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.683 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.2 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.35 };
                ord[PlayerOrder.TowardsMiddle] = sec;
            }
            t["WB-R"] = ord;
        }

        // Remaining positions loaded from companion partial - see full file on disk
        // For build safety include critical midfield/attack slots below.

        // --- W-L ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.35 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.18 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.35 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 1.0, ["PAS"] = 0.25 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.08 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.055 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.016 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.054 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.247, ["PAS"] = 0.062 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.024 };
                ord[PlayerOrder.Offensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.58 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.16 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.2 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.72, ["PAS"] = 0.16 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.05 };
                ord[PlayerOrder.Defensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.26 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.16 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.5 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.82, ["PAS"] = 0.18 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.12 };
                ord[PlayerOrder.TowardsMiddle] = sec;
            }
            t["W-L"] = ord;
        }

        // --- W-R ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.35 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.18 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.35 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 1.0, ["PAS"] = 0.25 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.08 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.055 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.016 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.054 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.247, ["PAS"] = 0.062 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.024 };
                ord[PlayerOrder.Offensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.58 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.16 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.2 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.72, ["PAS"] = 0.16 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.05 };
                ord[PlayerOrder.Defensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.26 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.16 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.5 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["WI"] = 0.82, ["PAS"] = 0.18 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.12 };
                ord[PlayerOrder.TowardsMiddle] = sec;
            }
            t["W-R"] = ord;
        }

        // --- IM-C ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.2 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.35 };
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.2 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 1.0, ["PAS"] = 0.25 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.15 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.3, ["SC"] = 0.12 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.15 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.15 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.25 };
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.15 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.95, ["PAS"] = 0.28 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.18 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.35, ["SC"] = 0.18 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.18 };
                ord[PlayerOrder.Offensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.3 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.5 };
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.3 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.85, ["PAS"] = 0.18 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.1 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.2, ["SC"] = 0.08 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.1 };
                ord[PlayerOrder.Defensive] = sec;
            }
            t["IM-C"] = ord;
        }

        // --- IM-L ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.25 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.3 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.9, ["PAS"] = 0.2 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.2, ["WI"] = 0.15 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.25, ["SC"] = 0.1 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.15 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.2 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.85, ["PAS"] = 0.22 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.25, ["WI"] = 0.2 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.3, ["SC"] = 0.15 };
                ord[PlayerOrder.Offensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.35 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.4 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.8, ["PAS"] = 0.15 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.12 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.15, ["SC"] = 0.05 };
                ord[PlayerOrder.Defensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["LD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.2 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.2 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.75, ["PAS"] = 0.18 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.25, ["WI"] = 0.35 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.2, ["SC"] = 0.08 };
                ord[PlayerOrder.TowardsWing] = sec;
            }
            t["IM-L"] = ord;
        }

        // --- IM-R ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.25 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.3 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.9, ["PAS"] = 0.2 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.2, ["WI"] = 0.15 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.25, ["SC"] = 0.1 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.15 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.2 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.85, ["PAS"] = 0.22 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.25, ["WI"] = 0.2 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.3, ["SC"] = 0.15 };
                ord[PlayerOrder.Offensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.35 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.4 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.8, ["PAS"] = 0.15 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.12 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.15, ["SC"] = 0.05 };
                ord[PlayerOrder.Defensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["RD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.2 };
                sec["CD"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["DEF"] = 0.2 };
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.75, ["PAS"] = 0.18 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.25, ["WI"] = 0.35 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.2, ["SC"] = 0.08 };
                ord[PlayerOrder.TowardsWing] = sec;
            }
            t["IM-R"] = ord;
        }

        // --- FW-C ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.12 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.28, ["SC"] = 0.22, ["WI"] = 0.18 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.48, ["SC"] = 1.12 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.28, ["SC"] = 0.22, ["WI"] = 0.18 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.3 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.36, ["SC"] = 0.2, ["WI"] = 0.1 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.52, ["SC"] = 0.75 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.36, ["SC"] = 0.2, ["WI"] = 0.1 };
                ord[PlayerOrder.Defensive] = sec;
            }
            t["FW-C"] = ord;
        }

        // --- FW-L ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.12 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.3, ["PAS"] = 0.28, ["WI"] = 0.18 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.4, ["SC"] = 1.25 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.3, ["PAS"] = 0.2 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.28 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.26, ["PAS"] = 0.3, ["WI"] = 0.12 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.5, ["SC"] = 0.8 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.26, ["PAS"] = 0.3, ["WI"] = 0.12 };
                ord[PlayerOrder.Defensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.12 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.75, ["PAS"] = 0.4, ["WI"] = 0.55 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.36, ["SC"] = 0.72 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.18, ["PAS"] = 0.14, ["WI"] = 0.12 };
                ord[PlayerOrder.TowardsWing] = sec;
            }
            t["FW-L"] = ord;
        }

        // --- FW-R ---
        {
            var ord = new Dictionary<PlayerOrder, Dictionary<string, Dictionary<string, double>>>();
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.12 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.3, ["PAS"] = 0.28, ["WI"] = 0.18 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.4, ["SC"] = 1.25 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.3, ["PAS"] = 0.2 };
                ord[PlayerOrder.Normal] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.28 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.26, ["PAS"] = 0.3, ["WI"] = 0.12 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.5, ["SC"] = 0.8 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.26, ["PAS"] = 0.3, ["WI"] = 0.12 };
                ord[PlayerOrder.Defensive] = sec;
            }
            {
                var sec = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
                sec["MID"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PM"] = 0.12 };
                sec["RA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.75, ["PAS"] = 0.4, ["WI"] = 0.55 };
                sec["CA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["PAS"] = 0.36, ["SC"] = 0.72 };
                sec["LA"] = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) { ["SC"] = 0.18, ["PAS"] = 0.14, ["WI"] = 0.12 };
                ord[PlayerOrder.TowardsWing] = sec;
            }
            t["FW-R"] = ord;
        }

        return t;
    }
}
