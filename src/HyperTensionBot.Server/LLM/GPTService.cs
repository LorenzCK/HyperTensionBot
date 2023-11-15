using Microsoft.AspNetCore.DataProtection.KeyManagement;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace HyperTensionBot.Server.LLM {
    public class GPTService {
        private OpenAIAPI? api;
        private string? gptKey;
        private List<ChatMessage> calculateDays = new();


        public GPTService(WebApplicationBuilder builder) {
            ConfigureKey(builder);
            AnalysisTime();
        }

        private void ConfigureKey(WebApplicationBuilder builder) {
            var confGpt = builder.Configuration.GetSection("OpenAI");
            if (!confGpt.Exists() && confGpt["OpenKey"] != null)
                throw new ArgumentException("Configuration Gpt: OpenAi Key is not set");
            this.gptKey = confGpt["Openkey"];
            this.api = new OpenAIAPI(gptKey);
        }

        private void AnalysisTime() {
            this.calculateDays = new List<ChatMessage> {
                new ChatMessage(ChatMessageRole.User, "A input che si basano su richieste inerenti ai dati o medie su frequenza e/o pressione devi rispondere con il solo valore numerico" +
                    " che indica il numero di giorni indicato dalla frase data. Per richieste che indicano tutti i dati anche implicitamente l'output deve essere -1, per richieste sugli ultimi dati " +
                    "o su dati recenti il valore è 1, mentre per le richieste sull'ultimo valore l'output è 0"),
                new ChatMessage(ChatMessageRole.Assistant, "Certo."),
                new ChatMessage(ChatMessageRole.User, "Dammi i dati registrati"),
                new ChatMessage(ChatMessageRole.Assistant, "-1"),
                new ChatMessage(ChatMessageRole.User, "Dammi i dati dell'ultimo mese"),
                new ChatMessage(ChatMessageRole.Assistant, "30"),
                new ChatMessage(ChatMessageRole.User, "Voglio i dati più recenti sulla pressione "),
                new ChatMessage(ChatMessageRole.Assistant, "1"),
                new ChatMessage(ChatMessageRole.User, "Dammi l'ultima misurazione"),
                new ChatMessage(ChatMessageRole.Assistant, "0"),
            };
        }

        public async Task<string> CallGpt(TypeConversation t, string userMessage = "", List<ChatMessage>? conversation = null) {
            if (api is not null) {
                if (t == TypeConversation.Communication)
                    conversation!.Add(new ChatMessage(ChatMessageRole.User, userMessage));
                else
                    calculateDays.Add(new ChatMessage(ChatMessageRole.User, userMessage));

                var response = await api.Chat.CreateChatCompletionAsync(
                    model: Model.ChatGPTTurbo,
                    messages: (t == TypeConversation.Communication) ? conversation : calculateDays,
                    max_tokens: 200);
                return response.ToString();
            }
            return "Error Service";
        }
    }
}
