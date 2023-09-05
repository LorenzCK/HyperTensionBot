using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
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

        public ConversationState HandleUpdate(User? from, Chat chat) {
            if (from != null) {
                if (!_userMemory.TryGetValue(from.Id, out var userInformation)) {
                    userInformation = new UserInformation(from.Id);
                }
                userInformation.FirstName = from.FirstName;
                userInformation.LastName = from.LastName;
                userInformation.LastConversationUpdate = DateTime.UtcNow;
                _userMemory.AddOrUpdate(from.Id, userInformation, (_, _) => userInformation);
                _logger.LogTrace("Updated user memory");
            }

            if (!_chatMemory.TryGetValue(chat.Id, out var chatInformation)) {
                chatInformation = new ConversationInformation(chat.Id);
            }
            chatInformation.LastConversationUpdate = DateTime.UtcNow;
            _chatMemory.AddOrUpdate(chat.Id, chatInformation, (_, _) => chatInformation);
            _logger.LogTrace("Updated chat memory");

            return chatInformation.State;
        }

        public void SetState(Chat chat, ConversationState state) {
            _chatMemory.AddOrUpdate(chat.Id, new ConversationInformation(chat.Id) { State = state }, (_, existing) => {
                existing.State = state;
                return existing;
            });
            _logger.LogTrace("Updated conversation state to {0} for chat {1}", state, chat.Id);
        }

        public void SetTemporaryMeasurement(Chat chat, Measurement measurement) {
            _chatMemory.AddOrUpdate(chat.Id, new ConversationInformation(chat.Id) { TemporaryMeasurement = measurement }, (_, existing) => {
                existing.TemporaryMeasurement = measurement;
                return existing;
            });
            _logger.LogTrace("Stored temporary measurement for chat {0}", chat.Id);
        }

        public void PersistMeasurement(User from, Chat chat) {
            if(!_chatMemory.TryGetValue(chat.Id, out var chatInformation)) {
                throw new Exception($"Tried persisting measurement but no information available about chat {chat.Id}");
            }
            if(chatInformation.TemporaryMeasurement == null) {
                throw new Exception($"Tried persisting measurement but no temporary measurement was recorded for chat {chat.Id}");
            }

            var newValue = new UserInformation(from.Id);
            newValue.Measurements.Add(chatInformation.TemporaryMeasurement);
            _userMemory.AddOrUpdate(from.Id, newValue, (_, existing) => {
                existing.Measurements.Add(chatInformation.TemporaryMeasurement);
                return existing;
            });

            _chatMemory.AddOrUpdate(chat.Id, new ConversationInformation(chat.Id), (_, existing) => {
                existing.State = ConversationState.Idle;
                existing.TemporaryMeasurement = null;
                return existing;
            });
        }
    }
}
