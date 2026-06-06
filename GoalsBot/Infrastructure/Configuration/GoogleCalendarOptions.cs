namespace GoalsBot.Infrastructure.Configuration;

public sealed class GoogleCalendarOptions
{
    public const string SectionName = "GoogleCalendar";

    // Either CredentialsJson (inline) or CredentialsPath (file mounted into the container)
    // can be set. If neither is set, /sync simply replies with a "not configured" message —
    // the rest of the bot still works.
    public string CredentialsJson { get; init; } = string.Empty;
    public string CredentialsPath { get; init; } = string.Empty;

    public string CalendarId { get; init; } = "primary";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(CredentialsJson) || !string.IsNullOrWhiteSpace(CredentialsPath);
}
