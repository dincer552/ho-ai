using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace HattrickAI.V5.Core;

public sealed class OpenAiService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public OpenAiService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_configuration["OPENAI_API_KEY"]);

    public string Model => _configuration["OPENAI_MODEL"]?.Trim() is { Length: > 0 } model
        ? model
        : "gpt-6-luna";

    public async Task<string> AnalyzeAsync(string question, string context, CancellationToken ct)
    {
        var apiKey = _configuration["OPENAI_API_KEY"]?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OPENAI_API_KEY Azure/GitHub deployment ortamında tanımlı değil.");

        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("AI sorusu boş olamaz.", nameof(question));

        var input = $"""
You are the AI analyst inside HattrickAI V5.
Use the supplied Hattrick data as factual context.
Do not invent player attributes or match data.
The deterministic V5 engines remain the source of truth for numerical ratings and lineup selection.
Your job is to explain, compare, identify trade-offs, and give actionable Hattrick analysis.

USER QUESTION:
{question.Trim()}

HATTRICKAI V5 CONTEXT:
{context}
""";

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = Model,
                input,
                store = false
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI API HTTP {(int)response.StatusCode}: {ExtractError(body)}");

        using var document = JsonDocument.Parse(body);
        if (document.RootElement.TryGetProperty("output_text", out var outputText))
            return outputText.GetString() ?? string.Empty;

        var pieces = new List<string>();
        if (document.RootElement.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                        pieces.Add(text.GetString() ?? string.Empty);
                }
            }
        }

        var result = string.Join("\n", pieces).Trim();
        return result.Length > 0 ? result : "OpenAI yanıtında okunabilir metin bulunamadı.";
    }

    private static string ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
                return message.GetString() ?? body;
        }
        catch { }

        return body.Length > 1000 ? body[..1000] : body;
    }
}
