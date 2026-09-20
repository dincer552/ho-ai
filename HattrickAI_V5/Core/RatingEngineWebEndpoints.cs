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

        // Direct V5 endpoint for arbitrary partial/full manual testing. One player is enough;
        // missing slots simply contribute zero, which makes this endpoint useful for
        // isolating position/crowding behaviour without fabricating an XI.
        app.MapPost("/api/v5/rating-engine/manual", (ManualV5RatingRequest request) =>
        {
            try
            {
                CustomV5RatingValidation.ValidateManual(request.Lineup, request.Players);
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
                return Results.Json(new { message = "Manuel V5 kadrosu hesaplanamadı.", detail = ex.Message },
                    statusCode: StatusCodes.Status500InternalServerError);
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

public sealed record CustomV5RatingRequest(
    Lineup Lineup,
    IReadOnlyList<Player> Players,
    RatingContext Context,
    HOEngineContext? HOContext = null);

public static class CustomV5RatingValidation
{
    public static void ValidateManual(Lineup lineup, IReadOnlyList<Player> players)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(players);
        if (lineup.Slots.Count < 1 || lineup.Slots.Count > 11)
            throw new ArgumentException("Manuel V5 testinde 1 ile 11 arası dolu mevki seçilebilir.");
        var activeIds = lineup.Slots.Select(x => x.PlayerId).Where(x => x > 0).ToArray();
        if (activeIds.Length != lineup.Slots.Count || activeIds.Distinct().Count() != activeIds.Length)
            throw new ArgumentException("Her oyuncu yalnızca bir mevkiye yerleştirilebilir.");
        var playerIds = players.Select(x => x.Id).ToHashSet();
        var missing = activeIds.Where(x => !playerIds.Contains(x)).Distinct().ToArray();
        if (missing.Length > 0)
            throw new ArgumentException($"Seçilen oyuncular takım verisinde bulunamadı: {string.Join(", ", missing)}");
        ValidateSlotCodes(lineup);
    }

    private static void ValidateSlotCodes(Lineup lineup)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "GK","WB-L","DEF-CL","DEF-C","DEF-CR","WB-R","DEF-L","DEF-R",
            "W-L","IM-L","IM-C","IM-R","W-R","FW-L","FW-C","FW-R"
        };
        var invalid = lineup.Slots.Select(x=>x.Code).Where(x=>!allowed.Contains(x)).Distinct().ToArray();
        if (invalid.Length > 0) throw new ArgumentException($"Geçersiz V5 pozisyon kodu: {string.Join(", ", invalid)}");
    }

    public static void Validate(Lineup lineup, IReadOnlyList<Player> players)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(players);

        if (lineup.Slots.Count != 11)
            throw new ArgumentException($"V5 custom XI exactly 11 slot ister; {lineup.Slots.Count} slot verildi.");

        var activeIds = lineup.Slots.Select(x => x.PlayerId).Where(x => x > 0).ToArray();
        if (activeIds.Length != 11 || activeIds.Distinct().Count() != 11)
            throw new ArgumentException("V5 custom XI 11 farklı aktif oyuncu içermeli.");

        var playerIds = players.Select(x => x.Id).ToHashSet();
        var missing = activeIds.Where(x => !playerIds.Contains(x)).Distinct().ToArray();
        if (missing.Length > 0)
            throw new ArgumentException($"V5 custom XI oyuncuları DB'de bulunamadı: {string.Join(", ", missing)}");

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "GK",
            "WB-L", "DEF-CL", "DEF-C", "DEF-CR", "WB-R",
            "DEF-L", "DEF-R",
            "W-L", "IM-L", "IM-C", "IM-R", "W-R",
            "FW-L", "FW-C", "FW-R"
        };

        var invalid = lineup.Slots
            .Select(x => x.Code)
            .Where(x => !allowed.Contains(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (invalid.Length > 0)
            throw new ArgumentException($"Geçersiz V5 pozisyon kodu: {string.Join(", ", invalid)}");
    }
}
