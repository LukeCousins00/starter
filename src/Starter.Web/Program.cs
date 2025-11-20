using System.Text.Json.Serialization;
using Scalar.AspNetCore;
using Starter.Infrastructure;
using Starter.ServiceDefaults;
using Starter.Web.AppConfiguration;
using Starter.Web.JsonConverters;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddConfigurationDependencies(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddHttpContextAccessor()
    .AddOpenApi();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.Converters.Add(new DateTimeOffsetJsonConverter());
});

var app = builder.Build();

app.UseFileServer();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseInfrastructureServices();

app.UseHttpsRedirection();

app.Run();