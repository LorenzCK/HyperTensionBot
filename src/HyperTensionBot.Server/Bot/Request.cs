using HyperTensionBot.Server.LLM;
using Telegram.Bot.Types;
using Telegram.Bot;
using HyperTensionBot.Server.Services;
using System.Drawing;
using ScottPlot;
using System.Text;
using HyperTensionBot.Server.Bot.Extensions;

namespace HyperTensionBot.Server.Bot {
    public static class Request {

        private static int days;
        private static List<Measurement> measurements = new();

        // manage request

        public static async Task FilterRequest(TelegramBotClient bot, GPTService gpt, Chat chat, string message, string[] button) {
            int.TryParse(await gpt.CallGpt(message, TypeConversation.Analysis), out days);
            Context.SendButton(bot,
                $"Ti fornirò i dati degli ultimi {days} giorni come richiesto. Scegli pure il formato😊",
                chat,
                button);
        }

        public static async Task ManageRequest(string resp, Memory mem, Chat chat, TelegramBotClient bot) {
            // recoverd data in a list and send plot/text in the chat

            mem.UserMemory.TryGetValue(chat.Id, out var info);

            if (info?.FirstMeasurement != null) {
                if (days == -1) {
                    days = DateTime.Now.Subtract(info.FirstMeasurement.Date).Days;
                }
                try {
                    switch (resp) {
                        case "grafFreq":
                            measurements = ProcessesRequest(info, x => x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                            await SendPlot(bot, chat, FrequencyPlot(measurements));
                            break;
                        case "listaFreq":
                            measurements = ProcessesRequest(info, x => x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                            await SendListFreq(bot, chat, measurements);
                            break;
                        case "grafPress":
                            measurements = ProcessesRequest(info,
                                    x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                            await SendPlot(bot, chat, PressionPlot(measurements));
                            break;
                        case "listaPress":
                            measurements = ProcessesRequest(info,
                                x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                            await SendListPress(bot, chat, measurements);
                            break;
                        case "grafTot":
                            measurements = ProcessesRequest(info,
                                x => (x.SystolicPressure != null && x.DiastolicPressure != null) ||
                                x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                            await SendPlot(bot, chat, TotalPlot(measurements));
                            break;
                        case "listaTot":
                            measurements = ProcessesRequest(info,
                                    x => (x.SystolicPressure != null && x.DiastolicPressure != null) ||
                                    x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                            await SendListTot(bot, chat, measurements);
                            break;
                    }
                }
                catch (ArgumentNullException) {
                    await bot.SendTextMessageAsync(chat.Id, "Vorrei fornirti le tue misurazioni ma non sono ancora state registrate, ricordati di farlo quotidianamente.😢\n\n" +
                        "(Pss..💕) Mi è stato riferito che il dottore non vede l'ora di studiare la tua situazione😁");
                }
                catch (ExceptionExtensions.InsufficientData) {
                    await bot.SendTextMessageAsync(chat.Id, "Per poterti generare il grafico necessito di almeno due misurazioni, ricordati di fornirmi giornalmente i tuoi dati.😢\n\n" +
                        "(Pss..💕) Mi è stato riferito che il dottore non vede l'ora di studiare la tua situazione😁");
                }
            }
        }

        private static List<Measurement> ProcessesRequest(UserInformation i, Predicate<Measurement> p) {
            var result =  i.Measurements.FindAll(p);
            if (result is null || result.Count == 0) {
                throw new ArgumentNullException();
            }
            return result;
        }

        private static Plot PressionPlot(List<Measurement> m) {
            var plot = new Plot(600, 400);

            double[] date = m.Select(x => x.Date.ToOADate()).ToArray();
            double?[] systolic = m.Select(m => m.SystolicPressure).Where(x => x != null).ToArray();
            double?[] diastolic = m.Select(m => m.DiastolicPressure).Where(x => x != null).ToArray();

            if (systolic.Length > 1 && diastolic.Length > 1) {
                plot.AddScatterLines(date, systolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                    System.Drawing.Color.Red, 1, LineStyle.Solid, "Pressione Sistolica");
                plot.AddScatterLines(date, diastolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                    System.Drawing.Color.Blue, 1, LineStyle.Solid, "Pressione Diastolica");
            }
            else throw new ExceptionExtensions.InsufficientData();

            plot.XAxis.DateTimeFormat(true);
            plot.YLabel("Pressione (mmHg)");
            plot.XLabel("Data");
            plot.Legend();

            return plot; 
        }

        private static Plot FrequencyPlot(List<Measurement> m) {
            var plot = new Plot(600, 400);

            double[] date = m.Select(x => x.Date.ToOADate()).ToArray();
            double?[] frequence = m.Select(m => m.HeartRate).Where(x => x != null).ToArray();

            if (frequence.Length > 1)
                plot.AddScatterLines(date, frequence.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                    System.Drawing.Color.Red, 1, LineStyle.Solid, "frequenza cardiaca");
            else
                throw new ExceptionExtensions.InsufficientData();
                
            plot.XAxis.DateTimeFormat(true);
            plot.YLabel("Frequenza (bpm)");
            plot.XLabel("Data");
            plot.Legend();

            return plot;
        }

        private static Plot TotalPlot(List<Measurement> m) {
            var plot = new Plot(600, 400);

            double[] date = m.Select(x => x.Date.ToOADate()).ToArray();
            double?[] systolic = m.Select(m => m.SystolicPressure).Where(x => x != null).ToArray();
            double?[] diastolic = m.Select(m => m.DiastolicPressure).Where(x => x != null).ToArray();
            double?[] frequence = m.Select(m => m.HeartRate).Where(x => x != null).ToArray();

            if (systolic.Length > 1 && diastolic.Length > 1 && frequence.Length > 1) {
                plot.AddScatterLines(date, systolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                    System.Drawing.Color.Red, 1, LineStyle.Solid, "Pressione Sistolica");
                plot.AddScatterLines(date, diastolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                    System.Drawing.Color.Blue, 1, LineStyle.Solid, "Pressione Diastolica");
                plot.AddScatterLines(date, frequence.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                    System.Drawing.Color.Yellow, 1, LineStyle.Solid, "frequenza cardiaca");
            }
            else throw new ExceptionExtensions.InsufficientData();
                

            plot.XAxis.DateTimeFormat(true);
            plot.YLabel("Pressione (mmHg) e Frequenza (bpm)");
            plot.XLabel("Data");
            plot.Legend();

            return plot;
        }

        private static async Task SendPlot(TelegramBotClient bot, Chat chat, Plot plot) {
            Bitmap b = plot.Render();
            using (var ms = new MemoryStream()) {
                b.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                await bot.SendPhotoAsync(chat.Id, new InputFileStream(ms));
            }
        }

        private static async Task SendListTot(TelegramBotClient bot, Chat chat, List<Measurement> measurements) {
            await SendListFreq(bot, chat, measurements);
            await SendListPress(bot, chat, measurements);
        }

        private static async Task SendListPress(TelegramBotClient bot, Chat chat, List<Measurement> measurements) {
            var sb = new StringBuilder();
            await bot.SendTextMessageAsync(chat.Id, "Ecco la lista richiesta sulle misurazioni della pressione\n\n ");

            foreach (var m in measurements) {
                if (m.SystolicPressure != null && m.DiastolicPressure != null) 
                    sb.AppendLine($"Pressione {m.SystolicPressure}/{m.DiastolicPressure} mmgh misurata il {m.Date}");
            }
            await bot.SendTextMessageAsync(chat.Id, sb.ToString());
        }

        private static async Task SendListFreq(TelegramBotClient bot, Chat chat, List<Measurement> measurements) {
            var sb = new StringBuilder();
            await bot.SendTextMessageAsync(chat.Id, "Ecco la lista richiesta sulle misurazioni della frequenza😊\n\n ");

            foreach (var m in measurements) {
                if (m.HeartRate != null)
                sb.AppendLine($"Frequenza {m.HeartRate} bpm misurata il {m.Date}");
            }
            await bot.SendTextMessageAsync(chat.Id, sb.ToString());
        }
    }
}
