using Stamped.Core.Llm;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stamped.Infrastructure.Llm;

public class AnthropicLlmProvider : ILlmProvider
{
    private readonly HttpClient _http;
    private readonly AnthropicOptions _opts;

    public AnthropicLlmProvider(HttpClient http, IOptions<LlmOptions> opts)
    {
        _http = http;
        _opts = opts.Value.Anthropic;
        _http.BaseAddress = new Uri("https://api.anthropic.com");
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = JsonContent.Create(new
            {
                model = _opts.Model,
                max_tokens = 1024,
                system = systemPrompt,
                messages = new[]
                {
                    new { role = "user", content = userPrompt }
                }
            })
        };

        request.Headers.Add("x-api-key", _opts.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content!.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _http.SendAsync(request, ct);

        // Read raw bytes rather than ReadFromJsonAsync/ReadAsStringAsync — avoids the same
        // charset-header parsing issue seen with Adzuna, and lets us surface the real error
        // body on failure instead of a generic status code exception.
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var json = System.Text.Encoding.UTF8.GetString(bytes);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Anthropic API error ({(int)response.StatusCode}): {json}");
        }

        var result = JsonSerializer.Deserialize<AnthropicResponse>(json);
        var textBlock = result?.content?.FirstOrDefault(c => c.type == "text");
        return textBlock?.text ?? "";
    }

    private class AnthropicResponse
    {
        public List<ContentBlock>? content { get; set; }
    }

    private class ContentBlock
    {
        public string? type { get; set; }
        public string? text { get; set; }
    }
}