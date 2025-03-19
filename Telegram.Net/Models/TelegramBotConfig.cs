using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Net.Interfaces;

namespace Telegram.Net.Models;

public class TelegramBotConfig : ITelegramBotConfig
{
    public TelegramBotConfig(string token)
    {
        Token = token;
    }

    public string Token { get; init; }
    public Func<ITelegramBotClient, Exception, CancellationToken, Task>? errorHandler { get; init; }
    public ReceiverOptions? ReceiverOptions { get; init; }
}