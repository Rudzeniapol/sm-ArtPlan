using GoalsBot.Application.Goals;
using GoalsBot.Application.Tasks;
using GoalsBot.Bot.Conversation;
using GoalsBot.Bot.Screens;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class AddGoalHandler(
    ITelegramBotClient bot,
    IGoalService goalService,
    ITaskService taskService,
    IConversationStateStore stateStore,
    ScreenManager screens,
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

        if (string.IsNullOrWhiteSpace(remainder))
        {
            // No date typed → show date picker.
            var (text, markup) = Views.DatePicker("a", today);
            await screens.ShowAsync(chatId, text, markup, ct);
            return;
        }

        var date = CommandParsing.ParseDateOrToday(remainder, today);
        await PromptForGoalsAsync(chatId, date, ct);
    }

    public async Task PromptForGoalsAsync(long chatId, DateOnly date, CancellationToken ct)
    {
        stateStore.Set(chatId, new AwaitingGoalText(date));
        var (text, markup) = Views.GoalPrompt(date);
        await screens.ShowAsync(chatId, text, markup, ct);
    }

    private async Task CompleteFlowAsync(Message msg, AwaitingGoalText pending, CancellationToken ct)
    {
        var chatId = msg.Chat.Id;
        var userId = msg.From!.Id;
        var rawInput = msg.Text!;

        // Step 1: show the "Analyzing…" message immediately so the user knows
        // we received their text. This replaces the goal-prompt view.
        await screens.ShowAsync(chatId, Views.AnalyzingText, markup: null, ct);
        await bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);

        try
        {
            await goalService.ParseAndSaveAsync(userId, pending.Date, rawInput, ct);
        }
        finally
        {
            stateStore.Clear(chatId);
        }

        // Step 2: replace "Analyzing…" with the daily plan view.
        var list = await taskService.GetTasksForDayAsync(userId, pending.Date, ct);
        var (text, markup) = Views.TasksList(pending.Date, list);
        await screens.ShowAsync(chatId, text, markup, ct);
    }
}
