using HyperTensionBot.Server.LLM;
using Telegram.Bot.Types;
using Telegram.Bot;
using HyperTensionBot.Server.Services;
using System.Drawing;
using ScottPlot;
using System.Text;
using HyperTensionBot.Server.Bot.Extensions;
using ScottPlot.Drawing.Colormaps;
using ScottPlot.Plottable;

namespace HyperTensionBot.Server.Bot {
    public static class Request {

        private static int days;
        private static List<Measurement> measurements = new();

        // manage request

        public static async Task FilterRequest(TelegramBotClient bot, GPTService gpt, Chat chat, string message, string[] button) {
            int.TryParse(await gpt.CallGpt(TypeConversation.Analysis, message), out days);
            await Context.SendButton(bot,
                $"Ti fornirò i dati come richiesto. Scegli pure il formato😊",
                chat,
                button);
        }

        public static async Task ManageRequest(string resp, Memory mem, Chat chat, TelegramBotClient bot) {
            // recoverd data in a list and send plot/text in the chat

            mem.UserMemory.TryGetValue(chat.Id, out var info);

            try {
                ControlDays(info);
                switch (resp) {
                    case "grafFreq":
                        measurements = ProcessesRequest(info, x => x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                        await SendPlot(bot, chat, CreatePlot(measurements, false, true));
                        break;
                    case "listaFreq":
                        measurements = ProcessesRequest(info, x => x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                        await SendDataList(bot, chat, measurements, false,
                            $"Ecco la lista richiesta sulle misurazioni della frequenza cardiaca negli ultimi {days} giorni");
                        break;
                    case "grafPress":
                        measurements = ProcessesRequest(info,
                                x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                        await SendPlot(bot, chat, CreatePlot(measurements, true, false));
                        break;
                    case "listaPress":
                        measurements = ProcessesRequest(info,
                            x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                        await SendDataList(bot, chat, measurements, true,
                            $"Ecco la lista richiesta sulle misurazioni della pressione arteriosa negli ultimi {days} giorni");
                        break;
                    case "grafTot":
                        measurements = ProcessesRequest(info,
                            x => (x.SystolicPressure != null && x.DiastolicPressure != null ||
                            x.HeartRate != null) && x.Date >= DateTime.Now.Date.AddDays(-days));
                        await SendPlot(bot, chat, CreatePlot(measurements, true, true));
                        break;
                    case "listaTot":
                        measurements = ProcessesRequest(info,
                                x => (x.SystolicPressure != null && x.DiastolicPressure != null ||
                                x.HeartRate != null) && x.Date >= DateTime.Now.Date.AddDays(-days));
                        await SendDataList(bot, chat, measurements, false,
                            $"Ecco la lista richiesta sulle misurazioni della frequenza cardiaca negli ultimi {days} giorni");
                        await SendDataList(bot, chat, measurements, true,
                            $"Ecco la lista richiesta sulle misurazioni della pressione arteriosa negli ultimi {days} giorni");
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

        private static void ControlDays(UserInformation? info) {
            if (info?.FirstMeasurement is null) { throw new ArgumentNullException(); }
            if (days == -1) {
                days = DateTime.Now.Subtract(info.FirstMeasurement!.Date).Days;
            }
        }

        private static List<Measurement> ProcessesRequest(UserInformation? i, Predicate<Measurement> p) {
            var result =  i?.Measurements.FindAll(p);
            if (result is null || result.Count == 0) {
                throw new ArgumentNullException();
            }
            return result;
        }

        private static Plot CreatePlot(List<Measurement> m, bool includePress, bool includeFreq) {
            var plot = new Plot(600, 400);

            if (includePress) {
                double[] datePressure = m.Where(m => m.SystolicPressure != null).Select(x => x.Date.ToOADate()).ToArray();
                double?[] systolic = m.Select(m => m.SystolicPressure).Where(x => x != null).ToArray();
                double?[] diastolic = m.Select(m => m.DiastolicPressure).Where(x => x != null).ToArray();
                if (systolic.Length > 1 && diastolic.Length > 1) {
                    plot.AddScatterLines(datePressure, systolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Chocolate, 1, LineStyle.Solid, "Pressione Sistolica");
                    plot.AddScatterPoints(datePressure, systolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Black, 7);
                    plot.AddScatterLines(datePressure, diastolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Blue, 1, LineStyle.Solid, "Pressione Diastolica");
                    plot.AddScatterPoints(datePressure, diastolic.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Black, 7);
                }
                else throw new ExceptionExtensions.InsufficientData();
            }
            if (includeFreq) {
                double[] dateFrequence = m.Where(m => m.HeartRate != null).Select(x => x.Date.ToOADate()).ToArray();
                double?[] frequence = m.Select(m => m.HeartRate).Where(x => x != null).ToArray();
                if (frequence.Length > 1) {
                    plot.AddScatterLines(dateFrequence, frequence.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Red, 1, LineStyle.Solid, "frequenza cardiaca");
                    plot.AddScatterPoints(dateFrequence, frequence.Where(d => d.HasValue).Select(d => d!.Value).ToArray(),
                        System.Drawing.Color.Black, 7);
                }
                else
                    throw new ExceptionExtensions.InsufficientData();
            }

            plot.XAxis.DateTimeFormat(true);
            plot.YLabel("Pressione (mmHg) / Frequenza (bpm)");
            plot.XLabel("Data");

            return plot;
        }

        private static async Task SendPlot(TelegramBotClient bot, Chat chat, Plot plot) {
            await bot.SendTextMessageAsync(chat.Id, $"Ecco il grafico richiesto prodotto sugli ultimi {days} giorni");
            Bitmap im = plot.Render();
            Bitmap leg = plot.RenderLegend();
            Bitmap b = new Bitmap(im.Width + leg.Width, im.Height);
            using Graphics g = Graphics.FromImage(b);
            g.Clear(System.Drawing.Color.White);
            g.DrawImage(im, 0, 0);
            g.DrawImage(leg, im.Width, (b.Height - b.Height) / 2);
            using (var ms = new MemoryStream()) {
                b.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                await bot.SendPhotoAsync(chat.Id, new InputFileStream(ms));
            }
        }

        private static async Task SendDataList(TelegramBotClient bot, Chat chat, List<Measurement> measurements, bool press, string mex) {
            var sb = new StringBuilder();
            await bot.SendTextMessageAsync(chat.Id, mex);
            foreach (var m in measurements) {

                if (press && m.SystolicPressure != null && m.DiastolicPressure != null) 
                    sb.AppendLine($"Pressione {m.SystolicPressure}/{m.DiastolicPressure} mmgh misurata il {m.Date}");
                else if (!press && m.HeartRate != null)
                    sb.AppendLine($"Frequenza {m.HeartRate} bpm misurata il {m.Date}");                    
            }
            await bot.SendTextMessageAsync(chat.Id, sb.ToString());
        }

        public static async Task SendGeneralInfo(TelegramBotClient bot, Memory memory, Chat chat) {

            StringBuilder sb = new StringBuilder();
            
            foreach(var s in memory.GetGeneralInfo(chat)) {
                sb.Append(s + "\n");
            }
            if (sb.Length > 0) 
                await bot.SendTextMessageAsync(chat.Id, "Ecco un elenco di tutte le informazioni generali registrate finora!!🗒️\n\n" + sb.ToString());
            else
                await bot.SendTextMessageAsync(chat.Id, "Non sono presenti dati personali nel tuo storico. Queste informazioni sono molto importanti perchè offrono al dottore" +
                    "una panoramica più ampia della tua situazione. Ogni informazione può essere preziosa🗒️");
            
                
        }

        public static List<double?> AverageData(Memory memory, Chat chat, int d, bool pressure, bool frequence) {
            List<double?> average = new();
            days = d;
            memory.UserMemory.TryGetValue(chat.Id, out var info);
            if (info?.FirstMeasurement == null)
                throw new ArgumentNullException();
            ControlDays(info);

            if (pressure) {
                var press = ProcessesRequest(info,
                    x => x.SystolicPressure != null && x.DiastolicPressure != null && x.Date >= DateTime.Now.Date.AddDays(-days));
                average.Add(press.Select(m => m.SystolicPressure).Where(x => x != null).Average());
                average.Add(press.Select(m => m.DiastolicPressure).Where(x => x != null).Average());
            }
            if (frequence) {
                var freq = ProcessesRequest(info,
                    x => x.HeartRate != null && x.Date >= DateTime.Now.Date.AddDays(-days));

                average.Add(freq.Select(x => x.HeartRate).Where(x => x != null).Average());
            }

            return average;
        }
    }
}
