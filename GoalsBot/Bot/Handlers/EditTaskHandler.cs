using GoalsBot.Application.Tasks;
using GoalsBot.Bot.Conversation;
using GoalsBot.Bot.Screens;
using GoalsBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class EditTaskHandler(
    ITaskService tasks,
    IConversationStateStore stateStore,
    ScreenManager screens,
    TasksHandler tasksHandler,
    TimeProvider clock) : IUpdateHandler
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
            await StartFromCommandAsync(chatId, remainder, ct);
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
                await StepEstimateAsync(chatId, msg.From!.Id, s, text, ct);
                break;
        }
    }

    private async Task StartFromCommandAsync(long chatId, string remainder, CancellationToken ct)
    {
        if (!Guid.TryParse(remainder, out var taskId))
        {
            await screens.ShowAsync(chatId, "Usage: /edit {taskId}", markup: null, ct);
            return;
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        await StartFlowAsync(chatId, taskId, today, ct);
    }

    // Entry point used by CallbackQueryHandler when user taps "✏️ Edit".
    public async Task StartFlowAsync(long chatId, Guid taskId, DateOnly date, CancellationToken ct)
    {
        stateStore.Set(chatId, new AwaitingEditTitle(taskId, date, new EditDraft()));
        await screens.ShowAsync(
            chatId,
            "✏️ Editing task\nSend a new title, or type /skip to keep the current one.",
            markup: null,
            ct);
    }

    private async Task StepTitleAsync(long chatId, AwaitingEditTitle state, string text, CancellationToken ct)
    {
        if (!string.Equals(text, SkipToken, StringComparison.OrdinalIgnoreCase))
        {
            state.Draft.Title = text.Length > 200 ? text[..200] : text;
            state.Draft.TitleSet = true;
        }

        stateStore.Set(chatId, new AwaitingEditDescription(state.TaskId, state.Date, state.Draft));
        await screens.ShowAsync(chatId, "Send a new description, or /skip.", markup: null, ct);
    }

    private async Task StepDescriptionAsync(long chatId, AwaitingEditDescription state, string text, CancellationToken ct)
    {
        if (!string.Equals(text, SkipToken, StringComparison.OrdinalIgnoreCase))
        {
            state.Draft.Description = text.Length > 1000 ? text[..1000] : text;
            state.Draft.DescriptionSet = true;
        }

        stateStore.Set(chatId, new AwaitingEditPriority(state.TaskId, state.Date, state.Draft));
        var (prText, prMarkup) = Views.PriorityPicker();
        await screens.ShowAsync(chatId, prText, prMarkup, ct);
    }

    private async Task StepEstimateAsync(long chatId, long userId, AwaitingEditEstimate state, string text, CancellationToken ct)
    {
        if (!string.Equals(text, SkipToken, StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(text, out var minutes) || minutes < 0)
            {
                await screens.ShowAsync(chatId, "Please send a positive number of minutes, or /skip.", markup: null, ct);
                return;
            }
            state.Draft.EstimatedMinutes = minutes;
            state.Draft.EstimateSet = true;
        }

        await PersistAndReturnAsync(chatId, userId, state.TaskId, state.Date, state.Draft, ct);
    }

    // Called by CallbackQueryHandler once the user taps a priority button.
    public async Task ApplyPriorityAsync(long chatId, TaskPriority? priority, CancellationToken ct)
    {
        if (stateStore.Get(chatId) is not AwaitingEditPriority state) return;

        state.Draft.Priority = priority;
        stateStore.Set(chatId, new AwaitingEditEstimate(state.TaskId, state.Date, state.Draft));
        await screens.ShowAsync(chatId, "Estimated minutes? Send a number, or /skip.", markup: null, ct);
    }

    private async Task PersistAndReturnAsync(long chatId, long userId, Guid taskId, DateOnly date, EditDraft draft, CancellationToken ct)
    {
        var dto = new UpdateTaskDto(
            draft.TitleSet ? draft.Title : null,
            draft.DescriptionSet ? draft.Description : null,
            draft.Priority,
            draft.EstimateSet ? draft.EstimatedMinutes : null);

        try
        {
            await tasks.UpdateAsync(taskId, dto, ct);
        }
        catch (TaskNotFoundException)
        {
            // Fall through to refresh — the tasks view will show whatever's there now.
        }
        finally
        {
            stateStore.Clear(chatId);
        }

        await tasksHandler.ShowTasksForDateAsync(chatId, userId, date, ct);
    }
}
