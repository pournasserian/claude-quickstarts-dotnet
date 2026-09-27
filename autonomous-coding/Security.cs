using System.Text.RegularExpressions;

namespace AutonomousCoding;

/// <summary>
/// Bash command security validation, ported from security.py.
/// Uses an allowlist approach - only explicitly permitted commands can run.
/// This is a security boundary: keep the parsing logic and test coverage in sync with the
/// original Python version (see test_security.py / SecurityTests.cs) if you change it.
/// </summary>
public static class Security
{
    public static readonly HashSet<string> AllowedCommands = new(StringComparer.Ordinal)
    {
        // File inspection
        "ls", "cat", "head", "tail", "wc", "grep",
        // File operations (agent uses SDK tools for most file ops, but cp/mkdir needed occasionally)
        "cp", "mkdir", "chmod", // For making scripts executable; validated separately
        // Directory
        "pwd",
        // Node.js development
        "npm", "node",
        // Version control
        "git",
        // Process management
        "ps", "lsof", "sleep", "pkill", // For killing dev servers; validated separately
        // Script execution
        "init.sh", // Init scripts; validated separately
    };

    private static readonly HashSet<string> CommandsNeedingExtraValidation = new(StringComparer.Ordinal)
    {
        "pkill", "chmod", "init.sh",
    };

    private static readonly HashSet<string> ShellKeywords = new(StringComparer.Ordinal)
    {
        "if", "then", "else", "elif", "fi", "for", "while", "until", "do", "done",
        "case", "esac", "in", "!", "{", "}",
    };

    private static readonly HashSet<string> AllowedPkillProcessNames = new(StringComparer.Ordinal)
    {
        "node", "npm", "npx", "vite", "next",
    };

    /// <summary>
    /// Splits a compound command into individual segments, handling && || and ; (not pipes -
    /// those are a single command). Mirrors split_command_segments in security.py.
    /// </summary>
    public static List<string> SplitCommandSegments(string commandString)
    {
        var segments = Regex.Split(commandString, @"\s*(?:&&|\|\|)\s*");

        var result = new List<string>();
        foreach (var segment in segments)
        {
            var subSegments = Regex.Split(segment, "(?<![\"'])\\s*;\\s*(?![\"'])");
            foreach (var sub in subSegments)
            {
                var trimmed = sub.Trim();
                if (trimmed.Length > 0)
                {
                    result.Add(trimmed);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts base command names from a shell command string, handling pipes, &&/||/;, and
    /// variable assignments. Mirrors extract_commands in security.py. Returns an empty list if
    /// the command couldn't be parsed (fail-safe: the caller should then block).
    /// </summary>
    public static List<string> ExtractCommands(string commandString)
    {
        var commands = new List<string>();
        var segments = Regex.Split(commandString, "(?<![\"'])\\s*;\\s*(?![\"'])");

        foreach (var rawSegment in segments)
        {
            var segment = rawSegment.Trim();
            if (segment.Length == 0) continue;

            string[] tokens;
            try
            {
                tokens = ShellTokenizer.Split(segment);
            }
            catch (FormatException)
            {
                // Malformed command (unclosed quotes, etc.) - fail-safe by blocking.
                return new List<string>();
            }

            if (tokens.Length == 0) continue;

            var expectCommand = true;
            foreach (var token in tokens)
            {
                if (token is "|" or "||" or "&&" or "&")
                {
                    expectCommand = true;
                    continue;
                }

                if (ShellKeywords.Contains(token))
                {
                    continue;
                }

                if (token.StartsWith('-'))
                {
                    continue;
                }

                if (token.Contains('=') && !token.StartsWith('='))
                {
                    continue;
                }

                if (expectCommand)
                {
                    var cmd = Path.GetFileName(token);
                    commands.Add(cmd);
                    expectCommand = false;
                }
            }
        }

        return commands;
    }

    /// <summary>Validates pkill commands - only allow killing dev-related processes.</summary>
    public static (bool Allowed, string Reason) ValidatePkillCommand(string commandString)
    {
        string[] tokens;
        try
        {
            tokens = ShellTokenizer.Split(commandString);
        }
        catch (FormatException)
        {
            return (false, "Could not parse pkill command");
        }

        if (tokens.Length == 0)
        {
            return (false, "Empty pkill command");
        }

        var args = tokens.Skip(1).Where(t => !t.StartsWith('-')).ToList();
        if (args.Count == 0)
        {
            return (false, "pkill requires a process name");
        }

        var target = args[^1];
        // For -f (full command line match), extract the first word as the process name,
        // e.g. "pkill -f 'node server.js'" -> target is "node server.js", process is "node".
        if (target.Contains(' '))
        {
            target = target.Split(' ')[0];
        }

        return AllowedPkillProcessNames.Contains(target)
            ? (true, "")
            : (false, $"pkill only allowed for dev processes: {string.Join(", ", AllowedPkillProcessNames)}");
    }

    /// <summary>Validates chmod commands - only allow making files executable with +x.</summary>
    public static (bool Allowed, string Reason) ValidateChmodCommand(string commandString)
    {
        string[] tokens;
        try
        {
            tokens = ShellTokenizer.Split(commandString);
        }
        catch (FormatException)
        {
            return (false, "Could not parse chmod command");
        }

        if (tokens.Length == 0 || tokens[0] != "chmod")
        {
            return (false, "Not a chmod command");
        }

        string? mode = null;
        var files = new List<string>();

        foreach (var token in tokens.Skip(1))
        {
            if (token.StartsWith('-'))
            {
                return (false, "chmod flags are not allowed");
            }

            if (mode is null)
            {
                mode = token;
            }
            else
            {
                files.Add(token);
            }
        }

        if (mode is null)
        {
            return (false, "chmod requires a mode");
        }

        if (files.Count == 0)
        {
            return (false, "chmod requires at least one file");
        }

        // Only allow +x variants (making files executable): +x, u+x, g+x, o+x, a+x, ug+x, etc.
        if (!Regex.IsMatch(mode, "^[ugoa]*\\+x$"))
        {
            return (false, $"chmod only allowed with +x mode, got: {mode}");
        }

        return (true, "");
    }

    /// <summary>Validates init.sh script execution - only allow ./init.sh.</summary>
    public static (bool Allowed, string Reason) ValidateInitScript(string commandString)
    {
        string[] tokens;
        try
        {
            tokens = ShellTokenizer.Split(commandString);
        }
        catch (FormatException)
        {
            return (false, "Could not parse init script command");
        }

        if (tokens.Length == 0)
        {
            return (false, "Empty command");
        }

        var script = tokens[0];
        if (script == "./init.sh" || script.EndsWith("/init.sh"))
        {
            return (true, "");
        }

        return (false, $"Only ./init.sh is allowed, got: {script}");
    }

    private static string GetCommandForValidation(string cmd, List<string> segments)
    {
        foreach (var segment in segments)
        {
            if (ExtractCommands(segment).Contains(cmd))
            {
                return segment;
            }
        }

        return "";
    }

    /// <summary>
    /// Validates a bash command against the allowlist. Returns (allowed, reason). This is the
    /// direct-call equivalent of bash_security_hook in security.py (there's no PreToolUse hook
    /// mechanism here - the Bash tool calls this itself before executing).
    /// </summary>
    public static (bool Allowed, string Reason) ValidateBashCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return (true, "");
        }

        var commands = ExtractCommands(command);
        if (commands.Count == 0)
        {
            // Could not parse - fail safe by blocking.
            return (false, $"Could not parse command for security validation: {command}");
        }

        var segments = SplitCommandSegments(command);

        foreach (var cmd in commands)
        {
            if (!AllowedCommands.Contains(cmd))
            {
                return (false, $"Command '{cmd}' is not in the allowed commands list");
            }

            if (CommandsNeedingExtraValidation.Contains(cmd))
            {
                var cmdSegment = GetCommandForValidation(cmd, segments);
                if (string.IsNullOrEmpty(cmdSegment))
                {
                    cmdSegment = command;
                }

                var (allowed, reason) = cmd switch
                {
                    "pkill" => ValidatePkillCommand(cmdSegment),
                    "chmod" => ValidateChmodCommand(cmdSegment),
                    "init.sh" => ValidateInitScript(cmdSegment),
                    _ => (true, ""),
                };

                if (!allowed)
                {
                    return (false, reason);
                }
            }
        }

        return (true, "");
    }
}
