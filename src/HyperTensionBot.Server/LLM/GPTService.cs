using Microsoft.AspNetCore.DataProtection.KeyManagement;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace HyperTensionBot.Server.LLM {
    public class GPTService {
        private OpenAIAPI api;
        private string gptKey;
        private List<ChatMessage> chat;

        public GPTService(WebApplicationBuilder builder) {
            if (ConfigureKey(builder)) {
                this.api = new OpenAIAPI(gptKey);
                this.chat = new List<ChatMessage> {
                    new ChatMessage(ChatMessageRole.User, "Sei un bot che si occupa di gestire e supportare l'ipertensione per conto di medici." +
                        "Ti chiedo di rispondere fornendo semplici spiegazioni al contesto ipertensione, mentre per altri contesti medici " +
                        "devi essere il più generico possibile offrendo al massimo dei suggerimenti. Usa un tono educato e rispondi in maniera chiara con al massimo 50 parole."),
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
        }

        private bool ConfigureKey(WebApplicationBuilder builder) {
            var confGpt = builder.Configuration.GetSection("OpenAI");
            if (!confGpt.Exists() || string.IsNullOrEmpty(confGpt["OpenKey"]))
                return false;
            else {
                this.gptKey = confGpt["Openkey"];
                return true;
            }
        }

        public async Task<string> CallGptService(string userMessage) {
            this.chat.Add(new ChatMessage(ChatMessageRole.User, userMessage));
            var response =  await api.Chat.CreateChatCompletionAsync(
                model: Model.ChatGPTTurbo,
                messages: chat,
                max_tokens: 100);
            return response.ToString();
        }
    }
}
