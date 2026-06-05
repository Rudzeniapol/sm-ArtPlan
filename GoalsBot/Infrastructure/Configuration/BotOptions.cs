using System.ComponentModel.DataAnnotations;

namespace GoalsBot.Infrastructure.Configuration;

public sealed class BotOptions
{
    public const string SectionName = "Bot";

    [Required]
    public string Token { get; init; } = string.Empty;

    public string? WebhookSecret { get; init; }

    [Required]
    public string DefaultTimeZoneId { get; init; } = "Etc/UTC";
}
