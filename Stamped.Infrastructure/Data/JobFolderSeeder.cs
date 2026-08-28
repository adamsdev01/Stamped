using Stamped.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Stamped.Infrastructure.Data
{
    public class JobFolderSeeder
    {
        public static async Task SeedAsync(StampedDbContext db, string jsonPath)
        {
            if (db.JobPostings.Any()) return;

            var json = await File.ReadAllTextAsync(jsonPath);
            var jobs = JsonSerializer.Deserialize<List<JobPosting>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            db.JobPostings.AddRange(jobs);
            await db.SaveChangesAsync();
        }
    }
}
