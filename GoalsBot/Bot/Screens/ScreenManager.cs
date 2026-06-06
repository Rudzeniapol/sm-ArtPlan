using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.ReplyMarkups;

namespace GoalsBot.Bot.Screens;

// The single channel through which we render a "screen" (text + inline keyboard).
// Each call deletes the previous active screen message and sends a fresh one —
// the new screen always appears at the bottom of the chat. Failures to delete
// are logged but never bubble up: the user's view of the world is what matters.
public sealed class ScreenManager(
    ITelegramBotClient bot,
    IChatScreenStore store,
    ILogger<ScreenManager> logger)
{
    public async Task ShowAsync(long chatId, string text, InlineKeyboardMarkup? markup, CancellationToken ct)
    {
        await DeletePreviousAsync(chatId, ct);
        var sent = await bot.SendMessage(chatId, text, replyMarkup: markup, cancellationToken: ct);
        store.Set(chatId, sent.MessageId);
    }

    public async Task DeletePreviousAsync(long chatId, CancellationToken ct)
    {
        if (store.GetMessageId(chatId) is not int previous) return;
        await SafeDeleteAsync(chatId, previous, ct);
        store.Clear(chatId);
    }

    public Task TryDeleteUserMessageAsync(long chatId, int messageId, CancellationToken ct) =>
        SafeDeleteAsync(chatId, messageId, ct);

    private async Task SafeDeleteAsync(long chatId, int messageId, CancellationToken ct)
    {
        try
        {
            await bot.DeleteMessage(chatId, messageId, cancellationToken: ct);
        }
        catch (ApiRequestException ex)
        {
            // Common: "message can't be deleted" (>48h), or already gone. Non-fatal.
            logger.LogDebug(ex, "Failed to delete message {MessageId} in {ChatId}.", messageId, chatId);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unexpected error deleting message {MessageId} in {ChatId}.", messageId, chatId);
        }
    }
}
