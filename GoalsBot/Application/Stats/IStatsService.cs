using GoalsBot.Domain.Enums;

namespace GoalsBot.Application.Stats;

public interface IStatsService
{
    Task<StatsDto> GetStatsAsync(long userId, StatsPeriod period, CancellationToken ct);
}
