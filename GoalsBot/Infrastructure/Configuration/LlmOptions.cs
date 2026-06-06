using System.ComponentModel.DataAnnotations;

namespace GoalsBot.Infrastructure.Configuration;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string Model { get; init; } = "deepseek-v4-flash";

    [Range(1, 600)]
    public int TimeoutSeconds { get; init; } = 30;
}
