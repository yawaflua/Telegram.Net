using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using Telegram.Net.Interfaces;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Telegram.Net.Services;

public class TelegramHostedService : IHostedService
{
    private TelegramBotClient Client { get; }
    private ITelegramBotConfig Config { get; }
    internal static Dictionary<string, Func<ITelegramBotClient, Message, CancellationToken, Task>> CommandHandler { get; } = new();
    internal static List<Func<ITelegramBotClient, Message, CancellationToken, Task>> EditedMessageHandler { get; } = new();
    internal static Dictionary<string, Func<ITelegramBotClient, CallbackQuery,CancellationToken, Task>> CallbackQueryHandler { get; } = new();
    internal static Dictionary<string, Func<ITelegramBotClient, InlineQuery ,CancellationToken, Task>> InlineHandler { get; } = new();
    internal static Func<ITelegramBotClient, PreCheckoutQuery,CancellationToken, Task>? PreCheckoutHandler { get; set; }
    internal static List<Func<ITelegramBotClient, Update, CancellationToken, Task>> DefaultUpdateHandler { get; } = new();

    public TelegramHostedService(ITelegramBotConfig config)
    {
        Client = new TelegramBotClient(config.Token);
        Config = config;

    }
    
    [SuppressMessage("ReSharper", "AsyncVoidLambda")]
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Client.StartReceiving(
            async (client, update, ctx) =>
            {
                switch (update)
                {
                    case { Message: { } message }:
                        await CommandHandler.FirstOrDefault(k => message.Text!.StartsWith(k.Key)).Value(client, message, cancellationToken);
                        break;
                    case { EditedMessage: { } message }:
                        EditedMessageHandler.ForEach(async k => await k(client, message, cancellationToken));
                        break;
                    case { CallbackQuery: { } callbackQuery }:
                        await CallbackQueryHandler.FirstOrDefault(k => callbackQuery.Data!.StartsWith(k.Key)).Value(client, callbackQuery, cancellationToken);
                        break;
                    case { InlineQuery: { } inlineQuery }:
                        await InlineHandler.FirstOrDefault(k => inlineQuery.Id.StartsWith(k.Key)).Value(client, inlineQuery, cancellationToken);
                        break;
                    case {PreCheckoutQuery: { } preCheckoutQuery}:
                        if (PreCheckoutHandler != null)
                            await PreCheckoutHandler(client, preCheckoutQuery, cancellationToken);
                        break;
                    default:
                        DefaultUpdateHandler.ForEach(async k => await k(client, update, ctx));
                        break;
                }
                
            },
            Config.errorHandler ?? ((_, _, _) => Task.CompletedTask),
            Config.ReceiverOptions,
            cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Client.DropPendingUpdates(cancellationToken);
    }
}