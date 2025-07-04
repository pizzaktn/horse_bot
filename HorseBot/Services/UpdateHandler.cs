using Google.Apis.Sheets.v4;
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
        if (update.Message?.Text is not { } messageText)
            return;

        var chatId = update.Message.Chat.Id;

        if (messageText.StartsWith("/add_student"))
        {
            var name = messageText.Replace("/add_student", "").Trim();
            await _sheetsService.AddStudentAsync(name, chatId);
            await bot.SendMessage(chatId, $"Ученик {name} добавлен", cancellationToken: token);
        }
        else if (messageText.StartsWith("/balance"))
        {
            var balance = await _sheetsService.GetBalanceAsync(chatId);
            await bot.SendMessage(chatId, $"Осталось занятий: {balance}", cancellationToken: token);
        }
        else if (messageText.StartsWith("/add_payment"))
        {
            var parts = messageText.Replace("/add_payment", "").Trim().Split(' ');
            if (parts.Length == 2 && int.TryParse(parts[0], out int amount) && int.TryParse(parts[1], out int lessons))
            {
                await _sheetsService.AddPaymentAsync(chatId, amount, lessons);
                await bot.SendMessage(chatId, "Оплата добавлена.", cancellationToken: token);
            }
            else
            {
                await bot.SendMessage(chatId, "Используй формат: /add_payment 3000 5", cancellationToken: token);
            }
        }
        else
        {
            await bot.SendMessage(chatId, "Неизвестная команда.", cancellationToken: token);
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Polling error: {exception.Message}");
        return Task.CompletedTask;
    }
}
