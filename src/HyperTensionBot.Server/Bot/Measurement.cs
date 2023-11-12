using HyperTensionBot.Server.Bot.Extensions;

namespace HyperTensionBot.Server.Bot {
    public class Measurement {
        public double? SystolicPressure { get; init; }

        public double? DiastolicPressure { get; init; }

        public double? HeartRate { get; init; }

        public DateTime Date { get; init; }

        public Measurement(double? systolicPressure, double? diastolicPressure, double? heartRate, DateTime date) {
            Check(systolicPressure, diastolicPressure, heartRate);

            SystolicPressure = systolicPressure;
            DiastolicPressure = diastolicPressure;
            HeartRate = heartRate;
            Date = date;
        }

        private void Check(double? systolicPressure, double? diastolicPressure, double? heartRate) {
            if (systolicPressure != null &&
                (systolicPressure < 40 || systolicPressure > 350))
                throw new ExceptionExtensions.ImpossibleSystolic();

            if (diastolicPressure != null &&
                (diastolicPressure < 10 || diastolicPressure > 180))
                throw new ExceptionExtensions.ImpossibleDiastolic();

            if (heartRate != null &&
                (heartRate < 20 || heartRate > 221))
                throw new ExceptionExtensions.ImpossibleSystolic();
        }
    }
}
