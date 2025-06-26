using Microsoft.Extensions.Options;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CompanyInfoBot;

public sealed class UpdateHandler(ITelegramBotClient bot, InnService inn, IOptions<BotSettings> opt, LastCommandMemory memory,ILogger<UpdateHandler> log)
{
    public async Task HandleAsync(Update u, CancellationToken ct)
    {
        if (u.Type != UpdateType.Message || u.Message!.Type != MessageType.Text) return;
        var chat = u.Message.Chat.Id;
        var text = (u.Message.Text ?? "").Trim();

        try
        {
            switch (text.Split(' ')[0])
            {
                case "/start":
                    await Reply("Привет! Отправь /help, чтобы увидеть команды.");
                    break;

                case "/help":
                    await Reply("""
                        /start — начать общение
                        /help  — список команд
                        /hello — контакты разработчика
                        /inn   <ИНН [ИНН...]> — информация о компаниях
                        /last  — повтор предыдущего ответа
                        """);
                    break;

                case "/hello":
                    var s = opt.Value;
                    await Reply($"{s.DeveloperName}\n{s.DeveloperEmail}\nGitHub: {s.GitHubUrl}\nHH: {s.HhUrl}");
                    break;

                case "/inn":
                    var inns = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1..];
                    if (inns.Length == 0)
                    {
                        await Reply("Укажите хотя бы один ИНН после /inn");
                        return;
                    }

                    var tasks = inns.Select(i => inn.GetCompanyAsync(i, ct));
                    var data = (await Task.WhenAll(tasks))
                                .Where(c => c is not null)!
                                .OrderBy(c => c!.Name);

                    if (!data.Any())
                    {
                        await Reply("По указанным ИНН ничего не найдено.");
                        return;
                    }

                    var sb = new StringBuilder();
                    foreach (var c in data)
                        sb.AppendLine($"• {c!.Name}\n  {c.Address}\n");

                    await Reply(sb.ToString().TrimEnd());
                    break;

                case "/last":
                    await Reply(memory.Get(chat) ?? "Я ещё ничего не отвечал.");
                    break;

                default:
                    if (text.StartsWith("/"))
                    {
                        await Reply("Неизвестная команда. Отправь /help, чтобы увидеть список команд.");
                    }
                    else
                    {
                        await Reply("Я не понимаю, что вы имеете в виду. Отправьте /help для получения помощи.");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Ошибка обработки команды");
            await bot.SendMessage(chat, "Упс, что-то пошло не так. Попробуйте позже.", cancellationToken: ct);
        }

        async Task Reply(string msg)
        {
            await bot.SendMessage(chat, msg, cancellationToken: ct);
            memory.Remember(chat, msg);
        }
    }
}
