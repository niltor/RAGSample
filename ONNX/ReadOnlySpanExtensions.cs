using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ONNX;
internal static class ReadOnlySpanExtensions
{
    /// <summary>
    /// 从模型输出的概率分布中提取每个 chunk 中最大值的索引
    /// </summary>
    /// <param name="span"></param>
    /// <param name="chunkSize"></param>
    /// <returns></returns>
    public static List<int> GetMaxValueIndexForChunks(this ReadOnlySpan<float> span, int chunkSize)
    {
        if (chunkSize <= 0)
        {
            return [];
        }

        var result = new List<int>();

        for (int i = 0; i < span.Length; i += chunkSize)
        {
            int currentChunkSize = Math.Min(chunkSize, span.Length - i);
            ReadOnlySpan<float> chunk = span.Slice(i, currentChunkSize);

            result.Add(chunk.GetMaxValueIndex());
        }

        return result;
    }

    public static int GetMaxValueIndex(this ReadOnlySpan<float> span)
    {
        float maxVal = span[0];
        int maxIndex = 0;

        for (int i = 1; i < span.Length; ++i)
        {
            var v = span[i];
            if (v > maxVal)
            {
                maxVal = v;
                maxIndex = i;
            }
        }

        return maxIndex;
    }
}
