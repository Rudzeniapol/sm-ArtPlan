namespace GoalsBot.Application.Tasks;

public sealed class TaskNotFoundException(Guid taskId)
    : InvalidOperationException($"Task {taskId} was not found.")
{
    public Guid TaskId { get; } = taskId;
}
