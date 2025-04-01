using Markdig;

namespace ApiService.PredictionPrcessing;

/// <summary>
/// markdown文档处理
/// </summary>
public class MarkdownProcessing
{
    /// <summary>
    /// 按二级标题拆分
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    public static List<string> SplitText(string content)
    {
        // 拆分
        var paragraphs = content.Split("\n## ", StringSplitOptions.RemoveEmptyEntries).ToList();
        return paragraphs.Select(s => Markdown.ToPlainText(s)).ToList();
    }


    public static List<string> ToPlainText(string content)
    {
        var res = Markdown.ToPlainText(content);
        return [res];
    }
}
