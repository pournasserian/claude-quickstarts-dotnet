using System.ComponentModel;
using System.Diagnostics;

namespace AutonomousCoding.Tools;

/// <summary>
/// Runs shell commands, gated by Security.ValidateBashCommand (ported from security.py). In the
/// original Python version, this validation ran as a Claude Code PreToolUse hook; here the tool
/// validates inline before executing, since we own the tool implementation directly.
///
/// Commands run through Git Bash ("bash" on PATH) rather than cmd.exe, since the allowlist
/// (ls, cat, head, tail, pkill, ...) assumes a POSIX shell, matching the original.
/// </summary>
public sealed class BashTool
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(5);

    private readonly string _projectDir;

    public BashTool(string projectDir)
    {
        _projectDir = Path.GetFullPath(projectDir);
    }

    [Description("Run a shell command in the project directory. Only an allowlisted set of commands is permitted: " +
                 "ls, cat, head, tail, wc, grep, cp, mkdir, chmod (+x only), pwd, npm, node, git, ps, lsof, sleep, " +
                 "pkill (dev processes only), ./init.sh. Anything else is blocked.")]
    public async Task<string> Bash([Description("The shell command to run.")] string command)
    {
        var (allowed, reason) = Security.ValidateBashCommand(command);
        if (!allowed)
        {
            return $"[BLOCKED] {reason}";
        }

        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            WorkingDirectory = _projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-lc");
        psi.ArgumentList.Add(command);

        try
        {
            using var process = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start bash process.");

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var waitTask = process.WaitForExitAsync();
            var delayTask = Task.Delay(CommandTimeout);

            var completed = await Task.WhenAny(waitTask, delayTask);
            if (completed == delayTask)
            {
                TryKill(process);
                return $"[Error] Command timed out after {CommandTimeout.TotalMinutes:0} minutes and was killed.";
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var output = (stdout + stderr).Trim();

            return process.ExitCode == 0
                ? (output.Length == 0 ? "(no output)" : output)
                : $"[Error] Exit code {process.ExitCode}\n{output}";
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            return $"[Error] Failed to run command: {ex.Message}";
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best effort - the process may have exited between the timeout check and this call.
        }
    }
}
