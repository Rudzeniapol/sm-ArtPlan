namespace GoalsBot.Domain.Entities;

public sealed class DailyGoal
{
    public Guid Id { get; set; }
    public long UserId { get; set; }
    public DateOnly Date { get; set; }
    public required string RawInput { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? User { get; set; }
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
