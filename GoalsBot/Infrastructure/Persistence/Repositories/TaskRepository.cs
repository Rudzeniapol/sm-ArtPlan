using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GoalsBot.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository(AppDbContext db) : ITaskRepository
{
    public Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct) =>
        db.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);

    public async Task<IReadOnlyList<TaskItem>> GetByUserAndDateAsync(long userId, DateOnly date, CancellationToken ct) =>
        await db.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date == date)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TaskItem>> GetByUserAndDateRangeAsync(long userId, DateOnly fromInclusive, DateOnly toInclusive, CancellationToken ct) =>
        await db.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.Date >= fromInclusive && t.Date <= toInclusive)
            .ToListAsync(ct);

    public async Task AddAsync(TaskItem task, CancellationToken ct)
    {
        await db.Tasks.AddAsync(task, ct);
    }

    public Task RemoveAsync(TaskItem task, CancellationToken ct)
    {
        db.Tasks.Remove(task);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
