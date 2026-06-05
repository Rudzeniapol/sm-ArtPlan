using GoalsBot.Domain.Entities;

namespace GoalsBot.Domain.Repositories;

public interface IGoalRepository
{
    Task<DailyGoal?> GetByUserAndDateAsync(long userId, DateOnly date, CancellationToken ct);
    Task AddAsync(DailyGoal goal, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
