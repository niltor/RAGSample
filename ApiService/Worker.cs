using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiService.Models;
using ApiService.Models.ProcessingDtos;
using ApiService.Processing;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;

namespace ApiService;
/// <summary>
/// embed worker
/// </summary>
#pragma warning disable SKEXP0001
public class Worker(
    ILogger<Worker> _logger,
    ITextEmbeddingGenerationService _embed,
    IVectorStore _vectorStore,
    IConfiguration configuration,
    IServiceProvider serviceProvider
    ) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var searchPath = configuration["Resources:ContentPath"];

        if (string.IsNullOrWhiteSpace(searchPath))
        {
            _logger.LogError("Search path is not configured.");
            return;
        }

        _logger.LogInformation("✨ start embed content from {path}", searchPath);
        var collection = _vectorStore.GetCollection<Guid, DocumentEmbedding>(DocumentEmbedding.DocName);
        await collection.CreateCollectionIfNotExistsAsync();
        var mdFiles = Directory.GetFiles(searchPath, "*.md", SearchOption.AllDirectories);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 4, // Specify the desired number of threads
            CancellationToken = cancellationToken
        };
        await Parallel.ForEachAsync(mdFiles, parallelOptions, async (mdFile, ct) =>
        {
            var mdContent = File.ReadAllText(mdFile);
            await EmbedAndSaveAsync(collection, mdContent);
            _logger.LogInformation("🆕 [{number}] Embedded!", mdFile);
        });
        _logger.LogInformation("✅ All files embedded!");

        // 知识图谱
        _logger.LogInformation("✨ start graph knowledge from {path}", searchPath);
        var dataPath = configuration["Resources:DataPath"];
        if (dataPath == null)
        {
            _logger.LogError("dataPath is null");
            return;
        }
        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        var dataFilePath = Path.Combine(dataPath, "knowledge.json");
        var knowledgeGraph = new KnowledgeGraphDto();
        if (File.Exists(dataFilePath))
        {
            var jsonContent = await File.ReadAllTextAsync(dataFilePath);
            knowledgeGraph = JsonSerializer.Deserialize<KnowledgeGraphDto>(jsonContent);
        }
        await Parallel.ForEachAsync(mdFiles, parallelOptions, async (mdFile, ct) =>
        {
            var mdContent = File.ReadAllText(mdFile);
            await GraphKnowledgeAsync(mdContent, knowledgeGraph);
            _logger.LogInformation("🆕 Got Knowledge:[{number}]", mdFile);
        });
        _logger.LogInformation("✅ All files graph knowledge!");

        // save knowledge graph
        var json = JsonSerializer.Serialize(knowledgeGraph);
        await File.WriteAllTextAsync(dataFilePath, json);
        _logger.LogInformation("✅ Save knowledge graph to {path}", dataFilePath);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 向量化存储
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    private async Task EmbedAndSaveAsync(IVectorStoreRecordCollection<Guid, DocumentEmbedding> collection, string content)
    {
        var paragraph = MarkdownProcessing.SplitText(content);

        var hash = MD5.HashData(Encoding.UTF8.GetBytes(content));
        var md5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        ReadOnlyMemory<float> zeroVector = new float[768];

        var searchResult = await collection.VectorizedSearchAsync(zeroVector, new VectorSearchOptions<DocumentEmbedding>
        {
            Filter = d => d.Hash == md5,
            Top = 1
        });
        if (await searchResult.Results.AnyAsync())
        {
            _logger.LogInformation("➡️ skip exist embed...{name}", md5);
            return;
        }
        var embeddings = await _embed.GenerateEmbeddingsAsync(paragraph);

        List<DocumentEmbedding> sentencesEmbeddings = [];
        for (int i = 0; i < embeddings.Count; i++)
        {
            sentencesEmbeddings.Add(new DocumentEmbedding
            {
                Hash = md5,
                Content = paragraph[i],
                DescriptionEmbedding = embeddings[i]
            });
        }
        try
        {
            if (sentencesEmbeddings.Count > 0)
            {
                var embedRes = collection.UpsertBatchAsync(sentencesEmbeddings);
                await foreach (var item in embedRes)
                {
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during upsert");
        }
    }

    /// <summary>
    /// 知识图谱
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    private async Task GraphKnowledgeAsync(string content, KnowledgeGraphDto knowledgeGraph)
    {
        var paragraph = MarkdownProcessing.SplitText(content);
        using (var scope = serviceProvider.CreateScope())
        {
            var processing = scope.ServiceProvider.GetRequiredService<KnowledgeProcessing>();

            var relationList = new List<RelationDto>();

            foreach (var text in paragraph)
            {
                var md5 = MD5.HashData(Encoding.UTF8.GetBytes(content));
                var hash = BitConverter.ToString(md5).Replace("-", "").ToLowerInvariant();

                if (knowledgeGraph.HashSet.TryGetValue(hash, out string? val))
                {
                    _logger.LogInformation("➡️ skip exist graph...{name}", hash);
                    continue;
                }

                var relation = await processing.RelationExtractionAsync(text);
                if (relation != null)
                {
                    relation = [.. relation.Where(r => !string.IsNullOrWhiteSpace(r.Subject))];
                    relationList.AddRange(relation);
                    knowledgeGraph.HashSet.Add(hash);
                }
            }
            knowledgeGraph.RelationDtos.AddRange(relationList);
        }
    }
}
