using HyperTensionBot.Server;
using HyperTensionBot.Server.Services;
using Newtonsoft.Json;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureTelegramBot();
builder.Services.AddSingleton<Memory>();

var app = builder.Build();
app.SetupTelegramBot();

app.MapPost("/webhook", async (HttpContext context, Memory memory, ILogger<Program> logger) => {
    if(!context.Request.HasJsonContentType()) {
        throw new BadHttpRequestException("HTTP request must be of type application/json");
    }

    using var sr = new StreamReader(context.Request.Body);
    var update = JsonConvert.DeserializeObject<Update>(await sr.ReadToEndAsync());
    if(update == null) {
        throw new BadHttpRequestException("Could not deserialize JSON payload as Telegram bot update");
    }

    logger.LogDebug("Received update {0} of type {1}", update.Id, update.Type);

    if (update.Message == null) {
        logger.LogDebug("Ignoring update without message");
        return Results.Ok();
    }
    if (update.Message.From == null) {
        logger.LogDebug("Ignoring message update without user information");
        return Results.Ok();
    }

    memory.HandleUpdate(update.Message);

    // Handle direct data updates

    return Results.Ok();
});

app.Run();
