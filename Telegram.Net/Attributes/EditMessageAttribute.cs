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
        var methods = typeof(IUpdatePollingSerivce).GetMethods()
            .Where(m => m.GetCustomAttribute(this.GetType()) != null);

        foreach (var method in methods)
        {
            if (IsValidHandlerMethod(method))
            {
                var handler = (Func<ITelegramBotClient, Message, CancellationToken, Task>)
                    Delegate.CreateDelegate(typeof(Func<ITelegramBotClient, Message, CancellationToken, Task>), null,
                        method);

                TelegramHostedService.EditedMessageHandler.Add(handler);
            }
        }
    }
    
    static bool IsValidHandlerMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return method.ReturnType == typeof(Task) &&
               parameters.Length == 3 &&
               parameters[0].ParameterType == typeof(ITelegramBotClient) &&
               parameters[1].ParameterType == typeof(Message) &&
               parameters[2].ParameterType == typeof(CancellationToken);
    }
}