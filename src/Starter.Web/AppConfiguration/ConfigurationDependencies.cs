namespace Starter.Web.AppConfiguration;

public static class ConfigurationDependencies
{
    public static IServiceCollection AddConfigurationDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenRouterOptions>(configuration.GetSection(OpenRouterOptions.SectionName))
            .AddOptionsWithValidateOnStart<OpenRouterOptions>();

        services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.SectionName))
            .AddOptionsWithValidateOnStart<FrontendOptions>();
        
        return services;
    }
}