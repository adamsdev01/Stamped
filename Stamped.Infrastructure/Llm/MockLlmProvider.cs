using Stamped.Core.Llm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stamped.Infrastructure.Llm
{

    /// <summary>
    /// Canned-response stand-in for ILlmProvider, used during judging to avoid burning
    /// trial API credits. Detects which flow is calling (resume parse, job scoring,
    /// cover letter) based on the system prompt, and returns a plausible, varied
    /// response for each so the WebMCP tool chain still demonstrates real behavior.
    /// </summary>
    public class MockLlmProvider : ILlmProvider
    {
        public Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            if (systemPrompt.Contains("employmentRanges", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(MockResumeParse());

            if (systemPrompt.Contains("scores how well a candidate fits", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(MockJobScore(userPrompt));

            if (systemPrompt.Contains("cover letters", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(MockCoverLetter(userPrompt));

            // Fallback — shouldn't normally hit this
            return Task.FromResult("{}");
        }

        private static string MockResumeParse()
        {
            return """
            {
                "mostRecentRole": "Senior Software Engineer",
                "skills": ["C#", ".NET", "SQL Server", "Azure", "React", "REST APIs"],
                "employmentRanges": [
                    { "start": "2021-04", "end": "present" },
                    { "start": "2018-01", "end": "2021-03" }
                ]
            }
            """;
        }

        private static string MockJobScore(string userPrompt)
        {
            var title = ExtractField(userPrompt, "Title") ?? "this role";
            var company = ExtractField(userPrompt, "Company") ?? "the company";

            // Deterministic but varied score per job, based on title hash,
            // so different postings don't all return identical numbers.
            var score = 55 + (Math.Abs(title.GetHashCode()) % 41); // range: 55–95

            var reasoning = score >= 80
                ? $"Strong alignment between the candidate's experience and the {title} role at {company}."
                : score >= 65
                ? $"Reasonable fit for {title}, with some gaps against {company}'s stated requirements."
                : $"Partial match for {title}; candidate's background only partially overlaps with {company}'s needs.";

            var json = JsonSerializer.Serialize(new { score, reasoning });
            return json;
        }

        private static string MockCoverLetter(string userPrompt)
        {
            var title = ExtractField(userPrompt, "Title") ?? "the position";
            var company = ExtractField(userPrompt, "Company") ?? "your company";
            var role = ExtractField(userPrompt, "Most recent role") ?? "my current role";

            return $"""
            I'm writing to apply for the {title} position at {company}. In my current role as {role}, I've built hands-on experience across the core skills this position calls for, and I'm confident that background translates directly to the work your team is doing.

            What draws me to {company} specifically is the opportunity to apply that experience against real, production-scale problems rather than isolated exercises. I've consistently focused on writing maintainable code, collaborating closely with cross-functional teams, and shipping features that hold up under real usage.

            I'd welcome the chance to discuss how my background fits {title} in more detail. Thank you for your consideration.
            """;
        }

        private static string? ExtractField(string prompt, string label)
        {
            var match = Regex.Match(prompt, $@"{Regex.Escape(label)}:\s*(.+)");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}

