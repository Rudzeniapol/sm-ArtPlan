using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Repositories;

namespace GoalsBot.Application.Tasks;

public sealed class TaskService(ITaskRepository repository) : ITaskService
{
    public async Task<List<TaskDto>> GetTasksForDayAsync(long userId, DateOnly date, CancellationToken ct)
    {
        var tasks = await repository.GetByUserAndDateAsync(userId, date, ct);
        return tasks.Select(Map).ToList();
    }

    public async Task<TaskDto> CreateAsync(long userId, CreateTaskDto dto, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new TaskItem
        {
            Id = Guid.NewGuid(),
            DailyGoalId = dto.DailyGoalId,
            UserId = userId,
            Date = dto.Date,
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            EstimatedMinutes = dto.EstimatedMinutes,
            IsCompleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<TaskDto> UpdateAsync(Guid taskId, UpdateTaskDto dto, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);

        if (dto.Title is not null) entity.Title = dto.Title;
        if (dto.Description is not null) entity.Description = dto.Description;
        if (dto.Priority is not null) entity.Priority = dto.Priority.Value;
        if (dto.EstimatedMinutes is not null) entity.EstimatedMinutes = dto.EstimatedMinutes;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid taskId, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);

        await repository.RemoveAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    public async Task MarkCompleteAsync(Guid taskId, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(taskId, ct)
            ?? throw new TaskNotFoundException(taskId);

        if (entity.IsCompleted) return;

        var now = DateTimeOffset.UtcNow;
        entity.IsCompleted = true;
        entity.CompletedAt = now;
        entity.UpdatedAt = now;

        await repository.SaveChangesAsync(ct);
    }

    private static TaskDto Map(TaskItem t) => new(
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
