var builder = DistributedApplication.CreateBuilder(args);

var backend = builder.AddProject<Projects.BizSrt_Api>("backend");

builder.AddNpmApp("frontend", "../frontend", "dev")
    .WithReference(backend)
    .WithEnvironment("NEXT_PUBLIC_API_URL", backend.GetEndpoint("http"))
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
