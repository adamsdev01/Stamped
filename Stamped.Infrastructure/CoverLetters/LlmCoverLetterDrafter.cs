using Stamped.Core.Llm;
using Stamped.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Stamped.Core.CoverLetters;

namespace Stamped.Infrastructure.CoverLetters
{
    public class LlmCoverLetterDrafter : ICoverLetterDrafter
    {
        private readonly ILlmProvider _llm;

        public LlmCoverLetterDrafter(ILlmProvider llm) => _llm = llm;

        public async Task<string> DraftAsync(Resume resume, JobPosting job, CancellationToken ct = default)
        {
            var systemPrompt = "You write concise, specific cover letters. No fluff, no generic phrases like 'I am excited to apply'. 3 short paragraphs max. Respond with ONLY the letter text, no markdown, no subject line.";

            var userPrompt = $"""
            CANDIDATE
            Most recent role: {resume.MostRecentRole}
            Years experience: {resume.YearsExperience}
            Skills: {string.Join(", ", resume.Skills)}

            JOB
            Title: {job.Title}
            Company: {job.Company}
            Description: {job.Description}
            """;

            return await _llm.CompleteAsync(systemPrompt, userPrompt, ct);
        }
    }
}
