using Telegram.Bot;
using Telegram.Bot.Polling;

namespace Telegram.Net.Interfaces;

public interface ITelegramBotConfig
{
    /// <summary>
    /// Token of telegram bot. You can take it from @BotFather
    /// </summary>
    public string Token { internal get; init; }
    /// <summary>
    /// Custom error handler for bot. You can add custom logger or anything. 
    /// </summary>
    public Func<ITelegramBotClient, Exception, CancellationToken, Task>? errorHandler { get; init; }
    /// <summary>
    /// Custom receiver options
    /// </summary>
    public ReceiverOptions? ReceiverOptions { get; init; }
}