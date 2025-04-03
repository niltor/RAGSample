using ApiService.Models;
using ApiService.Plugins;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ApiService.Handlers;


#pragma warning disable SKEXP0001
public static class SLMHandler
{
    public static async Task<List<string>> NerAsync(string text, IChatCompletionService chat)
    {

        string prompt = $$"""
            {{text}}

            请分析该句子，识别其中的内容，以便进行分类，并返回json格式内容。

            要识别的类别为: 地点，技术名词，专有名词，其他名词

            返回的json格式如下：
            {
                "技术名词":["",""],
                "地名":["",""]，
                "专有名词":["",""],
                "其他名词":["",""]
            }
            """;

        var response = await chat.GetChatMessageContentsAsync(prompt, new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                { "max_token", 1000 }
            },

        });
        var result = response.FirstOrDefault()?.Content;

        return [result];

    }


    /// <summary>
    /// 搜索
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="question"></param>
    /// <param name="chat"></param>
    /// <param name="plugin"></param>
    /// <returns></returns>
    public static async Task SearchAsync(
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


}
