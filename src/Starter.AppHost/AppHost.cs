using Projects;
using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var web = builder.AddProject<Starter_Web>("web")
    .WithExternalHttpEndpoints();

var ui = builder.AddPnpmApp("ui", "../starter.Client", scriptName: "dev")
    .WithPnpmPackageInstallation()
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithEnvironment("VITE_API_URL", web.GetEndpoint("https"))
    .WithExternalHttpEndpoints();

web.WithEnvironment($"FrontendOptions__BaseAddress", ui.GetEndpoint("http"));

builder.AddScalarApiReference(options =>
    {
        options
            .WithTheme(ScalarTheme.BluePlanet)
            .PreferHttpsEndpoint()
            .AllowSelfSignedCertificates();
    })
    .WithApiReference(web);

builder.Build().Run();