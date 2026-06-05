using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Enums;

namespace GoalsBot.Domain.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct);
    Task<IReadOnlyList<TaskItem>> GetByUserAndDateAsync(long userId, DateOnly date, CancellationToken ct);
    Task<IReadOnlyList<TaskItem>> GetByUserAndDateRangeAsync(long userId, DateOnly fromInclusive, DateOnly toInclusive, CancellationToken ct);
    Task AddAsync(TaskItem task, CancellationToken ct);
    Task RemoveAsync(TaskItem task, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
