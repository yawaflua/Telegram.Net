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
    }
}