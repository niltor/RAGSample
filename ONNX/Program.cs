
using Microsoft.ML.OnnxRuntime;
using ONNX;


var onnxPath = @"D:\onnxModels\mBERT\model.onnx";
var vocabPath = @"D:\onnxModels\mBERT\vocab.txt";
var configPath = @"D:\onnxModels\mBERT\config.json";


var sentence = "Bob Dylan is from Duluth, Minnesota and is an American singer-songwriter";
var sentence2 = "这只是一个开始，在构建知识图谱前的实体识别";

var bertProcess = new BertProcess(onnxPath, vocabPath, configPath);

var res = await bertProcess.ProcessAsync(sentence);

if (res == null)
{
    Console.WriteLine("No results found.");
    return;
}

foreach (var item in res)
{

    Console.WriteLine($"Text: {item.Text}, Type: {item.Type}");
}


res = await bertProcess.ProcessAsync(sentence2);

if (res == null)
{
    Console.WriteLine("No results found.");
    return;
}

foreach (var item in res)
{
    Console.WriteLine($"Text: {item.Text}, Type: {item.Type}");
}
