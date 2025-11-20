using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

var sqlServer = builder
    .AddAzureSqlServer("sqlserver")
    .RunAsContainer(cfg =>
    {
        cfg.WithContainerName("starter-sql-server");
        cfg.WithLifetime(ContainerLifetime.Persistent);
        cfg.WithDataVolume("starter-sql-server-data");
    });

var db = sqlServer.AddDatabase("db", databaseName: "Database");

var api = builder.AddProject<Starter_Web>("web")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(db).WaitFor(db)
    .WithUrls(context =>
    {
        foreach (var u in context.Urls)
        {
            u.DisplayLocation = UrlDisplayLocation.DetailsOnly;
        }

        context.Urls.Add(new()
        {
            Url = "/scalar",
            DisplayText = "API Reference",
            Endpoint = context.GetEndpoint("https")
        });
    })
    .PublishAsAzureContainerApp((infra, app) =>
    {
        // Scale to zero when idle
        app.Template.Scale.MinReplicas = 0;
    });

var frontend = builder.AddViteApp("ui", "../Starter.Client")
    .WithEndpoint("http", e => e.Port = 9080)
    .WithReference(api)
    .WithUrl("", "Starter");

// Publish: Embed frontend build output in API container
api.PublishWithContainerFiles(frontend, "wwwroot");

builder.Build().Run();