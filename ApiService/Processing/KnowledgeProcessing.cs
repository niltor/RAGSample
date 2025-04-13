using System.Text.Json;
using ApiService.Models.ProcessingDtos;
using Microsoft.SemanticKernel.ChatCompletion;

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
            要识别的类别为: 技术名词，专有名词，摘要；识别的内容保持原语言表示，其中：

            技术名词对应TechNoun,是一个字符串数组;
            专有名词对应ProperNoun,是一个字符串数组;
            摘要对应Summary,是一个字符串;

            要求返回的json格式如下：{
                "TechNoun":["",""],
                "ProperNoun":["",""]，
                "Summary":""
            }

            如果无法处理该文本，请返回空对象{}
            
            返回内容严格遵循上述要求的json格式，包括值类型，要求是合法的JSON，即要对一些特殊字符进行转义；仅返回Json格式内容本身，不要有其他内容。
            """;

        try
        {
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
                    _logger.LogError(ex, "JsonDeserialize error: {result}", result);
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NerAsync error: {text}", text);
            return null;
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
            注意，如果是代码片段，则忽略，不要识别代码中的内容。

            如果文本中包含多个关系，请提取所有关系。如"属于","包含","依赖于"等关系
            
            将结果以 JSON 数组的形式输出，每个元素包含:
            Subject (实体1)，字符串类型;
            Relation (关系), 字符串类型；
            Target (实体2)，字符串类型；

            返回的json格式示例：[
            {"Subject":"","Relation":"","Target":""}
            ]

            返回内容严格遵循上述要求的json格式，包括值类型，要求是合法的JSON，即要对一些特殊字符进行转义；仅返回Json格式内容本身，不要有其他内容。

            如果无法处理该文本，请返回空数组[]。

            文本内容：
            {{text}}
            """;

        try
        {
            var response = await _chat.GetChatMessageContentsAsync(prompt);
            var result = response.FirstOrDefault()?.Content;
            if (result != null)
            {
                result = MarkdownProcessing.RemoveCodeBlock(result, "json");
                try
                {
                    return JsonSerializer.Deserialize<List<RelationDto>>(result);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Json Deserialize error: {result}", result);
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RelationExtractionAsync error: {text}", text);
            return null;
        }

        return null;
    }
}
