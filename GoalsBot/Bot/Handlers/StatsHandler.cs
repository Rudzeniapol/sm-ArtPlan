using GoalsBot.Application.Stats;
using GoalsBot.Bot.Screens;
using GoalsBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class StatsHandler(
    ITelegramBotClient bot,
    IStatsService stats,
    ScreenManager screens) : IUpdateHandler
{
    public bool CanHandle(Update update) =>
        update.Type == UpdateType.Message &&
        CommandParsing.TryParseCommand(update.Message?.Text, "/stats", out _);

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var msg = update.Message!;
        CommandParsing.TryParseCommand(msg.Text, "/stats", out var remainder);

        if (string.IsNullOrWhiteSpace(remainder))
        {
            var (text, markup) = Views.StatsMenu();
            await screens.ShowAsync(msg.Chat.Id, text, markup, ct);
            return;
        }

        var period = remainder.Equals("month", StringComparison.OrdinalIgnoreCase)
            ? StatsPeriod.Month
            : StatsPeriod.Week;

        await ShowStatsAsync(msg.Chat.Id, msg.From!.Id, period, ct);
    }

    public async Task ShowStatsAsync(long chatId, long userId, StatsPeriod period, CancellationToken ct)
    {
        await bot.SendChatAction(chatId, ChatAction.Typing, cancellationToken: ct);
        var result = await stats.GetStatsAsync(userId, period, ct);
        var (text, markup) = Views.Stats(result);
        await screens.ShowAsync(chatId, text, markup, ct);
    }
}
