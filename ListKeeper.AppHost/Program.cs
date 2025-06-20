var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.ListKeeper_ApiService>("apiservice");

//builder.AddProject<Projects.ListKeeper_Web>("webfrontend")
//    .WithExternalHttpEndpoints()
//    .WithReference(apiService)
//    .WaitFor(apiService);

builder.Build().Run();
