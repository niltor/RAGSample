using System.Security.Cryptography;
using System.Text;
using ApiService.Models;
using ApiService.PredictionProcessing;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;

namespace ApiService;
/// <summary>
/// embed worker
/// </summary>
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class Worker(
    ILogger<Worker> _logger,
    ITextEmbeddingGenerationService _embed,
    IVectorStore _vectorStore
    ) : IHostedService
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    //private const string SearchPath = @"E:\codes\EasyBlog\Content";
    private const string SearchPath = @"D:\codes\MyBlog\Content";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var collection = _vectorStore.GetCollection<ulong, DocumentEmbedding>(DocumentEmbedding.DocName);

        await collection.CreateCollectionIfNotExistsAsync();

        var mdFiles = Directory.GetFiles(SearchPath, "*.md", SearchOption.AllDirectories);
        ulong id = 1;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 4, // Specify the desired number of threads
            CancellationToken = cancellationToken
        };
        await Parallel.ForEachAsync(mdFiles, parallelOptions, async (mdFile, ct) =>
        {
            var mdContent = File.ReadAllText(mdFile);
            var paragraph = MarkdownProcessing.ToPlainText(mdContent);

            var hash = MD5.HashData(Encoding.UTF8.GetBytes(mdFile));
            var md5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

            ReadOnlyMemory<float> zeroVector = new float[768];

            var searchResult = await collection.VectorizedSearchAsync(zeroVector, new VectorSearchOptions<DocumentEmbedding>
            {
                Filter = d => d.Sha == md5,
                Top = 1
            });
            if (await searchResult.Results.AnyAsync())
            {
                _logger.LogInformation("➡️ skip...{name}", mdFile);
                return;
            }
            _logger.LogInformation("🆕 embedding...{name}", mdFile);
            var embeddings = await _embed.GenerateEmbeddingsAsync(paragraph);

            List<DocumentEmbedding> sentencesEmbeddings = [];
            for (int i = 0; i < embeddings.Count; i++)
            {
                sentencesEmbeddings.Add(new DocumentEmbedding
                {
                    Id = id++,
                    Sha = md5,
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
        });
        _logger.LogInformation("✨ [{number}]Done!", id);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
