using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Enums;
using GoalsBot.Domain.Repositories;

namespace GoalsBot.Application.Stats;

public sealed class StatsService(ITaskRepository repository, TimeProvider clock) : IStatsService
{
    public async Task<StatsDto> GetStatsAsync(long userId, StatsPeriod period, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var from = period switch
        {
            StatsPeriod.Week => today.AddDays(-6),
            StatsPeriod.Month => today.AddDays(-29),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };

        var tasks = await repository.GetByUserAndDateRangeAsync(userId, from, today, ct);

        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t => t.IsCompleted);
        var completionRate = totalTasks == 0
            ? 0m
            : Math.Round((decimal)completedTasks / totalTasks * 100, 1);

        var totalEstimated = tasks.Sum(t => t.EstimatedMinutes ?? 0);
        var totalCompletedMinutes = tasks.Where(t => t.IsCompleted).Sum(t => t.EstimatedMinutes ?? 0);

        var byPriority = Enum.GetValues<TaskPriority>()
            .ToDictionary(p => p, p => tasks.Count(t => t.Priority == p));

        var streak = ComputeStreak(tasks, today);

        return new StatsDto(
            period,
            from,
            today,
            totalTasks,
            completedTasks,
            completionRate,
            totalEstimated,
            totalCompletedMinutes,
            byPriority,
            streak);
    }

    private static int ComputeStreak(IReadOnlyList<TaskItem> tasks, DateOnly today)
    {
        var completedDays = tasks
            .Where(t => t.IsCompleted)
            .Select(t => t.Date)
            .ToHashSet();

        var streak = 0;
        for (var d = today; completedDays.Contains(d); d = d.AddDays(-1))
            streak++;
        return streak;
    }
}
