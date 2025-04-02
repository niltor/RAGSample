
using Microsoft.ML.OnnxRuntime;
using ONNX;


var onnxPath = @"D:\codes\mBERT\model.onnx";
var vocabPath = @"D:\codes\mBERT\vocab.txt";


var sentence = "Bob Dylan is from Duluth, Minnesota and is an American singer-songwriter";
var sentence2 = "这只是一个开始，在构建知识图谱前的实体识别";

var bertProcess = new BertProcess(onnxPath, vocabPath);

bertProcess.ProcessAsync(sentence);
bertProcess.ProcessAsync(sentence2);
