using ApiService;
using ApiService.Models;
using ApiService.Plugins;
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

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapPost("/search", SearchAsync);

app.Run();


#pragma warning disable SKEXP0001
static async Task SearchAsync(
    HttpContext httpContext,
    QuestionModel question,
    IChatCompletionService chat,
    InternalDocumentsPlugin plugin
    )
{

    httpContext.Response.ContentType = "text/plain;charset=utf-8";
    var searchResult = await plugin.SearchAsync(question.Content);
    Console.WriteLine($"🤟 {searchResult}");
    string systemPrompt = $@"
以下是从本地文档中搜索到的相关内容：
{searchResult}

请根据上述内容来回答用户的问题。如果文档中信息不足，请说明缺失的部分。

";

    ChatHistory history = [];
    history.AddUserMessage(question.Content);
    history.AddSystemMessage(systemPrompt);

    var response = await chat.GetChatMessageContentsAsync(history);

    foreach (var item in response)
    {
        Console.WriteLine(item.Content);
        await httpContext.Response.WriteAsync(item.Content ?? "");
    }

    await httpContext.Response.CompleteAsync();
}
