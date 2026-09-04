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
            "mostRecentRole": "Accounting Manager",
            "skills": ["GAAP", "Financial Reporting", "Month-End Close", "Budgeting & Forecasting", "QuickBooks", "NetSuite", "Excel", "Team Leadership", "Audit Coordination"],
            "employmentRanges": [
                { "start": "2020-06", "end": "present" },
                { "start": "2016-09", "end": "2020-05" }
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
                ? $"Strong alignment between the candidate's month-end close and GAAP reporting experience and the {title} role at {company}."
                : score >= 65
                ? $"Reasonable fit for {title}, with some gaps against {company}'s stated requirements around forecasting or team leadership scope."
                : $"Partial match for {title}; candidate's accounting background only partially overlaps with {company}'s needs.";

            var json = System.Text.Json.JsonSerializer.Serialize(new { score, reasoning });
            return json;
        }

        private static string MockCoverLetter(string userPrompt)
        {
            var title = ExtractField(userPrompt, "Title") ?? "the position";
            var company = ExtractField(userPrompt, "Company") ?? "your company";
            var role = ExtractField(userPrompt, "Most recent role") ?? "my current role as Accounting Manager";

            return $"""
        I'm writing to apply for the {title} position at {company}. In my current role as {role}, I've led month-end close, GAAP-compliant financial reporting, and budgeting cycles, and I'm confident that background translates directly to the work your team is doing.

        What draws me to {company} specifically is the opportunity to apply that experience against real, growing financial operations rather than static reporting cycles. I've consistently focused on tightening close timelines, coordinating clean audits, and building forecasting processes the rest of the business can rely on.

        I'd welcome the chance to discuss how my background fits {title} in more detail. Thank you for your consideration.
        """;
        }

        private static string? ExtractField(string prompt, string label)
        {
            var match = System.Text.RegularExpressions.Regex.Match(prompt, $@"{System.Text.RegularExpressions.Regex.Escape(label)}:\s*(.+)");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}

