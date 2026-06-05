using GoalsBot.Application.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GoalsBot.Bot.Handlers;

public sealed class DeleteTaskHandler(ITelegramBotClient bot, ITaskService tasks) : IUpdateHandler
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
            await bot.SendMessage(msg.Chat.Id, "Usage: /delete {taskId}", cancellationToken: ct);
            return;
        }

        var existing = (await tasks.GetTasksForDayAsync(msg.From!.Id, DateOnly.FromDateTime(DateTime.UtcNow), ct))
            .FirstOrDefault(t => t.Id == taskId);
        // We don't need to enforce date — just show a confirmation that references the title if we have it.
        var title = existing?.Title ?? taskId.ToString();

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Yes, delete", $"delete:confirm:{taskId}"),
                InlineKeyboardButton.WithCallbackData("Cancel", "delete:cancel")
            }
        });

        await bot.SendMessage(msg.Chat.Id, $"Delete '{title}'?", replyMarkup: keyboard, cancellationToken: ct);
    }
}
