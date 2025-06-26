using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace CompanyInfoBot;

public sealed class BotHostedService(ITelegramBotClient bot, IServiceProvider sp, ILogger<BotHostedService> log) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken ct)
    {
        var opts = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            DropPendingUpdates = true  
        };

        bot.StartReceiving(
            async (_, update, token) =>
            {
                using var scope = sp.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
                await handler.HandleAsync(update, token);
            },
            (_, ex, _) => log.LogError(ex, "Telegram polling error"),
            opts,
            cancellationToken: ct);

        log.LogInformation("Bot started");
        return Task.CompletedTask;
    }
}
