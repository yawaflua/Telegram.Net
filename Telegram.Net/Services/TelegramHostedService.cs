using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    internal TelegramBotClient Client { get; set; }
    private ITelegramBotConfig Config { get; }
    internal Dictionary<string, Func<ITelegramBotClient, Message, CancellationToken, Task>> CommandHandler { get; } = new();
    internal List<Func<ITelegramBotClient, Message, CancellationToken, Task>> EditedMessageHandler { get; } = new();
    internal Dictionary<string, Func<ITelegramBotClient, CallbackQuery,CancellationToken, Task>> CallbackQueryHandler { get; } = new();
    internal Dictionary<string, Func<ITelegramBotClient, InlineQuery ,CancellationToken, Task>> InlineHandler { get; } = new();
    internal Func<ITelegramBotClient, PreCheckoutQuery,CancellationToken, Task>? PreCheckoutHandler { get; set; }
    internal List<Func<ITelegramBotClient, Update, CancellationToken, Task>> DefaultUpdateHandler { get; } = new();
    internal static ILogger<TelegramHostedService> _logger;
    
    public TelegramHostedService(ITelegramBotConfig config, IServiceCollection isc, ILogger<TelegramHostedService> logger)
    {
        try
        {
            _logger = logger;
            Client = new TelegramBotClient(config.Token);
            Config = config;
            this.isc = isc;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Critical, new EventId(), ex, "Catched exception when creating TelegramHostedService: ");
        }
    }
    internal static bool IsValidHandlerMethod(MethodInfo method, Type parameterType)
    {
        try
        {
            var parameters = method.GetParameters();
            return method.ReturnType == typeof(Task) &&
                   parameters.Length == 3 &&
                   parameters[0].ParameterType == typeof(ITelegramBotClient) &&
                   parameters[1].ParameterType == parameterType &&
                   parameters[2].ParameterType == typeof(CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catched exception in parsing and checking params.");
            return false;
        }
    }

    internal static Func<ITelegramBotClient, T, CancellationToken, Task> CreateDelegate<T>(MethodInfo method)
    {
        try
        {
            var delegateType = typeof(Func<ITelegramBotClient, T, CancellationToken, Task>);
            return (Delegate.CreateDelegate(delegateType, null, method) as
                Func<ITelegramBotClient, T, CancellationToken, Task>)!;
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Critical, new EventId(), ex, "Catched exception in CreateDelegate function: ");
            return null;
        }

    }

    internal async Task AddAttributes(CancellationToken cancellationToken)
    {
        await Task.Run(async () =>
        {
            try
            {
                var implementations = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => typeof(IUpdatePollingService).IsAssignableFrom(t) && !t.IsInterface);

                foreach (var implementation in implementations)
                {
                    isc.AddScoped(implementation);
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
                
                if (methods.Count() == 0)
                {
                    _logger.LogWarning("Not founded methods with attributes.");
                }
                
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                foreach (var method in methods)
                {
                    var commandAttr = method.GetCustomAttribute<CommandAttribute>();
                    if (commandAttr != null)
                    {
                        if (IsValidHandlerMethod(method, typeof(Message)))
                        {
                            var handler = CreateDelegate<Message>(method);
                            if (!CommandHandler.TryAdd(commandAttr.Command, handler))
                                throw new Exception($"Failed to add in commandHandler: {commandAttr.Command}");
                        }

                        continue;
                    }

                    var callbackAttr = method.GetCustomAttribute<CallbackAttribute>();
                    if (callbackAttr != null)
                    {
                        if (IsValidHandlerMethod(method, typeof(CallbackQuery)))
                        {
                            var handler = CreateDelegate<CallbackQuery>(method);
                            if (!CallbackQueryHandler.TryAdd(callbackAttr.QueryId, handler))
                                throw new Exception($"Failed to add in callbacKQuery: {callbackAttr.QueryId}");;
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
                            if (!InlineHandler.TryAdd(inlineAttr.InlineId, handler))
                                throw new Exception($"Failed to add in inlineHandler: {inlineAttr.InlineId}");;
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
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Critical, new EventId(), ex, "Catched new exception when added methods: ");
            }
        }, cancellationToken);
    }
    
    
    internal async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken ctx)
    {
        try
        {
            switch (update)
            {
                case { Message: { } message }:
                    await CommandHandler.FirstOrDefault(k => message.Text!.StartsWith(k.Key))
                        .Value(client, message, ctx);
                    break;
                case { EditedMessage: { } message }:
                    EditedMessageHandler.ForEach(async k => await k(client, message, ctx));
                    break;
                case { CallbackQuery: { } callbackQuery }:
                    await CallbackQueryHandler.FirstOrDefault(k => callbackQuery.Data!.StartsWith(k.Key))
                        .Value(client, callbackQuery, ctx);
                    break;
                case { InlineQuery: { } inlineQuery }:
                    await InlineHandler.FirstOrDefault(k => inlineQuery.Id.StartsWith(k.Key))
                        .Value(client, inlineQuery, ctx);
                    break;
                case { PreCheckoutQuery: { } preCheckoutQuery }:
                    if (PreCheckoutHandler != null) await PreCheckoutHandler(client, preCheckoutQuery, ctx);
                    break;
                default:
                    DefaultUpdateHandler.ForEach(async k => await k(client, update, ctx));
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, new EventId(), ex, "Catched exception in UpdateHandler: ");
        }
    }
    
    [SuppressMessage("ReSharper", "AsyncVoidLambda")]
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await AddAttributes(cancellationToken);



            Client.StartReceiving(
                UpdateHandler,
                Config.errorHandler ?? ((_, _, _) => Task.CompletedTask),
                Config.ReceiverOptions,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Critical, new EventId(), ex, "Failed to start. Catched exception: ");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Client.DropPendingUpdates(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to stop. Exception: ");
        }
    }
}