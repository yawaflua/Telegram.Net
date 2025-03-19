using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Net.Interfaces;
using Telegram.Net.Services;

namespace Telegram.Net.Attributes;

/// <summary>
/// Attribute for registering handlers for edited messages in a Telegram bot.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class EditMessageAttribute : Attribute
{
    public bool IsReusable => true;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="EditMessageAttribute"/> class,
    /// registering methods marked with this attribute as handlers for edited messages.
    /// <code>
    /// [EditMessage]
    /// public async Task EditMessage(ITelegramBotClient client, Message message, CancellationToken ctx){
    ///     Console.WriteLine(message.Id);
    /// }
    /// </code>
    /// </summary>
    public EditMessageAttribute()
    {
    }
}