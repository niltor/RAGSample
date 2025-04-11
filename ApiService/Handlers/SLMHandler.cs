using ApiService.Models;
using ApiService.Models.ProcessingDtos;
using ApiService.Models.SLMDtos;
using ApiService.Plugins;
using ApiService.Processing;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ApiService.Handlers;


#pragma warning disable SKEXP0001
public static class SLMHandler
{
    public static async Task<NerResultDto?> NerAsync(TextRequestDto dto, KnowledgeProcessing processing)
    {
        return await processing.NerAsync(dto.Text);
    }

    public static async Task<List<RelationDto>?> RelationExtractionAsync(TextRequestDto dto, KnowledgeProcessing processing)
    {
        return await processing.RelationExtractionAsync(dto.Text);
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
