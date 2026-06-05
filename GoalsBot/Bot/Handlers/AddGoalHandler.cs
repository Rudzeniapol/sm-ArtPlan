using System.Globalization;
using GoalsBot.Application.Goals;
using GoalsBot.Application.Tasks;
using GoalsBot.Bot.Conversation;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class AddGoalHandler(
    ITelegramBotClient bot,
    IGoalService goalService,
    ITaskService taskService,
    IConversationStateStore stateStore,
    TimeProvider clock) : IUpdateHandler
{
    public bool CanHandle(Update update)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text is null)
            return false;

        if (CommandParsing.TryParseCommand(update.Message.Text, "/add", out _))
            return true;

        if (stateStore.Get(update.Message.Chat.Id) is not AwaitingGoalText)
            return false;

        // Any other slash-command escapes the pending flow — let it route normally.
        return !update.Message.Text.TrimStart().StartsWith('/');
    }

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var msg = update.Message!;
        var chatId = msg.Chat.Id;

        if (CommandParsing.TryParseCommand(msg.Text, "/add", out var remainder))
        {
            await StartFlowAsync(chatId, remainder, ct);
            return;
        }

        if (stateStore.Get(chatId) is AwaitingGoalText pending)
        {
            await CompleteFlowAsync(msg, pending, ct);
        }
    }

    private async Task StartFlowAsync(long chatId, string remainder, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var date = CommandParsing.ParseDateOrToday(remainder, today);

        stateStore.Set(chatId, new AwaitingGoalText(date));

        var prompt = $"Tell me your goals for {date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}. Write freely.";
        await bot.SendMessage(chatId, prompt, cancellationToken: ct);
    }

    private async Task CompleteFlowAsync(Message msg, AwaitingGoalText pending, CancellationToken ct)
    {
        var chatId = msg.Chat.Id;
        var userId = msg.From!.Id;
        var rawInput = msg.Text!;

        await bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

        try
        {
            await goalService.ParseAndSaveAsync(userId, pending.Date, rawInput, ct);
        }
        finally
        {
            stateStore.Clear(chatId);
        }

        var allTasks = await taskService.GetTasksForDayAsync(userId, pending.Date, ct);
        var text = TaskFormatter.FormatList(allTasks, pending.Date);
        var keyboard = TaskFormatter.BuildTaskKeyboard(allTasks, pending.Date);

        await bot.SendMessage(chatId, text, replyMarkup: keyboard, cancellationToken: ct);
    }
}
