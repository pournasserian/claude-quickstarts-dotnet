using CustomerSupportAgent.Api.Models;
using CustomerSupportAgent.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ISupportCategoriesProvider, SupportCategoriesProvider>();
builder.Services.AddSingleton<IKnowledgeBaseRetriever, BedrockKnowledgeBaseRetriever>();
builder.Services.AddSingleton<ConversationSessionStore>();
builder.Services.AddSingleton<ClaudeChatService>();

var app = builder.Build();

// Available Claude models and knowledge bases, ported from the model/knowledgeBases arrays that
// used to live in components/ChatArea.tsx - now backend-owned config instead of hardcoded in the UI.
var availableModels = new List<ModelOption>
{
    new("claude-3-haiku-20240307", "Claude 3 Haiku"),
    new("claude-haiku-4-5-20251001", "Claude 4.5 Haiku"),
    new("claude-3-5-sonnet-20240620", "Claude 3.5 Sonnet"),
};

var availableKnowledgeBases = new List<KnowledgeBaseOption>
{
    new(app.Configuration["Aws:KnowledgeBaseId"] ?? "your-knowledge-base-id", "Default Knowledge Base"),
};

app.MapGet("/api/config", () => new ConfigResponse(availableModels, availableKnowledgeBases));

app.MapPost("/api/chat", async (
    ChatRequest request,
    ConversationSessionStore sessions,
    ClaudeChatService chat,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "message is required" });
    }

    var session = sessions.GetOrCreate(request.SessionId);
    session.LastModel = request.Model;
    session.LastKnowledgeBaseId = request.KnowledgeBaseId;

    var userMessage = new ChatMessageDto(Guid.NewGuid().ToString("n"), "user", request.Message);
    session.Messages.Add(userMessage);

    var turn = await chat.GetReplyAsync(session, request.Message, request.Model, request.KnowledgeBaseId, cancellationToken);

    var assistantMessage = new ChatMessageDto(
        Guid.NewGuid().ToString("n"),
        "assistant",
        turn.Reply.Response,
        turn.Reply.SuggestedQuestions,
        turn.Reply.RedirectToAgent is null
            ? null
            : new RedirectToAgentDto(turn.Reply.RedirectToAgent.ShouldRedirect, turn.Reply.RedirectToAgent.Reason));
    session.Messages.Add(assistantMessage);

    var timestamp = DateTimeOffset.UtcNow.ToString("O");

    sessions.RecordThinking(session, new ThinkingEntryDto(
        assistantMessage.Id,
        turn.Reply.Thinking,
        turn.Reply.UserMood,
        turn.Reply.Debug.ContextUsed,
        turn.Reply.MatchedCategories,
        timestamp));

    if (turn.Reply.Debug.ContextUsed && turn.Rag.Sources.Count > 0)
    {
        sessions.RecordRagSources(session, new RagHistoryEntryDto(request.Message, timestamp, turn.Rag.Sources));
    }

    var response = new ChatResponse(
        SessionId: session.Id,
        Id: assistantMessage.Id,
        Response: turn.Reply.Response,
        UserMood: turn.Reply.UserMood,
        SuggestedQuestions: turn.Reply.SuggestedQuestions,
        MatchedCategories: turn.Reply.MatchedCategories,
        RedirectToAgent: turn.Reply.RedirectToAgent is null
            ? null
            : new RedirectToAgentDto(turn.Reply.RedirectToAgent.ShouldRedirect, turn.Reply.RedirectToAgent.Reason),
        Debug: new DebugInfoDto(turn.Reply.Debug.ContextUsed),
        Messages: session.Messages,
        ThinkingHistory: session.ThinkingHistory,
        RagHistory: session.RagHistory,
        LastModel: session.LastModel,
        LastKnowledgeBaseId: session.LastKnowledgeBaseId);

    return Results.Ok(response);
});

// Fetched by the SSR frontend on every page render (initial load, refresh, after a Server Action)
// so the page always has the full conversation to render, not just right after sending a message.
app.MapGet("/api/session", (string? sessionId, ConversationSessionStore sessions) =>
{
    var session = sessions.GetOrCreate(sessionId);
    return Results.Ok(new SessionStateResponse(
        session.Id,
        session.Messages,
        session.ThinkingHistory,
        session.RagHistory,
        session.LastModel,
        session.LastKnowledgeBaseId));
});

app.Run();
