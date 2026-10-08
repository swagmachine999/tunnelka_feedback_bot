using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var token = config["Bot:Token"] ?? throw new Exception("Не задан Bot:Token");
var adminId = long.Parse(config["Bot:AdminId"] ?? throw new Exception("Не задан Bot:AdminId"));

var bot = new TelegramBotClient(token);

bot.OnMessage += async (msg, type) =>
{
    if (msg.Chat.Type != ChatType.Private) return;

    if (msg.Chat.Id == adminId)
        await HandleAdmin(msg);
    else
        await HandleUser(msg);
};

bot.OnError += (ex, source) =>
{
    Console.WriteLine($"Ошибка: {ex.Message}");
    return Task.CompletedTask;
};

Console.WriteLine("Бот запущен.");
await Task.Delay(Timeout.Infinite);

async Task HandleUser(Message msg)
{
    if (msg.Text == "/start")
    {
        await bot.SendMessage(msg.Chat.Id, "Привет! Напиши сюда вопрос, идею или проблему с Tunnelka, и я отвечу.");
        return;
    }

    if (msg.Text is null)
    {
        await bot.SendMessage(msg.Chat.Id, "Пока я принимаю только текст.");
        return;
    }

    var name = msg.From?.Username is { } username ? "@" + username : msg.From?.FirstName ?? "без имени";

    await bot.SendMessage(adminId, $"От {name}\nid: {msg.Chat.Id}\n\n{msg.Text}");
    await bot.SendMessage(msg.Chat.Id, "Спасибо! Сообщение отправлено, ответ придёт сюда.");
}

async Task HandleAdmin(Message msg)
{
    var original = msg.ReplyToMessage?.Text;

    if (original is null || msg.Text is null)
    {
        await bot.SendMessage(adminId, "Чтобы ответить человеку, нажми «Ответить» на его сообщение и напиши текст.");
        return;
    }

    var match = Regex.Match(original, @"id: (\d+)");
    if (!match.Success)
    {
        await bot.SendMessage(adminId, "Не нашёл id в этом сообщении.");
        return;
    }

    var userId = long.Parse(match.Groups[1].Value);

    try
    {
        await bot.SendMessage(userId, msg.Text);
        await bot.SendMessage(adminId, "Отправлено.");
    }
    catch (Exception ex)
    {
        await bot.SendMessage(adminId, $"Не удалось отправить: {ex.Message}");
    }
}
