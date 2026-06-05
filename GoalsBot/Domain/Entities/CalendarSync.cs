namespace GoalsBot.Domain.Entities;

public sealed class CalendarSync
{
    public Guid Id { get; set; }
    public long UserId { get; set; }
    public DateOnly Date { get; set; }
    public required string GoogleEventId { get; set; }
    public DateTimeOffset LastSyncedAt { get; set; }

    public User? User { get; set; }
}
