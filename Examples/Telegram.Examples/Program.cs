using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Net;

var webHost = Host.CreateDefaultBuilder()
    .ConfigureLogging(l => l.ClearProviders().AddConsole())
    .ConfigureServices(k =>
    {
        k.ConnectTelegram(new("TOKEN")
        {
            errorHandler = async (client, exception, ctx) =>
            {
                Console.WriteLine(exception);
            }
        });
    });
webHost.Build().Run();