using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Net.Models;
using Telegram.Net.Services;

namespace Telegram.Net;

public static class ServiceBindings
{
    public static IServiceCollection ConnectTelegram(this IServiceCollection isc, TelegramBotConfig config)
    {
        var logger = isc.BuildServiceProvider().GetRequiredService<ILogger<TelegramHostedService>>();
        isc.AddHostedService<TelegramHostedService>(k => new(config, isc, logger));
        return isc;
    }
}