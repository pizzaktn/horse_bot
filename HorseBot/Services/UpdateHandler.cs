using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<long, (string Student, string Action)> _userStates = new();

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
        if (update.Type == UpdateType.Message)
        {
            var msg = update.Message;
            if (msg.Type == MessageType.Text)
            {
                if (_userStates.TryGetValue(msg.Chat.Id, out var state))
                {
                    if (state.Action == "pay_custom")
                    {
                        if (int.TryParse(msg.Text, out int count))
                        {
                            await _sheetsService.AddPaymentAsync(state.Student, count);
                            await bot.SendMessage(msg.Chat.Id, $"Добавлено {count} занятий для {state.Student}.");
                            _userStates.TryRemove(msg.Chat.Id, out _);
                        }
                        else
                        {
                            await bot.SendMessage(msg.Chat.Id, "Введите число, например: 5");
                        }
                        return;
                    }
                    else if (state.Action == "add_student_name")
                    {
                        var name = msg.Text.Trim();
                        if (!string.IsNullOrEmpty(name))
                        {
                            await _sheetsService.AddStudentAsync(name);
                            await bot.SendMessage(msg.Chat.Id, $"Ученик {name} добавлен.");
                        }
                        else
                        {
                            await bot.SendMessage(msg.Chat.Id, "Имя не может быть пустым. Попробуйте снова.");
                        }
                        _userStates.TryRemove(msg.Chat.Id, out _);
                        return;
                    }
                }

                if (msg.Text == "/students")
                {
                    var students = await _sheetsService.GetAllStudentNamesAsync();

                    foreach (var name in students)
                    {
                        var buttons = new InlineKeyboardMarkup(new[]
                        {
                            new []
                            {
                                InlineKeyboardButton.WithCallbackData("📥 Оплатить", $"pay_{name}"),
                                InlineKeyboardButton.WithCallbackData("✅ Посещение", $"attend_{name}"),
                                InlineKeyboardButton.WithCallbackData("📊 Статус", $"status_{name}")
                            }
                        });

                        await bot.SendMessage(msg.Chat.Id, name, replyMarkup: buttons);
                    }
                }

                if (msg.Text == "/add_student")
                {
                    _userStates[msg.Chat.Id] = (null, "add_student_name");
                    await bot.SendMessage(msg.Chat.Id, "Введите имя нового ученика:");
                    return;
                }
            }
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            var query = update.CallbackQuery;
            var data = query.Data;

            if (data.StartsWith("pay_"))
            {
                var name = data.Substring(4);
                var paymentOptions = new InlineKeyboardMarkup(new[]
                {
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1 занятие", $"payval_{name}_1"),
                        InlineKeyboardButton.WithCallbackData("4 занятия", $"payval_{name}_4"),
                        InlineKeyboardButton.WithCallbackData("⬆️ Ввести своё", $"paycustom_{name}")
                    }
                });

                await bot.AnswerCallbackQuery(query.Id);
                await bot.SendMessage(query.Message.Chat.Id,
                    $"Выберите количество занятий для {name}:", replyMarkup: paymentOptions);
            }
            else if (data.StartsWith("payval_"))
            {
                var parts = data.Split('_');
                var name = parts[1];
                var count = int.Parse(parts[2]);

                await _sheetsService.AddPaymentAsync(name, count);
                await bot.AnswerCallbackQuery(query.Id);
                await bot.SendMessage(query.Message.Chat.Id,
                    $"Добавлено {count} занятий для {name}.");
            }
            else if (data.StartsWith("paycustom_"))
            {
                var name = data.Substring("paycustom_".Length);
                _userStates[query.Message.Chat.Id] = (name, "pay_custom");

                await bot.AnswerCallbackQuery(query.Id);
                await bot.SendMessage(query.Message.Chat.Id,
                    $"Введите количество занятий для {name}:");
            }
            else if (data.StartsWith("attend_"))
            {
                var name = data.Substring(7);
                var success = await _sheetsService.RegisterAttendanceAsync(name);
                var msg = success
                    ? $"Посещение для {name} зарегистрировано."
                    : $"У {name} нет оплаченных занятий!";
                await bot.AnswerCallbackQuery(query.Id);
                await bot.SendMessage(query.Message.Chat.Id, msg);
            }
            else if (data.StartsWith("status_"))
            {
                var name = data.Substring(7);
                var status = await _sheetsService.GetStatusAsync(name);
                await bot.AnswerCallbackQuery(query.Id);
                await bot.SendMessage(query.Message.Chat.Id, status);
            }
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
