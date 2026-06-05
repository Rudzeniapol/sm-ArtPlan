using GoalsBot.Application.Tasks;
using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Repositories;
using GoalsBot.Infrastructure.LlmApi;
using Microsoft.Extensions.Logging;

namespace GoalsBot.Application.Goals;

public sealed class GoalService(
    IGoalRepository goalRepository,
    ITaskRepository taskRepository,
    ILlmClient llmClient,
    ILogger<GoalService> logger) : IGoalService
{
    public async Task<DailyGoalDto> ParseAndSaveAsync(long userId, DateOnly date, string rawInput, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            throw new ArgumentException("Raw input must not be empty.", nameof(rawInput));

        var parsed = await llmClient.ParseGoalsAsync(rawInput, ct);
        logger.LogInformation("LLM extracted {Count} tasks for user {UserId} on {Date}.", parsed.Count, userId, date);

        var now = DateTimeOffset.UtcNow;
        var goal = new DailyGoal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = date,
            RawInput = rawInput,
            CreatedAt = now
        };

        var tasks = parsed.Select(p => new TaskItem
        {
            Id = Guid.NewGuid(),
            DailyGoalId = goal.Id,
            UserId = userId,
            Date = date,
            Title = Truncate(p.Title, 200),
            Description = p.Description is null ? null : Truncate(p.Description, 1000),
            Priority = p.Priority,
            EstimatedMinutes = p.EstimatedMinutes,
            IsCompleted = false,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        await goalRepository.AddAsync(goal, ct);
        foreach (var t in tasks)
            await taskRepository.AddAsync(t, ct);

        // Single SaveChangesAsync flushes the goal and all tasks in one transaction.
        await goalRepository.SaveChangesAsync(ct);

        return new DailyGoalDto(
            goal.Id,
            goal.UserId,
            goal.Date,
            goal.RawInput,
            tasks.Select(MapTask).ToList(),
            goal.CreatedAt);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static TaskDto MapTask(TaskItem t) => new(
        t.Id,
        t.DailyGoalId,
        t.UserId,
        t.Date,
        t.Title,
        t.Description,
        t.Priority,
        t.EstimatedMinutes,
        t.IsCompleted,
        t.CompletedAt,
        t.CreatedAt,
        t.UpdatedAt);
}
