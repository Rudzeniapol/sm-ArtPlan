using GoalsBot.Application.Tasks;
using GoalsBot.Bot.Screens;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class TasksHandler(
    ITelegramBotClient bot,
    ITaskService tasks,
    ScreenManager screens,
    TimeProvider clock) : IUpdateHandler
{
    public bool CanHandle(Update update) =>
        update.Type == UpdateType.Message &&
        CommandParsing.TryParseCommand(update.Message?.Text, "/tasks", out _);

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var msg = update.Message!;
        CommandParsing.TryParseCommand(msg.Text, "/tasks", out var remainder);

        var chatId = msg.Chat.Id;
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        if (string.IsNullOrWhiteSpace(remainder))
        {
            // No explicit date → show date picker so the user doesn't have to type.
            var (text, markup) = Views.DatePicker("t", today);
            await screens.ShowAsync(chatId, text, markup, ct);
            return;
        }

        var date = CommandParsing.ParseDateOrToday(remainder, today);
        await ShowTasksForDateAsync(chatId, msg.From!.Id, date, ct);
    }

    public async Task ShowTasksForDateAsync(long chatId, long userId, DateOnly date, CancellationToken ct)
    {
        await bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);
        var list = await tasks.GetTasksForDayAsync(userId, date, ct);
        var (text, markup) = Views.TasksList(date, list);
        await screens.ShowAsync(chatId, text, markup, ct);
    }
}
