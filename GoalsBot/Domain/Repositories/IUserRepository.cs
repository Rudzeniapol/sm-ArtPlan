using GoalsBot.Domain.Entities;

namespace GoalsBot.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long telegramId, CancellationToken ct);
    Task<User> UpsertAsync(long telegramId, string? telegramUsername, string defaultTimeZoneId, CancellationToken ct);
}
