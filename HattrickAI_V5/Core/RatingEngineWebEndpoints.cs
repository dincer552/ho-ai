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
            @default = RatingEngineKind.HatFor.ToString(),
            engines = new RatingEngineRegistry().All.Select(x => new { id = x.Kind.ToString(), name = x.Name }).ToArray()
        }));

        app.MapGet("/api/v5/rating-engine/selection", (HttpContext http) =>
        {
            return Results.Ok(new { selected = RatingEngineKind.HatFor.ToString(), @default = RatingEngineKind.HatFor.ToString() });
        });

        app.MapPost("/api/v5/rating-engine/selection", (HttpContext http, RatingEngineSelectionRequest request) =>
        {
            if (!RatingEngineKindParse.TryParse(request.Engine, out _))
                return Results.BadRequest(new { message = "Geçersiz rating engine. Yalnızca HatFor aktif.", allowed = new[] { "HatFor" } });
            http.Session.SetString("v5.rating.selected", RatingEngineKind.HatFor.ToString());
            return Results.Ok(new { selected = RatingEngineKind.HatFor.ToString(), @default = RatingEngineKind.HatFor.ToString() });
        });

        app.MapGet("/api/v5/rating-engines/compare", (HttpContext http) =>
        {
            var playersJson = http.Session.GetString("v5.rating.players");
            var lineupJson = http.Session.GetString("v5.rating.lineup");
            var contextJson = http.Session.GetString("v5.rating.context");
            if (string.IsNullOrWhiteSpace(playersJson) || string.IsNullOrWhiteSpace(lineupJson) || string.IsNullOrWhiteSpace(contextJson))
                return Results.Conflict(new { message = "Önce güncel analiz çalıştırılmalı; rating engine karşılaştırma bağlamı hazır değil." });
            try
            {
                var players = JsonSerializer.Deserialize<List<Player>>(playersJson, SessionJsonOptions) ?? new();
                var lineup = DeserializeStoredLineup(lineupJson);
                var context = JsonSerializer.Deserialize<RatingContext>(contextJson, SessionJsonOptions) ?? throw new InvalidOperationException("Rating context deserialize edilemedi.");
                var request = new RatingEngineRequest(lineup, players, context, null);
                var result = new RatingEngineRegistry().Calculate(RatingEngineKind.HatFor, request);
                return Results.Ok(new
                {
                    baseline = "HatFor",
                    selected = "HatFor",
                    engines = new[] { result }
                });
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

        app.MapPost("/api/v5/rating-engine/manual", (CustomV5RatingRequest request) =>
        {
            try
            {
                CustomV5RatingValidation.ValidateManual(request.Lineup, request.Players);
                var result = new HatForRatingEngine().Calculate(new RatingEngineRequest(
                    request.Lineup, request.Players, request.Context, null, request.HOContext));
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Json(new { message = "Manuel HatFor kadrosu hesaplanamadı.", detail = ex.Message },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        app.MapPost("/api/v5/rating-engine/custom", (CustomV5RatingRequest request) =>
        {
            try
            {
                CustomV5RatingValidation.Validate(request.Lineup, request.Players);
                var result = new HatForRatingEngine().Calculate(new RatingEngineRequest(
                    request.Lineup, request.Players, request.Context, null, request.HOContext));
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Json(new { message = "Özel HatFor kadrosu hesaplanamadı.", detail = ex.Message },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        app.MapGet("/api/v5/rating-engine/selected", (HttpContext http) =>
        {
            var playersJson = http.Session.GetString("v5.rating.players");
            var lineupJson = http.Session.GetString("v5.rating.lineup");
            var contextJson = http.Session.GetString("v5.rating.context");
            if (string.IsNullOrWhiteSpace(playersJson) || string.IsNullOrWhiteSpace(lineupJson) || string.IsNullOrWhiteSpace(contextJson))
                return Results.Conflict(new { message = "Önce analiz çalıştırılmalı." });
            try
            {
                var players = JsonSerializer.Deserialize<List<Player>>(playersJson, SessionJsonOptions) ?? new();
                var lineup = DeserializeStoredLineup(lineupJson);
                var context = JsonSerializer.Deserialize<RatingContext>(contextJson, SessionJsonOptions) ?? throw new InvalidOperationException("Rating context deserialize edilemedi.");
                return Results.Ok(new RatingEngineRegistry().Calculate(RatingEngineKind.HatFor, new RatingEngineRequest(lineup, players, context, null)));
            }
            catch (Exception ex)
            {
                return Results.Json(new
                {
                    message = "HatFor rating engine hesaplanamadı.",
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
            throw new ArgumentException("Manuel testte 1 ile 11 arası dolu mevki seçilebilir.");
        var activeIds = lineup.Slots.Select(x => x.PlayerId).Where(x => x > 0).ToArray();
        if (activeIds.Length != lineup.Slots.Count || activeIds.Distinct().Count() != activeIds.Length)
            throw new ArgumentException("Her oyuncu yalnızca bir mevkiye yerleştirilebilir.");
        if (lineup.Slots.Select(x => x.Code).Distinct(StringComparer.Ordinal).Count() != lineup.Slots.Count)
            throw new ArgumentException("Aynı mevki birden fazla kez seçilemez.");
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
        if (invalid.Length > 0) throw new ArgumentException($"Geçersiz pozisyon kodu: {string.Join(", ", invalid)}");
    }

    public static void Validate(Lineup lineup, IReadOnlyList<Player> players)
    {
        ArgumentNullException.ThrowIfNull(lineup);
        ArgumentNullException.ThrowIfNull(players);

        if (lineup.Slots.Count != 11)
            throw new ArgumentException($"Custom XI exactly 11 slot ister; {lineup.Slots.Count} slot verildi.");

        var activeIds = lineup.Slots.Select(x => x.PlayerId).Where(x => x > 0).ToArray();
        if (activeIds.Length != 11 || activeIds.Distinct().Count() != 11)
            throw new ArgumentException("Custom XI 11 farklı aktif oyuncu içermeli.");

        var playerIds = players.Select(x => x.Id).ToHashSet();
        var missing = activeIds.Where(x => !playerIds.Contains(x)).Distinct().ToArray();
        if (missing.Length > 0)
            throw new ArgumentException($"Custom XI oyuncuları DB'de bulunamadı: {string.Join(", ", missing)}");

        ValidateSlotCodes(lineup);
    }
}
