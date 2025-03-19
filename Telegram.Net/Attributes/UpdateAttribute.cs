using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Net.Interfaces;
using Telegram.Net.Services;

namespace Telegram.Net.Attributes;

/// <summary>
/// Attribute for default update handler. Using:
/// <code>
/// [Update]
/// public async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken ctx){
///     Console.WriteLine(Update.Message?.Text);
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class UpdateAttribute : Attribute
{
    public bool IsReusable => true;
    public UpdateAttribute()
    {
        var methods = typeof(IUpdatePollingSerivce).GetMethods()
            .Where(m => m.GetCustomAttribute(this.GetType()) != null);

        foreach (var method in methods)
        {
            if (IsValidHandlerMethod(method))
            {
                var handler = (Func<ITelegramBotClient, Update, CancellationToken, Task>)
                    Delegate.CreateDelegate(typeof(Func<ITelegramBotClient, Update, CancellationToken, Task>), null,
                        method);

                TelegramHostedService.DefaultUpdateHandler.Add(handler);
            }
        }
    }
    
    static bool IsValidHandlerMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return method.ReturnType == typeof(Task) &&
               parameters.Length == 3 &&
               parameters[0].ParameterType == typeof(ITelegramBotClient) &&
               parameters[1].ParameterType == typeof(Update) &&
               parameters[2].ParameterType == typeof(CancellationToken);
    }
}