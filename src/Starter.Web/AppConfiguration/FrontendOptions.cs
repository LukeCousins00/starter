using System.ComponentModel.DataAnnotations;

namespace Starter.Web.AppConfiguration;

public class FrontendOptions
{
    public const string SectionName = nameof(FrontendOptions);

    [Required] public string BaseAddress { get; init; } = null!;
}