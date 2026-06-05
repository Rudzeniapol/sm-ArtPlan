using GoalsBot.Application.Tasks;

namespace GoalsBot.Application.Goals;

public sealed record DailyGoalDto(
    Guid Id,
    long UserId,
    DateOnly Date,
    string RawInput,
    IReadOnlyList<TaskDto> Tasks,
    DateTimeOffset CreatedAt);
