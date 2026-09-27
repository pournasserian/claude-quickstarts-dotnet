using System.Text.Json;
using Anthropic;
using Microsoft.Agents.AI;
using CustomerSupportAgent.Api.Models;

namespace CustomerSupportAgent.Api.Services;

public sealed class ClaudeTurnResult
{
    public required ClaudeStructuredReply Reply { get; init; }
    public required RagRetrievalResult Rag { get; init; }
}

/// <summary>
/// Ports app/api/chat/route.ts: builds the same system prompt (categories + RAG context +
/// JSON-only response contract), calls Claude, and parses/validates the JSON reply the same
/// defensive way the original route does.
/// </summary>
public sealed class ClaudeChatService
{
    private readonly string _apiKey;
    private readonly ISupportCategoriesProvider _categories;
    private readonly IKnowledgeBaseRetriever _retriever;
    private readonly ILogger<ClaudeChatService> _logger;

    // One agent+session per ConversationSession, rebuilt only if the selected model changes.
    private readonly Dictionary<string, (string Model, AIAgent Agent, AgentSession Session)> _agentsBySession = new();

    public ClaudeChatService(
        IConfiguration configuration,
        ISupportCategoriesProvider categories,
        IKnowledgeBaseRetriever retriever,
        ILogger<ClaudeChatService> logger)
    {
        _apiKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured. Set it via user-secrets or the ANTHROPIC__APIKEY environment variable.");
        _categories = categories;
        _retriever = retriever;
        _logger = logger;
    }

    public async Task<ClaudeTurnResult> GetReplyAsync(
        ConversationSession session,
        string userMessage,
        string model,
        string? knowledgeBaseId,
        CancellationToken cancellationToken = default)
    {
        var rag = await _retriever.RetrieveAsync(userMessage, knowledgeBaseId, cancellationToken);

        var (agent, agentSession) = await GetOrCreateAgentAsync(session, model);

        var promptedMessage = BuildUserTurn(userMessage, rag);

        string responseText;
        try
        {
            var response = await agent.RunAsync(promptedMessage, agentSession, cancellationToken: cancellationToken);
            responseText = response.Text ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude generation failed");
            return new ClaudeTurnResult
            {
                Reply = FallbackReply(),
                Rag = rag
            };
        }

        var reply = ParseReply(responseText);
        return new ClaudeTurnResult { Reply = reply, Rag = rag };
    }

    private async Task<(AIAgent Agent, AgentSession Session)> GetOrCreateAgentAsync(ConversationSession session, string model)
    {
        if (_agentsBySession.TryGetValue(session.Id, out var cached) && cached.Model == model)
        {
            return (cached.Agent, cached.Session);
        }

        AnthropicClient client = new() { ApiKey = _apiKey };
        AIAgent agent = client.AsAIAgent(
            model: model,
            name: "CustomerSupportAgent",
            instructions: BuildSystemInstructions());

        AgentSession agentSession = await agent.CreateSessionAsync();
        _agentsBySession[session.Id] = (model, agent, agentSession);
        return (agent, agentSession);
    }

    private string BuildSystemInstructions()
    {
        var categoryIds = string.Join(", ", _categories.Categories.Select(c => c.Id));

        return $$"""
            You are acting as an Anthropic customer support assistant chatbot inside a chat window on a website. You are chatting with a human user who is asking for help about Anthropic's products and services. When responding to the user, aim to provide concise and helpful responses while maintaining a polite and professional tone.

            Each user turn may include a block of retrieved knowledge-base context above the actual question. Use it when relevant; if it says no information was found, or the information isn't relevant, you can redirect the user to a human agent for further assistance. Only use information you've been given - do not invent details about Anthropic's products.

            To help with our internal classification of inquiries, categorize inquiries in addition to answering them. We have provided {{_categories.Categories.Count}} customer support categories.
            Check if your response fits into any category and include the category IDs in your "matched_categories" array.
            The available categories are: {{categoryIds}}
            If multiple categories match, include multiple category IDs. If no categories match, return an empty array.

            If the question is unrelated to Anthropic's products and services, you should redirect the user to a human agent.

            You are the first point of contact for the user and should try to resolve their issue or provide relevant information. If you are unable to help the user or if the user explicitly asks to talk to a human, you can redirect them to a human agent for further assistance.

            To display your responses correctly, you must format your ENTIRE reply as a single valid JSON object, with no markdown code fences and no text before or after it, with exactly this structure:
            {
                "thinking": "Brief explanation of your reasoning for how you should address the user's query",
                "response": "Your concise response to the user",
                "user_mood": "positive|neutral|negative|curious|frustrated|confused",
                "suggested_questions": ["Question 1?", "Question 2?", "Question 3?"],
                "debug": {
                  "context_used": true|false
                },
                "matched_categories": ["category_id1", "category_id2"],
                "redirect_to_agent": {
                  "should_redirect": boolean,
                  "reason": "Reason for redirection (optional, include only if should_redirect is true)"
                }
            }

            Example of a response without redirection to a human agent:
            {
              "thinking": "Providing relevant information from the knowledge base",
              "response": "Here's the information you requested...",
              "user_mood": "curious",
              "suggested_questions": ["How do I update my account?", "What are the payment options?"],
              "debug": { "context_used": true },
              "matched_categories": ["account", "billing"],
              "redirect_to_agent": { "should_redirect": false }
            }

            Example of a response with redirection to a human agent:
            {
              "thinking": "User request requires human intervention",
              "response": "I understand this is a complex issue. Let me connect you with a human agent who can assist you better.",
              "user_mood": "frustrated",
              "suggested_questions": [],
              "debug": { "context_used": false },
              "matched_categories": ["technical"],
              "redirect_to_agent": { "should_redirect": true, "reason": "Complex technical issue requiring human expertise" }
            }
            """;
    }

    private static string BuildUserTurn(string userMessage, RagRetrievalResult rag)
    {
        var context = rag.IsRagWorking && !string.IsNullOrWhiteSpace(rag.Context)
            ? rag.Context
            : "No information found for this query.";

        return $"""
            Retrieved knowledge-base context (may or may not be relevant):
            {context}

            User question: {userMessage}
            """;
    }

    private ClaudeStructuredReply ParseReply(string responseText)
    {
        var jsonSlice = ExtractJsonObject(responseText);

        try
        {
            var parsed = JsonSerializer.Deserialize<ClaudeStructuredReply>(jsonSlice, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Response))
            {
                return parsed;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Claude's JSON reply: {Raw}", responseText);
        }

        return FallbackReply();
    }

    /// <summary>Mirrors the original route's defensive parsing: Claude is asked for JSON-only output,
    /// but we still defensively pull out the first {...} block in case of stray text around it.</summary>
    private static string ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }

    private static ClaudeStructuredReply FallbackReply() => new()
    {
        Response = "Sorry, there was an issue processing your request. Please try again later.",
        Thinking = "Error occurred during message generation.",
        UserMood = "neutral",
        Debug = new ClaudeDebugDto { ContextUsed = false }
    };
}
