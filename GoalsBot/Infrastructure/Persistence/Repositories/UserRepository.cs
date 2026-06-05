using GoalsBot.Domain.Entities;
using GoalsBot.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GoalsBot.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(long telegramId, CancellationToken ct) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == telegramId, ct);

    public async Task<User> UpsertAsync(long telegramId, string? telegramUsername, string defaultTimeZoneId, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == telegramId, ct);
        if (user is null)
        {
            user = new User
            {
                Id = telegramId,
                TelegramUsername = telegramUsername,
                TimeZoneId = defaultTimeZoneId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Users.Add(user);
        }
        else if (user.TelegramUsername != telegramUsername)
        {
            user.TelegramUsername = telegramUsername;
        }

        await db.SaveChangesAsync(ct);
        return user;
    }
}
