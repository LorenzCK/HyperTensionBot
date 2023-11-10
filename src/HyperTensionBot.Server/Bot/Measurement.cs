using System.Data;

namespace HyperTensionBot.Server.Bot {
    public class Measurement {
        public double? SystolicPressure { get; init; }

        public double? DiastolicPressure { get; init; }

        public double? HeartRate { get; init; }

        public DateTime Date { get; init; }
    }
}
