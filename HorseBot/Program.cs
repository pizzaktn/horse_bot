using HorseBot.Configuration;
using HorseBot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Management;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HorseBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            ////var builder = WebApplication.CreateBuilder(args);

            //builder.WebHost.UseUrls("http://localhost:8080");

            //// Add services to the container.
            //var botConfigSection = builder.Configuration.GetSection("BotConfiguration");
            //builder.Services.Configure<BotConfiguration>(botConfigSection);
            ////builder.Services.AddHttpClient("tgwebhook").RemoveAllLoggers().AddTypedClient<ITelegramBotClient>(
            ////    httpClient => new TelegramBotClient(botConfigSection.Get<BotConfiguration>()!.BotToken, httpClient));
            //builder.Services.AddSingleton<IUpdateHandler, UpdateHandler>();

            //builder.Services.AddControllers();
            //// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            //builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            //var app = builder.Build();

            //// Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}

            //app.UseHttpsRedirection();

            //app.UseAuthorization();


            //app.MapControllers();

            // Автоустановка Webhook
            //var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
            //var botOptions = botConfigSection.Get<BotConfiguration>();
            //await botClient.SetWebhook($"{botOptions.BotWebhookUrl.AbsoluteUri}/webhook");

            //app.run();

            var builder = WebApplication.CreateBuilder(args);

            // Конфигурация
            builder.Services.Configure<GoogleSheetsConfiguration>(builder.Configuration.GetSection("GoogleSheetsConfiguration"));
            builder.Services.AddSingleton<IUpdateHandler, UpdateHandler>();

            builder.Services.AddSingleton<ITelegramBotClient>(sp =>
            {   
                //return new TelegramBotClient(options.BotToken);
                return new TelegramBotClient(Environment.GetEnvironmentVariable("TELEGRAM_TOKEN"));
            });

            builder.Services.AddSingleton<GoogleSheetsService>();
            builder.Services.AddHostedService<BotPollingService>();
            builder.Services.AddControllers();

            var app = builder.Build();

            app.MapControllers();

            app.Run();

        }
    }
}
