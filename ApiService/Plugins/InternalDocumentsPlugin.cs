using System.ComponentModel;
using ApiService.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace ApiService.Plugins;

#pragma warning disable SKEXP0001
public class InternalDocumentsPlugin
{
    private readonly ITextEmbeddingGenerationService _embed;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<InternalDocumentsPlugin> _logger;

    public InternalDocumentsPlugin(ITextEmbeddingGenerationService textEmbeddingGenerationService, IVectorStore indexClient, ILogger<InternalDocumentsPlugin> logger)
    {
        _embed = textEmbeddingGenerationService;
        _vectorStore = indexClient;
        _logger = logger;
    }

    [KernelFunction("Search")]
    [Description("Search document")]
    public async Task<string> SearchAsync(string query)
    {
        ReadOnlyMemory<float> searchEmbedding = await _embed.GenerateEmbeddingAsync(query);
        var collection = _vectorStore.GetCollection<ulong, DocumentEmbedding>("docs");
        var searchResult = await collection.VectorizedSearchAsync(searchEmbedding);
        var firstResult = await searchResult.Results.FirstOrDefaultAsync();

        return firstResult?.Record.Content ?? string.Empty;
    }
}
