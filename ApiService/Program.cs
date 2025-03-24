using ApiService;
using ApiService.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(op =>
{
    op.AddSimpleConsole();
});

// Add services to the container.

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services
    .AddOllamaChatCompletion("llama3.2:1b", new Uri("http://localhost:49394"))
    .AddOllamaTextEmbeddingGeneration("nomic-embed-text", new Uri("http://localhost:49394"))
    .AddQdrantVectorStore("localhost", 49383, apiKey: "U3gr80UJhy880CkFnxfh1f");



builder.Services.AddTransient((serviceProvider) =>
{
    return new Kernel(serviceProvider);
});

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapPost("/search", SearchAsync);

app.Run();


#pragma warning disable SKEXP0001
static async Task<string> SearchAsync(HttpContext context, QuestionModel question, ITextEmbeddingGenerationService embed,
    IVectorStore vectorStore)
{

    var collection = vectorStore.GetCollection<ulong, DocumentEmbedding>("doc-embedding");
    var searchEmbedding = await embed.GenerateEmbeddingAsync(question.Content);
    var searchResult = await collection.VectorizedSearchAsync(searchEmbedding);

    var firstResult = await searchResult.Results.FirstOrDefaultAsync();
    if (firstResult == null)
    {
        Console.WriteLine("result is null");
    }
    Console.WriteLine("item:{0},score:{1}", firstResult?.Record, firstResult?.Score);

    return "Hello World!";
}
