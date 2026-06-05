using GoalsBot.Application.Tasks;
using GoalsBot.Bot.Conversation;
using GoalsBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GoalsBot.Bot.Handlers;

public sealed class EditTaskHandler(
    ITelegramBotClient bot,
    ITaskService tasks,
    IConversationStateStore stateStore) : IUpdateHandler
{
    private const string SkipToken = "/skip";

    public bool CanHandle(Update update)
    {
        if (update.Type != UpdateType.Message || update.Message?.Text is null) return false;

        if (CommandParsing.TryParseCommand(update.Message.Text, "/edit", out _))
            return true;

        var state = stateStore.Get(update.Message.Chat.Id);
        if (state is not (AwaitingEditTitle or AwaitingEditDescription or AwaitingEditEstimate))
            return false;

        // Allow /skip inside the flow; any other slash-command escapes it.
        var text = update.Message.Text.TrimStart();
        return !text.StartsWith('/') || text.Equals(SkipToken, StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var msg = update.Message!;
        var chatId = msg.Chat.Id;
        var text = msg.Text!.Trim();

        if (CommandParsing.TryParseCommand(text, "/edit", out var remainder))
        {
            await StartFlowAsync(chatId, remainder, ct);
            return;
        }

        var state = stateStore.Get(chatId);
        switch (state)
        {
            case AwaitingEditTitle s:
                await StepTitleAsync(chatId, s, text, ct);
                break;
            case AwaitingEditDescription s:
                await StepDescriptionAsync(chatId, s, text, ct);
                break;
            case AwaitingEditEstimate s:
                await StepEstimateAsync(chatId, s, text, ct);
                break;
        }
    }

    private async Task StartFlowAsync(long chatId, string remainder, CancellationToken ct)
    {
        if (!Guid.TryParse(remainder, out var taskId))
        {
            await bot.SendMessage(chatId, "Usage: /edit {taskId}", cancellationToken: ct);
            return;
        }

        stateStore.Set(chatId, new AwaitingEditTitle(taskId, new EditDraft()));
        await bot.SendMessage(chatId, "New title? Send the new text, or /skip to keep the current one.", cancellationToken: ct);
    }

    private async Task StepTitleAsync(long chatId, AwaitingEditTitle state, string text, CancellationToken ct)
    {
        if (!string.Equals(text, SkipToken, StringComparison.OrdinalIgnoreCase))
        {
            state.Draft.Title = text.Length > 200 ? text[..200] : text;
            state.Draft.TitleSet = true;
        }
        stateStore.Set(chatId, new AwaitingEditDescription(state.TaskId, state.Draft));
        await bot.SendMessage(chatId, "New description? Send the text, or /skip.", cancellationToken: ct);
    }

    private async Task StepDescriptionAsync(long chatId, AwaitingEditDescription state, string text, CancellationToken ct)
    {
        if (!string.Equals(text, SkipToken, StringComparison.OrdinalIgnoreCase))
        {
            state.Draft.Description = text.Length > 1000 ? text[..1000] : text;
            state.Draft.DescriptionSet = true;
        }
        stateStore.Set(chatId, new AwaitingEditPriority(state.TaskId, state.Draft));

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Low", $"priority:{state.TaskId}:Low"),
                InlineKeyboardButton.WithCallbackData("Medium", $"priority:{state.TaskId}:Medium"),
                InlineKeyboardButton.WithCallbackData("High", $"priority:{state.TaskId}:High")
            },
            new[] { InlineKeyboardButton.WithCallbackData("Skip", $"priority:{state.TaskId}:skip") }
        });
        await bot.SendMessage(chatId, "Pick a priority:", replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task StepEstimateAsync(long chatId, AwaitingEditEstimate state, string text, CancellationToken ct)
    {
        if (!string.Equals(text, SkipToken, StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(text, out var minutes) || minutes < 0)
            {
                await bot.SendMessage(chatId, "Please send a positive number of minutes, or /skip.", cancellationToken: ct);
                return;
            }
            state.Draft.EstimatedMinutes = minutes;
            state.Draft.EstimateSet = true;
        }

        await PersistAsync(chatId, state.TaskId, state.Draft, ct);
    }

    // Called by CallbackQueryHandler once the user taps a priority button.
    public async Task ApplyPriorityAsync(long chatId, Guid taskId, TaskPriority? priority, CancellationToken ct)
    {
        if (stateStore.Get(chatId) is not AwaitingEditPriority state || state.TaskId != taskId)
            return;

        state.Draft.Priority = priority;
        stateStore.Set(chatId, new AwaitingEditEstimate(taskId, state.Draft));
        await bot.SendMessage(chatId, "Estimated minutes? Send a number, or /skip.", cancellationToken: ct);
    }

    private async Task PersistAsync(long chatId, Guid taskId, EditDraft draft, CancellationToken ct)
    {
        var dto = new UpdateTaskDto(
            draft.TitleSet ? draft.Title : null,
            draft.DescriptionSet ? draft.Description : null,
            draft.Priority,
            draft.EstimateSet ? draft.EstimatedMinutes : null);

        try
        {
            var updated = await tasks.UpdateAsync(taskId, dto, ct);
            stateStore.Clear(chatId);
            await bot.SendMessage(chatId, $"✅ Updated: {updated.Title} ({updated.Priority})", cancellationToken: ct);
        }
        catch (TaskNotFoundException)
        {
            stateStore.Clear(chatId);
            await bot.SendMessage(chatId, "Task not found.", cancellationToken: ct);
        }
    }
}
