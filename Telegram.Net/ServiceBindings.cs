using Microsoft.Extensions.DependencyInjection;
using Telegram.Net.Models;
using Telegram.Net.Services;

namespace Telegram.Net;

public static class ServiceBindings
{
    public static IServiceCollection ConnectTelegram(this IServiceCollection isc, TelegramBotConfig config)
    {
        isc.AddHostedService<TelegramHostedService>(k => new(config));
        return isc;
    }
}