using System.Globalization;
using GoalsBot.Application.Tasks;
using GoalsBot.Bot.Screens;
using GoalsBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

// Single entry point for every inline-button callback. Each branch:
//   1. always answers the callback (removes the spinner),
//   2. delegates to the relevant handler so /tasks-style refresh logic stays in one place.
public sealed class CallbackQueryHandler(
    ITelegramBotClient bot,
    ITaskService tasks,
    ScreenManager screens,
    AddGoalHandler addHandler,
    TasksHandler tasksHandler,
    StatsHandler statsHandler,
    SyncHandler syncHandler,
    EditTaskHandler editHandler,
    TimeProvider clock) : IUpdateHandler
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public bool CanHandle(Update update) =>
        update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.Data is not null;

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var query = update.CallbackQuery!;
        var data = query.Data!;
        var chatId = query.Message!.Chat.Id;
        var userId = query.From.Id;

        // Always answer the callback first to remove the loading spinner.
        await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);

        try
        {
            await RouteAsync(data, chatId, userId, ct);
        }
        catch (TaskNotFoundException)
        {
            await screens.ShowAsync(chatId, "That task no longer exists.", markup: null, ct);
        }
    }

    private async Task RouteAsync(string data, long chatId, long userId, CancellationToken ct)
    {
        // ---- Menu ---------------------------------------------------------
        if (data == Cb.Menu)
        {
            var (text, markup) = Views.MainMenu();
            await screens.ShowAsync(chatId, text, markup, ct);
            return;
        }
        if (data == Cb.MenuAdd)   { await ShowPickerAsync(chatId, "a", ct); return; }
        if (data == Cb.MenuTasks) { await ShowPickerAsync(chatId, "t", ct); return; }
        if (data == Cb.MenuSync)  { await ShowPickerAsync(chatId, "s", ct); return; }
        if (data == Cb.MenuStats)
        {
            var (text, markup) = Views.StatsMenu();
            await screens.ShowAsync(chatId, text, markup, ct);
            return;
        }
        if (data == Cb.StatsWeek)  { await statsHandler.ShowStatsAsync(chatId, userId, StatsPeriod.Week,  ct); return; }
        if (data == Cb.StatsMonth) { await statsHandler.ShowStatsAsync(chatId, userId, StatsPeriod.Month, ct); return; }

        // ---- Date picker --------------------------------------------------
        if (data == Cb.PickCancel)
        {
            var (text, markup) = Views.MainMenu();
            await screens.ShowAsync(chatId, text, markup, ct);
            return;
        }
        if (TryStripPrefix(data, Cb.PickAddPrefix, out var addDateStr) && TryParseDate(addDateStr, out var addDate))
        {
            await addHandler.PromptForGoalsAsync(chatId, addDate, ct);
            return;
        }
        if (TryStripPrefix(data, Cb.PickTasksPrefix, out var tDateStr) && TryParseDate(tDateStr, out var tDate))
        {
            await tasksHandler.ShowTasksForDateAsync(chatId, userId, tDate, ct);
            return;
        }
        if (TryStripPrefix(data, Cb.PickSyncPrefix, out var sDateStr) && TryParseDate(sDateStr, out var sDate))
        {
            await syncHandler.SyncAndAnnounceAsync(chatId, userId, sDate, ct);
            return;
        }

        // ---- Tasks-view refresh / back ------------------------------------
        if (TryStripPrefix(data, Cb.TasksPrefix, out var refreshDateStr) && TryParseDate(refreshDateStr, out var refreshDate))
        {
            await tasksHandler.ShowTasksForDateAsync(chatId, userId, refreshDate, ct);
            return;
        }

        // ---- Complete -----------------------------------------------------
        if (TryStripPrefix(data, Cb.CompleteConfirmPrefix, out var ccBody) &&
            TryParseTaskAndDate(ccBody, out var ccTaskId, out var ccDate))
        {
            await tasks.MarkCompleteAsync(ccTaskId, ct);
            // Refresh the tasks view so the buttons for the just-completed task disappear.
            await tasksHandler.ShowTasksForDateAsync(chatId, userId, ccDate, ct);
            return;
        }
        if (TryStripPrefix(data, Cb.CompleteCancelPrefix, out var cxDateStr) && TryParseDate(cxDateStr, out var cxDate))
        {
            await tasksHandler.ShowTasksForDateAsync(chatId, userId, cxDate, ct);
            return;
        }
        if (TryStripPrefix(data, Cb.CompletePrefix, out var cBody) &&
            TryParseTaskAndDate(cBody, out var cTaskId, out var cDate))
        {
            var iso = cDate.ToString("yyyy-MM-dd", Inv);
            var (text, markup) = Views.Confirm(
                "Mark this task as complete?",
                "Confirm",
                $"{Cb.CompleteConfirmPrefix}{cTaskId}:{iso}",
                $"{Cb.CompleteCancelPrefix}{iso}");
            await screens.ShowAsync(chatId, text, markup, ct);
            return;
        }

        // ---- Delete -------------------------------------------------------
        if (TryStripPrefix(data, Cb.DeleteConfirmPrefix, out var dcBody) &&
            TryParseTaskAndDate(dcBody, out var dcTaskId, out var dcDate))
        {
            await tasks.DeleteAsync(dcTaskId, ct);
            await tasksHandler.ShowTasksForDateAsync(chatId, userId, dcDate, ct);
            return;
        }
        if (TryStripPrefix(data, Cb.DeleteCancelPrefix, out var dxDateStr) && TryParseDate(dxDateStr, out var dxDate))
        {
            await tasksHandler.ShowTasksForDateAsync(chatId, userId, dxDate, ct);
            return;
        }
        if (TryStripPrefix(data, Cb.DeletePrefix, out var dBody) &&
            TryParseTaskAndDate(dBody, out var dTaskId, out var dDate))
        {
            var iso = dDate.ToString("yyyy-MM-dd", Inv);
            var (text, markup) = Views.Confirm(
                "Delete this task?",
                "Yes, delete",
                $"{Cb.DeleteConfirmPrefix}{dTaskId}:{iso}",
                $"{Cb.DeleteCancelPrefix}{iso}");
            await screens.ShowAsync(chatId, text, markup, ct);
            return;
        }

        // ---- Edit ---------------------------------------------------------
        if (TryStripPrefix(data, Cb.EditPrefix, out var eBody) &&
            TryParseTaskAndDate(eBody, out var eTaskId, out var eDate))
        {
            await editHandler.StartFlowAsync(chatId, eTaskId, eDate, ct);
            return;
        }

        // ---- Priority (during /edit) --------------------------------------
        if (TryStripPrefix(data, Cb.PriorityPrefix, out var prValue))
        {
            TaskPriority? priority = prValue switch
            {
                "Low" => TaskPriority.Low,
                "Medium" => TaskPriority.Medium,
                "High" => TaskPriority.High,
                _ => null
            };
            await editHandler.ApplyPriorityAsync(chatId, priority, ct);
            return;
        }

        // Unknown callback — silently ignore (spinner was already cleared).
    }

    private async Task ShowPickerAsync(long chatId, string kind, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var (text, markup) = Views.DatePicker(kind, today);
        await screens.ShowAsync(chatId, text, markup, ct);
    }

    private static bool TryStripPrefix(string data, string prefix, out string remainder)
    {
        if (data.StartsWith(prefix, StringComparison.Ordinal))
        {
            remainder = data[prefix.Length..];
            return true;
        }
        remainder = string.Empty;
        return false;
    }

    private static bool TryParseDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", Inv, DateTimeStyles.None, out date);

    private static bool TryParseTaskAndDate(string body, out Guid taskId, out DateOnly date)
    {
        // body has the form "{guid}:{yyyy-MM-dd}"
        var colon = body.IndexOf(':');
        if (colon > 0 &&
            Guid.TryParse(body[..colon], out taskId) &&
            DateOnly.TryParseExact(body[(colon + 1)..], "yyyy-MM-dd", Inv, DateTimeStyles.None, out date))
        {
            return true;
        }
        taskId = Guid.Empty;
        date = default;
        return false;
    }
}
