using Stamped.Core.Llm;
using Stamped.Core.Models;
using Stamped.Core.Resumes;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace Stamped.Infrastructure.Resumes;

public class PdfResumeParser : IResumeParser
{
    private readonly ILlmProvider _llm;

    public PdfResumeParser(ILlmProvider llm) => _llm = llm;

    public async Task<Resume> ParseAsync(Stream pdfStream, string fileName, CancellationToken ct = default)
    {
        // 1. Extract raw text from PDF
        using var ms = new MemoryStream();
        await pdfStream.CopyToAsync(ms, ct);
        ms.Position = 0;

        var text = new StringBuilder();
        using (var doc = PdfDocument.Open(ms))
        {
            foreach (var page in doc.GetPages())
                text.AppendLine(page.Text);
        }
        var rawText = text.ToString();

        // 2. Ask the LLM for role/skills AND raw date ranges (no arithmetic asked of it)
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var systemPrompt = """
            You extract structured data from resumes. Respond with ONLY valid JSON, no markdown fences, no commentary.

            For employmentRanges: list every job's start and end date as YYYY-MM. Use "present" (lowercase) 
            for current roles instead of an end date. If only a year is given, use January of that year 
            for a start date and December of that year for an end date. Do not calculate anything — 
            just list the raw ranges exactly as they appear.

            Schema: {"mostRecentRole": string, "skills": string[], "employmentRanges": [{"start": string, "end": string}]}
            """ + $"\n\nToday's date is {today}.";

        var raw = await _llm.CompleteAsync(systemPrompt, rawText, ct);
        var cleaned = raw.Trim().TrimStart('`').TrimEnd('`').Replace("json", "", StringComparison.OrdinalIgnoreCase).Trim();

        ParsedFields fields;
        try
        {
            fields = JsonSerializer.Deserialize<ParsedFields>(cleaned,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch (JsonException)
        {
            fields = new ParsedFields();
        }

        // 3. Compute years deterministically from the LLM's extracted ranges
        int yearsExperience = ComputeYearsFromRanges(fields.employmentRanges);

        // 4. Failsafe: LLM extraction gave us nothing usable — try a direct regex scan of the raw text
        if (yearsExperience < 0)
        {
            yearsExperience = ComputeYearsFromRegexScan(rawText);
        }

        // 5. Still nothing — leave it as an explicit "unknown" sentinel (-1) rather than guessing.
        //    The UI checks for this and prompts the user to fill it in manually instead of trusting a made-up number.

        return new Resume
        {
            FileName = fileName,
            RawText = rawText,
            MostRecentRole = fields.mostRecentRole ?? "",
            YearsExperience = yearsExperience,
            Skills = fields.skills ?? new List<string>(),
            ParsedAt = DateTime.UtcNow
        };
    }

    // Merges overlapping date ranges before summing, so concurrent roles don't double-count.
    private static int ComputeYearsFromRanges(List<DateRangeDto>? ranges)
    {
        if (ranges is null || ranges.Count == 0) return -1;

        var parsed = new List<(DateTime start, DateTime end)>();
        var today = DateTime.UtcNow;

        foreach (var r in ranges)
        {
            if (!TryParseYearMonth(r.start, out var start)) continue;

            DateTime end;
            if (string.IsNullOrWhiteSpace(r.end) || r.end.Trim().Equals("present", StringComparison.OrdinalIgnoreCase))
                end = today;
            else if (!TryParseYearMonth(r.end, out end))
                continue;

            if (end < start) continue; // malformed range, skip rather than let it corrupt the total
            parsed.Add((start, end));
        }

        if (parsed.Count == 0) return -1;

        parsed.Sort((a, b) => a.start.CompareTo(b.start));

        var merged = new List<(DateTime start, DateTime end)> { parsed[0] };
        foreach (var current in parsed.Skip(1))
        {
            var last = merged[^1];
            if (current.start <= last.end)
            {
                merged[^1] = (last.start, current.end > last.end ? current.end : last.end);
            }
            else
            {
                merged.Add(current);
            }
        }

        var totalDays = merged.Sum(r => (r.end - r.start).TotalDays);
        var years = (int)Math.Round(totalDays / 365.25);

        return years is >= 0 and <= 60 ? years : -1; // sanity bound; anything outside this is untrustworthy
    }

    private static bool TryParseYearMonth(string? value, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        // Accepts "2021-06", "2021-6", or just "2021"
        var parts = value.Trim().Split('-');
        if (parts.Length == 1 && int.TryParse(parts[0], out var yearOnly))
        {
            result = new DateTime(yearOnly, 1, 1);
            return true;
        }
        if (parts.Length == 2 && int.TryParse(parts[0], out var year) && int.TryParse(parts[1], out var month) && month is >= 1 and <= 12)
        {
            result = new DateTime(year, month, 1);
            return true;
        }
        return false;
    }

    // Failsafe #2: if the LLM's structured extraction didn't give us usable ranges at all,
    // scan the raw resume text directly for "YYYY - YYYY" / "YYYY - Present" style patterns
    // and take the span from the earliest year found to the latest (or today).
    private static int ComputeYearsFromRegexScan(string rawText)
    {
        var matches = Regex.Matches(rawText, @"(19|20)\d{2}");
        var years = matches.Select(m => int.Parse(m.Value))
                            .Where(y => y >= 1970 && y <= DateTime.UtcNow.Year)
                            .ToList();

        var hasPresent = Regex.IsMatch(rawText, @"\bpresent\b", RegexOptions.IgnoreCase);

        if (years.Count == 0) return -1;

        var earliest = years.Min();
        var latest = hasPresent ? DateTime.UtcNow.Year : years.Max();

        var span = latest - earliest;
        return span is >= 0 and <= 60 ? span : -1;
    }

    private class ParsedFields
    {
        public string? mostRecentRole { get; set; }
        public List<string>? skills { get; set; }
        public List<DateRangeDto>? employmentRanges { get; set; }
    }

    private class DateRangeDto
    {
        public string? start { get; set; }
        public string? end { get; set; }
    }
}