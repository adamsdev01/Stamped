using Microsoft.Extensions.Options;
using Stamped.Core.Llm;
using Stamped.Infrastructure.Llm;
using Stamped.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection("Llm"));
builder.Services.AddHttpClient<OllamaLlmProvider>();

builder.Services.AddScoped<ILlmProvider>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
    return opts.Provider switch
    {
        "Ollama" => sp.GetRequiredService<OllamaLlmProvider>(),
        // "Anthropic" => sp.GetRequiredService<AnthropicLlmProvider>(), 
        // "OpenAI"    => sp.GetRequiredService<OpenAiLlmProvider>(), 
        _ => throw new NotSupportedException($"Unknown LLM provider: {opts.Provider}")
    };
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
