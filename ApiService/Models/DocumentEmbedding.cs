using Microsoft.Extensions.VectorData;

namespace ApiService.Models;

public class DocumentEmbedding
{
    public static string DocName = "dusi";

    [VectorStoreRecordKey]
    public ulong Id { get; set; }

    [VectorStoreRecordData]
    public string Sha { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public string Content { get; set; } = string.Empty;

    [VectorStoreRecordVector(768)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public List<string> Tags { get; set; } = [];
}
