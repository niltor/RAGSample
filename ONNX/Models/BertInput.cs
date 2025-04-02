using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;

namespace ONNX.Models;
public class BertInput
{
    public const int MaxTokens = 512;

    [ColumnName("input_ids")]
    public long[] InputIds { get; set; } = [];

    [ColumnName("attention_mask")]
    public long[] AttentionMask { get; set; } = [];

    [ColumnName("token_type_ids")]
    public long[] TokenTypeIds { get; set; } = [];
}
