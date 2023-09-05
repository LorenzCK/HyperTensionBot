using HyperTensionBot.Server;
using Newtonsoft.Json;
using Telegram.Bot.Types;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureTelegramBot();

var app = builder.Build();
app.SetupTelegramBot();

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

Task HandleUpdate(Update update, ILogger<Program> logger) {
    logger.LogDebug("Received update {0} of type {1}", update.Id, update.Type);

    return Task.CompletedTask;
}

app.Run();
