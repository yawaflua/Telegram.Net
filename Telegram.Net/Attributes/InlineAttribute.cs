using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Net.Interfaces;
using Telegram.Net.Services;

namespace Telegram.Net.Attributes;


/// <summary>
/// Attribute for registering inline query handlers in a Telegram bot.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class InlineAttribute : Attribute
{
    public bool IsReusable => true;
    public string InlineId { get; }
    
    
    /// <summary>
    /// Initializes a new instance of the <see cref="InlineAttribute"/> class,
    /// registering methods marked with this attribute as inline query handlers.
    /// <code>
    /// [Inline("InlineId")] // You can use it like with StartsWith
    /// public async Task Inline(ITelegramBotClient client, InlineQuery inline, CancellationToken ctx){
    ///     Console.WriteLine(inline.From.Id);
    /// }
    /// </code>
    /// </summary>
    /// <param name="inlineId">Unique identifier for the inline query.</param>
    public InlineAttribute(string inlineId)
    {
        this.InlineId = inlineId;
        var methods = typeof(IUpdatePollingSerivce).GetMethods()
            .Where(m => m.GetCustomAttribute(this.GetType()) != null);

        foreach (var method in methods)
        {
            if (IsValidHandlerMethod(method))
            {
                var attr = method.GetCustomAttribute<InlineAttribute>();
                var handler = (Func<ITelegramBotClient, InlineQuery, CancellationToken, Task>)
                    Delegate.CreateDelegate(typeof(Func<ITelegramBotClient, InlineQuery, CancellationToken, Task>), null,
                        method);

                TelegramHostedService.InlineHandler.Add(attr!.InlineId, handler);
            }
        }
    }
    
    static bool IsValidHandlerMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return method.ReturnType == typeof(Task) &&
               parameters.Length == 3 &&
               parameters[0].ParameterType == typeof(ITelegramBotClient) &&
               parameters[1].ParameterType == typeof(InlineQuery) &&
               parameters[2].ParameterType == typeof(CancellationToken);
    }
}