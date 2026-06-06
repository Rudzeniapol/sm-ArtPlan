using System.Globalization;
using GoalsBot.Application.Calendar;
using GoalsBot.Bot.Screens;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class SyncHandler(
    ITelegramBotClient bot,
    ICalendarService calendar,
    ScreenManager screens,
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

        if (string.IsNullOrWhiteSpace(remainder))
        {
            var (text, markup) = Views.DatePicker("s", today);
            await screens.ShowAsync(msg.Chat.Id, text, markup, ct);
            return;
        }

        var date = CommandParsing.ParseDateOrToday(remainder, today);
        await SyncAndAnnounceAsync(msg.Chat.Id, msg.From!.Id, date, ct);
    }

    public async Task SyncAndAnnounceAsync(long chatId, long userId, DateOnly date, CancellationToken ct)
    {
        await bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);
        await screens.ShowAsync(chatId, "📅 Syncing to Google Calendar…", markup: null, ct);

        try
        {
            await calendar.SyncDayAsync(userId, date, ct);
        }
        catch (CalendarNotConfiguredException)
        {
            await screens.ShowAsync(chatId, "Google Calendar isn't configured on this bot. See README for setup.", markup: null, ct);
            return;
        }

        var iso = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var (text, markup) = Views.MainMenu();
        await screens.ShowAsync(chatId, $"📅 Synced {iso} to Google Calendar.\n\n" + text, markup, ct);
    }
}
