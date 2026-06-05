using Telegram.Bot.Types;

namespace GoalsBot.Bot;

// Our app-level routing interface. Distinct from Telegram.Bot.Polling.IUpdateHandler
// (which the BotPollingWorker implements to consume the receiver loop).
public interface IUpdateHandler
{
    bool CanHandle(Update update);
    Task HandleAsync(Update update, CancellationToken ct);
}
