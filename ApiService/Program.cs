using ApiService;
using ApiService.Plugins;
using ApiService.Processing;
using ApiService.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(op =>
{
    op.AddSimpleConsole();
});


string? qdrantKey = builder.Configuration["Secret:QdrantKey"];
#pragma warning disable SKEXP0070
builder.Services
    .AddOllamaChatCompletion("phi4-mini", new HttpClient
    {
        BaseAddress = new Uri("http://localhost:49394"),
        Timeout = TimeSpan.FromSeconds(120)
    })
    .AddOllamaTextEmbeddingGeneration("nomic-embed-text", new Uri("http://localhost:49394"))
    .AddQdrantVectorStore("localhost", 49383, apiKey: qdrantKey);

builder.Services.AddScoped<InternalDocumentsPlugin>();

builder.Services.AddTransient((serviceProvider) =>
{
    var kernelBuilder = Kernel.CreateBuilder();
    var searchPlugin = serviceProvider.GetRequiredService<InternalDocumentsPlugin>();
    var kernel = kernelBuilder.Build();

    kernel.ImportPluginFromObject(searchPlugin, "Search");
    return kernel;
});

builder.Services.AddScoped<KnowledgeProcessing>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddHostedService<Worker>();
var app = builder.Build();

app.MapEndpoints();
app.Run();
