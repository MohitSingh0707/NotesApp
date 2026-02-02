using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AISummaryService.Services;

public class OpenAiSummaryService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OpenAiSummaryService(string apiKey, string model)
    {
        _model = model;
        _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> GenerateSummaryAsync(string content)
    {
        var request = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "Summarize the note in 2-3 sentences." },
                new { role = "user", content }
            }
        };

        var json = JsonSerializer.Serialize(request);

        var response = await _httpClient.PostAsync(
            // 🔥 GROQ ENDPOINT (NOT OPENAI)
            "https://api.groq.com/openai/v1/chat/completions",
            new StringContent(json, Encoding.UTF8, "application/json")
        );

        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception(responseJson);

        using var doc = JsonDocument.Parse(responseJson);

        return doc
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()!
            .Trim();
    }
}
