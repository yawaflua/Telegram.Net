using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Net;

var webHost = Host.CreateDefaultBuilder()
    .ConfigureLogging(l => l.ClearProviders().AddConsole())
    .ConfigureServices(k =>
    {
        var _conf = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        k.AddSingleton(_conf);
        k.ConnectTelegram(new(_conf.GetValue<string>("telegram_test_token")));
    });
webHost.Build().Run();