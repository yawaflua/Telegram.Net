# Telegram Bot Attribute Handlers

This project provides a set of C# attributes to facilitate the handling of different types of Telegram bot updates using reflection.

## Features
- **Inline Query Handling** (`InlineAttribute`)
- **Edited Message Handling** (`EditMessageAttribute`)
- **Command Handling** (`CommandHandlerAttribute`)
- **Callback Query Handling** (`CallbackAttribute`)
- **PreCheckout Query Handling** (`PreCheckoutAttribute`)
- **General Update Handling** (`UpdateAttribute`)
- **Auto-generate telegram client**

## Installation
Ensure you have the required dependencies installed:

```sh
 dotnet add package Telegram.Bot
```

## Usage

### Provide dependencies in class
You can provide dependencies in class from constructor, and after it use it like `static`.
```csharp
public class Example : IUpdatePollingService
{
    private static MyCoolService _service; // It should to be static!
    
    public Example(MyCoolService service)
    {
        _service = service;
    }
}
```

### Inline Query Handling
Use the `InlineAttribute` to register a method as an inline query handler.

```csharp
[Inline("example_query")]
public static async Task HandleInlineQuery(ITelegramBotClient bot, InlineQuery query, CancellationToken cancellationToken)
{
    // Handle inline query
}
```

### Edited Message Handling
Use the `EditMessageAttribute` to register a method as a handler for edited messages.

```csharp
[EditMessage]
public static async Task HandleEditedMessage(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
{
    // Handle edited message
}
```

### Command Handling
Use the `CommandHandlerAttribute` to register a method as a command handler.
You can provide only begin of command text. Like, `/start act-` 
```csharp
[CommandHandler("/start")]
public static async Task StartCommand(ITelegramBotClient bot, Message message, CancellationToken cancellationToken)
{
    // Handle start command
}
```

### Callback Query Handling
Use the `CallbackAttribute` to register a method as a callback query handler.
You can provide only begin of callback data text
```csharp
[Callback("button_click")]
public static async Task HandleCallbackQuery(ITelegramBotClient bot, CallbackQuery query, CancellationToken cancellationToken)
{
    // Handle callback query
}
```

### PreCheckout Query Handling
Use the `PreCheckoutAttribute` to register a method as a pre-checkout query handler.

```csharp
[PreCheckout]
public static async Task HandlePreCheckoutQuery(ITelegramBotClient bot, PreCheckoutQuery query, CancellationToken cancellationToken)
{
    // Handle pre-checkout query
}
```

### General Update Handling
Use the `UpdateAttribute` to register a method as a generic update handler.

```csharp
[Update]
public static async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    // Handle general update
}
```

## License
This project is open-source and available under the [Apache 2.0 license](LICENSE)