using GoalsBot.Domain.Enums;

namespace GoalsBot.Domain.Entities;

// Named TaskItem (not Task) to avoid collision with System.Threading.Tasks.Task.
// Maps to the "Tasks" table — see TaskItemConfiguration.
public sealed class TaskItem
{
    public Guid Id { get; set; }
    public Guid DailyGoalId { get; set; }
    public long UserId { get; set; }
    public DateOnly Date { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public int? EstimatedMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public DailyGoal? DailyGoal { get; set; }
    public User? User { get; set; }
}
