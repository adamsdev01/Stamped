using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Core.Llm
{
    public class LlmOptions
    {
        public string Provider { get; set; } = "OpenAI";
        public OllamaOptions Ollama { get; set; } = new();
        public AnthropicOptions Anthropic { get; set; } = new();
        public OpenAiOptions OpenAI { get; set; } = new();
    }

    public class OllamaOptions { 
        public string BaseUrl { get; set; } = "";
        public string Model { get; set; } = ""; 
    }
    public class AnthropicOptions {
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = ""; 
    }
    public class OpenAiOptions { 
        public string ApiKey { get; set; } = ""; 
        public string Model { get; set; } = ""; 
    }
}
