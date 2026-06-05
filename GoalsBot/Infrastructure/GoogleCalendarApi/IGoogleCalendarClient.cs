using GoalsBot.Infrastructure.GoogleCalendarApi.Models;

namespace GoalsBot.Infrastructure.GoogleCalendarApi;

public interface IGoogleCalendarClient
{
    Task<string> CreateEventAsync(string calendarId, GoogleEventPayload payload, CancellationToken ct);
    Task PatchEventAsync(string calendarId, string eventId, GoogleEventPayload payload, CancellationToken ct);
}
