namespace GoalsBot.Application.Goals;

public interface IGoalService
{
    Task<DailyGoalDto> ParseAndSaveAsync(long userId, DateOnly date, string rawInput, CancellationToken ct);
}
