using Microsoft.Extensions.Hosting;
using Telegram.Net;

var webHost = Host.CreateDefaultBuilder()
    .ConfigureServices(k =>
    {
        k.ConnectTelegram(new("YOUR-TOKEN")
        {
            errorHandler = async (client, exception, ctx) =>
            {
                await Console.Out.WriteLineAsync(exception.Message);
            }
        });
    });