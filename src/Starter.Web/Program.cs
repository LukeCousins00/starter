using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Scalar.AspNetCore;
using Starter.Infrastructure;
using Starter.ServiceDefaults;
using Starter.Web.AppConfiguration;
using Starter.Web.JsonConverters;
using Starter.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddConfigurationDependencies(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddHttpContextAccessor()
    .AddOpenApi()
    .AddSingleton<GameStateService>()
    .AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

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

app.MapDefaultEndpoints();

app.UseInfrastructureServices();

app.UseCors();

app.UseHttpsRedirection();

// Game board SSE endpoints
var gameStateService = app.Services.GetRequiredService<GameStateService>();

app.MapGet("/api/game/events", (CancellationToken cancellationToken) =>
{
    async IAsyncEnumerable<object> GetGameEvents(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Send a ping immediately to establish connection
        yield return new { type = "ping", data = "connected" };

        await foreach (var evt in gameStateService.Subscribe("default", cancellationToken))
        {
            // Include event type in payload for frontend to handle
            yield return new { type = evt.Type, data = evt.Payload };
        }
    }

    return TypedResults.ServerSentEvents(GetGameEvents(cancellationToken));
})
.WithName("GetGameEvents")
.WithOpenApi();

app.MapPost("/api/game/token/move", (MoveTokenRequest request, GameStateService service) =>
{
    service.MoveToken("default", request.TokenId, request.X, request.Y);
    return TypedResults.Ok(new { success = true });
})
.WithName("MoveToken")
.WithOpenApi();

app.MapPost("/api/game/token/add", (AddTokenRequest request, GameStateService service) =>
{
    var token = new GameStateService.Token(
        request.Id,
        request.UserId,
        request.Username,
        request.Color,
        request.X,
        request.Y
    );
    service.AddToken("default", token);
    return TypedResults.Ok(new { success = true });
})
.WithName("AddToken")
.WithOpenApi();

app.MapPost("/api/game/background", (SetBackgroundRequest request, GameStateService service) =>
{
    service.SetBackground("default", request.Url);
    return TypedResults.Ok(new { success = true });
})
.WithName("SetBackground")
.WithOpenApi();

app.Run();

// Request models
record MoveTokenRequest(string TokenId, int X, int Y);
record AddTokenRequest(string Id, string UserId, string Username, string Color, int X, int Y);
record SetBackgroundRequest(string Url);