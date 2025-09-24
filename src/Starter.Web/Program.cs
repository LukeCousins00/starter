using System.Reflection;
using System.Text.Json.Serialization;
using MassTransit;
using Starter.ServiceDefaults;
using Starter.Web.AppConfiguration;
using Starter.Web.Features.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddConfigurationDependencies(builder.Configuration)
    .AddAIDependencies(builder.Configuration)
    .AddOpenApi();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(cfg =>
{
    cfg.AddDefaultPolicy(x =>
    {
        x.WithOrigins(builder.Configuration[$"{FrontendOptions.SectionName}:{nameof(FrontendOptions.BaseAddress)}"]!);
        x.AllowAnyMethod();
        x.AllowAnyHeader();
    });
});

builder.AddMongoDBClient(connectionName: "projectl");

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(Assembly.GetExecutingAssembly());

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var host = builder.Configuration.GetConnectionString("queue");
        cfg.Host(host);
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/tasks", () => Results.Ok(new TaskItem("task_342jg2vfk2", "cool task")))
    .Produces<TaskItem>();

app.UseHttpsRedirection();

app.Run();

public record TaskItem(string Id, string Description);