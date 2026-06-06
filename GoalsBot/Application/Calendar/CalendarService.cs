using System.Globalization;
using System.Text;
using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Repositories;
using GoalsBot.Infrastructure.Configuration;
using GoalsBot.Infrastructure.GoogleCalendarApi;
using GoalsBot.Infrastructure.GoogleCalendarApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoalsBot.Application.Calendar;

public sealed class CalendarService(
    ITaskRepository taskRepository,
    ICalendarSyncRepository calendarSyncRepository,
    IGoogleCalendarClient calendarClient,
    IOptions<GoogleCalendarOptions> options,
    TimeProvider clock,
    ILogger<CalendarService> logger) : ICalendarService
{
    public async Task SyncDayAsync(long userId, DateOnly date, CancellationToken ct)
    {
        if (!options.Value.IsConfigured)
            throw new CalendarNotConfiguredException();

        var tasks = await taskRepository.GetByUserAndDateAsync(userId, date, ct);
        var payload = BuildPayload(date, tasks);
        var calendarId = options.Value.CalendarId;

        var existing = await calendarSyncRepository.GetByUserAndDateAsync(userId, date, ct);
        var now = clock.GetUtcNow();

        if (existing is null)
        {
            var eventId = await calendarClient.CreateEventAsync(calendarId, payload, ct);
            await calendarSyncRepository.AddAsync(new CalendarSync
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = date,
                GoogleEventId = eventId,
                LastSyncedAt = now
            }, ct);
            logger.LogInformation("Created Google Calendar event {EventId} for user {UserId} on {Date}.", eventId, userId, date);
        }
        else
        {
            await calendarClient.PatchEventAsync(calendarId, existing.GoogleEventId, payload, ct);
            existing.LastSyncedAt = now;
            logger.LogInformation("Patched Google Calendar event {EventId} for user {UserId} on {Date}.", existing.GoogleEventId, userId, date);
        }

        await calendarSyncRepository.SaveChangesAsync(ct);
    }

    private static GoogleEventPayload BuildPayload(DateOnly date, IReadOnlyList<TaskItem> tasks)
    {
        var summary = $"Goals for {date.ToString("MMM dd", CultureInfo.InvariantCulture)}";

        var description = new StringBuilder();
        if (tasks.Count == 0)
        {
            description.Append("No tasks yet.");
        }
        else
        {
            for (var i = 0; i < tasks.Count; i++)
            {
                var t = tasks[i];
                var prefix = t.IsCompleted ? "✅" : "◻️";
                description.Append(i + 1).Append(". ").Append(prefix).Append(' ').Append(t.Title);
                if (t.EstimatedMinutes is { } minutes)
                    description.Append(" — ").Append(minutes).Append(" min");
                description.Append('\n');
            }
        }

        var iso = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var nextIso = date.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return new GoogleEventPayload(
            summary,
            description.ToString(),
            new GoogleEventDate(iso),
            new GoogleEventDate(nextIso));
    }
}
