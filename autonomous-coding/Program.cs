// Autonomous Coding Agent Demo - .NET port
// Ported from autonomous_agent_demo.py. See ../autonomous-coding/README.md for the original.

using AutonomousCoding;

const string DefaultModel = "claude-sonnet-4-5-20250929";

string projectDirArg = "./autonomous_demo_project";
int? maxIterations = null;
string model = DefaultModel;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--project-dir":
            projectDirArg = RequireValue(args, ref i, "--project-dir");
            break;
        case "--max-iterations":
            maxIterations = int.Parse(RequireValue(args, ref i, "--max-iterations"));
            break;
        case "--model":
            model = RequireValue(args, ref i, "--model");
            break;
        case "-h" or "--help":
            PrintHelp();
            return;
        default:
            Console.WriteLine($"Unknown argument: {args[i]}");
            PrintHelp();
            return;
    }
}

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
{
    Console.WriteLine("Error: ANTHROPIC_API_KEY environment variable not set");
    Console.WriteLine("\nGet your API key from: https://console.anthropic.com/");
    Console.WriteLine("\nThen set it:");
    Console.WriteLine("  export ANTHROPIC_API_KEY='your-api-key-here'   (bash)");
    Console.WriteLine("  $env:ANTHROPIC_API_KEY = 'your-api-key-here'   (PowerShell)");
    return;
}

// Automatically place projects under generations/, unless already specified - matches the
// original's behavior in autonomous_agent_demo.py's main().
var projectDir = projectDirArg;
var normalized = projectDir.Replace('\\', '/');
if (!normalized.StartsWith("generations/") && !Path.IsPathRooted(projectDir))
{
    projectDir = Path.Combine("generations", projectDir);
}

try
{
    await AgentRunner.RunAutonomousAgentAsync(projectDir, model, maxIterations);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n\nInterrupted");
    Console.WriteLine("To resume, run the same command again");
}
catch (Exception ex)
{
    Console.WriteLine($"\nFatal error: {ex.Message}");
    throw;
}

static string RequireValue(string[] args, ref int i, string flag)
{
    if (i + 1 >= args.Length)
    {
        throw new ArgumentException($"{flag} requires a value");
    }
    return args[++i];
}

static void PrintHelp()
{
    Console.WriteLine("""
        Autonomous Coding Agent Demo - Long-running agent harness

        Usage:
          dotnet run -- --project-dir <dir> [--max-iterations <n>] [--model <model>]

        Options:
          --project-dir <dir>       Directory for the project (default: ./autonomous_demo_project).
                                    Relative paths are automatically placed under generations/.
          --max-iterations <n>      Maximum number of agent iterations (default: unlimited)
          --model <model>           Claude model to use (default: claude-sonnet-4-5-20250929)

        Examples:
          dotnet run -- --project-dir ./claude_clone
          dotnet run -- --project-dir ./claude_clone --max-iterations 5

        Environment Variables:
          ANTHROPIC_API_KEY    Your Anthropic API key (required)
        """);
}
