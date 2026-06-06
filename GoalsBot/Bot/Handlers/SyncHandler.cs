using System.Globalization;
using GoalsBot.Application.Calendar;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class SyncHandler(
    ITelegramBotClient bot,
    ICalendarService calendar,
    TimeProvider clock) : IUpdateHandler
{
    public bool CanHandle(Update update) =>
        update.Type == UpdateType.Message &&
        CommandParsing.TryParseCommand(update.Message?.Text, "/sync", out _);

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var msg = update.Message!;
        CommandParsing.TryParseCommand(msg.Text, "/sync", out var remainder);

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var date = CommandParsing.ParseDateOrToday(remainder, today);

        await bot.SendChatAction(msg.Chat.Id, ChatAction.Typing, cancellationToken: ct);

        try
        {
            await calendar.SyncDayAsync(msg.From!.Id, date, ct);
        }
        catch (CalendarNotConfiguredException)
        {
            await bot.SendMessage(msg.Chat.Id, "Google Calendar isn't configured on this bot. See README for setup.", cancellationToken: ct);
            return;
        }

        var iso = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await bot.SendMessage(msg.Chat.Id, $"📅 Synced to Google Calendar for {iso}.", cancellationToken: ct);
    }
}
