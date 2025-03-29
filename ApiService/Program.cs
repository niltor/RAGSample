using ApiService;
using ApiService.Models;
using ApiService.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;


var builder = WebApplication.CreateBuilder(args);


string qdrantKey = "TNQ1X75CZWrMx9ehseu2q0";

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

    string systemPrompt = @"
你是一个通过检索回答问题的助手。
使用简单的markdown格式来回复内容。

使用检索工具和查找的相关信息时，需要在返回结果的最后添加:

 <citation filename='string' page_number='number'>exact quote here</citation>

quote最大6个字符，从搜索结果中按顺序获取。

不要提及引用的存在；只需在末尾发出这些标签，周围没有文字。

";

    ChatHistory history = [];
    history.AddUserMessage($"query: {question.Content}");
    history.AddSystemMessage(systemPrompt);

    var searchResult = await plugin.SearchAsync(question.Content);

    Console.WriteLine($"🤟 {searchResult}");

    history.AddAssistantMessage($"search result: {systemPrompt}");

    var response = await chat.GetChatMessageContentsAsync(history);

    foreach (var item in response)
    {
        Console.WriteLine(item.Content);
        await httpContext.Response.WriteAsync(item.Content ?? "");
    }

    await httpContext.Response.CompleteAsync();
}
