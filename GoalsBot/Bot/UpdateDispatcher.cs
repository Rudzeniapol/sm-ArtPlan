using GoalsBot.Bot.Middleware;
using GoalsBot.Bot.Screens;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot;

public sealed class UpdateDispatcher(
    UserRegistrationMiddleware userRegistration,
    IEnumerable<IUpdateHandler> handlers,
    ScreenManager screens,
    ITelegramBotClient bot,
    ILogger<UpdateDispatcher> logger)
{
    public async Task DispatchAsync(Update update, CancellationToken ct)
    {
        try
        {
            await userRegistration.HandleAsync(update, ct);

            var handler = handlers.FirstOrDefault(h => h.CanHandle(update));
            if (handler is null)
            {
                logger.LogDebug("No handler for update {UpdateId} (type {Type}).", update.Id, update.Type);
                return;
            }

            await handler.HandleAsync(update, ct);

            // Single-screen UX: hide the user's command/text after we've handled it,
            // so only the bot's current view remains. Works in private chats; in
            // groups the bot needs delete-message perms (silently ignored if not).
            if (update.Type == UpdateType.Message && update.Message is { } msg)
                await screens.TryDeleteUserMessageAsync(msg.Chat.Id, msg.MessageId, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Update {UpdateId} failed.", update.Id);
            await TryNotifyUserAsync(update, ct);
        }
    }

    private async Task TryNotifyUserAsync(Update update, CancellationToken ct)
    {
        var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
        if (chatId is null) return;

        try
        {
            await bot.SendMessage(chatId, "⚠️ Something went wrong. Please try again in a moment.", cancellationToken: ct);
        }
        catch (Exception notifyEx)
        {
            logger.LogWarning(notifyEx, "Failed to notify user about prior error.");
        }
    }
}
