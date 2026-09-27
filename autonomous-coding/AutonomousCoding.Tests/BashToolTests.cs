using AutonomousCoding.Tools;
using Xunit;

namespace AutonomousCoding.Tests;

/// <summary>Exercises BashTool end-to-end (through a real bash process) rather than just the
/// allowlist logic in isolation.</summary>
public class BashToolTests : IDisposable
{
    private readonly string _projectDir;
    private readonly BashTool _tool;

    public BashToolTests()
    {
        _projectDir = Path.Combine(Path.GetTempPath(), "autonomous_coding_bash_tests_" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_projectDir);
        _tool = new BashTool(_projectDir);
    }

    public void Dispose() => Directory.Delete(_projectDir, recursive: true);

    [Fact]
    public async Task Bash_RunsAllowedCommand_AndReturnsOutput()
    {
        File.WriteAllText(Path.Combine(_projectDir, "marker.txt"), "");
        var result = await _tool.Bash("ls");
        Assert.Contains("marker.txt", result);
    }

    [Fact]
    public async Task Bash_BlocksDisallowedCommand_WithoutExecutingIt()
    {
        // echo is deliberately not allowlisted (matches the original's test_security.py).
        var result = await _tool.Bash("echo hello-from-bash-tool");

        Assert.StartsWith("[BLOCKED]", result);
        Assert.DoesNotContain("hello-from-bash-tool", result);
    }

    [Fact]
    public async Task Bash_RunsInProjectDirectory()
    {
        var result = await _tool.Bash("pwd");
        Assert.Contains(Path.GetFileName(_projectDir), result.Replace('\\', '/'));
    }
}
