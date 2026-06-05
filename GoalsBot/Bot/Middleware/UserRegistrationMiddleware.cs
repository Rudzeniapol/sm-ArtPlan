using GoalsBot.Domain.Repositories;
using GoalsBot.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace GoalsBot.Bot.Middleware;

// Runs before any IUpdateHandler so the rest of the pipeline can assume the User row exists.
public sealed class UserRegistrationMiddleware(
    IUserRepository users,
    IOptions<BotOptions> botOptions)
{
    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var from = update.Message?.From ?? update.CallbackQuery?.From;
        if (from is null || from.IsBot) return;

        await users.UpsertAsync(from.Id, from.Username, botOptions.Value.DefaultTimeZoneId, ct);
    }
}
