using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.ModelML;
using HyperTensionBot.Server.Services;
using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;

namespace HyperTensionBot.Server.Bot {
    public static class Context {

        public static async Task ControlFlow(TelegramBotClient bot, GPTService gpt, Memory memory, Intent conmessage, string message, Chat chat) {
            switch (conmessage) {
                // take time frame and elaborate request 
                case Intent.richiestaStatsFreq:
                case Intent.richiestaStatsPress:
                    ProcessesRequestPress(bot, gpt, message, chat.Id);
                    break;
                case Intent.richiestaStatsGener:
                    ProcessesRequestTot(bot, gpt, message, chat.Id);
                    break;

                // ask conferme and storage data 
                case Intent.inserDatiGener:
                case Intent.inserDatiPress:
                    StorageDataPress(bot, message, chat, memory);
                    break;
                case Intent.inserDatiFreq:
                    StorageDataFreq(bot, message, chat, memory);
                    break;
                case Intent.inserDatiTot:
                    StorageDataTot(bot, message, chat, memory);
                    break;

                // chat.Idn
                case Intent.pazienAllarmato:
                case Intent.pazienteSereno:
                    await bot.SendTextMessageAsync(
                        chat.Id, "Metodo non ancora disponibile!");
                    break;

                // gpt 
                case Intent.saluti:
                case Intent.fuoriCont:
                case Intent.spiegazioni:
                case Intent.fuoriContMed:
                    await bot.SendTextMessageAsync(
                        chat.Id, await gpt.CallGpt(message, TypeConversation.Communication));
                    break;
                case Intent.richiestaInsDati:
                    await bot.SendTextMessageAsync(
                        chat.Id,
                        "Certo. Registra pure le tue misurazioni, io farò il resto! Ti ricordo che potrai inserire " +
                        "misurazioni sulla pressione, sulla frequenza o entrambi!");
                    break;

            }
        }

        

        // manage request 
        private static async void ProcessesRequestPress(TelegramBotClient bot, GPTService gpt, string message, long id) {
            int.TryParse(await gpt.CallGpt(message, TypeConversation.Analysis), out int days);
            await bot.SendTextMessageAsync(
                id, $"Ti fornisco i dati relativi agli ultimi {days} giorni");
        }

        private static async void ProcessesRequestTot(TelegramBotClient bot, GPTService gpt, string message, long id) {
            int.TryParse(await gpt.CallGpt(message, TypeConversation.Analysis), out int days);
            await bot.SendTextMessageAsync(
                id, $"Ti fornisco i dati relativi agli ultimi {days} giorni");
        }

        // manage meuserment
        private static void StorageDataTot(TelegramBotClient bot, string message, Chat chat, Memory memory) {
            // Match values
            try {
                var measurement = RegexExtensions.ExtractMeasurement(message);
                // send message and button
                memory.SetTemporaryMeasurement(chat, new Measurement {
                    SystolicPressure = measurement[0],
                    DiastolicPressure = measurement[1],
                    HeartRate = measurement[2]
                });

                string text = $"Grazie per avermi inviato pressione e frequenza\\.\n\n🔺 Pressione sistolica: *{measurement[0].ToString("F2")}* mmHg\n🔻 Pressione diastolica: *{measurement[1].ToString("F2")}* mmHg\n" +
                    $"❤️ Frequenza: *{measurement[2].ToString("F2")}* bpm\n\nHo capito bene?Ho capito bene?";

                SendButton(bot, text, chat);

            }
            catch (ArgumentException) {
                bot.SendTextMessageAsync(chat.Id, "Non ho compreso i dati. Prova a riscrivere il messaggio in un altro modo😊\nProva inserendo prima i valori di pressione e poi la frequenza🤞.");
            }

        }


        private static void StorageDataPress(TelegramBotClient bot, string message, Chat chat, Memory memory) {
            // Match values
            try {
                var pressure = RegexExtensions.ExtractPressure(message);
                // send message and button
                memory.SetTemporaryMeasurement(chat, new Measurement {
                    SystolicPressure = pressure[0],
                    DiastolicPressure = pressure[1],
                    HeartRate = null
                });

                string text = $"Grazie per avermi inviato la tua pressione\\.\n\n🔺 Pressione sistolica: *{pressure[0].ToString("F2")}* mmHg\n🔻 Pressione diastolica: *{pressure[1].ToString("F2")}* mmHg\n" +
                    $"Ho capito bene?";

                SendButton(bot, text, chat);

            } catch(ArgumentException) {
                bot.SendTextMessageAsync(chat.Id, "Non ho compreso i dati. Prova a riscrivere il messaggio in un altro modo😊🤞.");
            }
            
        }

        private static void StorageDataFreq(TelegramBotClient bot, string message, Chat chat, Memory memory) {
            // Match values
            try {
                var freq = RegexExtensions.ExtractFreq(message);

                // send message and button
                memory.SetTemporaryMeasurement(chat, new Measurement {
                    SystolicPressure = null,
                    DiastolicPressure = null,
                    HeartRate = freq
                });

                string text = $"Grazie per avermi inviato la tua frequenza\\.\n\n❤️ Frequenza: *{freq.ToString("F2")}* bpm\nHo capito bene?";

                SendButton(bot, text, chat);

            } catch (ArgumentException) {
                bot.SendTextMessageAsync(chat.Id, "Non ho compreso i dati. Prova a riscrivere il messaggio in un altro modo😊🤞.");
            }
        }

        private static void SendButton(TelegramBotClient bot, string text, Chat chat) {
            bot.SendTextMessageAsync(chat.Id, text,
                parseMode: ParseMode.MarkdownV2,
                replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[] {
                new InlineKeyboardButton("Sì, registra!") { CallbackData = "yes" },
                new InlineKeyboardButton("No") { CallbackData = "no" },
                })
            );
        }

        // manage button
        public static async Task ValuteMeasurement(string resp, User from, Chat chat, TelegramBotClient bot, Memory memory) {
            if (resp == "yes") {
                await HandleConfirmRegisterMeasurement(from, chat, bot, memory);
            }
            else if (resp == "no") {
                await HandleRefuseRegisterMeasurement(chat, bot, memory);
            }
        }

        private static async Task HandleConfirmRegisterMeasurement(User from, Chat chat, TelegramBotClient bot, Memory memory) {
            memory.PersistMeasurement(from, chat);

            await bot.SendTextMessageAsync(chat.Id,
                new string[] {
                    "Perfetto, tutto chiaro\\! Inserisco subito i tuoi dati\\. Ricordati di inviarmi una nuova misurazione domani\\. ⌚",
                    "Il dottore sarà impaziente di vedere i tuoi dati\\. Ricordati di inviarmi una nuova misurazione domani\\. ⌚",
                    "I dati sono stati inseriti, spero solo che il dottore capisca la mia calligrafia\\! Ricordati di inviarmi una nuova misurazione domani\\. ⌚",
                    "Perfetto, grazie\\! Ricordati di inviarmi una nuova misurazione domani\\. ⌚"
                        }.PickRandom(),
                        parseMode: ParseMode.MarkdownV2
            );
        }

        private static async Task HandleRefuseRegisterMeasurement(Chat chat, TelegramBotClient bot, Memory memory) {

            await bot.SendTextMessageAsync(chat.Id,
                new string[] {
                    "No? Mandami pure i dati corretti allora\\.\nInvia le misure rilevate in un *unico messaggio di testo.\\.",
                    "Devo aver capito male, puoi ripetere i dati della misurazione?\nInvia le misure rilevate in un *unico messaggio di testo*\\.",
                    "Forse ho capito male, puoi ripetere?\nInvia le misure rilevate in un *unico messaggio di testo*\\.",
                        }.PickRandom(),
                        parseMode: ParseMode.MarkdownV2
            );
        }
    }    
}
