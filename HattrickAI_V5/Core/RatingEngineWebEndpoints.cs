using System.Text.Json;

namespace HattrickAI.V5.Core;

public static class RatingEngineWebEndpoints
{
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
                var players = JsonSerializer.Deserialize<List<Player>>(playersJson) ?? new();
                var lineup = DeserializeStoredLineup(lineupJson);
                var context = JsonSerializer.Deserialize<RatingContext>(contextJson) ?? throw new InvalidOperationException("Rating context deserialize edilemedi.");
                var canonical = string.IsNullOrWhiteSpace(canonicalJson) ? null : JsonSerializer.Deserialize<RegionalRatingSnapshot>(canonicalJson);
                var selected = Enum.TryParse<RatingEngineKind>(http.Session.GetString("v5.rating.selected"), true, out var s) ? s : RatingEngineKind.V5;
                return Results.Ok(new RatingEngineComparisonService().Compare(new RatingEngineRequest(lineup, players, context, canonical), selected));
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 500); }
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
                var players = JsonSerializer.Deserialize<List<Player>>(playersJson) ?? new();
                var lineup = DeserializeStoredLineup(lineupJson);
                var context = JsonSerializer.Deserialize<RatingContext>(contextJson) ?? throw new InvalidOperationException("Rating context deserialize edilemedi.");
                var canonical = string.IsNullOrWhiteSpace(canonicalJson) ? null : JsonSerializer.Deserialize<RegionalRatingSnapshot>(canonicalJson);
                var selected = Enum.TryParse<RatingEngineKind>(http.Session.GetString("v5.rating.selected"), true, out var s) ? s : RatingEngineKind.V5;
                return Results.Ok(new RatingEngineRegistry().Calculate(selected, new RatingEngineRequest(lineup, players, context, canonical)));
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 500); }
        });
    }

    private static Lineup DeserializeStoredLineup(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var teamName = root.TryGetProperty("teamName", out var team) ? team.GetString() ?? string.Empty : string.Empty;
        var formation = root.TryGetProperty("formation", out var form) ? form.GetString() ?? string.Empty : string.Empty;
        if (!root.TryGetProperty("slots", out var slotsElement))
            throw new InvalidOperationException("Stored lineup slots bulunamadı.");
        var slots = JsonSerializer.Deserialize<List<Slot>>(slotsElement.GetRawText()) ?? new();
        return new Lineup(teamName, formation, slots);
    }
}

public sealed record RatingEngineSelectionRequest(string Engine);
