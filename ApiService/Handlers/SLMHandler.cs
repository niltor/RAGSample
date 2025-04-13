using ApiService.Models;
using ApiService.Models.ProcessingDtos;
using ApiService.Models.SLMDtos;
using ApiService.Processing;
using ApiService.Services;
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
    /// <param name="search"></param>
    /// <returns></returns>
    public static async Task SearchAsync(
        HttpContext httpContext,
        QuestionModel question,
        IChatCompletionService chat,
        SearchService search
    )
    {
        httpContext.Response.ContentType = "text/plain;charset=utf-8";
        var searchResults = await search.SearchAsync(question.Content);
        string searchContent = string.Empty;

        if (searchResults.Count > 0)
        {
            foreach (var item in searchResults)
            {
                searchContent += item + Environment.NewLine;
            }
        }

        string systemPrompt = $@"
以下是从本地文档中搜索到的相关内容：
{searchContent}

仅根据上述搜索结果来回答用户的问题。如果没有足够的内容来回答，则提示没有找到相关信息。
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
