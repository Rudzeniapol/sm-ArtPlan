using GoalsBot.Domain.Enums;

namespace GoalsBot.Application.Stats;

public sealed record StatsDto(
    StatsPeriod Period,
    DateOnly FromInclusive,
    DateOnly ToInclusive,
    int TotalTasks,
    int CompletedTasks,
    decimal CompletionRate,
    int TotalEstimatedMinutes,
    int TotalCompletedMinutes,
    IReadOnlyDictionary<TaskPriority, int> TasksByPriority,
    int StreakDays);
