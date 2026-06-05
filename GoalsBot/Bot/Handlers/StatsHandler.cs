using System.Globalization;
using System.Text;
using GoalsBot.Application.Stats;
using GoalsBot.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class StatsHandler(ITelegramBotClient bot, IStatsService stats) : IUpdateHandler
{
    public bool CanHandle(Update update) =>
        update.Type == UpdateType.Message &&
        CommandParsing.TryParseCommand(update.Message?.Text, "/stats", out _);

    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var msg = update.Message!;
        CommandParsing.TryParseCommand(msg.Text, "/stats", out var remainder);

        var period = remainder.Equals("month", StringComparison.OrdinalIgnoreCase)
            ? StatsPeriod.Month
            : StatsPeriod.Week;

        await bot.SendChatAction(msg.Chat.Id, ChatAction.Typing, cancellationToken: ct);

        var result = await stats.GetStatsAsync(msg.From!.Id, period, ct);
        await bot.SendMessage(msg.Chat.Id, Format(result), cancellationToken: ct);
    }

    private static string Format(StatsDto s)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine($"📊 Stats ({s.Period}, {s.FromInclusive.ToString("yyyy-MM-dd", inv)} → {s.ToInclusive.ToString("yyyy-MM-dd", inv)})");
        sb.AppendLine($"📝 Total: {s.TotalTasks}");
        sb.AppendLine($"✅ Completed: {s.CompletedTasks} ({s.CompletionRate.ToString("0.0", inv)}%)");
        sb.AppendLine($"⏱ Estimated: {s.TotalEstimatedMinutes} min  |  Completed: {s.TotalCompletedMinutes} min");
        sb.AppendLine("⚖️ By priority:");
        foreach (var kv in s.TasksByPriority.OrderByDescending(kv => kv.Key))
            sb.AppendLine($"  • {kv.Key}: {kv.Value}");
        sb.Append($"🔥 Streak: {s.StreakDays} day(s)");
        return sb.ToString();
    }
}
