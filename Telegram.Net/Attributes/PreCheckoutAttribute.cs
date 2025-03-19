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
    }
}