using Stamped.Core.Llm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Stamped.Infrastructure.Llm
{
    public class OllamaLlmProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly OllamaOptions _opts;

        public OllamaLlmProvider(HttpClient http, IOptions<LlmOptions> opts)
        {
            _http = http;
            _opts = opts.Value.Ollama;
            _http.BaseAddress = new Uri(_opts.BaseUrl);
        }

        public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync("/api/generate", new
            {
                model = _opts.Model,
                prompt = $"{systemPrompt}\n\n{userPrompt}",
                stream = false
            }, ct);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct);
            return result?.response ?? "";
        }

        private record OllamaResponse(string response);
    }
}
