using System.Text.Json;

namespace HattrickAI.V5.Core;

public static class RatingEngineWebEndpoints
{
    private static readonly JsonSerializerOptions SessionJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void Map(WebApplication app)
    {
        app.MapGet("/api/v5/rating-engines", () => Results.Ok(new
        {
            @default = RatingEngineKind.V5.ToString(),
            engines = new RatingEngineRegistry().All.Select(x => new { id = x.Kind.ToString(), name = x.Name }).ToArray()
        }));

        app.MapGet("/api/v5/rating-engine/selection", (HttpContext http) =>
        {
            var selected = Enum.TryParse<RatingEngineKind>(http.Session.GetString("v5.rating.selected"), true, out var value)
                ? value
                : RatingEngineKind.V5;
            return Results.Ok(new { selected = selected.ToString(), @default = RatingEngineKind.V5.ToString() });
        });

        app.MapPost("/api/v5/rating-engine/selection", (HttpContext http, RatingEngineSelectionRequest request) =>
        {
            if (!Enum.TryParse<RatingEngineKind>(request.Engine, true, out var engine))
                return Results.BadRequest(new { message = "Geçersiz rating engine.", allowed = Enum.GetNames<RatingEngineKind>() });
            http.Session.SetString("v5.rating.selected", engine.ToString());
            return Results.Ok(new { selected = engine.ToString(), @default = RatingEngineKind.V5.ToString() });
        });

        app.MapGet("/api/v5/rating-engines/compare", (HttpContext http) =>
        {
            var playersJson = http.Session.GetString("v5.rating.players");
            var lineupJson = http.Session.GetString("v5.rating.lineup");
            var contextJson = http.Session.GetString("v5.rating.context");
            var canonicalJson = http.Session.GetString("v5.rating.canonical");
            if (string.IsNullOrWhiteSpace(playersJson) || string.IsNullOrWhiteSpace(lineupJson) || string.IsNullOrWhiteSpace(contextJson))
                return Results.Conflict(new { message = "Önce güncel analiz çalıştırılmalı; rating engine karşılaştırma bağlamı hazır değil." });
            try
            {
                var players = JsonSerializer.Deserialize<List<Player>>(playersJson, SessionJsonOptions) ?? new();
                var lineup = DeserializeStoredLineup(lineupJson);
                var context = JsonSerializer.Deserialize<RatingContext>(contextJson, SessionJsonOptions) ?? throw new InvalidOperationException("Rating context deserialize edilemedi.");
                var canonical = string.IsNullOrWhiteSpace(canonicalJson) ? null : JsonSerializer.Deserialize<RegionalRatingSnapshot>(canonicalJson, SessionJsonOptions);
                var selected = Enum.TryParse<RatingEngineKind>(http.Session.GetString("v5.rating.selected"), true, out var s) ? s : RatingEngineKind.V5;
                var request = new RatingEngineRequest(lineup, players, context, canonical);
                var comparison = new RatingEngineComparisonService().Compare(request, selected);
                return Results.Ok(comparison);
            }
            catch (Exception ex)
            {
                return Results.Json(new
                {
                    message = "Rating engine karşılaştırması hesaplanamadı.",
                    detail = ex.Message,
                    exceptionType = ex.GetType().FullName
                }, statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        // Direct V5 endpoint for arbitrary XI/slot selection. Formation is descriptive;
        // the selected slot codes determine the positional calculation.
        app.MapPost("/api/v5/rating-engine/custom", (CustomV5RatingRequest request) =>
        {
            try
            {
                CustomV5RatingValidation.Validate(request.Lineup, request.Players);
                var result = new V5RatingEngine().Calculate(new RatingEngineRequest(
                    request.Lineup, request.Players, request.Context, null, request.HOContext));
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Json(new { message = "Özel V5 kadrosu hesaplanamadı.", detail = ex.Message },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        app.MapGet("/api/v5/rating-engine/selected", (HttpContext http) =>
        {
            var playersJson = http.Session.GetString("v5.rating.players");
            var lineupJson = http.Session.GetString("v5.rating.lineup");
            var contextJson = http.Session.GetString("v5.rating.context");
            var canonicalJson = http.Session.GetString("v5.rating.canonical");
            if (string.IsNullOrWhiteSpace(playersJson) || string.IsNullOrWhiteSpace(lineupJson) || string.IsNullOrWhiteSpace(contextJson))
                return Results.Conflict(new { message = "Önce analiz çalıştırılmalı." });
            try
            {
                var players = JsonSerializer.Deserialize<List<Player>>(playersJson, SessionJsonOptions) ?? new();
                var lineup = DeserializeStoredLineup(lineupJson);
                var context = JsonSerializer.Deserialize<RatingContext>(contextJson, SessionJsonOptions) ?? throw new InvalidOperationException("Rating context deserialize edilemedi.");
                var canonical = string.IsNullOrWhiteSpace(canonicalJson) ? null : JsonSerializer.Deserialize<RegionalRatingSnapshot>(canonicalJson, SessionJsonOptions);
                var selected = Enum.TryParse<RatingEngineKind>(http.Session.GetString("v5.rating.selected"), true, out var s) ? s : RatingEngineKind.V5;
                return Results.Ok(new RatingEngineRegistry().Calculate(selected, new RatingEngineRequest(lineup, players, context, canonical)));
            }
            catch (Exception ex)
            {
                return Results.Json(new
                {
                    message = "Seçili rating engine hesaplanamadı.",
                    detail = ex.Message,
                    exceptionType = ex.GetType().FullName
                }, statusCode: StatusCodes.Status500InternalServerError);
            }
        });
    }

    private static Lineup DeserializeStoredLineup(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var teamName = ReadString(root, "TeamName", "teamName");
        var formation = ReadString(root, "Formation", "formation");
        if (!root.TryGetProperty("slots", out var slotsElement) && !root.TryGetProperty("Slots", out slotsElement))
            throw new InvalidOperationException("Stored lineup slots bulunamadı.");
        var slots = JsonSerializer.Deserialize<List<Slot>>(slotsElement.GetRawText(), SessionJsonOptions) ?? new();
        if (slots.Count == 0) throw new InvalidOperationException("Stored lineup boş.");
        return new Lineup(teamName, formation, slots);
    }

    private static string ReadString(JsonElement root, string primaryName, string alternateName)
    {
        if (root.TryGetProperty(primaryName, out var primary)) return primary.GetString() ?? string.Empty;
        if (root.TryGetProperty(alternateName, out var alternate)) return alternate.GetString() ?? string.Empty;
        return string.Empty;
    }
}

public sealed record RatingEngineSelectionRequest(string Engine);
\npublic sealed record CustomV5RatingRequest(\n    Lineup Lineup,\n    IReadOnlyList<Player> Players,\n    RatingContext Context,\n    HOEngineContext? HOContext = null);\n\npublic static class CustomV5RatingValidation\n{\n    public static void Validate(Lineup lineup, IReadOnlyList<Player> players)\n    {\n        ArgumentNullException.ThrowIfNull(lineup);\n        ArgumentNullException.ThrowIfNull(players);\n        if (lineup.Slots.Count != 11)\n            throw new ArgumentException($"V5 custom XI exactly 11 slot ister; {lineup.Slots.Count} slot verildi.");\n        var activeIds = lineup.Slots.Select(x => x.PlayerId).Where(x => x > 0).ToArray();\n        if (activeIds.Length != 11 || activeIds.Distinct().Count() != 11)\n            throw new ArgumentException("V5 custom XI 11 farklı aktif oyuncu içermeli.");\n        var playerIds = players.Select(x => x.Id).ToHashSet();\n        var missing = activeIds.Where(x => !playerIds.Contains(x)).Distinct().ToArray();\n        if (missing.Length > 0)\n            throw new ArgumentException($"V5 custom XI oyuncuları DB'de bulunamadı: {string.Join(", ", missing)}");\n        var allowed = new HashSet<string>(StringComparer.Ordinal) { "GK", "DEF-L", "DEF-C", "DEF-CL", "DEF-CR", "DEF-R", "W-L", "W-R", "IM-L", "IM-C", "IM-R", "FW-L", "FW-C", "FW-R" };\n        var invalid = lineup.Slots.Select(x => x.Code).Where(x => !allowed.Contains(x)).Distinct(StringComparer.Ordinal).ToArray();\n        if (invalid.Length > 0)\n            throw new ArgumentException($"Geçersiz V5 pozisyon kodu: {string.Join(", ", invalid)}");\n    }\n}\n