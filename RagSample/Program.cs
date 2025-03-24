var builder = DistributedApplication.CreateBuilder(args);
var ollama = builder.AddOllama("ollama", port: 49394)
    .WithGPUSupport()
    .WithContainerRuntimeArgs("--gpus=all")
    .WithDataVolume();


var chat = ollama.AddModel("chat", "llama3.2:1b");
//var chat = ollama.AddModel("chat", "phi4");
var embed = ollama.AddModel("embed", "nomic-embed-text");

var qdrant = builder.AddQdrant("qdrant", httpPort: 49384, grpcPort: 49383).WithDataVolume();


builder.AddProject<Projects.ApiService>("apiservice")
    .WithReference(chat)
    .WithReference(embed)
    .WithReference(qdrant)
    .WaitFor(chat)
    .WaitFor(embed)
    .WaitFor(qdrant);

builder.Build().Run();
