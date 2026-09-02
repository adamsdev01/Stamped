using Microsoft.Extensions.Options;
using Stamped.Core.Jobs;
using Stamped.Core.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Stamped.Infrastructure.Jobs
{
    public class AdzunaJobSource : IJobSource
    {
        private readonly HttpClient _http;
        private readonly AdzunaOptions _opts;

        public AdzunaJobSource(HttpClient http, IOptions<JobOptions> opts)
        {
            _http = http;
            _opts = opts.Value.Adzuna;
        }

        public async Task<List<JobPosting>> SearchAsync(string query, string location, CancellationToken ct = default)
        {
            var url = $"https://api.adzuna.com/v1/api/jobs/{_opts.Country}/search/1" +
                       $"?app_id={_opts.AppId}&app_key={_opts.AppKey}" +
                       $"&what={Uri.EscapeDataString(query)}" +
                       $"&where={Uri.EscapeDataString(location)}" +
                       $"&results_per_page=5";

            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            // Adzuna's Content-Type header includes a charset .NET's HttpContent parser rejects,
            // so read raw bytes and decode manually instead of ReadAsStringAsync/GetFromJsonAsync,
            // both of which inspect the header and throw before we ever see the body.
            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var json = Encoding.UTF8.GetString(bytes);

            var result = JsonSerializer.Deserialize<AdzunaResponse>(json);
            if (result?.results is null) return new List<JobPosting>();

            return result.results.Select(r => new JobPosting
            {
                Title = r.title ?? "",
                Company = r.company?.display_name ?? "",
                Location = r.location?.display_name ?? "",
                Description = r.description ?? "",
                Source = "adzuna",
                ExternalId = r.id ?? "",
                ExternalUrl = r.redirect_url ?? ""
            }).ToList();
        }

        private class AdzunaResponse { public List<AdzunaJob>? results { get; set; } }
        private class AdzunaJob
        {
            public string? id { get; set; }
            public string? title { get; set; }
            public string? description { get; set; }
            public string? redirect_url { get; set; }
            public AdzunaCompany? company { get; set; }
            public AdzunaLocation? location { get; set; }
        }
        private class AdzunaCompany { public string? display_name { get; set; } }
        private class AdzunaLocation { public string? display_name { get; set; } }
    }
}
