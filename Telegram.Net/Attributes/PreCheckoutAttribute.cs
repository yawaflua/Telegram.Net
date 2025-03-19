using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using Telegram.Net.Interfaces;
using Telegram.Net.Services;

namespace Telegram.Net.Attributes;

/// <summary>
/// Attribute for pre checkout handler. Using:
/// <code>
/// [PreCheckout]
/// public async Task PreCheckout(ITelegramBotClient client, PreCheckoutQuery preCheckout, CancellationToken ctx){
///     Console.WriteLine(preCheckout.Id);
/// }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class PreCheckoutAttribute : Attribute
{
    public bool IsReusable => true;
    public PreCheckoutAttribute()
    {
        var methods = typeof(IUpdatePollingSerivce).GetMethods()
            .Where(m => m.GetCustomAttribute(this.GetType()) != null);

        foreach (var method in methods)
        {
            if (IsValidHandlerMethod(method))
            {
                var handler = (Func<ITelegramBotClient, PreCheckoutQuery, CancellationToken, Task>)
                    Delegate.CreateDelegate(typeof(Func<ITelegramBotClient, PreCheckoutQuery, CancellationToken, Task>), null,
                        method);

                TelegramHostedService.PreCheckoutHandler = (handler);
            }
        }
    }
    
    static bool IsValidHandlerMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return method.ReturnType == typeof(Task) &&
               parameters.Length == 3 &&
               parameters[0].ParameterType == typeof(ITelegramBotClient) &&
               parameters[1].ParameterType == typeof(PreCheckoutQuery) &&
               parameters[2].ParameterType == typeof(CancellationToken);
    }
}