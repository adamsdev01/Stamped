using Stamped.Core.Llm;
using Stamped.Core.Matching;
using Stamped.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Stamped.Infrastructure.Matching
{
    /// <summary>
    /// One Llm call per job posting, with the resume and job description as input. 
    /// The LLM will return a score and a list of reasons for the match.
    /// </summary>
    public class LlmJobMatcher : IJobMatcher
    {
        private readonly ILlmProvider _llm;

        public LlmJobMatcher(ILlmProvider llm) => _llm = llm;

        public async Task<List<JobMatch>> MatchAsync(Resume resume, List<JobPosting> jobs, CancellationToken ct = default)
        {
            var results = new List<JobMatch>();
            var systemPrompt = "You score how well a candidate fits a job. Respond with ONLY valid JSON, no markdown fences, no commentary. Schema: {\"score\": number (0-100), \"reasoning\": string (one sentence)}";

            foreach (var job in jobs)
            {
                var userPrompt = $"""
                CANDIDATE
                Most recent role: {resume.MostRecentRole}
                Years of experience: {resume.YearsExperience}
                Skills: {string.Join(", ", resume.Skills)}

                JOB
                Title: {job.Title}
                Company: {job.Company}
                Description: {job.Description}
                """;

                var raw = await _llm.CompleteAsync(systemPrompt, userPrompt, ct);
                var cleaned = raw.Trim().TrimStart('`').TrimEnd('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();

                ScoreResult parsed;
                try
                {
                    parsed = JsonSerializer.Deserialize<ScoreResult>(cleaned,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                }
                catch (JsonException)
                {
                    parsed = new ScoreResult { score = 0, reasoning = "Could not score this match." };
                }

                results.Add(new JobMatch
                {
                    ResumeId = resume.Id,
                    JobPostingId = job.Id,
                    MatchScore = Math.Clamp(parsed.score, 0, 100),
                    Reasoning = parsed.reasoning ?? ""
                });
            }

            return results.OrderByDescending(m => m.MatchScore).ToList();
        }

        private class ScoreResult
        {
            public int score { get; set; }
            public string? reasoning { get; set; }
        }
    }
}
