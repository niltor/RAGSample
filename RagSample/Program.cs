var builder = DistributedApplication.CreateBuilder(args);
var ollama = builder.AddOllama("ollama")
    .WithGPUSupport()
    .WithContainerRuntimeArgs("--gpus=all")
    .WithDataVolume();

var chat = ollama.AddModel("chat", "llama3.2:1b");
var embed = ollama.AddModel("embed", "nomic-embed-text");


builder.AddProject<Projects.ApiService>("apiservice")
    .WithReference(chat)
    .WithReference(embed)
    .WaitFor(chat)
    .WaitFor(embed);

builder.Build().Run();
