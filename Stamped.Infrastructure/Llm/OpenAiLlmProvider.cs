using Microsoft.Extensions.Options;
using Stamped.Core.Llm;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Stamped.Infrastructure.Llm
{
    public class OpenAiLlmProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly OpenAiOptions _opts;

        public OpenAiLlmProvider(HttpClient http, IOptions<LlmOptions> opts)
        {
            _http = http;
            _opts = opts.Value.OpenAI;
            _http.BaseAddress = new Uri("https://api.openai.com");
        }

        public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = JsonContent.Create(new
                {
                    model = _opts.Model,
                    max_tokens = 1024,
                    messages = new[]
                    {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opts.ApiKey);
            request.Content!.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _http.SendAsync(request, ct);

            // Read raw bytes rather than ReadFromJsonAsync/ReadAsStringAsync — avoids the same
            // charset-header parsing issue seen with Adzuna, and lets us surface the real error
            // body on failure instead of a generic status code exception.
            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var json = System.Text.Encoding.UTF8.GetString(bytes);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"OpenAI API error ({(int)response.StatusCode}): {json}");
            }

            var result = JsonSerializer.Deserialize<OpenAiResponse>(json);
            var message = result?.choices?.FirstOrDefault()?.message;
            return message?.content ?? "";
        }

        private class OpenAiResponse
        {
            public List<Choice>? choices { get; set; }
        }

        private class Choice
        {
            public Message? message { get; set; }
        }

        private class Message
        {
            public string? role { get; set; }
            public string? content { get; set; }
        }
    }
}
