using Microsoft.AspNetCore.DataProtection.KeyManagement;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace HyperTensionBot.Server.LLM {
    public class GPTService {
        private OpenAIAPI? api;
        private string? gptKey;
        private List<ChatMessage> conversation = new();
        private List<ChatMessage> calculateDays = new();


        public GPTService(WebApplicationBuilder builder) {
            ConfigureKey(builder);
            CreateConversation();
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
                new ChatMessage(ChatMessageRole.User, "A input che si basano su richieste inerenti ai dati su frequenza o pressione devi rispondere con il solo valore numerico" +
                    " che indica il numero di giorni indicato dalla frase data. Per richieste che indicano tutti i dati anche implicitamente l'output deve essere -1, per richieste su dati recenti " +
                    "il valore è 1, mentre per le richieste sull'ultimo valore l'output è 0"),
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

        private void CreateConversation() {
            this.conversation = new List<ChatMessage> {
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


        public async Task<string> CallGpt(string userMessage, TypeConversation t) {
            if (api is not null) {
                if (t == TypeConversation.Communication)
                    this.conversation.Add(new ChatMessage(ChatMessageRole.User, userMessage));
                else
                    this.calculateDays.Add(new ChatMessage(ChatMessageRole.User, userMessage));
                var response = await api.Chat.CreateChatCompletionAsync(
                    model: Model.ChatGPTTurbo,
                    messages: (t == TypeConversation.Communication) ? conversation : calculateDays,
                    max_tokens: 100);
                return response.ToString();
            }
            return "Error Service";
        }
    }
}
