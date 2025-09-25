using Projects;
using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("sqlserver-data");

var db = sqlServer.AddDatabase("db", databaseName: "Database");

var web = builder.AddProject<Starter_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(db).WaitFor(db);

var ui = builder.AddViteApp("ui", "../starter.Client", packageManager: "pnpm")
    .WithPnpmPackageInstallation()
    .WithEnvironment("API_URL", web.GetEndpoint("https"))
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