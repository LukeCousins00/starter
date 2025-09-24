using System.ComponentModel.DataAnnotations;

namespace Starter.Web.AppConfiguration;

public class OpenRouterOptions
{
    public const string SectionName = nameof(OpenRouterOptions);
    
    [Required] public string BaseAddress { get; init; } = null!;
    [Required] public string ApiKey { get; init; } = null!;
}