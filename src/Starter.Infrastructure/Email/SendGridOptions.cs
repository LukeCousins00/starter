namespace Starter.Infrastructure.Email;

public class SendGridOptions
{
    public const string SectionName = nameof(SendGridOptions);

    public string ApiKey { get; set; } = string.Empty;
    public string SourceEmail { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
}