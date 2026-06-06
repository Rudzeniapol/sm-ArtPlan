using Microsoft.Extensions.Caching.Memory;

namespace GoalsBot.Bot.Screens;

// Remembers which bot message is currently the "active view" for each chat,
// so the next view-transition can delete it before sending its replacement.
// Single-process in-memory storage; fine for the polling worker.
public sealed class MemoryChatScreenStore(IMemoryCache cache) : IChatScreenStore
{
    private static readonly TimeSpan Sliding = TimeSpan.FromDays(2);

    public int? GetMessageId(long chatId) =>
        cache.TryGetValue(Key(chatId), out int id) ? id : null;

    public void Set(long chatId, int messageId) =>
        cache.Set(Key(chatId), messageId, new MemoryCacheEntryOptions { SlidingExpiration = Sliding });

    public void Clear(long chatId) => cache.Remove(Key(chatId));

    private static string Key(long chatId) => $"screen:{chatId}";
}
