using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.Payments;
using Telegram.Net.Attributes;
using Telegram.Net.Interfaces;
using Telegram.Net.Services;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Net.Models;

namespace Telegram.Tests
{
    [TestFixture]
    public class TelegramHostedServiceTests
    {
        private TelegramBotConfig _configMock;
        private ServiceCollection _services;
        private ILogger<TelegramHostedService> logger;

        [SetUp]
        public void Setup()
        {
            var conf = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
                .AddEnvironmentVariables()
                .Build();
            var loggerFactory = new LoggerFactory();
            logger = loggerFactory.CreateLogger<TelegramHostedService>();
            _configMock = new TelegramBotConfig(conf.GetValue<string>("telegram_test_token") ?? throw new Exception("Provide telegram token first"));
            _services = new ServiceCollection();
        }

        public class TestHandler : IUpdatePollingService
        {
            [Command("/start")]
            public async Task HandleStart(ITelegramBotClient client, Message message, CancellationToken ct)
                => await client.SendTextMessageAsync(message.Chat.Id, "Started", cancellationToken: ct);

            [Callback("test_callback")]
            public async Task HandleCallback(ITelegramBotClient client, CallbackQuery query, CancellationToken ct)
                => await client.AnswerCallbackQueryAsync(query.Id, "Callback handled", cancellationToken: ct);

            [EditMessage]
            public async Task HandleEdit(ITelegramBotClient client, Message message, CancellationToken ct)
                => await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "Edited", cancellationToken: ct);

            [Inline("search")]
            public async Task HandleInline(ITelegramBotClient client, InlineQuery query, CancellationToken ct)
                => await client.AnswerInlineQueryAsync(query.Id, new[] { new InlineQueryResultArticle("1", "Test", new InputTextMessageContent("Test")) }, cancellationToken: ct);

            [PreCheckout]
            public async Task HandlePayment(ITelegramBotClient client, PreCheckoutQuery query, CancellationToken ct)
                => await client.AnswerPreCheckoutQueryAsync(query.Id, cancellationToken: ct);

            [Update]
            public async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken ct)
                => await client.SendTextMessageAsync(123, "Update handled", cancellationToken: ct);
        }

        [Test]
        public void AddAttributes_RegistersCommandHandlersCorrectly()
        {
            _services.AddSingleton<TestHandler>();
            var service = new TelegramHostedService(_configMock, _services, logger);

            service.AddAttributes(CancellationToken.None).Wait();

            Assert.Multiple(() =>
            {
                Assert.That(service.CommandHandler, Contains.Key("/start"));
                Assert.That(service.CallbackQueryHandler, Contains.Key("test_callback"));
                Assert.That(service.EditedMessageHandler, Has.Count.EqualTo(1));
                Assert.That(service.InlineHandler, Contains.Key("search"));
                Assert.That(service.PreCheckoutHandler, Is.Not.Null);
                Assert.That(service.DefaultUpdateHandler, Has.Count.EqualTo(1)); 
            });
        }

        [Test]
        public void IsValidHandlerMethod_ValidMessageHandler_ReturnsTrue()
        {
            // Arrange
            var method = typeof(TestHandler).GetMethod("HandleStart");

            // Act
            var result = TelegramHostedService.IsValidHandlerMethod(method, typeof(Message));

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsValidHandlerMethod_InvalidParameters_ReturnsFalse()
        {
            // Arrange
            var method = typeof(TestHandler).GetMethod("HandleStart");

            // Act
            var result = TelegramHostedService.IsValidHandlerMethod(method, typeof(CallbackQuery));

            // Assert
            Assert.That(result, Is.False);
        }
    }

    [TestFixture]
    public class IntegrationTests
    {
        private Mock<ITelegramBotClient> _botClientMock;
        private TelegramHostedService _hostedService;
        private Mock<ITelegramBotConfig> _configMock;

        [SetUp]
        public void Setup()
        {
            var conf = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
                .AddEnvironmentVariables()
                .Build();
            
            _configMock = new Mock<ITelegramBotConfig>();
            _configMock.Setup(c => c.Token).Returns(conf.GetValue<string>("telegram_test_token") ?? throw new Exception("Provide token first"));
            _configMock.Setup(c => c.ReceiverOptions).Returns(new ReceiverOptions());
            
            var services = new ServiceCollection();
            services.AddSingleton<TelegramHostedServiceTests.TestHandler>();
            _botClientMock = new Mock<ITelegramBotClient>();
            var logger = new LoggerFactory().CreateLogger<TelegramHostedService>();
            _hostedService = new TelegramHostedService(_configMock.Object, services, logger);
            _hostedService.AddAttributes(CancellationToken.None).Wait();
        }

        [Test]
        public async Task HandleMessage_ValidCommand_ExecutesHandler()
        {
            // Arrange
            var message = new Message { Text = "/start", Chat = new Chat { Id = 123 } };
            var update = new Update { Message = message };

            // Act
            await _hostedService.StartAsync(CancellationToken.None);
            await InvokeUpdateHandler(update);

            // Assert
            _botClientMock.Verify();
        }

        [Test]
        public async Task HandleCallback_ValidQuery_ExecutesHandler()
        {
            // Arrange
            var callback = new CallbackQuery { Data = "test_callback", Id = "cb_id" };
            var update = new Update { CallbackQuery = callback };

            // Act
            await _hostedService.StartAsync(CancellationToken.None);
            await InvokeUpdateHandler(update);

            // Assert
            _botClientMock.Verify();
        }

        private async Task InvokeUpdateHandler(Update update)
        {
            await _hostedService.UpdateHandler(_botClientMock.Object, update, CancellationToken.None);
        }
    }
}