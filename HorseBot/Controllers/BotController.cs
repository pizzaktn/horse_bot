using HorseBot.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace HorseBot.Controllers;

[ApiController]
[Route("[controller]")]
public class BotController : ControllerBase
{
    private readonly IOptions<BotConfiguration> _botConfig;
    private readonly ITelegramBotClient _botClient;
    private readonly IUpdateHandler _updateHandler;
    public BotController(IOptions<BotConfiguration> config, ITelegramBotClient botClient, IUpdateHandler updateHandler)
    {
        _botConfig = config;
        _botClient = botClient;
        _updateHandler = updateHandler;
    }

    [HttpGet("setWebhook")]
    public async Task<string> SetWebHook(CancellationToken ct)
    {
        var webhookUrl = _botConfig.Value.BotWebhookUrl.AbsoluteUri;
        await _botClient.SetWebhook(webhookUrl, allowedUpdates: [], secretToken: _botConfig.Value.SecretToken, cancellationToken: ct);
        return $"Webhook set to {webhookUrl}";
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update, CancellationToken ct)
    {
        if (Request.Headers["X-Telegram-Bot-Api-Secret-Token"] != _botConfig.Value.SecretToken)
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
