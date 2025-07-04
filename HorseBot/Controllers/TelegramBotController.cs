using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot;
using HorseBot.Services;

namespace HorseBot.Controllers
{  
        [ApiController]
        [Route("webhook")]
        public class TelegramBotControllers : ControllerBase
        {
            private readonly ITelegramBotClient _botClient;
            private readonly GoogleSheetsService _sheetsService;

            public TelegramBotControllers(ITelegramBotClient botClient, GoogleSheetsService sheetsService)
            {
                _botClient = botClient;
                _sheetsService = sheetsService;
            }

            //[HttpPost]
            //public async Task<IActionResult> Post([FromBody] Update update)
            //{
            //    if (update.Message?.Text != null)
            //    {
            //        var chatId = update.Message.Chat.Id;
            //        var text = update.Message.Text;

            //        if (text.StartsWith("/add_student"))
            //        {
            //            var name = text.Replace("/add_student", "").Trim();
            //            await _sheetsService.AddStudentAsync(name, chatId);
            //            //await _botClient.SendTextMessageAsync(chatId, $"Ученик {name} добавлен");
            //        }
            //        else if (text.StartsWith("/balance"))
            //        {
            //            var balance = await _sheetsService.GetBalanceAsync(chatId);
            //            //await _botClient.SendTextMessageAsync(chatId, $"Осталось занятий: {balance}");
            //        }
            //        else if (text.StartsWith("/add_payment"))
            //        {
            //            var parts = text.Replace("/add_payment", "").Trim().Split(' ');
            //            if (parts.Length == 2 && int.TryParse(parts[0], out int amount) && int.TryParse(parts[1], out int lessons))
            //            {
            //                await _sheetsService.AddPaymentAsync(chatId, amount, lessons);
            //                //await _botClient.SendTextMessageAsync(chatId, "Оплата добавлена.");
            //            }
            //            else
            //            {
            //                //await _botClient.SendTextMessageAsync(chatId, "Используй формат: /add_payment 3000 5");
            //            }
            //        }
            //    }

            //    return Ok();
            //}
        }
}
