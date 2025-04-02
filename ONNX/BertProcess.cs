using System.Runtime.CompilerServices;
using FastBertTokenizer;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json.Linq;
using ONNX.Models;

namespace ONNX;
public class BertProcess
{
    private readonly string onnxPath;
    private readonly string vocabPath;
    public int LabelsCount { get; private set; } = 0;

    public BertProcess(string onnxPath, string vocabPath)
    {
        this.onnxPath = onnxPath;
        this.vocabPath = vocabPath;


    }

    public async Task ProcessAsync(string sentence)
    {
        List<EntityResult>? result = [];

        var tokenizer = new BertTokenizer();
        await tokenizer.LoadVocabularyAsync(vocabPath, false);

        var (inputIds, attentionMask, tokenTypeIds) = tokenizer.Encode(sentence, BertInput.MaxTokens);

        var tokens = inputIds.ToArray();
        using var inputIdsOrtValue = OrtValue.CreateTensorValueFromMemory(tokens, [1, inputIds.Length]);
        using var attentionMaskOrtValue = OrtValue.CreateTensorValueFromMemory(attentionMask.ToArray(), [1, attentionMask.Length]);
        using var tokenTypeIdsOrtValue = OrtValue.CreateTensorValueFromMemory(tokenTypeIds.ToArray(), [1, tokenTypeIds.Length]);

        var inputs = new Dictionary<string, OrtValue>
        {
            { "input_ids", inputIdsOrtValue },
            { "attention_mask", attentionMaskOrtValue },
            { "token_type_ids", tokenTypeIdsOrtValue }
        };

        //inputs.Add("token_type_ids", tokenTypeIdsOrtValue);

        using var runOptions = new RunOptions();
        using (InferenceSession session = new(onnxPath))
        {

            using (var output = session.Run(runOptions, inputs, session.OutputNames))
            {
                if (output?.Count > 0)
                {
                    var outputData = output.First().GetTensorDataAsSpan<float>();
                    var batchedResult = outputData.GetMaxValueIndexForChunks(LabelsCount);
                    if (batchedResult?.Count == 0)
                    {
                        //return result;
                    }

                    var predictedLabels = batchedResult?.Select(res => configuration.IdTolabel?[res.ToString()]);

                    result = predictedLabels?.Zip(tokens, (label, value) =>
                    {
                        return new EntityResult
                        {
                            Text = tokenizer.Decode([0, value])[_configuration.NumberOfTokens..],
                            Type = label ?? string.Empty,
                        };
                    })
                   ?.Where(x => x.Label != "O")
                   ?.ToList();
                }
            }
        }
    }
}
