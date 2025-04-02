using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;

namespace ONNX.Models;

public class BertNerOutput
{
    // logits数组长度 = MaxSeqLength * NumLabels
    public float[] Logits { get; set; } = [];
    public const int NumLabels = 9; // 例如：0=O, 1=B-PER, 2=I-PER, 3=B-LOC, 4=I-LOC, 5=B-ORG, 6=I-ORG, 7=B-MISC, 8=I-MISC
}