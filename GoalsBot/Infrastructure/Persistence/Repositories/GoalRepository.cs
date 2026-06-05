using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GoalsBot.Infrastructure.Persistence.Repositories;

public sealed class GoalRepository(AppDbContext db) : IGoalRepository
{
    public Task<DailyGoal?> GetByUserAndDateAsync(long userId, DateOnly date, CancellationToken ct) =>
        db.DailyGoals
            .AsNoTracking()
            .Include(g => g.Tasks)
            .FirstOrDefaultAsync(g => g.UserId == userId && g.Date == date, ct);

    public async Task AddAsync(DailyGoal goal, CancellationToken ct)
    {
        await db.DailyGoals.AddAsync(goal, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
