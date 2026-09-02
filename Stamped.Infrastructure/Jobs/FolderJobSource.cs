using Stamped.Core.Jobs;
using Stamped.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Stamped.Infrastructure.Jobs
{
    public class FolderJobSource : IJobSource
    {
        private readonly string _jsonPath;
        public FolderJobSource(string jsonPath) => _jsonPath = jsonPath;

        public async Task<List<JobPosting>> SearchAsync(string query, string location, CancellationToken ct = default)
        {
            var json = await File.ReadAllTextAsync(_jsonPath, ct);
            return JsonSerializer.Deserialize<List<JobPosting>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
    }
}
