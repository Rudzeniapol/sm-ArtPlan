using GoalsBot.Domain.Entities;

namespace GoalsBot.Domain.Repositories;

public interface ICalendarSyncRepository
{
    Task<CalendarSync?> GetByUserAndDateAsync(long userId, DateOnly date, CancellationToken ct);
    Task AddAsync(CalendarSync entry, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
