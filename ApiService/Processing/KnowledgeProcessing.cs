using ApiService.Models.ProcessingDtos;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace ApiService.Processing;

/// <summary>
/// 知识内容处理
/// </summary>
public class KnowledgeProcessing
{
    private readonly IChatCompletionService _chat;
    private readonly ILogger<KnowledgeProcessing> _logger;

    public KnowledgeProcessing(IChatCompletionService chat, ILogger<KnowledgeProcessing> logger)
    {
        _chat = chat;
        _logger = logger;
    }

    /// <summary>
    /// 实体识别
    /// </summary>
    /// <param name="text"></param>
    /// <param name="chat"></param>
    /// <returns></returns>
    public async Task<NerResultDto?> NerAsync(string text)
    {
        string prompt = $$"""
            {{text}}
            请分析该句子，识别其中的内容，以便进行分类
            要识别的类别为: 技术名词，专有名词，摘要；识别的内容保持原语言表示

            要求返回的json格式如下：{
                "TechNoun":["",""],
                "ProperNoun":["",""]，
                "Summary":""
            }

            返回内容严格遵循json格式，是合法的JSON，不能有其他内容。请注意，json的键名必须与上面一致，值可以是空数组或空字符串。
            """;
        var response = await _chat.GetChatMessageContentsAsync(prompt);

        var result = response.FirstOrDefault()?.Content;

        if (result != null)
        {
            result = MarkdownProcessing.RemoveCodeBlock(result, "json");
            try
            {
                return JsonSerializer.Deserialize<NerResultDto>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NerAsync error: {result}", result);
            }
        }
        return null;
    }

    /// <summary>
    /// 关系抽取
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public async Task<List<RelationDto>?> RelationExtractionAsync(string text)
    {
        string prompt = $$"""
            请分析以下文本内容，识别其中的技术名词、专有名词和概念等，并提取它们之间的关系。

            如果文本中包含多个关系，请提取所有关系。如"属于","包含","依赖于"等关系
            
            将结果以 JSON 数组的形式输出，每个元素包含 "Subject" (实体1), "Relation" (关系), 和 "Target" (实体2) 字段。如果没有对应的Target，则不要返回该元素。

            返回的json格式如下：[
            {"Subject":"","Relation":"","Target":""}
            ]

            返回内容严格遵循json格式，是合法的JSON，不能有其他内容。请注意，json的键名必须与上面一致，值可以是空数组或空字符串。

            文本内容：
            {{text}}
            """;
        var response = await _chat.GetChatMessageContentsAsync(prompt);
        var result = response.FirstOrDefault()?.Content;
        if (result != null)
        {
            result = MarkdownProcessing.RemoveCodeBlock(result, "json");
            try
            {
                return JsonSerializer.Deserialize<List<RelationDto>>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NerAsync error: {result}", result);
            }
        }
        return null;
    }
}
