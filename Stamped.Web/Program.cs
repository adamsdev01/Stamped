using KristofferStrube.Blazor.WebMCP;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stamped.Core.CoverLetters;
using Stamped.Core.Llm;
using Stamped.Core.Jobs;
using Stamped.Core.Resumes;
using Stamped.Infrastructure.CoverLetters;
using Stamped.Infrastructure.Data;
using Stamped.Infrastructure.Llm;
using Stamped.Infrastructure.Jobs;
using Stamped.Infrastructure.Resumes;
using Stamped.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register db
builder.Services.AddDbContext<StampedDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StampedDb")));

#region Register the LLM provider/Services
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection("Llm"));
builder.Services.AddHttpClient<OllamaLlmProvider>();
builder.Services.AddHttpClient<AnthropicLlmProvider>();
builder.Services.AddHttpClient<OpenAiLlmProvider>();

if (builder.Configuration.GetValue<bool>("UseMockLlm"))
{
    builder.Services.AddScoped<ILlmProvider, MockLlmProvider>();
}
else
{
    builder.Services.AddScoped<ILlmProvider>(sp =>
    {
        var opts = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
        return opts.Provider switch
        {
            "Ollama" => sp.GetRequiredService<OllamaLlmProvider>(),
            "Anthropic" => sp.GetRequiredService<AnthropicLlmProvider>(),
            "OpenAI" => sp.GetRequiredService<OpenAiLlmProvider>(),
            _ => throw new NotSupportedException($"Unknown LLM provider: {opts.Provider}")
        };
    });
}
#endregion

#region Register the Adzuna Job API Source
builder.Services.Configure<JobOptions>(builder.Configuration.GetSection("Jobs"));
builder.Services.AddHttpClient<AdzunaJobSource>();

builder.Services.AddScoped<IJobSource>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<JobOptions>>().Value;
    return opts.Source switch
    {
        "Adzuna" => sp.GetRequiredService<AdzunaJobSource>(),
        "Folder" => new FolderJobSource(Path.Combine(builder.Environment.ContentRootPath, "Data", "jobs.json")),
        _ => throw new NotSupportedException($"Unknown job source: {opts.Source}")
    };
});
#endregion
builder.Services.AddScoped<IResumeParser, PdfResumeParser>();
builder.Services.AddScoped<IJobMatcher, LlmJobMatcher>();
builder.Services.AddScoped<ICoverLetterDrafter, LlmCoverLetterDrafter>();
builder.Services.AddScoped<IModelContextService, ModelContextService>();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StampedDbContext>();
    db.Database.Migrate();
}

app.Run();
