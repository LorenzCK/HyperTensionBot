namespace HyperTensionBot.Server {
    public class ConversationInformation {
        public ConversationInformation(long telegramChatId) {
            TelegramChatId = telegramChatId;
            LastConversationUpdate = DateTime.UtcNow;
        }

        public long TelegramChatId { get; init; }

        public DateTime LastConversationUpdate { get; set; }
    }
}
