using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GoalsBot.Infrastructure.Persistence.Repositories;

public sealed class CalendarSyncRepository(AppDbContext db) : ICalendarSyncRepository
{
    public Task<CalendarSync?> GetByUserAndDateAsync(long userId, DateOnly date, CancellationToken ct) =>
        db.CalendarSyncs.FirstOrDefaultAsync(c => c.UserId == userId && c.Date == date, ct);

    public async Task AddAsync(CalendarSync entry, CancellationToken ct)
    {
        await db.CalendarSyncs.AddAsync(entry, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
