using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Payments;
using Telegram.Net.Attributes;
using Telegram.Net.Interfaces;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Telegram.Net.Services;

public class TelegramHostedService : IHostedService
{
    private IServiceCollection isc { get; }
    private TelegramBotClient Client { get; }
    private ITelegramBotConfig Config { get; }
    internal static Dictionary<string, Func<ITelegramBotClient, Message, CancellationToken, Task>> CommandHandler { get; } = new();
    internal static List<Func<ITelegramBotClient, Message, CancellationToken, Task>> EditedMessageHandler { get; } = new();
    internal static Dictionary<string, Func<ITelegramBotClient, CallbackQuery,CancellationToken, Task>> CallbackQueryHandler { get; } = new();
    internal static Dictionary<string, Func<ITelegramBotClient, InlineQuery ,CancellationToken, Task>> InlineHandler { get; } = new();
    internal static Func<ITelegramBotClient, PreCheckoutQuery,CancellationToken, Task>? PreCheckoutHandler { get; set; }
    internal static List<Func<ITelegramBotClient, Update, CancellationToken, Task>> DefaultUpdateHandler { get; } = new();

    public TelegramHostedService(ITelegramBotConfig config, IServiceCollection isc)
    {
        Client = new TelegramBotClient(config.Token);
        Config = config;
        this.isc = isc;
    }
    private static bool IsValidHandlerMethod(MethodInfo method, Type parameterType)
    {
        var parameters = method.GetParameters();
        return method.ReturnType == typeof(Task) &&
               parameters.Length == 3 &&
               parameters[0].ParameterType == typeof(ITelegramBotClient) &&
               parameters[1].ParameterType == parameterType &&
               parameters[2].ParameterType == typeof(CancellationToken);
    }

    private static Func<ITelegramBotClient, T, CancellationToken, Task> CreateDelegate<T>(MethodInfo method)
    {
        var delegateType = typeof(Func<ITelegramBotClient, T, CancellationToken, Task>);
        return (Delegate.CreateDelegate(delegateType, null, method) as Func<ITelegramBotClient, T, CancellationToken, Task>)!;
    }

    internal async Task AddAttributes(CancellationToken cancellationToken)
    {
        var mutex = new Mutex();
        await Task.Run(async () =>
        {
            mutex.WaitOne();
            var implementations = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IUpdatePollingService).IsAssignableFrom(t) && !t.IsInterface);
            
            foreach (var implementation in implementations)
            {
                isc.AddSingleton(implementation);
            }
            
            var methods = implementations
                .SelectMany(t => t.GetMethods(
                    BindingFlags.Instance | 
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly))
                .Where(m =>
                    m.GetCustomAttribute<CommandAttribute>() != null ||
                    m.GetCustomAttribute<CallbackAttribute>() != null ||
                    m.GetCustomAttribute<EditMessageAttribute>() != null ||
                    m.GetCustomAttribute<InlineAttribute>() != null ||
                    m.GetCustomAttribute<PreCheckoutAttribute>() != null ||
                    m.GetCustomAttribute<UpdateAttribute>() != null);

            foreach (var method in methods)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                var commandAttr = method.GetCustomAttribute<CommandAttribute>();
                if (commandAttr != null)
                {
                    if (IsValidHandlerMethod(method, typeof(Message)))
                    {
                        var handler = CreateDelegate<Message>(method);
                        CommandHandler.Add(commandAttr.Command, handler);
                    }
                    continue;
                }

                var callbackAttr = method.GetCustomAttribute<CallbackAttribute>();
                if (callbackAttr != null)
                {
                    if (IsValidHandlerMethod(method, typeof(CallbackQuery)))
                    {
                        var handler = CreateDelegate<CallbackQuery>(method);
                        CallbackQueryHandler.Add(callbackAttr.QueryId, handler);
                    }
                    continue;
                }

                var editMessageAttr = method.GetCustomAttribute<EditMessageAttribute>();
                if (editMessageAttr != null)
                {
                    if (IsValidHandlerMethod(method, typeof(Message)))
                    {
                        var handler = CreateDelegate<Message>(method);
                        EditedMessageHandler.Add(handler);
                    }
                    continue;
                }

                var inlineAttr = method.GetCustomAttribute<InlineAttribute>();
                if (inlineAttr != null)
                {
                    if (IsValidHandlerMethod(method, typeof(InlineQuery)))
                    {
                        var handler = CreateDelegate<InlineQuery>(method);
                        InlineHandler.Add(inlineAttr.InlineId, handler);
                    }
                    continue;
                }

                var preCheckoutAttr = method.GetCustomAttribute<PreCheckoutAttribute>();
                if (preCheckoutAttr != null)
                {
                    if (IsValidHandlerMethod(method, typeof(PreCheckoutQuery)))
                    {
                        var handler = CreateDelegate<PreCheckoutQuery>(method);
                        PreCheckoutHandler = handler;
                    }
                    continue;
                }

                var updateAttr = method.GetCustomAttribute<UpdateAttribute>();
                if (updateAttr != null)
                {
                    if (IsValidHandlerMethod(method, typeof(Update)))
                    {
                        var handler = CreateDelegate<Update>(method);
                        DefaultUpdateHandler.Add(handler);
                    }
                    continue;
                }
            }
            mutex.ReleaseMutex();
        }, cancellationToken);
    }
    
    
    [SuppressMessage("ReSharper", "AsyncVoidLambda")]
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await AddAttributes(cancellationToken);
        
        
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