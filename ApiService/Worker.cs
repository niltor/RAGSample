
using System;
using System.Security.Cryptography;
using System.Text;
using ApiService.Models;
using Markdig;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using Qdrant.Client;

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
    private const string SearchPath = @"D:\codes\MyBlog\Content";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var collection = _vectorStore.GetCollection<ulong, DocumentEmbedding>("docs");

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
            var plainText = Markdown.ToPlainText(mdContent);
            // split plain text into sentences
            var sentenceSplitter = new char[] { '.', '!', '?', '。', '！', '？', '\n' };
            var sentences = plainText.Split(sentenceSplitter, StringSplitOptions.RemoveEmptyEntries);
            sentences = sentences.ToList().Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(mdFile));
            var md5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();


            // search from qdrant by fileName payload

            ReadOnlyMemory<float> zeroVector = new float[768];

            var searchResult = await collection.VectorizedSearchAsync(zeroVector, new VectorSearchOptions<DocumentEmbedding>
            {
                Filter = d => d.Sha == md5,
            });
            if (await searchResult.Results.AnyAsync())
            {
                _logger.LogInformation("skip...{name}", mdFiles);
                return;
            }
            _logger.LogInformation("embedding...{name}", mdFiles);
            var embeddings = await _embed.GenerateEmbeddingsAsync(sentences);

            _logger.LogInformation("saving to db");
            List<DocumentEmbedding> sentencesEmbeddings = new();
            foreach (var embedding in embeddings)
            {
                sentencesEmbeddings.Add(new DocumentEmbedding
                {
                    Id = id++,
                    Sha = md5,
                    DescriptionEmbedding = embedding
                });
            }
            try
            {
                var res = collection.UpsertBatchAsync(sentencesEmbeddings);
                await foreach (var item in res)
                {
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
