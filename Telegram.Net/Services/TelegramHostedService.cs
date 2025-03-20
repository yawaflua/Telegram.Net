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
using static System.Reflection.BindingFlags;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Telegram.Net.Services;

public class TelegramHostedService : IHostedService
{
    private IServiceCollection isc { get; }
    internal TelegramBotClient Client { get; set; }
    private ITelegramBotConfig Config { get; }
    internal Dictionary<string, Func<ITelegramBotClient, Message, CancellationToken, Task>> CommandHandler { get; set; } = new();
    internal List<Func<ITelegramBotClient, Message, CancellationToken, Task>> EditedMessageHandler { get; set; } = new();
    internal Dictionary<string, Func<ITelegramBotClient, CallbackQuery,CancellationToken, Task>> CallbackQueryHandler { get; set; } = new();
    internal Dictionary<string, Func<ITelegramBotClient, InlineQuery ,CancellationToken, Task>> InlineHandler { get; set; } = new();
    internal Func<ITelegramBotClient, PreCheckoutQuery,CancellationToken, Task>? PreCheckoutHandler { get; set; }
    internal List<Func<ITelegramBotClient, Update, CancellationToken, Task>> DefaultUpdateHandler { get; set; } = new();
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
                var attributeTypes = new HashSet<Type>
                {
                    typeof(CommandAttribute),
                    typeof(CallbackAttribute),
                    typeof(EditMessageAttribute),
                    typeof(InlineAttribute),
                    typeof(PreCheckoutAttribute),
                    typeof(UpdateAttribute)
                };

                var methods = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t => typeof(IUpdatePollingService).IsAssignableFrom(t) && !t.IsInterface)
                    .SelectMany(t => t.GetMethods(Instance | 
                                                  Public | 
                                                  NonPublic | 
                                                  DeclaredOnly))
                    .Where(m => m.GetCustomAttributes().Any(a => attributeTypes.Contains(a.GetType())))
                    .ToList();

                if (methods.Count == 0)
                {
                    _logger.LogWarning("No methods found with required attributes");
                }
                
                var isp = isc.BuildServiceProvider();
                foreach (var method in methods)
                {
                    var declaringType = method.DeclaringType!;
                    var constructor = declaringType.GetConstructors()[0];
                    var parameters = constructor.GetParameters()
                        .Select(param => isp.GetRequiredService(param.ParameterType))
                        .ToArray();
    
                    constructor.Invoke(parameters);

                    switch (method.GetCustomAttributes().First(t => attributeTypes.Contains(t.GetType())))
                    {
                        case CommandAttribute command when IsValidHandlerMethod(method, typeof(Message)):
                            var commandHandler = CreateDelegate<Message>(method);
                            if (!CommandHandler.TryAdd(command.Command, commandHandler))
                                throw new Exception($"Failed to add command: {command.Command}");
                            break;
        
                        case CallbackAttribute callback when IsValidHandlerMethod(method, typeof(CallbackQuery)):
                            var callbackHandler = CreateDelegate<CallbackQuery>(method);
                            if (!CallbackQueryHandler.TryAdd(callback.QueryId, callbackHandler))
                                throw new Exception($"Failed to add callback: {callback.QueryId}");
                            break;
        
                        case EditMessageAttribute _ when IsValidHandlerMethod(method, typeof(Message)):
                            EditedMessageHandler.Add(CreateDelegate<Message>(method));
                            break;
        
                        case InlineAttribute inline when IsValidHandlerMethod(method, typeof(InlineQuery)):
                            var inlineHandler = CreateDelegate<InlineQuery>(method);
                            if (!InlineHandler.TryAdd(inline.InlineId, inlineHandler))
                                throw new Exception($"Failed to add inline: {inline.InlineId}");
                            break;
        
                        case PreCheckoutAttribute _ when IsValidHandlerMethod(method, typeof(PreCheckoutQuery)):
                            PreCheckoutHandler = CreateDelegate<PreCheckoutQuery>(method);
                            break;
        
                        case UpdateAttribute _ when IsValidHandlerMethod(method, typeof(Update)):
                            DefaultUpdateHandler.Add(CreateDelegate<Update>(method));
                            break;
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
                Config.errorHandler ?? ((_, ex, _) =>
                {
                    _logger.LogError(ex, "Catched error in telegram bot working: ");
                    return Task.CompletedTask;
                }),
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