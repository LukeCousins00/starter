using System.Text.Json.Serialization;
using Starter.ServiceDefaults;
using Starter.Web.AppConfiguration;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddConfigurationDependencies(builder.Configuration)
    .AddOpenApi();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// CORS for if the frontend is hosted separately
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

app.UseHttpsRedirection();

app.Run();