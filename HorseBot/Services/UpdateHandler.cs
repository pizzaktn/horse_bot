using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace HorseBot.Services;

public class UpdateHandler : IUpdateHandler
{   
    private readonly ILogger<UpdateHandler> _logger;
    private readonly GoogleSheetsService _sheetsService;
    
    public UpdateHandler(ITelegramBotClient bot, ILogger<UpdateHandler> logger, GoogleSheetsService sheetsService)
    {
        _logger = logger;
        _sheetsService = sheetsService;
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HandleError: {Exception}", exception);
        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
    {
        if (update.Type != UpdateType.Message) return;
        var msg = update.Message;
        if (msg.Type != MessageType.Text) return;

        var parts = msg.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0].ToLower();
        var name = parts.Length > 1 ? parts[1] : null;

        switch (command)
        {
            case "/add_student":
                if (name == null) return;
                await _sheetsService.AddStudentAsync(name);
                await bot.SendMessage(msg.Chat.Id, $"Ученик {name} добавлен.");
                break;
            case "/pay":
                if (parts.Length < 3 || !int.TryParse(parts[2], out int count)) return;
                await _sheetsService.AddPaymentAsync(name, count);
                await bot.SendMessage(msg.Chat.Id, $"Добавлено {count} занятий для {name}.");
                break;
            case "/attend":
                if (name == null) return;
                var success = await _sheetsService.RegisterAttendanceAsync(name);
                var message = success
                    ? $"Посещение для {name} зарегистрировано, занятие вычтено."
                    : $"У {name} нет оплаченных занятий!";
                await bot.SendMessage(msg.Chat.Id, message);
                break;
            case "/status":
                if (name == null) return;
                var status = await _sheetsService.GetStatusAsync(name);
                await bot.SendMessage(msg.Chat.Id, status);
                break;
            case "/students":
                var list = await _sheetsService.GetAllStudentsAsync();
                await bot.SendMessage(msg.Chat.Id, list);
                break;
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Polling error: {exception.Message}");
        return Task.CompletedTask;
    }

    //public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
    //{
    //    if (update.Message?.Text is not { } messageText)
    //        return;

    //    var chatId = update.Message.Chat.Id;

    //    if (messageText.StartsWith("/add_student"))
    //    {
    //        var name = messageText.Replace("/add_student", "").Trim();
    //        await _sheetsService.AddStudentAsync(name, chatId);
    //        await bot.SendMessage(chatId, $"Ученик {name} добавлен", cancellationToken: token);
    //    }
    //    else if (messageText.StartsWith("/balance"))
    //    {
    //        var balance = await _sheetsService.GetBalanceAsync(chatId);
    //        await bot.SendMessage(chatId, $"Осталось занятий: {balance}", cancellationToken: token);
    //    }
    //    else if (messageText.StartsWith("/add_payment"))
    //    {
    //        var parts = messageText.Replace("/add_payment", "").Trim().Split(' ');
    //        if (parts.Length == 2 && int.TryParse(parts[0], out int amount) && int.TryParse(parts[1], out int lessons))
    //        {
    //            await _sheetsService.AddPaymentAsync(chatId, amount, lessons);
    //            await bot.SendMessage(chatId, "Оплата добавлена.", cancellationToken: token);
    //        }
    //        else
    //        {
    //            await bot.SendMessage(chatId, "Используй формат: /add_payment 3000 5", cancellationToken: token);
    //        }
    //    }
    //    else
    //    {
    //        await bot.SendMessage(chatId, "Неизвестная команда.", cancellationToken: token);
    //    }
    //}
}
