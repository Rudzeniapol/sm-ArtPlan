using GoalsBot.Application.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class TasksHandler(
    ITelegramBotClient bot,
    ITaskService tasks,
    TimeProvider clock) : IUpdateHandler
{
    public bool CanHandle(Update update) =>
        update.Type == UpdateType.Message &&
        CommandParsing.TryParseCommand(update.Message?.Text, "/tasks", out _);

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var msg = update.Message!;
        CommandParsing.TryParseCommand(msg.Text, "/tasks", out var remainder);

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var date = CommandParsing.ParseDateOrToday(remainder, today);

        await bot.SendChatAction(msg.Chat.Id, Telegram.Bot.Types.Enums.ChatAction.Typing, cancellationToken: ct);

        var list = await tasks.GetTasksForDayAsync(msg.From!.Id, date, ct);
        var text = TaskFormatter.FormatList(list, date);
        var keyboard = TaskFormatter.BuildTaskKeyboard(list, date);

        await bot.SendMessage(msg.Chat.Id, text, replyMarkup: keyboard, cancellationToken: ct);
    }
}
