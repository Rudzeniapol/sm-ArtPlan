using Microsoft.Extensions.Caching.Memory;

namespace GoalsBot.Bot.Conversation;

// Pending multi-turn state lives in-process with a sliding 30-min expiration.
// Good enough for a single-instance polling worker; swap to a distributed cache
// if we ever run multiple workers or move to webhook + scale-out.
public sealed class MemoryConversationStateStore(IMemoryCache cache) : IConversationStateStore
{
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(30);

    public ConversationState? Get(long chatId) =>
        cache.TryGetValue(Key(chatId), out ConversationState? state) ? state : null;

    public void Set(long chatId, ConversationState state) =>
        cache.Set(Key(chatId), state, new MemoryCacheEntryOptions { SlidingExpiration = SlidingExpiration });

    public void Clear(long chatId) => cache.Remove(Key(chatId));

    private static string Key(long chatId) => $"conv:{chatId}";
}
