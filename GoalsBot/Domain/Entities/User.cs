namespace GoalsBot.Domain.Entities;

public sealed class User
{
    public long Id { get; set; }
    public string? TelegramUsername { get; set; }
    public required string TimeZoneId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<DailyGoal> DailyGoals { get; set; } = new List<DailyGoal>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<CalendarSync> CalendarSyncs { get; set; } = new List<CalendarSync>();
}
