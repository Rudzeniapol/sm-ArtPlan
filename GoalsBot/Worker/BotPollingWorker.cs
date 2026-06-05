using GoalsBot.Bot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace GoalsBot.Worker;

public sealed class BotPollingWorker(
    ITelegramBotClient bot,
    IServiceScopeFactory scopeFactory,
    ILogger<BotPollingWorker> logger)
    : BackgroundService, Telegram.Bot.Polling.IUpdateHandler
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await bot.GetMe(stoppingToken);
        logger.LogInformation("Bot started: @{Username}", me.Username);

        var options = new ReceiverOptions { AllowedUpdates = [] };
        await bot.ReceiveAsync(this, options, stoppingToken);
    }

    async Task Telegram.Bot.Polling.IUpdateHandler.HandleUpdateAsync(
        ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        // Open a fresh DI scope per update so scoped services (DbContext, repos) don't leak.
        using var scope = scopeFactory.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<UpdateDispatcher>();
        await dispatcher.DispatchAsync(update, cancellationToken);
    }

    Task Telegram.Bot.Polling.IUpdateHandler.HandleErrorAsync(
        ITelegramBotClient _, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram polling error from {Source}.", source);
        return Task.CompletedTask;
    }
}
