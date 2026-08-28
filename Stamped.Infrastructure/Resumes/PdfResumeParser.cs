using Stamped.Core.Llm;
using Stamped.Core.Models;
using Stamped.Core.Resumes;
using System.Text.Json;
using UglyToad.PdfPig;

namespace Stamped.Infrastructure.Resumes
{
    public class PdfResumeParser : IResumeParser
    {
        private readonly ILlmProvider _llm;

        public PdfResumeParser(ILlmProvider llm) => _llm = llm;

        public async Task<Resume> ParseAsync(Stream pdfStream, string fileName, CancellationToken ct = default)
        {
            // Extract raw text from PDF
            using var ms = new MemoryStream();
            await pdfStream.CopyToAsync(ms, ct);
            ms.Position = 0;

            var text = new System.Text.StringBuilder();
            using (var doc = PdfDocument.Open(ms))
            {
                foreach (var page in doc.GetPages())
                    text.AppendLine(page.Text);
            }
            var rawText = text.ToString();

            // Ask the LLM to structure it
            var systemPrompt = "You extract structured data from resumes. Respond with ONLY valid JSON, no markdown fences, no commentary. Schema: {\"mostRecentRole\": string, \"yearsExperience\": number, \"skills\": string[]}";
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
                fields = new ParsedFields(); // fall back to empty rather than crash the pipeline
            }

            return new Resume
            {
                FileName = fileName,
                RawText = rawText,
                MostRecentRole = fields.mostRecentRole ?? "",
                YearsExperience = fields.yearsExperience,
                Skills = fields.skills ?? new List<string>(),
                ParsedAt = DateTime.UtcNow
            };
        }

        private class ParsedFields
        {
            public string? mostRecentRole { get; set; }
            public int yearsExperience { get; set; }
            public List<string>? skills { get; set; }
        }
    }
       
}
