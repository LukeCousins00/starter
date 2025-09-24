using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using Starter.Web.AppConfiguration;

namespace Starter.Web.Features.AI;

public static class AIDependencies
{
    private const string MainModel = "gemini-2.5";

    public static IServiceCollection AddAIDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        var client = new OpenAI.Chat.ChatClient(MainModel,
                new ApiKeyCredential(
                    configuration[$"{OpenRouterOptions.SectionName}:{nameof(OpenRouterOptions.ApiKey)}"]!),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(configuration[$"{OpenRouterOptions.SectionName}:{nameof(OpenRouterOptions.BaseAddress)}"]!),
                })
            .AsIChatClient();

        services.AddSingleton(client);

        return services;
    }
}