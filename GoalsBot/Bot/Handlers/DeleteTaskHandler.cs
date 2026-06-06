using GoalsBot.Bot.Screens;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

// Backstop for typed `/delete {taskId}` — the primary delete path is the 🗑
// button in the tasks view (handled by CallbackQueryHandler).
public sealed class DeleteTaskHandler(
    ScreenManager screens,
    TimeProvider clock) : IUpdateHandler
{
    public bool CanHandle(Update update) =>
        update.Type == UpdateType.Message &&
        CommandParsing.TryParseCommand(update.Message?.Text, "/delete", out _);

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var msg = update.Message!;
        CommandParsing.TryParseCommand(msg.Text, "/delete", out var remainder);

        if (!Guid.TryParse(remainder, out var taskId))
        {
            await screens.ShowAsync(msg.Chat.Id, "Usage: /delete {taskId} — or tap 🗑 on the task in /tasks.", markup: null, ct);
            return;
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var (text, markup) = Views.Confirm(
            "Delete this task?",
            confirmLabel: "Yes, delete",
            confirmCallback: $"{Cb.DeleteConfirmPrefix}{taskId}:{today:yyyy-MM-dd}",
            cancelCallback:  $"{Cb.DeleteCancelPrefix}{today:yyyy-MM-dd}");
        await screens.ShowAsync(msg.Chat.Id, text, markup, ct);
    }
}
