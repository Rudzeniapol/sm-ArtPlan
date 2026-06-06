using GoalsBot.Bot.Screens;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace GoalsBot.Bot.Handlers;

public sealed class StartCommandHandler(ScreenManager screens) : IUpdateHandler
{
    public bool CanHandle(Update update) =>
        update.Type == UpdateType.Message &&
        CommandParsing.TryParseCommand(update.Message?.Text, "/start", out _);

    public Task HandleAsync(Update update, CancellationToken ct)
    {
        var (text, markup) = Views.MainMenu();
        return screens.ShowAsync(update.Message!.Chat.Id, text, markup, ct);
    }
}
