using HyperTensionBot.Server;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureTelegramBot();

var app = builder.Build();
app.SetupTelegramBot();

ConcurrentDictionary<long, UserInformation> userMemory = new();
ConcurrentDictionary<long, ConversationInformation> chatMemory = new();

app.MapPost("/webhook", async (HttpContext context, ILogger<Program> logger) => {
    if(!context.Request.HasJsonContentType()) {
        throw new BadHttpRequestException("HTTP request must be of type application/json");
    }

    using var sr = new StreamReader(context.Request.Body);
    var update = JsonConvert.DeserializeObject<Update>(await sr.ReadToEndAsync());
    if(update == null) {
        throw new BadHttpRequestException("Could not deserialize JSON payload as Telegram bot update");
    }

    await HandleUpdate(update, logger);

    return Results.Ok();
});

async Task HandleUpdate(Update update, ILogger<Program> logger) {
    logger.LogDebug("Received update {0} of type {1}", update.Id, update.Type);

    if(update.Message == null) {
        logger.LogDebug("Ignoring update without message");
        return;
    }
    if(update.Message.From == null) {
        logger.LogDebug("Ignoring message update without user information");
        return;
    }

    UpdateMemory(update.Message);
}

void UpdateMemory(Message message) {
    if(!userMemory.TryGetValue(message.From!.Id, out var userInformation)) {
        userInformation = new UserInformation(message.From!.Id);
    }
    userInformation.FirstName = message.From!.FirstName;
    userInformation.LastName = message.From!.LastName;
    userInformation.LastConversationUpdate = DateTime.UtcNow;
    userMemory.AddOrUpdate(message.From!.Id, userInformation, (_, _) => userInformation);

    if(!chatMemory.TryGetValue(message.Chat.Id, out var chatInformation)) {
        chatInformation = new ConversationInformation(message.Chat.Id);
    }
    chatInformation.LastConversationUpdate = DateTime.UtcNow;
    chatMemory.AddOrUpdate(message.Chat.Id, chatInformation, (_, _) => chatInformation);
}

app.Run();
