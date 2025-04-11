using ApiService;
using ApiService.Handlers;
using ApiService.Models;
using ApiService.Plugins;
using ApiService.Processing;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;


var builder = WebApplication.CreateBuilder(args);


string qdrantKey = "U3gr80UJhy880CkFnxfh1f";

builder.Services.AddLogging(op =>
{
    op.AddSimpleConsole();
});

// Add services to the container.

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services
    .AddOllamaChatCompletion("phi4-mini", new Uri("http://localhost:49394"))
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
//builder.Services.AddHostedService<Worker>();
var app = builder.Build();


app.MapGet("/ttt", () =>
{
    return "Hello World!";
});
app.MapEndpoints();

app.Run();
