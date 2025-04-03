
using Microsoft.ML.OnnxRuntime;
using ONNX;


var onnxPath = @"D:\onnxModels\mBERT\model.onnx";
var vocabPath = @"D:\onnxModels\mBERT\vocab.txt";
var configPath = @"D:\onnxModels\mBERT\config.json";


var sentence = "The weather in Beijing is very good today. Everyone is very happy. I drank milk and ate bread in the morning.";

var sentence2 = "今天北京的天气很不错，大家都很高兴，我早上喝了牛奶，吃了面包";

var bertProcess = new BertProcess(onnxPath, vocabPath, configPath);

//var res = await bertProcess.ProcessAsync(sentence);

//if (res == null)
//{
//    Console.WriteLine("No results found.");
//    return;
//}

//foreach (var item in res)
//{

//    Console.WriteLine($"Text: {item.Text}, Type: {item.Type}");
//}

{
    var res = await bertProcess.ProcessAsync(sentence2);

    if (res == null)
    {
        Console.WriteLine("No results found.");
        return;
    }

    foreach (var item in res)
    {
        Console.WriteLine($"Text: {item.Text}, Type: {item.Type}");
    }
}

