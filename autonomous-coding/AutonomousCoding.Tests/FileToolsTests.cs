using AutonomousCoding.Tools;
using Xunit;

namespace AutonomousCoding.Tests;

/// <summary>
/// FileTools' project-directory restriction is new code (the original relied on Claude Code's own
/// Read(./**)/Write(./**) permission settings instead), so it doesn't have an original test suite
/// to port - these are a basic sanity check on the replacement.
/// </summary>
public class FileToolsTests : IDisposable
{
    private readonly string _projectDir;
    private readonly FileTools _tools;

    public FileToolsTests()
    {
        _projectDir = Path.Combine(Path.GetTempPath(), "autonomous_coding_tests_" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_projectDir);
        _tools = new FileTools(_projectDir);
    }

    public void Dispose() => Directory.Delete(_projectDir, recursive: true);

    [Fact]
    public void Write_ThenRead_RoundTrips()
    {
        _tools.Write("notes.txt", "hello world");
        Assert.Equal("hello world", _tools.Read("notes.txt"));
    }

    [Fact]
    public void Write_CreatesNestedDirectories()
    {
        _tools.Write("src/app/index.ts", "export {}");
        Assert.Equal("export {}", _tools.Read("src/app/index.ts"));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("../../etc/passwd")]
    [InlineData("subdir/../../escape.txt")]
    public void Read_BlocksPathTraversalOutsideProjectDir(string maliciousPath)
    {
        Assert.Throws<InvalidOperationException>(() => _tools.Read(maliciousPath));
    }

    [Fact]
    public void Read_BlocksAbsolutePathOutsideProjectDir()
    {
        var outsideFile = Path.Combine(Path.GetTempPath(), "outside_" + Guid.NewGuid().ToString("n") + ".txt");
        File.WriteAllText(outsideFile, "secret");
        try
        {
            Assert.Throws<InvalidOperationException>(() => _tools.Read(outsideFile));
        }
        finally
        {
            File.Delete(outsideFile);
        }
    }

    [Fact]
    public void Edit_ReplacesExactMatch()
    {
        _tools.Write("file.txt", "before-value-after");
        _tools.Edit("file.txt", "value", "replacement");
        Assert.Equal("before-replacement-after", _tools.Read("file.txt"));
    }

    [Fact]
    public void Glob_FindsMatchingFiles()
    {
        _tools.Write("a.ts", "");
        _tools.Write("b.ts", "");
        _tools.Write("c.js", "");

        var result = _tools.Glob("*.ts");

        Assert.Contains("a.ts", result);
        Assert.Contains("b.ts", result);
        Assert.DoesNotContain("c.js", result);
    }
}
