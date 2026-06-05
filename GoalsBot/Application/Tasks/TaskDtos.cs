using GoalsBot.Domain.Enums;

namespace GoalsBot.Application.Tasks;

public sealed record TaskDto(
    Guid Id,
    Guid DailyGoalId,
    long UserId,
    DateOnly Date,
    string Title,
    string? Description,
    TaskPriority Priority,
    int? EstimatedMinutes,
    bool IsCompleted,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateTaskDto(
    Guid DailyGoalId,
    DateOnly Date,
    string Title,
    string? Description,
    TaskPriority Priority,
    int? EstimatedMinutes);

public sealed record UpdateTaskDto(
    string? Title,
    string? Description,
    TaskPriority? Priority,
    int? EstimatedMinutes);
