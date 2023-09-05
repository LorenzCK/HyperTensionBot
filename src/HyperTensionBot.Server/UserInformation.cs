namespace HyperTensionBot.Server {
    public class UserInformation {
        public UserInformation(long telegramId) {
            TelegramId = telegramId;
            LastConversationUpdate = DateTime.UtcNow;
            Measurements = new();
        }

        public long TelegramId { get; init; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? FullName {
            get {
                return string.Join(" ", new string?[] { FirstName, LastName }.Where(s => s != null));
            }
        }

        public List<Measurement> Measurements { get; init; }

        public Measurement? LastMeasurement {
            get {
                if(Measurements.Count == 0) {
                    return null;
                }

                return Measurements[Measurements.Count - 1];
            }
        }

        public DateTime LastConversationUpdate { get; set; }

        public override bool Equals(object? obj) {
            if(obj is UserInformation userInformation) {
                return TelegramId == userInformation.TelegramId;
            }

            return false;
        }

        public override int GetHashCode() {
            return TelegramId.GetHashCode();
        }
    }
}
