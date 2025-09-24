using Projects;
using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var queue = builder.AddRabbitMQ("queue")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin()
    .WithDataVolume("project-l-rabbitmq");

var mongo = builder.AddMongoDB("mongo")
    .WithDataVolume("project-l-mongo")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithMongoExpress(options => options.WithLifetime(ContainerLifetime.Persistent))
    .WithDbGate();

var mongodb = mongo.AddDatabase("projectl");

var web = builder.AddProject<Starter_Web>("web")
    .WithReference(mongodb).WaitFor(mongodb)
    .WithReference(queue).WaitFor(queue)
    .WithExternalHttpEndpoints();

var ui = builder.AddPnpmApp("ui", "./../ProjectL.Client", scriptName: "dev")
    .WithPnpmPackageInstallation()
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithEnvironment("VITE_API_URL", web.GetEndpoint("https"))
    .WithExternalHttpEndpoints();

web.WithEnvironment("FrontendOptions__BaseAddress", ui.GetEndpoint("http"));

builder.AddScalarApiReference(options =>
    {
        options
            .WithTheme(ScalarTheme.BluePlanet)
            .PreferHttpsEndpoint()
            .AllowSelfSignedCertificates();
    })
    .WithApiReference(web);

builder.Build().Run();