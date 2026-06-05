namespace GoalsBot.Bot.Conversation;

public interface IConversationStateStore
{
    ConversationState? Get(long chatId);
    void Set(long chatId, ConversationState state);
    void Clear(long chatId);
}
