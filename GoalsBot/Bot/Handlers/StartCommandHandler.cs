using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class StartCommandHandler(ITelegramBotClient bot) : IUpdateHandler
{
    private const string Welcome =
        """
        👋 Hi! I'll help you plan and track your daily goals.

        Commands:
        /add — describe today's goals in free form
        /add YYYY-MM-DD — plan a specific day
        /tasks — show today's tasks
        /tasks YYYY-MM-DD — show a specific day's tasks
        /edit {taskId} — edit a task step by step
        /delete {taskId} — delete a task
        /stats week|month — see your stats
        /sync — push today's tasks to Google Calendar
        """;

    public bool CanHandle(Update update) =>
        update.Type == UpdateType.Message &&
        CommandParsing.TryParseCommand(update.Message?.Text, "/start", out _);

    public Task HandleAsync(Update update, CancellationToken ct) =>
        bot.SendMessage(update.Message!.Chat.Id, Welcome, cancellationToken: ct);
}
