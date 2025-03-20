using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Net.Attributes;
using Telegram.Net.Interfaces;

namespace Telegram.Examples.UpdatePolling;

public class Update : IUpdatePollingService
{
    private static IConfiguration _conf;
    public Update(IConfiguration conf)
    {
        _conf = conf;
    }
    [Update]
    public async Task UpdateExample(ITelegramBotClient client, Bot.Types.Update update, CancellationToken ctx)
    {
        if (update.Poll != null)
        {
            Console.WriteLine(update.Poll.IsClosed);
        }
    }

    [Callback("act-")]
    public async Task CallbackExample(ITelegramBotClient client, CallbackQuery query, CancellationToken ctx)
    {
        Console.WriteLine(query.Message!.Text);
    }

    [Command("/start")]
    public async Task StartCommand(ITelegramBotClient client, Message message, CancellationToken ctx)
    {
        if (message.Text!.Contains(" ") && message.Text.Split(" ")[1] == "test")
            await client.SendMessage(message.From!.Id, "Hello, I`m example bot. And this - command with subparam", cancellationToken: ctx);
        else
            await client.SendMessage(message.From!.Id, "Hello, I`m example bot.", cancellationToken: ctx);
    }

    [Command("/test_conf")]
    public async Task TestConfigurationBuilder(ITelegramBotClient client, Message message, CancellationToken cts)
    {
        await client.SendMessage(message.Chat.Id, _conf.GetValue<string>("ExampleMessage") ?? throw new Exception("Not found"));
    }

    [EditMessage]
    public async Task EditMessageExmaple(ITelegramBotClient client, Message message, CancellationToken ctx)
    {
        Console.WriteLine($"new message text: {message.Text}");
    }
    
}