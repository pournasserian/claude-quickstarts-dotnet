using Amazon;
using Amazon.BedrockAgentRuntime;
using Amazon.BedrockAgentRuntime.Model;
using CustomerSupportAgent.Api.Models;

namespace CustomerSupportAgent.Api.Services;

public sealed class RagRetrievalResult
{
    public string Context { get; init; } = "";
    public bool IsRagWorking { get; init; }
    public IReadOnlyList<RagSourceDto> Sources { get; init; } = Array.Empty<RagSourceDto>();
}

public interface IKnowledgeBaseRetriever
{
    Task<RagRetrievalResult> RetrieveAsync(string query, string? knowledgeBaseId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Ports app/lib/utils.ts's retrieveContext: queries an Amazon Bedrock Knowledge Base and
/// degrades gracefully (empty context, isRagWorking=false) whenever AWS isn't configured or the
/// call fails, exactly like the original TypeScript implementation.
/// </summary>
public sealed class BedrockKnowledgeBaseRetriever : IKnowledgeBaseRetriever
{
    private const int NumberOfResults = 3;

    private readonly AmazonBedrockAgentRuntimeClient? _client;
    private readonly ILogger<BedrockKnowledgeBaseRetriever> _logger;

    public BedrockKnowledgeBaseRetriever(IConfiguration configuration, ILogger<BedrockKnowledgeBaseRetriever> logger)
    {
        _logger = logger;

        var accessKey = configuration["Aws:AccessKeyId"];
        var secretKey = configuration["Aws:SecretAccessKey"];
        var region = configuration["Aws:Region"];

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogWarning("AWS Bedrock credentials are not configured; RAG retrieval is disabled.");
            _client = null;
            return;
        }

        _client = new AmazonBedrockAgentRuntimeClient(
            accessKey,
            secretKey,
            RegionEndpoint.GetBySystemName(string.IsNullOrWhiteSpace(region) ? "us-east-1" : region));
    }

    public async Task<RagRetrievalResult> RetrieveAsync(string query, string? knowledgeBaseId, CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            return new RagRetrievalResult();
        }

        if (string.IsNullOrWhiteSpace(knowledgeBaseId))
        {
            _logger.LogWarning("knowledgeBaseId was not provided");
            return new RagRetrievalResult();
        }

        try
        {
            var request = new RetrieveRequest
            {
                KnowledgeBaseId = knowledgeBaseId,
                RetrievalQuery = new KnowledgeBaseQuery { Text = query },
                RetrievalConfiguration = new KnowledgeBaseRetrievalConfiguration
                {
                    VectorSearchConfiguration = new KnowledgeBaseVectorSearchConfiguration
                    {
                        NumberOfResults = NumberOfResults
                    }
                }
            };

            var response = await _client.RetrieveAsync(request, cancellationToken);
            var results = response.RetrievalResults ?? new List<KnowledgeBaseRetrievalResult>();
            var withContent = results.Where(r => !string.IsNullOrEmpty(r.Content?.Text)).ToList();

            var sources = withContent
                .Select((r, index) =>
                {
                    var uri = r.Location?.S3Location?.Uri ?? "";
                    var fileName = uri.Split('/').LastOrDefault();
                    fileName = string.IsNullOrEmpty(fileName) ? $"Source-{index}.txt" : fileName;

                    string? chunkId = null;
                    if (r.Metadata is not null && r.Metadata.TryGetValue("x-amz-bedrock-kb-chunk-id", out var idValue))
                    {
                        chunkId = idValue.ToString();
                    }

                    return new RagSourceDto(
                        Id: string.IsNullOrEmpty(chunkId) ? $"chunk-{index}" : chunkId,
                        FileName: fileName.Replace('_', ' ').Replace(".txt", ""),
                        Snippet: r.Content?.Text ?? "",
                        Score: r.Score ?? 0);
                })
                .Take(1)
                .ToList();

            var context = string.Join("\n\n", withContent.Select(r => r.Content!.Text));

            return new RagRetrievalResult
            {
                Context = context,
                IsRagWorking = true,
                Sources = sources
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bedrock RAG retrieval failed for query: {Query}", query);
            return new RagRetrievalResult();
        }
    }
}
