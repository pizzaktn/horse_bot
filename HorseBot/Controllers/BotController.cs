using HorseBot.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace HorseBot.Controllers;

[ApiController]
[Route("bot")]
public class BotController : ControllerBase
{   
    private readonly ITelegramBotClient _botClient;
    private readonly IUpdateHandler _updateHandler;
    public BotController(ITelegramBotClient botClient, IUpdateHandler updateHandler)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
    }

    [HttpGet("setWebhook")]
    public async Task<string> SetWebHook(CancellationToken ct)
    {
        //var webhookUrl = _botConfig.Value.BotWebhookUrl.AbsoluteUri;
        var webhookUrl = Environment.GetEnvironmentVariable("PUBLIC_URL") + "/bot";
        await _botClient.SetWebhook(webhookUrl, allowedUpdates: null, secretToken: Environment.GetEnvironmentVariable("SECRET_TOKEN"), cancellationToken: ct);
        return $"Webhook set to {webhookUrl}";
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Post([FromBody] Update update, CancellationToken ct)
    {
        if (Request.Headers["X-Telegram-Bot-Api-Secret-Token"] != Environment.GetEnvironmentVariable("SECRET_TOKEN"))
            return Forbid();
        try
        {
            await _updateHandler.HandleUpdateAsync(_botClient, update, ct);
        }
        catch (Exception exception)
        {
            await _updateHandler.HandleErrorAsync(_botClient, exception, Telegram.Bot.Polling.HandleErrorSource.HandleUpdateError, ct);
        }
        return Ok();
    }
}
