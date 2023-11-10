using HyperTensionBot.Server.Bot;
using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.ModelML;
using HyperTensionBot.Server.Services;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureTelegramBot();
builder.Services.AddSingleton<Memory>();

// add model and GPT 
builder.Services.AddSingleton(new ClassificationModel(builder));
builder.Services.AddSingleton(new GPTService(builder));

var app = builder.Build();

// add model to service
app.SetupTelegramBot();

string[] TextPartsAffirmativeYes = new string[] {
    "sì",
    "certo",
    "certamente",
    "esatto",
    "perfetto",
    "ok",
    "va bene",
    "bene",
    "benissimo",
    "yes",
    "yep",
    "okappa",
};

string[] TextPartsAffirmativeNo = new string[] {
    "no",
    "sbagliato",
    "sbagli",
    "ti sbagli",
    "ho sbagliato",
    "mi sono sbagliat",
    "sbagliat",
    "errato",
    "errore",
    "negativo",
    "annulla",
    "torna indietro",
    "indietro",
    "ops",
    "whops",
    "uops",
};

AffirmativeReplyType DetermineAffirmativeReply(string text) {
    text = (text ?? string.Empty).Trim().ToLowerInvariant();

    if (TextPartsAffirmativeYes.Any(text.StartsWith))
        return AffirmativeReplyType.Yes;
    if (TextPartsAffirmativeNo.Any(text.StartsWith))
        return AffirmativeReplyType.No;

    return AffirmativeReplyType.Unknown;
}

/*
async Task HandleCallbacks(Update update, TelegramBotClient bot, Memory memory, ConversationState state, ILogger<Program> logger) {
    if (update.CallbackQuery?.Data == null || update.CallbackQuery?.Message?.Chat == null) {
        return;
    }

    switch (state) {
        case ConversationState.NewMeasurementReceived: {
                if (update.CallbackQuery.Data == "yes") {
                    await HandleConfirmRegisterMeasurement(update.CallbackQuery.From, update.CallbackQuery.Message.Chat, bot, memory, logger);
                }
                else if (update.CallbackQuery.Data == "no") {
                    await HandleRefuseRegisterMeasurement(update.CallbackQuery.Message.Chat, bot, memory, logger);
                }
            }
            break;
    }

    await bot.AnswerCallbackQueryAsync(update.CallbackQuery.Id);
}

async Task HandleConversation(Update update, TelegramBotClient bot, Memory memory, ConversationState state, ILogger<Program> logger) {
    if (update.Message?.Text == null) {
        return;
    }

    switch (state) {
        case ConversationState.NewMeasurementReceived: {
                switch (DetermineAffirmativeReply(update.Message.Text)) {
                    case AffirmativeReplyType.Yes:
                        await HandleConfirmRegisterMeasurement(update.Message.From!, update.Message.Chat, bot, memory, logger);
                        break;

                    case AffirmativeReplyType.No:
                        await HandleRefuseRegisterMeasurement(update.Message.Chat, bot, memory, logger);
                        break;

                    case AffirmativeReplyType.Unknown:
                    default:
                        await HandleCouldNotUnderstand(update.Message.Chat, bot, memory, logger);
                        break;
                }
            }
            break;
    }
}





Task HandleCouldNotUnderstand(Chat chat, TelegramBotClient bot, Memory memory, ILogger<Program> logger) {
    return bot.SendTextMessageAsync(chat.Id,
        new string[] {
            "Come scusa? Non ho capito\\.",
            "Non credo di aver capito\\!",
            "Potresti ripetere… oggi non sono proprio attento\\!",
            "Puoi ripetere? Non ti ho capito\\.",
            "Scusami… non ho capito\\!",
            "Ieri sera non avrei dovuto far così tardi… non ho capito, potresti ripetere?",
            "Hmmmmm… perdonami, non ho capito\\.",
        }.PickRandom(),
        parseMode: ParseMode.MarkdownV2
    );
}

var measurementRegex = new Regex("(?<v1>[0-9]{2,3})[^0-9]{1,10}(?<v2>[0-9]{2,3})([^0-9]{1,10}(?<v3>[0-9]{2,3}))?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
*/
app.MapPost("/webhook", async (HttpContext context, TelegramBotClient bot, Memory memory, ILogger<Program> logger, ClassificationModel model, GPTService gpt) => {
    if (!context.Request.HasJsonContentType()) {
        throw new BadHttpRequestException("HTTP request must be of type application/json");
    }

    using var sr = new StreamReader(context.Request.Body);
    var update = JsonConvert.DeserializeObject<Update>(await sr.ReadToEndAsync()) ?? throw new BadHttpRequestException("Could not deserialize JSON payload as Telegram bot update");
    logger.LogDebug("Received update {0} of type {1}", update.Id, update.Type);

    User? from = update.Message?.From ?? update.CallbackQuery?.From;
    Chat chat = update.Message?.Chat ?? update.CallbackQuery?.Message?.Chat ?? throw new Exception("Unable to detect chat ID");
    var state = memory.HandleUpdate(from, chat);

    logger.LogInformation("Chat {0} incoming {1}", chat.Id, update.Type switch {
        UpdateType.Message => $"message with text: {update.Message?.Text}",
        UpdateType.CallbackQuery => $"callback with data: {update.CallbackQuery?.Data}",
        _ => "update of unhandled type"
    });
    if (update.Message?.Text is not null) {
        var messageText = update.Message?.Text;
        if (messageText != null) {
            // add message to model input and predict intent
            var input = new ModelInput { Sentence = messageText };
            var result = model.Predict(input);

            await bot.SendTextMessageAsync(
                chat.Id,
                text: $"Il messaggio matcha con {result.ToString()}"
            );

            // manage operations 
            await Context.ControlFlow(bot, gpt, memory, result, messageText, chat);
        }
        
    }
    else if (update.CallbackQuery?.Data != null && update.CallbackQuery?.Message?.Chat != null) {
        await Context.ValuteMeasurement(update.CallbackQuery.Data, update.CallbackQuery.From, update.CallbackQuery.Message.Chat, bot, memory);
        await Request.ManageRequest(update.CallbackQuery.Data, memory, update.CallbackQuery.Message.Chat, bot);
        } else return Results.NotFound();

    return Results.Ok();
});

app.Run();
