using System.Collections.Concurrent;

namespace CompanyInfoBot;

public sealed class LastCommandMemory
{
    private readonly ConcurrentDictionary<long, string> _map = new();
    public void Remember(long chat, string answer) => _map[chat] = answer;
    public string? Get(long chat) => _map.TryGetValue(chat, out var v) ? v : null;
}
