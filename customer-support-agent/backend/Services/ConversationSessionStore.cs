using System.Collections.Concurrent;
using CustomerSupportAgent.Api.Models;

namespace CustomerSupportAgent.Api.Services;

public sealed class ConversationSession
{
    public string Id { get; } = Guid.NewGuid().ToString("n");
    public List<ChatMessageDto> Messages { get; } = new();
    public List<ThinkingEntryDto> ThinkingHistory { get; } = new();
    public List<RagHistoryEntryDto> RagHistory { get; } = new();
    public string? LastModel { get; set; }
    public string? LastKnowledgeBaseId { get; set; }
}

/// <summary>
/// In-memory, per-process conversation store keyed by an opaque session id that the frontend
/// keeps in a cookie. Resets on backend restart - fine for a quickstart, no database needed.
/// </summary>
public sealed class ConversationSessionStore
{
    private const int MaxHistoryEntries = 15;

    private readonly ConcurrentDictionary<string, ConversationSession> _sessions = new();

    public ConversationSession GetOrCreate(string? sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId) && _sessions.TryGetValue(sessionId, out var existing))
        {
            return existing;
        }

        var session = new ConversationSession();
        _sessions[session.Id] = session;
        return session;
    }

    public void RecordThinking(ConversationSession session, ThinkingEntryDto entry)
    {
        session.ThinkingHistory.Insert(0, entry);
        if (session.ThinkingHistory.Count > MaxHistoryEntries)
        {
            session.ThinkingHistory.RemoveRange(MaxHistoryEntries, session.ThinkingHistory.Count - MaxHistoryEntries);
        }
    }

    public void RecordRagSources(ConversationSession session, RagHistoryEntryDto entry)
    {
        session.RagHistory.Insert(0, entry);
        if (session.RagHistory.Count > MaxHistoryEntries)
        {
            session.RagHistory.RemoveRange(MaxHistoryEntries, session.RagHistory.Count - MaxHistoryEntries);
        }
    }
}
