using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Core.Llm
{
    /// <summary>
    /// Represents a provider for interacting with a large language model (LLM).
    /// </summary>
    public interface ILlmProvider
    {
        Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
    }
}
