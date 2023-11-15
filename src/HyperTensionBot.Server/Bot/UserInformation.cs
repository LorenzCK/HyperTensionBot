using HyperTensionBot.Server.LLM;
using OpenAI_API.Chat;

namespace HyperTensionBot.Server.Bot {
    public class UserInformation {
        public UserInformation(long telegramId) {
            TelegramId = telegramId;
            LastConversationUpdate = DateTime.UtcNow;
            Measurements = new();
            GeneralInfo = new();
            ChatMessages = ChatConfig();
        }

        private List<ChatMessage> ChatConfig() {
            return new List<ChatMessage> {
                new ChatMessage(ChatMessageRole.User, "Sei un assistente virtuale specializzato nel fornire informazioni generali sul contesto medico dell'ipertensione. " +
                    "Puoi rispondere a domande relative all'ipertensione, fornire consigli generali sulla salute e sul benessere, e discutere di argomenti medici in generale. " +
                    "Tuttavia, non sei in grado di fornire consigli medici specifici o rispondere a domande al di fuori di questo ambito di competenza." +
                    "Usa un tono educato e rispondi in maniera chiara con al massimo 50 parole se gli input sono inerenti al tuo ruolo, altrimenti sii generico e non fornire spiegazioni."),
                new ChatMessage(ChatMessageRole.Assistant, "Ho compreso il mio ruolo."),
                new ChatMessage(ChatMessageRole.User, "Salve, come posso effettuare delle misurazioni ottimali?"),
                new ChatMessage(ChatMessageRole.Assistant, "Posso darti i seguenti consigli: Ricordati di attendere qualche minuto in posizione seduta " +
                    "prima di effettuare le misurazioni. Evita di effettuare le misurazioni dopo: pasti, fumo di sigarette, consumo di alcolici, sforzi fisici o stress emotivi. " +
                    "Posiziona il bracciale uno o due centimetri sopra la piega del gomito. Durante le misurazioni resta in posizione seduta, comoda, con il braccio rilassato e appoggiato in modo che " +
                    "il bracciale si trovi all’altezza del cuore"),
                new ChatMessage(ChatMessageRole.User, "Oggi si è rotta la mia macchina, come potrei fare?"),
                new ChatMessage(ChatMessageRole.Assistant, "Non sono un esperto di vetture, posso solo consigliarti di recarti da un meccanico"),
                new ChatMessage(ChatMessageRole.User, "Mostrami una ricetta orginale"),
                new ChatMessage(ChatMessageRole.Assistant, "Mi dispiace ma non posso operare in campi che non sono di mia competenza. Sarò lieto di risponderti su temi dell'ipertensione."),
            };
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
                if (Measurements.Count == 0) {
                    return null;
                }

                return Measurements[Measurements.Count - 1];
            }
        }

        public Measurement? FirstMeasurement {
            get {
                if (Measurements.Count == 0) {
                    return null;
                }

                return Measurements[0];
            }
        }

        public DateTime LastConversationUpdate { get; set; }

        public List<string> GeneralInfo { get; set; }

        public List<ChatMessage> ChatMessages { get; set; }

        public override bool Equals(object? obj) {
            if (obj is UserInformation userInformation) {
                return TelegramId == userInformation.TelegramId;
            }

            return false;
        }

        public override int GetHashCode() {
            return TelegramId.GetHashCode();
        }
    }
}
