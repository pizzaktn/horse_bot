using System.Threading;
using System.Threading.Tasks;
using HorseBot.Services;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HorseBot.Services
{
    public class BotPollingService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IUpdateHandler _updateHandler;
        //private readonly GoogleSheetsService _sheetsService;

        public BotPollingService(ITelegramBotClient botClient, IUpdateHandler updateHandler, GoogleSheetsService sheetsService)
        {
            _updateHandler = updateHandler;
            _botClient = botClient;
            //_sheetsService = sheetsService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                async (bot, update, token) => await _updateHandler.HandleUpdateAsync(bot, update, token),
                _updateHandler.HandleErrorAsync,
                receiverOptions,
                stoppingToken
            );

            return Task.CompletedTask;
        }
    }
}
