namespace GoalsBot.Application.Calendar;

public interface ICalendarService
{
    Task SyncDayAsync(long userId, DateOnly date, CancellationToken ct);
}
