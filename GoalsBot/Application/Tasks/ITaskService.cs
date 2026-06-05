namespace GoalsBot.Application.Tasks;

public interface ITaskService
{
    Task<List<TaskDto>> GetTasksForDayAsync(long userId, DateOnly date, CancellationToken ct);
    Task<TaskDto> CreateAsync(long userId, CreateTaskDto dto, CancellationToken ct);
    Task<TaskDto> UpdateAsync(Guid taskId, UpdateTaskDto dto, CancellationToken ct);
    Task DeleteAsync(Guid taskId, CancellationToken ct);
    Task MarkCompleteAsync(Guid taskId, CancellationToken ct);
}
