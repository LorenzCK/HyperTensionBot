using System.Collections.Concurrent;
using Telegram.Bot.Types;

namespace HyperTensionBot.Server.Services {
    public class Memory {

        private readonly ConcurrentDictionary<long, UserInformation> _userMemory = new();
        private readonly ConcurrentDictionary<long, ConversationInformation> _chatMemory = new();

        private readonly ILogger<Memory> _logger;

        public Memory(
            ILogger<Memory> logger
        ) {
            _logger = logger;
        }

        public void HandleUpdate(Message message) {
            if (!_userMemory.TryGetValue(message.From!.Id, out var userInformation)) {
                userInformation = new UserInformation(message.From!.Id);
            }
            userInformation.FirstName = message.From!.FirstName;
            userInformation.LastName = message.From!.LastName;
            userInformation.LastConversationUpdate = DateTime.UtcNow;
            _userMemory.AddOrUpdate(message.From!.Id, userInformation, (_, _) => userInformation);
            _logger.LogTrace("Updated user memory");

            if (!_chatMemory.TryGetValue(message.Chat.Id, out var chatInformation)) {
                chatInformation = new ConversationInformation(message.Chat.Id);
            }
            chatInformation.LastConversationUpdate = DateTime.UtcNow;
            _chatMemory.AddOrUpdate(message.Chat.Id, chatInformation, (_, _) => chatInformation);
            _logger.LogTrace("Updated chat memory");
        }
    }
}
