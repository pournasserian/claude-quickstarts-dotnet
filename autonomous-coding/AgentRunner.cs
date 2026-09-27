using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AutonomousCoding;

/// <summary>Session and main-loop logic, ported from agent.py.</summary>
public static class AgentRunner
{
    private const int AutoContinueDelaySeconds = 3;

    public static async Task<(string Status, string ResponseText)> RunAgentSessionAsync(
        AIAgent agent,
        AgentSession session,
        string message,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Sending prompt to Claude...\n");

        var responseText = new StringBuilder();

        try
        {
            await foreach (var update in agent.RunStreamingAsync(message, session, cancellationToken: cancellationToken))
            {
                foreach (var content in update.Contents)
                {
                    switch (content)
                    {
                        case TextContent text:
                            responseText.Append(text.Text);
                            Console.Write(text.Text);
                            break;

                        case FunctionCallContent call:
                            Console.WriteLine();
                            Console.WriteLine($"[Tool: {call.Name}]");
                            var argsPreview = call.Arguments is null
                                ? ""
                                : string.Join(", ", call.Arguments.Select(a => $"{a.Key}={a.Value}"));
                            Console.WriteLine(argsPreview.Length > 200
                                ? $"   Input: {argsPreview[..200]}..."
                                : $"   Input: {argsPreview}");
                            break;

                        case FunctionResultContent result:
                            var resultText = result.Result?.ToString() ?? "";
                            if (resultText.Contains("[BLOCKED]"))
                            {
                                Console.WriteLine($"   {resultText}");
                            }
                            else if (result.Exception is not null)
                            {
                                var truncated = resultText.Length > 500 ? resultText[..500] : resultText;
                                Console.WriteLine($"   [Error] {truncated}");
                            }
                            else
                            {
                                Console.WriteLine("   [Done]");
                            }
                            break;
                    }
                }
            }

            Console.WriteLine("\n" + new string('-', 70) + "\n");
            return ("continue", responseText.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during agent session: {ex.Message}");
            return ("error", ex.Message);
        }
    }

    public static async Task RunAutonomousAgentAsync(string projectDir, string model, int? maxIterations)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("  AUTONOMOUS CODING AGENT DEMO");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine();
        Console.WriteLine($"Project directory: {projectDir}");
        Console.WriteLine($"Model: {model}");
        Console.WriteLine(maxIterations is { } mi ? $"Max iterations: {mi}" : "Max iterations: Unlimited (will run until completion)");
        Console.WriteLine();

        Directory.CreateDirectory(projectDir);

        var testsFile = Path.Combine(projectDir, "feature_list.json");
        var isFirstRun = !File.Exists(testsFile);

        if (isFirstRun)
        {
            Console.WriteLine("Fresh start - will use initializer agent");
            Console.WriteLine();
            Console.WriteLine(new string('=', 70));
            Console.WriteLine("  NOTE: First session takes 10-20+ minutes!");
            Console.WriteLine("  The agent is generating 200 detailed test cases.");
            Console.WriteLine("  This may appear to hang - it's working. Watch for [Tool: ...] output.");
            Console.WriteLine(new string('=', 70));
            Console.WriteLine();
            Prompts.CopySpecToProject(projectDir);
        }
        else
        {
            Console.WriteLine("Continuing existing project");
            Progress.PrintProgressSummary(projectDir);
        }

        var iteration = 0;

        while (true)
        {
            iteration++;

            if (maxIterations is { } max && iteration > max)
            {
                Console.WriteLine($"\nReached max iterations ({max})");
                Console.WriteLine("To continue, run the script again without --max-iterations");
                break;
            }

            Progress.PrintSessionHeader(iteration, isFirstRun);

            var resources = await ClaudeAgentFactory.CreateAsync(projectDir, model);
            var session = await resources.Agent.CreateSessionAsync();

            var prompt = isFirstRun ? Prompts.GetInitializerPrompt() : Prompts.GetCodingPrompt();
            isFirstRun = false;

            var (status, _) = await RunAgentSessionAsync(resources.Agent, session, prompt, CancellationToken.None);

            if (resources.McpClient is not null)
            {
                await resources.McpClient.DisposeAsync();
            }

            if (status == "continue")
            {
                Console.WriteLine($"\nAgent will auto-continue in {AutoContinueDelaySeconds}s...");
                Progress.PrintProgressSummary(projectDir);
                await Task.Delay(TimeSpan.FromSeconds(AutoContinueDelaySeconds));
            }
            else if (status == "error")
            {
                Console.WriteLine("\nSession encountered an error");
                Console.WriteLine("Will retry with a fresh session...");
                await Task.Delay(TimeSpan.FromSeconds(AutoContinueDelaySeconds));
            }

            if (maxIterations is null || iteration < maxIterations)
            {
                Console.WriteLine("\nPreparing next session...\n");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("  SESSION COMPLETE");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"\nProject directory: {projectDir}");
        Progress.PrintProgressSummary(projectDir);

        Console.WriteLine("\n" + new string('-', 70));
        Console.WriteLine("  TO RUN THE GENERATED APPLICATION:");
        Console.WriteLine(new string('-', 70));
        Console.WriteLine($"\n  cd {Path.GetFullPath(projectDir)}");
        Console.WriteLine("  ./init.sh           # Run the setup script");
        Console.WriteLine("  # Or manually:");
        Console.WriteLine("  npm install && npm run dev");
        Console.WriteLine("\n  Then open http://localhost:3000 (or check init.sh for the URL)");
        Console.WriteLine(new string('-', 70));

        Console.WriteLine("\nDone!");
    }
}
