using System.Globalization;
using GoalsBot.Application.Calendar;
using GoalsBot.Application.Tasks;
using GoalsBot.Bot.Conversation;
using GoalsBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GoalsBot.Bot.Handlers;

public sealed class CallbackQueryHandler(
    ITelegramBotClient bot,
    ITaskService tasks,
    ICalendarService calendar,
    EditTaskHandler editHandler,
    IConversationStateStore stateStore,
    TimeProvider clock) : IUpdateHandler
{
    public bool CanHandle(Update update) =>
        update.Type == UpdateType.CallbackQuery && update.CallbackQuery?.Data is not null;

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var query = update.CallbackQuery!;
        var data = query.Data!;
        var chatId = query.Message!.Chat.Id;
        var userId = query.From.Id;

        var parts = data.Split(':');
        var action = parts[0];

        try
        {
            switch (action)
            {
                case "complete":
                    await HandleCompleteAsync(query, chatId, userId, parts, ct);
                    break;
                case "delete":
                    await HandleDeleteAsync(query, chatId, parts, ct);
                    break;
                case "edit":
                    await HandleEditAsync(query, chatId, parts, ct);
                    break;
                case "priority":
                    await HandlePriorityAsync(query, chatId, parts, ct);
                    break;
                case "sync":
                    await HandleSyncAsync(query, chatId, userId, parts, ct);
                    break;
                default:
                    await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
                    break;
            }
        }
        catch (TaskNotFoundException)
        {
            await bot.AnswerCallbackQuery(query.Id, "Task not found.", cancellationToken: ct);
        }
    }

    private async Task HandleCompleteAsync(CallbackQuery query, long chatId, long userId, string[] parts, CancellationToken ct)
    {
        // complete:{guid}                — show confirm
        // complete:confirm:{guid}        — apply
        // complete:cancel                — discard
        if (parts.Length == 2 && Guid.TryParse(parts[1], out var taskId))
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            var list = await tasks.GetTasksForDayAsync(userId, today, ct);
            var t = list.FirstOrDefault(x => x.Id == taskId);
            var title = t?.Title ?? taskId.ToString();

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Confirm", $"complete:confirm:{taskId}"),
                    InlineKeyboardButton.WithCallbackData("Cancel", "complete:cancel")
                }
            });

            await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
            await bot.SendMessage(chatId, $"Mark '{title}' as complete?", replyMarkup: keyboard, cancellationToken: ct);
            return;
        }

        if (parts.Length == 3 && parts[1] == "confirm" && Guid.TryParse(parts[2], out var confirmId))
        {
            await tasks.MarkCompleteAsync(confirmId, ct);
            await bot.AnswerCallbackQuery(query.Id, "Marked complete ✅", cancellationToken: ct);
            await bot.EditMessageText(chatId, query.Message!.MessageId, "✅ Done.", cancellationToken: ct);
            return;
        }

        if (parts.Length == 2 && parts[1] == "cancel")
        {
            await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
            await bot.EditMessageText(chatId, query.Message!.MessageId, "Cancelled.", cancellationToken: ct);
        }
    }

    private async Task HandleDeleteAsync(CallbackQuery query, long chatId, string[] parts, CancellationToken ct)
    {
        // delete:{guid}, delete:confirm:{guid}, delete:cancel
        if (parts.Length == 2 && Guid.TryParse(parts[1], out var taskId))
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Yes, delete", $"delete:confirm:{taskId}"),
                    InlineKeyboardButton.WithCallbackData("Cancel", "delete:cancel")
                }
            });
            await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
            await bot.SendMessage(chatId, "Delete this task?", replyMarkup: keyboard, cancellationToken: ct);
            return;
        }

        if (parts.Length == 3 && parts[1] == "confirm" && Guid.TryParse(parts[2], out var confirmId))
        {
            await tasks.DeleteAsync(confirmId, ct);
            await bot.AnswerCallbackQuery(query.Id, "Deleted 🗑", cancellationToken: ct);
            await bot.EditMessageText(chatId, query.Message!.MessageId, "🗑 Deleted.", cancellationToken: ct);
            return;
        }

        if (parts.Length == 2 && parts[1] == "cancel")
        {
            await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
            await bot.EditMessageText(chatId, query.Message!.MessageId, "Cancelled.", cancellationToken: ct);
        }
    }

    private async Task HandleEditAsync(CallbackQuery query, long chatId, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 2 || !Guid.TryParse(parts[1], out var taskId))
        {
            await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
            return;
        }

        stateStore.Set(chatId, new AwaitingEditTitle(taskId, new EditDraft()));
        await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
        await bot.SendMessage(chatId, "New title? Send the new text, or /skip to keep the current one.", cancellationToken: ct);
    }

    private async Task HandlePriorityAsync(CallbackQuery query, long chatId, string[] parts, CancellationToken ct)
    {
        // priority:{guid}:Low|Medium|High|skip
        if (parts.Length != 3 || !Guid.TryParse(parts[1], out var taskId))
        {
            await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
            return;
        }

        TaskPriority? priority = parts[2] switch
        {
            "Low" => TaskPriority.Low,
            "Medium" => TaskPriority.Medium,
            "High" => TaskPriority.High,
            _ => null
        };

        await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
        await editHandler.ApplyPriorityAsync(chatId, taskId, priority, ct);
    }

    private async Task HandleSyncAsync(CallbackQuery query, long chatId, long userId, string[] parts, CancellationToken ct)
    {
        if (parts.Length != 2 ||
            !DateOnly.TryParseExact(parts[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
            return;
        }

        await bot.AnswerCallbackQuery(query.Id, "Syncing…", cancellationToken: ct);
        await bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);
        await calendar.SyncDayAsync(userId, date, ct);
        await bot.SendMessage(chatId, $"📅 Synced to Google Calendar for {parts[1]}.", cancellationToken: ct);
    }
}
