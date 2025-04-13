using System.Text.Json;
using ApiService.Models;
using ApiService.Models.ProcessingDtos;
using ApiService.Processing;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
namespace ApiService.Services;

#pragma warning disable SKEXP0001

/// <summary>
/// 搜索服务
/// </summary>
public class SearchService
{
    private readonly ITextEmbeddingGenerationService _embed;
    private readonly IVectorStoreRecordCollection<Guid, DocumentEmbedding> _collection;
    private readonly KnowledgeProcessing _knowledgeProcessing;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        ITextEmbeddingGenerationService embed,
        IVectorStore _vectorStore,
        IConfiguration configuration,
        ILogger<SearchService> logger,
        KnowledgeProcessing knowledgeProcessing)
    {
        _embed = embed;
        _logger = logger;
        _collection = _vectorStore.GetCollection<Guid, DocumentEmbedding>(DocumentEmbedding.DocName);
        _knowledgeProcessing = knowledgeProcessing;

        var dataPath = configuration["Resources:DataPath"];
        if (dataPath == null)
        {
            return;
        }
        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        var dataFilePath = Path.Combine(dataPath, "knowledge.json");
        // 加载知识图谱
        if (File.Exists(dataFilePath))
        {
            var jsonContent = File.ReadAllText(dataFilePath);
            var knowledgeGraph = JsonSerializer.Deserialize<KnowledgeGraphDto>(jsonContent);

            var relationDtos = knowledgeGraph?.RelationDtos
                .Where(r => !string.IsNullOrWhiteSpace(r.Target) && !string.IsNullOrWhiteSpace(r.Subject))
                .ToList();

            relationDtos?.ForEach(GraphDataProcessing.AddRelation);
        }
    }


    /// <summary>
    /// 搜索
    /// </summary>
    /// <param name="searchContent"></param>
    /// <param name="searchCount"></param>
    /// <returns></returns>
    public async Task<List<string>> SearchAsync(string searchContent, int searchCount = 10)
    {
        var searchResults = new List<string>();
        // 向量搜索
        var vectorResults = await SearchVectorAsync(searchContent);

        if (vectorResults?.Length > 0)
        {
            searchResults.AddRange(vectorResults ?? []);
        }

        // 搜索知识图谱，再进行向量搜索
        var relationQueryList = await SearchGraphAsync(searchContent);

        if (relationQueryList?.Length > 0)
        {
            relationQueryList = [.. relationQueryList.Take(searchCount)];
            foreach (var item in relationQueryList)
            {
                _logger.LogInformation("🔍 Graph knowledge search: {item}", item);
                var vectorResult = await SearchVectorAsync(item);
                if (vectorResult != null && vectorResult.Length > 0)
                {
                    searchResults.AddRange(vectorResult);
                }
            }
        }
        else
        {
            _logger.LogWarning("⚠️ No Graph knowledge itmes");
        }
        return searchResults;
    }


    /// <summary>
    /// 向量搜索
    /// </summary>
    /// <param name="searchContent"></param>
    /// <returns></returns>
    public async Task<string[]?> SearchVectorAsync(string searchContent)
    {
        var results = Array.Empty<string>();
        var vector = await _embed.GenerateEmbeddingAsync(searchContent);
        var result = await _collection.VectorizedSearchAsync(vector, new VectorSearchOptions<DocumentEmbedding>
        {
            Top = 2,
        });
        if (await result.Results.AnyAsync())
        {
            await foreach (var item in result.Results)
            {
                if (!string.IsNullOrEmpty(item.Record.Content))
                {
                    results = [.. results, item.Record.Content];
                }
            }
        }
        return results;
    }

    /// <summary>
    /// 实体识别
    /// </summary>
    /// <param name="searchContent"></param>
    /// <returns></returns>
    public async Task<string[]?> SearchNerAsync(string searchContent)
    {
        var res = Array.Empty<string>();
        var data = await _knowledgeProcessing.NerAsync(searchContent);

        if (data != null)
        {
            if (data.TechNoun != null)
            {
                foreach (var item in data.TechNoun)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        res = [.. res, item];
                    }
                }
            }
            if (data.ProperNoun != null)
            {
                foreach (var item in data.ProperNoun)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        res = [.. res, item];
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(data.Summary))
            {
                res = [.. res, data.Summary];
            }
        }

        var logRes = string.Join(",", res);
        _logger.LogInformation("➡️ Ner result: {res}", logRes);
        return res;
    }

    /// <summary>
    /// 搜索知识图谱
    /// </summary>
    /// <param name="searchContent"></param>
    /// <returns></returns>
    public async Task<string[]?> SearchGraphAsync(string searchContent)
    {
        var results = Array.Empty<string>();
        var nerResults = await SearchNerAsync(searchContent);
        if (nerResults?.Length == 0)
        {
            return null;
        }

        var relationResult = new List<RelationDto>();
        foreach (var ner in nerResults!)
        {
            var relations = GraphDataProcessing.QueryRelations(ner, ner);
            if (relations != null)
            {
                relationResult.AddRange(relations);
            }
        }

        relationResult = [.. relationResult.Distinct()];
        if (relationResult.Count > 0)
        {
            foreach (var item in relationResult)
            {
                if (!string.IsNullOrWhiteSpace(item.Target))
                {
                    results = [.. results, $"{item.Subject} {item.Relation} {item.Target}"];
                }
            }
        }
        return results;
    }
}


