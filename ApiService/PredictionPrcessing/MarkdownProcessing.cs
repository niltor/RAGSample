using System.Text;
using Markdig;
using Markdig.Syntax;

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
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var document = Markdown.Parse(content, pipeline);

        // 按二级标题分割
        var result = new List<string>();
        var current = new StringBuilder();
        foreach (var block in document)
        {
            // skip the first heading
            if (block is HeadingBlock headingBlock1 && headingBlock1.Level == 1)
            {
                continue;
            }

            if (block is HeadingBlock headingBlock && headingBlock.Level == 2)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            current.AppendLine(block.ToString());
        }
        return result.Select(s => Markdown.ToPlainText(s)).ToList();
    }
}
