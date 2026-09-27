namespace CustomerSupportAgent.Api.Models;

public sealed record ModelOption(string Id, string Name);

public sealed record KnowledgeBaseOption(string Id, string Name);

public sealed record ConfigResponse(
    IReadOnlyList<ModelOption> Models,
    IReadOnlyList<KnowledgeBaseOption> KnowledgeBases);

public sealed record SupportCategory(string Id, string Name, IReadOnlyList<string> Keywords);

public sealed record SupportCategoriesFile(IReadOnlyList<SupportCategory> Categories);
