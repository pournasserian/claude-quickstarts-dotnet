using Anthropic;
using AutonomousCoding.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Client;

namespace AutonomousCoding;

public sealed record AgentResources(AIAgent Agent, McpClient? McpClient);

/// <summary>
/// Builds the agent and its tools, ported from client.py. Since there's no C# equivalent of the
/// Claude Agent SDK's managed runtime, this rebuilds the tool-calling loop directly on Agent
/// Framework's direct-model-inference path: function tools for file/bash access (see Tools/),
/// gated by the same allowlist as the original (Security.cs), plus the same Puppeteer MCP server
/// for browser-based verification. What's NOT reproduced: Claude Code's OS-level sandbox - only
/// the allowlist and project-directory restriction layers carry over.
/// </summary>
public static class ClaudeAgentFactory
{
    private const string SystemPromptText =
        "You are an expert full-stack developer building a production-quality web application.";

    public static async Task<AgentResources> CreateAsync(string projectDir, string model)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException(
                "ANTHROPIC_API_KEY environment variable not set.\nGet your API key from: https://console.anthropic.com/");

        var fileTools = new FileTools(projectDir);
        var bashTool = new BashTool(projectDir);

        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(fileTools.Read),
            AIFunctionFactory.Create(fileTools.Write),
            AIFunctionFactory.Create(fileTools.Edit),
            AIFunctionFactory.Create(fileTools.Glob),
            AIFunctionFactory.Create(fileTools.Grep),
            AIFunctionFactory.Create(bashTool.Bash),
        };

        McpClient? mcpClient = null;
        try
        {
            mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "puppeteer",
                Command = "npx",
                Arguments = ["-y", "puppeteer-mcp-server"],
            }));

            var mcpTools = await mcpClient.ListToolsAsync();
            tools.AddRange(mcpTools.Cast<AITool>());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: could not start the Puppeteer MCP server ({ex.Message}).");
            Console.WriteLine("Browser automation tools won't be available this session.");
        }

        AnthropicClient client = new() { ApiKey = apiKey };
        var agent = client.AsAIAgent(
            model: model,
            name: "AutonomousCodingAgent",
            instructions: SystemPromptText,
            tools: tools);

        return new AgentResources(agent, mcpClient);
    }
}
