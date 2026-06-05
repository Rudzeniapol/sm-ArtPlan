using System.ComponentModel.DataAnnotations;

namespace GoalsBot.Infrastructure.Configuration;

public sealed class GoogleCalendarOptions
{
    public const string SectionName = "GoogleCalendar";

    [Required]
    public string CredentialsJson { get; init; } = string.Empty;

    public string CalendarId { get; init; } = "primary";
}
