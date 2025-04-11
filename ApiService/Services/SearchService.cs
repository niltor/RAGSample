namespace ApiService.Services;

/// <summary>
/// 搜索服务
/// </summary>
public class SearchService
{
    public string[] SearchResults { get; set; } = [];


    public void Search(string searchContent)
    {

        // 向量搜索

        // 实体识别，再进行向量搜索

        // 搜索知识图谱，再进行向量搜索


        // 合并结果
    }


    public string? SearchVector(string searchContent)
    {

        return default;
    }

    public string[]? SearchNer(string searchContent)
    {
        return default;
    }

    public string[]? SearchGraph(string searchContent)
    {
        return default;
    }

}


