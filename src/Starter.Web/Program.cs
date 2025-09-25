using System.Text.Json.Serialization;
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

builder.Services.AddCors(cfg =>
{
    cfg.AddDefaultPolicy(x =>
    {
        x.WithOrigins(builder.Configuration[$"{FrontendOptions.SectionName}:{nameof(FrontendOptions.BaseAddress)}"]!);
        x.AllowAnyMethod();
        x.AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseInfrastructureServices();

app.UseHttpsRedirection();

app.Run();