using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using HattrickAI.V5.Core;

const string EmbeddedConsumerKey = "4CzYYAnSg7SSHkQyDVMLIV";

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "hattrickai.v5";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.IdleTimeout = TimeSpan.FromHours(8);
});
builder.Services.AddScoped<ChppV5>(sp =>
{
    var configuredKey = builder.Configuration["CHPP_CONSUMER_KEY"];
    var key = string.IsNullOrWhiteSpace(configuredKey) ? EmbeddedConsumerKey : configuredKey.Trim();
    var secret = builder.Configuration["CHPP_CONSUMER_SECRET"]?.Trim() ?? string.Empty;
    return new ChppV5(new Credentials(key, secret), sp.GetRequiredService<IHttpContextAccessor>());
});
builder.Services.AddScoped<AnalysisService>();
builder.Services.AddScoped<ReferenceMatchService>();
builder.Services.AddScoped<ChppMatchOrderReadService>();

var app = builder.Build();
var portText = Environment.GetEnvironmentVariable("PORT");
var port = int.TryParse(portText, out var parsed) ? parsed : 10000;
app.Urls.Add($"http://0.0.0.0:{port}");
app.UseSession();
app.UseDefaultFiles();
app.UseStaticFiles();

var build = Environment.GetEnvironmentVariable("V5_BUILD")
    ?? Environment.GetEnvironmentVariable("BUILD_SHA")
    ?? Environment.GetEnvironmentVariable("GITHUB_SHA")
    ?? "dev";
if (build.Length > 7) build = build[..7];

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "HattrickAI V5", build }));
app.MapGet("/api/v5/build", () => Results.Ok(new { build }));
app.MapGet("/api/v5/status", (ChppV5 chpp) => Results.Ok(new { connected = chpp.Connected, configured = !string.IsNullOrWhiteSpace(builder.Configuration["CHPP_CONSUMER_SECRET"]), canSetMatchOrder = chpp.CanSetMatchOrder }));
app.MapGet("/api/v5/motor-logs", (HttpContext http) =>
{
    var log = MotorRunLogStore.GetLatest(http.Session.Id);
    return log is null ? Results.Ok(new { available = false }) : Results.Ok(new { available = true, log });
});
app.MapGet("/api/v5/motor-database/latest", () =>
    MotorResultArchive.TryGetLatest(out var json)
        ? Results.Text(json, "application/json; charset=utf-8")
        : Results.NotFound(new { available = false, message = "Henüz kaydedilmiş motor JSON snapshot yok." }));
app.MapGet("/api/v5/motor-database/list", () =>
    Results.Ok(new { available = Directory.Exists(MotorResultArchive.RootDirectory), entries = MotorResultArchive.List() }));
app.MapGet("/api/v5/motor-database/{runId}", (string runId) =>
    MotorResultArchive.TryGetByRunId(runId, out var json)
        ? Results.Text(json, "application/json; charset=utf-8")
        : Results.NotFound(new { available = false, message = "İstenen runId için motor JSON snapshot bulunamadı.", runId }));

app.MapPost("/api/v5/questionnaire", (HttpContext http, QuestionnaireRequest request) =>
{
    if (!Enum.TryParse<CoachStyle>(request.CoachStyle, true, out var coach)) return Results.BadRequest(new { message = "Teknik direktör seçimi geçersiz." });
    if (!Enum.TryParse<TeamSpiritLevel>(request.TeamSpirit, true, out var spirit)) return Results.BadRequest(new { message = "Takım ruhu seçimi geçersiz." });
    if (!Enum.TryParse<TeamAttitude>(request.MatchImportance, true, out var attitude)) return Results.BadRequest(new { message = "Maç önemi geçersiz." });
    http.Session.SetString("v5.coach", coach.ToString());
    http.Session.SetString("v5.spirit", spirit.ToString());
    http.Session.SetString("v5.attitude", attitude.ToString());
    return Results.Ok(new { ok = true });
});

app.MapGet("/api/v5/questionnaire", (HttpContext http) =>
{
    var questionnaire = new MatchQuestionnaire(
        Enum.TryParse<CoachStyle>(http.Session.GetString("v5.coach"), true, out var coach) ? coach : CoachStyle.Neutral,
        Enum.TryParse<TeamSpiritLevel>(http.Session.GetString("v5.spirit"), true, out var spirit) ? spirit : TeamSpiritLevel.Composed,
        Enum.TryParse<TeamAttitude>(http.Session.GetString("v5.attitude"), true, out var attitude) ? attitude : TeamAttitude.Normal);
    return Results.Ok(questionnaire);
});

app.MapGet("/api/v5/analysis", async (HttpContext http, AnalysisService service, ChppV5 chpp, CancellationToken ct) =>
{
    if (!chpp.Connected) return Results.Unauthorized();
    var runId = MotorRunLogStore.Start(http.Session.Id);
    try
    {
        using var logScope = MotorRunLogContext.Push(runId);
        MotorRunLogStore.StartMotor(runId, "M3", "CHPP verileri hazırlanıyor");
        var questionnaire = new MatchQuestionnaire(
            Enum.TryParse<CoachStyle>(http.Session.GetString("v5.coach"), true, out var coach) ? coach : CoachStyle.Neutral,
            Enum.TryParse<TeamSpiritLevel>(http.Session.GetString("v5.spirit"), true, out var spirit) ? spirit : TeamSpiritLevel.Composed,
            Enum.TryParse<TeamAttitude>(http.Session.GetString("v5.attitude"), true, out var attitude) ? attitude : TeamAttitude.Normal);
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

// TEAM_PLAYER_CHPP_JSON_EXPORT_V1: lightweight DEV data collection endpoint.
app.MapGet("/api/v5/team-player-export", async (ChppV5 chpp, CancellationToken ct) =>
{
    if (!chpp.Connected) return Results.Unauthorized();
    try { return Results.Ok(await new TeamPlayerChppExportService(chpp).ExportAsync(build, ct)); }
    catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
    catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 502); }
});

app.MapGet("/api/v5/offline-export", async (HttpContext http, AnalysisService service, ChppV5 chpp, CancellationToken ct) =>
{
    if (!chpp.Connected) return Results.Unauthorized();
    try
    {
        var questionnaire = new MatchQuestionnaire(
            Enum.TryParse<CoachStyle>(http.Session.GetString("v5.coach"), true, out var coach) ? coach : CoachStyle.Neutral,
            Enum.TryParse<TeamSpiritLevel>(http.Session.GetString("v5.spirit"), true, out var spirit) ? spirit : TeamSpiritLevel.Composed,
            Enum.TryParse<TeamAttitude>(http.Session.GetString("v5.attitude"), true, out var attitude) ? attitude : TeamAttitude.Normal);
        var exporter = new OfflineExportService(chpp, service);
        return Results.Ok(await exporter.ExportAsync(build, questionnaire, ct));
    }
    catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
    catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 502); }
});

app.MapGet("/api/v5/reference-match", async (ReferenceMatchService service, ChppV5 chpp, CancellationToken ct) =>
{
    if (!chpp.Connected) return Results.Unauthorized();
    try { return Results.Ok(await service.GetAsync(ct)); }
    catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 502); }
});

app.MapGet("/api/v5/chpp-export/target", async (ChppMatchOrderReadService reader, ChppV5 chpp, CancellationToken ct) =>
{
    if (!chpp.Connected) return Results.Unauthorized();
    try
    {
        var target = await reader.ReadUpcomingAsync(ct);
        return Results.Ok(new
        {
            target.MatchId,
            target.TeamId,
            target.MatchDate,
            target.HomeTeamId,
            target.AwayTeamId,
            target.HomeTeamName,
            target.AwayTeamName,
            target.OrdersSet,
            canWrite = chpp.CanSetMatchOrder,
            writeEnabled = string.Equals(builder.Configuration["CHPP_MATCHORDER_WRITE_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        });
    }
    catch (UnauthorizedAccessException ex) { return Results.Unauthorized(); }
    catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 502); }
});

app.MapPost("/api/v5/chpp-export", async (ChppMatchOrderExportRequest request, ChppMatchOrderReadService reader, ChppV5 chpp, CancellationToken ct) =>
{
    if (!chpp.Connected) return Results.Unauthorized();
    if (!string.Equals(builder.Configuration["CHPP_MATCHORDER_WRITE_ENABLED"], "true", StringComparison.OrdinalIgnoreCase))
        return Results.Problem("CHPP match order canlı aktarımı yapılandırma ile kapalı. CHPP_MATCHORDER_WRITE_ENABLED=true olmadan gerçek write yapılmaz.", statusCode: 403);
    if (!chpp.CanSetMatchOrder)
        return Results.Problem("CHPP set_matchorder yetkisi yok.", statusCode: 403);
    try
    {
        if (!Enum.TryParse<TeamTactic>(request.Tactic, true, out var tactic))
            return Results.BadRequest(new { message = $"Desteklenmeyen Hattrick taktiği: {request.Tactic}" });
        if (!Enum.TryParse<TeamAttitude>(request.Attitude, true, out var attitude) || attitude == TeamAttitude.Auto)
            return Results.BadRequest(new { message = "Desteklenmeyen maç tutumu." });
        if (request.Slots is null || request.Slots.Count != 11)
            return Results.BadRequest(new { message = "İlk 11 tam olarak 11 oyuncu içermeli." });
        if (request.Slots.Any(x => x.PlayerId <= 0 || string.IsNullOrWhiteSpace(x.Code)))
            return Results.BadRequest(new { message = "İlk 11 oyuncu/pozisyon verisi geçersiz." });
        var orderValues = request.Slots.Select(x => x.Order).ToArray();
        if (orderValues.Any(x => !Enum.IsDefined(typeof(PlayerOrder), x)))
            return Results.BadRequest(new { message = "Oyuncu davranış kodu geçersiz." });

        var slots = request.Slots.Select(x => new Slot(x.Code, x.Code, string.Empty, null, x.PlayerId, 0, 0, 0, (PlayerOrder)x.Order)).ToList();
        var lineup = new Lineup("CHPP", request.Formation ?? string.Empty, slots);
        var payload = ChppMatchOrderPayloadBuilder.Build(lineup, request.Bench, tactic, attitude);
        var target = await reader.ReadUpcomingAsync(ct);
        var result = await new ChppMatchOrderWriteService(chpp).WriteAndVerifyAsync(payload, target, null, ct);
        if (!result.Verified)
            return Results.Problem(result.Reason, statusCode: 502);
        return Results.Ok(new
        {
            ok = true,
            verified = true,
            result.Write.MatchId,
            result.Write.TeamId,
            result.Write.OrdersSet,
            result.Write.Tactic,
            result.Reason
        });
    }
    catch (UnauthorizedAccessException ex) { return Results.Problem(ex.Message, statusCode: 403); }
    catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 502); }
});

app.MapGet("/auth/chpp/start", async (HttpContext http, ChppV5 chpp, CancellationToken ct) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(builder.Configuration["CHPP_CONSUMER_SECRET"])) return Results.Redirect("/?error=" + Uri.EscapeDataString("CHPP_CONSUMER_SECRET Azure Environment Variables içinde tanımlı değil."));
        var proto = http.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? http.Request.Scheme;
        var callback = $"{proto}://{http.Request.Host}/auth/chpp/callback";
        return Results.Redirect(await chpp.StartAsync(callback, ct));
    }
    catch (Exception ex) { return Results.Redirect("/?error=" + Uri.EscapeDataString(ex.Message)); }
});

app.MapGet("/auth/chpp/callback", async (ChppV5 chpp, string? oauth_token, string? oauth_verifier, CancellationToken ct) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(oauth_token) || string.IsNullOrWhiteSpace(oauth_verifier)) return Results.Redirect("/?error=" + Uri.EscapeDataString("CHPP callback eksik parametre ile geldi."));
        await chpp.CompleteAsync(oauth_token, oauth_verifier, ct);
        return Results.Redirect("/");
    }
    catch (Exception ex) { return Results.Redirect("/?error=" + Uri.EscapeDataString(ex.Message)); }
});
app.MapPost("/auth/chpp/logout", (ChppV5 chpp) => { chpp.Disconnect(); return Results.Ok(new { ok = true }); });
app.Run();

public sealed record QuestionnaireRequest(string CoachStyle, string TeamSpirit, string MatchImportance);