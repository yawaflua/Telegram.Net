using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Net.Interfaces;
using Telegram.Net.Services;

namespace Telegram.Net.Attributes;

/// <summary>
/// Attribute for registering command handlers in a Telegram bot.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public bool IsReusable => true;
    public string Command { get; set; } 
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandAttribute"/> class,
    /// registering methods marked with this attribute as command handlers.
    /// <code>
    /// [Command("/start")] // You can use it like with StartsWith
    /// public async Task Start(ITelegramBotClient client, Message message, CancellationToken ctx){
    ///     Console.WriteLine(message.Text);
    /// }
    /// </code>
    /// </summary>
    /// <param name="command">The command to be handled.</param>
    public CommandAttribute(string command)
    {
        Command = command;
        
        var methods = typeof(IUpdatePollingSerivce).GetMethods()
            .Where(m => m.GetCustomAttribute(this.GetType()) != null);

        foreach (var method in methods)
        {
            if (IsValidHandlerMethod(method))
            {
                var attr = method.GetCustomAttribute<CommandAttribute>();
                var handler = (Func<ITelegramBotClient, Message, CancellationToken, Task>)
                    Delegate.CreateDelegate(typeof(Func<ITelegramBotClient, Message, CancellationToken, Task>), null,
                        method);

                TelegramHostedService.CommandHandler.Add(attr!.Command, handler);
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