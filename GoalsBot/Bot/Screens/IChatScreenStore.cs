namespace GoalsBot.Bot.Screens;

public interface IChatScreenStore
{
    int? GetMessageId(long chatId);
    void Set(long chatId, int messageId);
    void Clear(long chatId);
}
