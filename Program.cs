using CompanyInfoBot;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Telegram.Bot;

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.AddJsonFile("appsettings.json", optional: true)
           .AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<BotSettings>(ctx.Configuration.GetSection("Bot"));

        var botToken = ctx.Configuration["Bot:TelegramToken"]
                    ?? Environment.GetEnvironmentVariable("Bot__TelegramToken")
                    ?? throw new ArgumentNullException("Telegram token not found");

        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

        services.AddHttpClient<InnService>();
        services.AddSingleton<LastCommandMemory>();
        services.AddSingleton<UpdateHandler>();
        services.AddHostedService<BotHostedService>();
    })
    .Build()
    .Run();
