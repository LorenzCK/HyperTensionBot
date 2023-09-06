using HyperTensionBot.Server;
using HyperTensionBot.Server.Services;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureTelegramBot();
builder.Services.AddSingleton<Memory>();

var app = builder.Build();
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

Task HandleConfirmRegisterMeasurement(User from, Chat chat, TelegramBotClient bot, Memory memory, ILogger<Program> logger) {
    memory.PersistMeasurement(from, chat);

    return bot.SendTextMessageAsync(chat.Id,
        new string[] {
            "Perfetto, tutto chiaro\\! Inserisco subito i tuoi dati\\. Ricordati di inviarmi una nuova misurazione domani\\. ⌚",
            "Il dottore sarà impaziente di vedere i tuoi dati\\. Ricordati di inviarmi una nuova misurazione domani\\. ⌚",
            "I dati sono stati inseriti, spero solo che il dottore capisca la mia calligrafia\\! Ricordati di inviarmi una nuova misurazione domani\\. ⌚",
            "Perfetto, grazie\\! Ricordati di inviarmi una nuova misurazione domani\\. ⌚"
        }.PickRandom(),
        parseMode: ParseMode.MarkdownV2
    );
}

Task HandleRefuseRegisterMeasurement(Chat chat, TelegramBotClient bot, Memory memory, ILogger<Program> logger) {
    memory.SetState(chat, ConversationState.Idle);

    return bot.SendTextMessageAsync(chat.Id,
        new string[] {
            "No? Mandami pure i dati corretti allora\\.\nInvia le misure rilevate in un *unico messaggio di testo*, separando *pressione minima*, *massima* e *frequenza cardiaca* con uno spazio\\.",
            "Devo aver capito male, puoi ripetere i dati della misurazione?\nInvia le misure rilevate in un *unico messaggio di testo*, separando *pressione minima*, *massima* e *frequenza cardiaca* con uno spazio\\.",
            "Forse ho capito male, puoi ripetere?\nInvia le misure rilevate in un *unico messaggio di testo*, separando *pressione minima*, *massima* e *frequenza cardiaca* con uno spazio\\.",
        }.PickRandom(),
        parseMode: ParseMode.MarkdownV2
    );
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

app.MapPost("/webhook", async (HttpContext context, TelegramBotClient bot, Memory memory, ILogger<Program> logger) => {
    if (!context.Request.HasJsonContentType()) {
        throw new BadHttpRequestException("HTTP request must be of type application/json");
    }

    using var sr = new StreamReader(context.Request.Body);
    var update = JsonConvert.DeserializeObject<Update>(await sr.ReadToEndAsync());
    if (update == null) {
        throw new BadHttpRequestException("Could not deserialize JSON payload as Telegram bot update");
    }

    logger.LogDebug("Received update {0} of type {1}", update.Id, update.Type);

    User? from = update.Message?.From ?? update.CallbackQuery?.From;
    Chat chat = update.Message?.Chat ?? update.CallbackQuery?.Message?.Chat ?? throw new Exception("Unable to detect chat ID");
    var state = memory.HandleUpdate(from, chat);

    logger.LogInformation("Chat {0} incoming {1}", chat.Id, update.Type switch {
        UpdateType.Message => $"message with text: {update.Message?.Text}",
        UpdateType.CallbackQuery => $"callback with data: {update.CallbackQuery?.Data}",
        _ => "update of unhandled type"
    });

    await HandleCallbacks(update, bot, memory, state, logger);
    await HandleConversation(update, bot, memory, state, logger);

    // Handle direct data updates
    var measurementMatch = measurementRegex.Match(update.Message?.Text ?? string.Empty);
    if(measurementMatch.Success) {
        logger.LogDebug("Incoming message matches measurement regex");
        var v1 = measurementMatch.GetIntMatch("v1");
        var v2 = measurementMatch.GetIntMatch("v2");
        var v3 = measurementMatch.GetOptionalIntMatch("v3");

        var systolic = Math.Max(v1, v2);
        var diastolic = Math.Min(v1, v2);

        memory.SetTemporaryMeasurement(update.Message!.Chat, new Measurement {
            SystolicPressure = systolic,
            DiastolicPressure = diastolic,
            HeartRate = v3
        });

        memory.SetState(update.Message!.Chat, ConversationState.NewMeasurementReceived);

        var sb = new StringBuilder();
        sb.Append($"Grazie per avermi inviato la tua misurazione\\.\n\n🔺 Pressione sistolica: *{systolic}* mmHg\n🔻 Pressione diastolica: *{diastolic}* mmHg\n");
        if(v3.HasValue) {
            sb.Append($"🩺 Frequenza cardiaca: *{v3}* bpm\n");
        }
        sb.Append("\nHo capito bene?");

        await bot.SendTextMessageAsync(update.Message!.Chat.Id, sb.ToString(),
            parseMode: ParseMode.MarkdownV2,
            replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[] {
                new InlineKeyboardButton("Sì, registra!") { CallbackData = "yes" },
                new InlineKeyboardButton("No") { CallbackData = "no" },
            })
        );

        return Results.Ok();
    }

    // Default
    await bot.SendTextMessageAsync(chat.Id,
        new string[] {
            "Come scusa? Non ho capito\\.\n\n🩺 Inviami le tue misure come messaggio di testo, separando *pressione minima*, *massima* e *frequenza cardiaca* con uno spazio\\.",
            "Non credo di aver capito\\.\n\n🩺 Inviami le tue misure come messaggio di testo, separando *pressione minima*, *massima* e *frequenza cardiaca* con uno spazio\\.",
            "Puoi ripetere?\n\n🩺 Inviami le tue misure come messaggio di testo, separando *pressione minima*, *massima* e *frequenza cardiaca* con uno spazio\\.",
            "Scusami, non ho capito\\.\n\n🩺 Inviami le tue misure come messaggio di testo, separando *pressione minima*, *massima* e *frequenza cardiaca* con uno spazio\\."
        }.PickRandom(),
        parseMode: ParseMode.MarkdownV2
    );

    return Results.Ok();
});

app.Run();
