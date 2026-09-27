using System.Text.Json.Serialization;

namespace CustomerSupportAgent.Api.Models;

public sealed record ChatRequest(string? SessionId, string Message, string Model, string? KnowledgeBaseId);

public sealed record ChatMessageDto(
    string Id,
    string Role,
    string Content,
    IReadOnlyList<string>? SuggestedQuestions = null,
    RedirectToAgentDto? RedirectToAgent = null);

public sealed record RedirectToAgentDto(bool ShouldRedirect, string? Reason);

public sealed record DebugInfoDto(bool ContextUsed);

public sealed record RagSourceDto(string Id, string FileName, string Snippet, double Score);

public sealed record ThinkingEntryDto(
    string Id,
    string Content,
    string UserMood,
    bool ContextUsed,
    IReadOnlyList<string> MatchedCategories,
    string Timestamp);

public sealed record RagHistoryEntryDto(
    string Query,
    string Timestamp,
    IReadOnlyList<RagSourceDto> Sources);

public sealed record ChatResponse(
    string SessionId,
    string Id,
    string Response,
    string UserMood,
    IReadOnlyList<string> SuggestedQuestions,
    IReadOnlyList<string> MatchedCategories,
    RedirectToAgentDto? RedirectToAgent,
    DebugInfoDto Debug,
    IReadOnlyList<ChatMessageDto> Messages,
    IReadOnlyList<ThinkingEntryDto> ThinkingHistory,
    IReadOnlyList<RagHistoryEntryDto> RagHistory,
    string? LastModel,
    string? LastKnowledgeBaseId);

/// <summary>Full current state of a conversation, fetched by the SSR frontend on every page render
/// (initial load, refresh, and after each Server Action) - not just right after sending a message.</summary>
public sealed record SessionStateResponse(
    string SessionId,
    IReadOnlyList<ChatMessageDto> Messages,
    IReadOnlyList<ThinkingEntryDto> ThinkingHistory,
    IReadOnlyList<RagHistoryEntryDto> RagHistory,
    string? LastModel,
    string? LastKnowledgeBaseId);

/// <summary>Shape Claude is instructed to reply with; mirrors the original Zod schema's wire format.</summary>
public sealed class ClaudeStructuredReply
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = "";

    [JsonPropertyName("thinking")]
    public string Thinking { get; set; } = "";

    [JsonPropertyName("user_mood")]
    public string UserMood { get; set; } = "neutral";

    [JsonPropertyName("suggested_questions")]
    public List<string> SuggestedQuestions { get; set; } = new();

    [JsonPropertyName("matched_categories")]
    public List<string> MatchedCategories { get; set; } = new();

    [JsonPropertyName("debug")]
    public ClaudeDebugDto Debug { get; set; } = new();

    [JsonPropertyName("redirect_to_agent")]
    public ClaudeRedirectDto? RedirectToAgent { get; set; }
}

public sealed class ClaudeDebugDto
{
    [JsonPropertyName("context_used")]
    public bool ContextUsed { get; set; }
}

public sealed class ClaudeRedirectDto
{
    [JsonPropertyName("should_redirect")]
    public bool ShouldRedirect { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
