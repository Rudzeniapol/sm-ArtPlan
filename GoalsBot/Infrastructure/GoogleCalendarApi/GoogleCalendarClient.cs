using System.Net.Http.Json;
using System.Text.Json;
using GoalsBot.Infrastructure.GoogleCalendarApi.Models;

namespace GoalsBot.Infrastructure.GoogleCalendarApi;

public sealed class GoogleCalendarClient(HttpClient http) : IGoogleCalendarClient
{
    public async Task<string> CreateEventAsync(string calendarId, GoogleEventPayload payload, CancellationToken ct)
    {
        var path = $"calendars/{Uri.EscapeDataString(calendarId)}/events";
        using var content = JsonContent.Create(payload, GoogleCalendarJsonContext.Default.GoogleEventPayload);
        using var response = await http.PostAsync(path, content, ct);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync(
            GoogleCalendarJsonContext.Default.GoogleEventResponse, ct)
            ?? throw new InvalidOperationException("Google returned an empty event response.");
        return created.Id;
    }

    public async Task PatchEventAsync(string calendarId, string eventId, GoogleEventPayload payload, CancellationToken ct)
    {
        var path = $"calendars/{Uri.EscapeDataString(calendarId)}/events/{Uri.EscapeDataString(eventId)}";
        using var content = JsonContent.Create(payload, GoogleCalendarJsonContext.Default.GoogleEventPayload);
        using var request = new HttpRequestMessage(HttpMethod.Patch, path) { Content = content };
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}
