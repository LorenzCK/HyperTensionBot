using HyperTensionBot.Server.LLM;
using Telegram.Bot.Types;
using Telegram.Bot;
using HyperTensionBot.Server.Services;
using System.Drawing;
using ScottPlot;

namespace HyperTensionBot.Server.Bot {
    public static class Request {

        private static int days;

        // manage request

        public static async Task FilterRequest(TelegramBotClient bot, GPTService gpt, Chat chat, string message, string[] button) {
            int.TryParse(await gpt.CallGpt(message, TypeConversation.Analysis), out days);
            Context.SendButton(bot,
                "Ti fornirò i dati come richiesto. Scegli pure il formato😊",
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
                            var freq = ProcessesRequest(info, x => x.HeartRate != null && x.Date >= DateTime.Now.AddDays(-days));
                            await SendPlot(bot, chat, FrequencyPlot(freq));
                            break;
                        case "listaFreq":
                            ProcessesRequest(info, x => x.HeartRate != null && x.Date >= DateTime.Now.AddDays(-days));
                            break;
                        case "grafPress":
                            var press = ProcessesRequest(info,
                                    x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.AddDays(-days));
                            await SendPlot(bot, chat, PressionPlot(press));
                            break;
                        case "listaPress":
                            ProcessesRequest(info,
                                x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.AddDays(-days));
                            break;
                        case "grafTot":
                            var measurements = ProcessesRequest(info,
                                x => x.SystolicPressure != null && x.DiastolicPressure != null &&
                                x.HeartRate != null && x.Date >= DateTime.Now.AddDays(-days));
                            await SendPlot(bot, chat, TotalPlot(measurements));
                            break;
                        case "listaTot":
                            ProcessesRequest(info,
                                    x => x.SystolicPressure != null && x.DiastolicPressure != null &&
                                    x.HeartRate != null && x.Date >= DateTime.Now.AddDays(-days));
                            break;
                    }
                } catch(ArgumentNullException) {
                    await bot.SendTextMessageAsync(chat.Id, "Non sono presenti misurazioni per generare la tua richiesta!😢");
                }
            }
        }

        private static List<Measurement> ProcessesRequest(UserInformation i, Predicate<Measurement> p) {
            return i.Measurements.FindAll(p); 
        }

        private static Plot PressionPlot(List<Measurement> m) {
            var plot = new Plot(600, 400);

            double[] date = m.Select(x => x.Date.ToOADate()).ToArray();
            double?[] systolic = m.Select(m => m.SystolicPressure).ToArray();
            double?[] diastolic = m.Select(m => m.DiastolicPressure).ToArray();

            if (systolic != null && diastolic != null) {
                plot.AddScatterLines(date, systolic.Where(d => d.HasValue).Select(d => d.Value).ToArray(),
                    System.Drawing.Color.Red, 1, LineStyle.Solid, "Pressione Sistolica");
                plot.AddScatterLines(date, diastolic.Where(d => d.HasValue).Select(d => d.Value).ToArray(),
                    System.Drawing.Color.Blue, 1, LineStyle.Solid, "Pressione Diastolica");
            }
            else throw new ArgumentNullException();

            plot.XAxis.DateTimeFormat(true);
            plot.YLabel("Pressione (mmHg)");
            plot.XLabel("Data");
            plot.Legend();

            return plot; 
        }

        private static Plot FrequencyPlot(List<Measurement> m) {
            var plot = new Plot(600, 400);

            double[] date = m.Select(x => x.Date.ToOADate()).ToArray();
            double?[] frequence = m.Select(m => m.HeartRate).ToArray();

            if (frequence != null) {
                plot.AddScatterLines(date, frequence.Where(d => d.HasValue).Select(d => d.Value).ToArray(),
                    System.Drawing.Color.Red, 1, LineStyle.Solid, "frequenza cardiaca");
            }
            else throw new ArgumentNullException();

            plot.XAxis.DateTimeFormat(true);
            plot.YLabel("Frequenza (bpm)");
            plot.XLabel("Data");
            plot.Legend();

            return plot;
        }

        private static Plot TotalPlot(List<Measurement> m) {
            var plot = new Plot(600, 400);

            double[] date = m.Select(x => x.Date.ToOADate()).ToArray();
            double?[] systolic = m.Select(m => m.SystolicPressure).ToArray();
            double?[] diastolic = m.Select(m => m.DiastolicPressure).ToArray();
            double?[] frequence = m.Select(m => m.HeartRate).ToArray();

            if (systolic != null && diastolic != null && frequence != null) {
                plot.AddScatterLines(date, systolic.Where(d => d.HasValue).Select(d => d.Value).ToArray(),
                    System.Drawing.Color.Red, 1, LineStyle.Solid, "Pressione Sistolica");
                plot.AddScatterLines(date, diastolic.Where(d => d.HasValue).Select(d => d.Value).ToArray(),
                    System.Drawing.Color.Blue, 1, LineStyle.Solid, "Pressione Diastolica");
                plot.AddScatterLines(date, frequence.Where(d => d.HasValue).Select(d => d.Value).ToArray(),
                    System.Drawing.Color.Yellow, 1, LineStyle.Solid, "frequenza cardiaca");
            }
            else throw new ArgumentNullException();

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
    }
}
