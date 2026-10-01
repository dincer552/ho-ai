using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using HattrickAI.V5.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromHours(8);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ChppV5>();
builder.Services.AddSingleton<AnalysisService>();

var app = builder.Build();
app.UseSession();
app.UseDefaultFiles();
app.UseStaticFiles();

var build = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT")
    ?? Environment.GetEnvironmentVariable("GIT_COMMIT")
    ?? "local";

app.MapGet("/api/v5/health", () => Results.Ok(new { status = "ok", build, engine = "HatFor" }));

app.MapPost("/api/v5/analyze", async (HttpContext http, AnalysisService service, CancellationToken ct) =>
{
    MatchQuestionnaire? questionnaire = null;
    try
    {
        if (http.Request.ContentLength > 0)
        {
            var body = await JsonSerializer.DeserializeAsync<QuestionnaireRequest>(http.Request.Body, cancellationToken: ct);
            if (body is not null)
                questionnaire = MatchQuestionnaire.FromStrings(body.CoachStyle, body.TeamSpirit, body.MatchImportance);
        }
    }
    catch { /* optional body */ }

    var runId = MotorRunLogStore.Start(build);
    try
    {
        var result = await service.RunAsync(build, questionnaire, ct);
        MotorRunLogStore.Finish(runId, true, "Analiz tamamlandı");
        var motorLog = MotorRunLogStore.Get(runId);
        MotorResultArchive.Save(runId, result, result.MotorPipeline!, motorLog, build);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        MotorRunLogStore.Finish(runId, false, ex.Message);
        return Results.Problem(ex.Message, statusCode: 502);
    }
});

// Rating calculation JSON dump (HatFor production engine only).
app.MapGet("/api/v5/rating-calculation-details", (HttpContext http) =>
{
    var session = http.Session;
    var playersJson = session.GetString("v5.rating.players");
    var lineupJson = session.GetString("v5.rating.lineup");
    var contextJson = session.GetString("v5.rating.context");

    if (string.IsNullOrWhiteSpace(playersJson) || string.IsNullOrWhiteSpace(lineupJson) || string.IsNullOrWhiteSpace(contextJson))
        return Results.BadRequest(new { message = "Önce analiz çalıştırılmalı; rating hesaplama oturumu bulunamadı." });

    var selectedEngine = session.GetString("v5.rating.engine")
        ?? session.GetString("v5.rating.selected")
        ?? RatingEngineKind.HatFor.ToString();
    if (!RatingEngineKindParse.TryParse(selectedEngine, out _))
        return Results.Conflict(new { message = $"Hesap JSON dökümü HatFor motoru için kullanılabilir. Seçili motor: {selectedEngine}." });

    try
    {
        var players = JsonSerializer.Deserialize<List<Player>>(playersJson) ?? [];
        var storedLineup = JsonSerializer.Deserialize<StoredLineup>(lineupJson);
        var storedSlots = storedLineup?.Slots ?? Array.Empty<Slot>();
        var validSlots = storedSlots
            .Where(s => s is not null && s.PlayerId > 0 && !string.IsNullOrWhiteSpace(s.Code))
            .ToList();
        var lineup = storedLineup is null
            ? null
            : new Lineup(
                storedLineup.TeamName ?? "Takım",
                storedLineup.Formation ?? string.Empty,
                validSlots);
        var context = JsonSerializer.Deserialize<RatingContext>(contextJson);
        if (lineup is null || context is null || players.Count == 0 || validSlots.Count == 0)
            return Results.BadRequest(new { message = "Kaydedilmiş rating hesaplama verisi eksik." });

        var confidence = int.TryParse(
            session.GetString("v5.rating.confidence"),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsedConfidence)
            ? Math.Clamp(parsedConfidence, 0, 9)
            : 4;

        // Production: HatFor only — old RegionalRatingEngineFixed path removed from JSON dump.
        var trace = new HatForRatingEngine().CalculateLineupWithTrace(lineup, players, context);
        var attackMultiplier = Math.Clamp(1.0 + (confidence - 4.0) * 0.05, 0.80, 1.25);
        var finalRating = ConfidenceRatingAdjuster.Apply(trace.EngineRatingBeforeConfidence, confidence);
        trace = trace with
        {
            ConfidenceLevel = confidence,
            ConfidenceAttackMultiplier = attackMultiplier,
            FinalRating = finalRating
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(
            trace,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            });

        var safeTeam = string.Concat((lineup.TeamName ?? "team").Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
        if (string.IsNullOrWhiteSpace(safeTeam)) safeTeam = "team";
        var filename = $"hattrickai-hatfor-rating-calculation-{safeTeam}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
        return Results.File(json, "application/json; charset=utf-8", filename);
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { message = "Kaydedilmiş rating hesaplama JSON'u çözümlenemedi." });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 502);
    }
});

app.MapGet("/api/v5/motor-result/latest", () =>
{
    return MotorResultArchive.TryGetLatest(out var json)
        ? Results.Text(json, "application/json; charset=utf-8")
        : Results.NotFound(new { message = "Motor sonucu yok." });
});

app.MapGet("/api/v5/motor-result/{runId}", (string runId) =>
{
    return MotorResultArchive.TryGetByRunId(runId, out var json)
        ? Results.Text(json, "application/json; charset=utf-8")
        : Results.NotFound(new { message = "Motor sonucu bulunamadı." });
});

app.MapGet("/api/v5/team-player-export", async (ChppV5 chpp, CancellationToken ct) =>
{
    if (!chpp.Connected) return Results.Unauthorized();
    try
    {
        var data = await new TeamPlayerChppExportService(chpp).ExportAsync(build, ct);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var filename = $"hattrickai-team-players-{DateTimeOffset.UtcNow:yyyy-MM-ddTHH-mm-ss-fffZ}.json";
        return Results.File(bytes, "application/json", filename);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 502);
    }
});

RatingEngineWebEndpoints.Map(app);
app.Run();

public sealed record QuestionnaireRequest(string CoachStyle, string TeamSpirit, string MatchImportance);
public sealed record StoredLineup(string TeamName, string Formation, IReadOnlyList<Slot> Slots);
