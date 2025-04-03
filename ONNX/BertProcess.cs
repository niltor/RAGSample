using System.Text.Json;
using FastBertTokenizer;
using Microsoft.ML.Data;
using Microsoft.ML.OnnxRuntime;
using ONNX.Models;

namespace ONNX;
public class BertProcess
{
    private readonly string onnxPath;
    private readonly string vocabPath;
    private string configPath = "./config.json";
    private int LabelsCount { get; set; } = 0;
    public Dictionary<string, string>? IdToLabel = [];

    public BertProcess(string onnxPath, string vocabPath, string? configPath = null)
    {
        this.onnxPath = onnxPath;
        this.vocabPath = vocabPath;
        this.configPath = configPath ?? this.configPath;

        if (File.Exists(configPath))
        {
            var config = File.ReadAllText(configPath);
            var jsonConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(config);
            if (jsonConfig != null)
            {
                if (jsonConfig.ContainsKey("id2label"))
                {
                    IdToLabel = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonConfig["id2label"].ToString());
                    LabelsCount = IdToLabel?.Count ?? 0;
                }
            }
        }
    }

    public async Task<List<EntityResult>?> ProcessAsync(string sentence)
    {
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
            //{ "token_type_ids", tokenTypeIdsOrtValue }
        };

        using var runOptions = new RunOptions();
        using (InferenceSession session = new(onnxPath))
        {
            using (var output = session.Run(runOptions, inputs, session.OutputNames))
            {
                if (output?.Count > 0)
                {
                    var outputData = output.First().GetTensorDataAsSpan<float>();
                    var predictLabels = outputData.GetMaxValueIndexForChunks(LabelsCount);

                    return EntityProcess(predictLabels, tokenizer, tokens);

                }
            }
        }
        return null;
    }

    public List<EntityResult>? EntityProcess(List<int> predictLabels, BertTokenizer tokenizer, long[] tokens)
    {
        if (IdToLabel == null)
        {
            throw new Exception("IdToLabel is null");
        }

        var predictedLabels = predictLabels.Select(id => IdToLabel[id.ToString()]);
        var result = predictedLabels?.Zip(tokens, (label, token) =>
        {
            var decodedText = tokenizer.Decode(new[] { token });
            Console.WriteLine($"Token: {token}, Decoded Text: {decodedText}, Label: {label}");

            return new EntityResult
            {
                Text = tokenizer.Decode([0, token])[5..],
                Type = label ?? string.Empty,
            };
        })
       ?.Where(x => x.Type != "O")
       ?.ToList();

        return result;
    }
}
