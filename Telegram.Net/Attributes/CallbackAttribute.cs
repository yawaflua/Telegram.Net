using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Net.Interfaces;
using Telegram.Net.Services;

namespace Telegram.Net.Attributes;

/// <summary>
/// Attribute for registering callback query handlers in a Telegram bot.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CallbackAttribute : Attribute
{
    public bool IsReusable => true;
    public string QueryId { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CallbackAttribute"/> class,
    /// registering methods marked with this attribute as callback query handlers.
    /// <code>
    /// [Callback("auth-")] // You can use id like with StartsWith
    /// public async Task PreCheckout(ITelegramBotClient client, CallbackQuery callback, CancellationToken ctx){
    ///     Console.WriteLine(callback.Data);
    /// }
    /// </code>
    /// </summary>
    /// <param name="queryId">The unique identifier for the callback query.</param>
    public CallbackAttribute(string queryId)
    {
        this.QueryId = queryId;
    }
}