using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace AutonomousCoding.Tools;

/// <summary>
/// File tools exposed to the agent, all restricted to the project directory. In the original
/// Python version, this restriction came from Claude Code's own permission settings
/// (Read(./**), Write(./**), etc.); here each tool re-validates the resolved path itself, since
/// we own the tool implementations directly instead of delegating to Claude Code's runtime.
/// </summary>
public sealed class FileTools
{
    private const int MaxGrepMatches = 200;
    private readonly string _projectDir;

    public FileTools(string projectDir)
    {
        _projectDir = Path.GetFullPath(projectDir);
    }

    private string ResolveWithinProject(string path)
    {
        var combined = Path.IsPathRooted(path) ? path : Path.Combine(_projectDir, path);
        var full = Path.GetFullPath(combined);

        var projectWithSeparator = _projectDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.Equals(_projectDir, StringComparison.OrdinalIgnoreCase) &&
            !full.StartsWith(projectWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Access denied: '{path}' resolves outside the project directory.");
        }

        return full;
    }

    [Description("Read the contents of a file within the project directory.")]
    public string Read([Description("Path to the file, relative to the project directory.")] string path)
    {
        var full = ResolveWithinProject(path);
        return File.Exists(full) ? File.ReadAllText(full) : $"Error: file not found: {path}";
    }

    [Description("Write content to a file within the project directory, creating it (and parent directories) or overwriting it.")]
    public string Write(
        [Description("Path to the file, relative to the project directory.")] string path,
        [Description("The full content to write.")] string content)
    {
        var full = ResolveWithinProject(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return $"Wrote {content.Length} characters to {path}";
    }

    [Description("Replace one exact string match with another in a file within the project directory.")]
    public string Edit(
        [Description("Path to the file, relative to the project directory.")] string path,
        [Description("The exact text to find (must match exactly once).")] string oldString,
        [Description("The text to replace it with.")] string newString)
    {
        var full = ResolveWithinProject(path);
        if (!File.Exists(full))
        {
            return $"Error: file not found: {path}";
        }

        var text = File.ReadAllText(full);
        if (!text.Contains(oldString, StringComparison.Ordinal))
        {
            return $"Error: old_string not found in {path}";
        }

        File.WriteAllText(full, ReplaceFirst(text, oldString, newString));
        return $"Edited {path}";
    }

    private static string ReplaceFirst(string text, string search, string replace)
    {
        var index = text.IndexOf(search, StringComparison.Ordinal);
        return index < 0 ? text : string.Concat(text.AsSpan(0, index), replace, text.AsSpan(index + search.Length));
    }

    [Description("List files within the project directory matching a glob pattern, e.g. **/*.ts")]
    public string Glob([Description("Glob pattern, relative to the project directory.")] string pattern)
    {
        var matcher = new Matcher();
        matcher.AddInclude(pattern);
        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(_projectDir)));
        var files = result.Files.Select(f => f.Path).ToList();
        return files.Count == 0 ? "No files matched." : string.Join("\n", files);
    }

    [Description("Search for a regular expression across text files within the project directory.")]
    public string Grep(
        [Description("Regular expression to search for.")] string pattern,
        [Description("Optional glob to restrict which files are searched, e.g. **/*.ts")] string? glob = null)
    {
        Regex regex;
        try
        {
            regex = new Regex(pattern);
        }
        catch (ArgumentException ex)
        {
            return $"Error: invalid regex: {ex.Message}";
        }

        IEnumerable<string> files;
        if (!string.IsNullOrWhiteSpace(glob))
        {
            var matcher = new Matcher();
            matcher.AddInclude(glob);
            files = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(_projectDir)))
                .Files.Select(f => Path.Combine(_projectDir, f.Path));
        }
        else
        {
            files = Directory.EnumerateFiles(_projectDir, "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}")
                            && !f.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}"));
        }

        var matches = new List<string>();
        foreach (var file in files)
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (IOException)
            {
                continue;
            }

            for (var i = 0; i < lines.Length && matches.Count < MaxGrepMatches; i++)
            {
                if (regex.IsMatch(lines[i]))
                {
                    var relative = Path.GetRelativePath(_projectDir, file);
                    matches.Add($"{relative}:{i + 1}:{lines[i]}");
                }
            }

            if (matches.Count >= MaxGrepMatches) break;
        }

        return matches.Count == 0 ? "No matches found." : string.Join("\n", matches);
    }
}
