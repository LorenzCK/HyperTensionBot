using HyperTensionBot.Server.LLM;
using HyperTensionBot.Server.ModelML;
using System.Data.SqlTypes;
using Telegram.Bot;

namespace HyperTensionBot.Server.Bot {
    public static class Context {

        public static async Task ControlFlow(TelegramBotClient bot, GPTService gpt, Intent context, string text, long id) {
            switch (context) {
                // take time frame and elaborate request 
                case Intent.richiestaStatsFreq:
                case Intent.richiestaStatsPress:
                case Intent.richiestaStatsGener:
                    ProcessesRequest(bot, gpt, text, id);
                    break;

                // ask conferme and storage data 
                case Intent.inserDatiGener:
                case Intent.inserDatiPress:
                case Intent.inserDatiFreq:
                case Intent.inserDatiTot:
                    StorageData(bot, text, id);
                    break;

                // idn
                case Intent.pazienAllarmato:
                case Intent.pazienteSereno:
                    await bot.SendTextMessageAsync(
                        id, "Metodo non ancora disponibile!");
                    break;

                // gpt 
                case Intent.saluti:
                case Intent.fuoriCont:
                case Intent.spiegazioni:
                case Intent.fuoriContMed:
                case Intent.richiestaInsDati:
                    await bot.SendTextMessageAsync(
                        id, await gpt.CallGpt(text, TypeConversation.Communication));
                    break;

            }
        }

        private static async void StorageData(TelegramBotClient bot, string text, long id) {
            await bot.SendTextMessageAsync(
                id, "Metodo non ancora disponibile!");
        }

        // elaboration
        private static async void ProcessesRequest(TelegramBotClient bot, GPTService gpt, string text, long id) {
            int.TryParse(await gpt.CallGpt(text, TypeConversation.Analysis), out int days);
            await bot.SendTextMessageAsync(
                id, $"Ti fornisco i dati relativi agli ultimi {days} giorni");
        }

    }
}
