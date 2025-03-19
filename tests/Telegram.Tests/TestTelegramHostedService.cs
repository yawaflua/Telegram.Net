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
using Moq;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Tests
{
    [TestFixture]
    public class TelegramHostedServiceTests
    {
        private Mock<ITelegramBotConfig> _configMock;
        private ServiceCollection _services;

        [SetUp]
        public void Setup()
        {
            var conf = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false)
                .AddEnvironmentVariables()
                .Build();
            _configMock = new Mock<ITelegramBotConfig>();
            _configMock.Setup(c => c.Token).Returns(conf.GetValue<string>("telegram_test_token"));
            _services = new ServiceCollection();
        }

        public class TestHandler : IUpdatePollingService
        {
            [Command("start")]
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
            var service = new TelegramHostedService(_configMock.Object, _services);

            service.AddAttributes(CancellationToken.None).Wait();

            Assert.Multiple(() =>
            {
                Assert.That(TelegramHostedService.CommandHandler, Contains.Key("start"));
                Assert.That(TelegramHostedService.CallbackQueryHandler, Contains.Key("test_callback"));
                Assert.That(TelegramHostedService.EditedMessageHandler, Has.Count.EqualTo(1));
                Assert.That(TelegramHostedService.InlineHandler, Contains.Key("search"));
                Assert.That(TelegramHostedService.PreCheckoutHandler, Is.Not.Null);
                Assert.That(TelegramHostedService.DefaultUpdateHandler, Has.Count.EqualTo(1));
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
            _configMock = new Mock<ITelegramBotConfig>();
            _configMock.Setup(c => c.Token).Returns("test_token");
            _configMock.Setup(c => c.ReceiverOptions).Returns(new ReceiverOptions());
            
            var services = new ServiceCollection();
            services.AddSingleton<TelegramHostedServiceTests.TestHandler>();
            _botClientMock = new Mock<ITelegramBotClient>();
            
            _hostedService = new TelegramHostedService(_configMock.Object, services);
            _hostedService.AddAttributes(CancellationToken.None).Wait();
        }

        [Test]
        public async Task HandleMessage_ValidCommand_ExecutesHandler()
        {
            // Arrange
            var message = new Message { Text = "/start", Chat = new Chat { Id = 123 } };
            var update = new Update { Message = message };

            _botClientMock.Setup(b => b.SendMessage(
                It.IsAny<ChatId>(),
                "Started",
                It.IsAny<ParseMode>(),
                It.IsAny<ReplyParameters>(),
                It.IsAny<ReplyMarkup>(),
                It.IsAny<LinkPreviewOptions>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<MessageEntity>>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()
            )).Verifiable();

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

            _botClientMock.Setup(b => b.AnswerCallbackQueryAsync(
                "cb_id",
                "Callback handled",
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()
                )).Verifiable();

            // Act
            await _hostedService.StartAsync(CancellationToken.None);
            await InvokeUpdateHandler(update);

            // Assert
            _botClientMock.Verify();
        }

        private async Task InvokeUpdateHandler(Update update)
        {
            var clientField = typeof(TelegramHostedService).GetField("Client", BindingFlags.NonPublic | BindingFlags.Instance);
            clientField.SetValue(_hostedService, _botClientMock.Object);

            var updateHandler = _botClientMock.Invocations
                .First(i => i.Method.Name == "StartReceiving")
                .Arguments[0] as Func<ITelegramBotClient, Update, CancellationToken, Task>;

            await updateHandler(_botClientMock.Object, update, CancellationToken.None);
        }
    }
}