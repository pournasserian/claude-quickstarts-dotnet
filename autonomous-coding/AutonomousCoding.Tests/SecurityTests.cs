using AutonomousCoding;
using Xunit;

namespace AutonomousCoding.Tests;

/// <summary>Ported from test_security.py - same test cases, same expected outcomes.</summary>
public class SecurityTests
{
    [Theory]
    [InlineData("ls -la", new[] { "ls" })]
    [InlineData("npm install && npm run build", new[] { "npm", "npm" })]
    [InlineData("cat file.txt | grep pattern", new[] { "cat", "grep" })]
    [InlineData("/usr/bin/node script.js", new[] { "node" })]
    [InlineData("VAR=value ls", new[] { "ls" })]
    [InlineData("git status || git init", new[] { "git", "git" })]
    public void ExtractCommands_MatchesExpected(string command, string[] expected)
    {
        Assert.Equal(expected, Security.ExtractCommands(command));
    }

    [Theory]
    [InlineData("chmod +x init.sh", true)]
    [InlineData("chmod +x script.sh", true)]
    [InlineData("chmod u+x init.sh", true)]
    [InlineData("chmod a+x init.sh", true)]
    [InlineData("chmod ug+x init.sh", true)]
    [InlineData("chmod +x file1.sh file2.sh", true)]
    [InlineData("chmod 777 init.sh", false)]
    [InlineData("chmod 755 init.sh", false)]
    [InlineData("chmod +w init.sh", false)]
    [InlineData("chmod +r init.sh", false)]
    [InlineData("chmod -x init.sh", false)]
    [InlineData("chmod -R +x dir/", false)]
    [InlineData("chmod --recursive +x dir/", false)]
    [InlineData("chmod +x", false)]
    public void ValidateChmodCommand_MatchesExpected(string command, bool shouldAllow)
    {
        var (allowed, _) = Security.ValidateChmodCommand(command);
        Assert.Equal(shouldAllow, allowed);
    }

    [Theory]
    [InlineData("./init.sh", true)]
    [InlineData("./init.sh arg1 arg2", true)]
    [InlineData("/path/to/init.sh", true)]
    [InlineData("../dir/init.sh", true)]
    [InlineData("./setup.sh", false)]
    [InlineData("./init.py", false)]
    [InlineData("bash init.sh", false)]
    [InlineData("sh init.sh", false)]
    [InlineData("./malicious.sh", false)]
    [InlineData("./init.sh; rm -rf /", false)]
    public void ValidateInitScript_MatchesExpected(string command, bool shouldAllow)
    {
        var (allowed, _) = Security.ValidateInitScript(command);
        Assert.Equal(shouldAllow, allowed);
    }

    [Theory]
    // Not in allowlist - dangerous system commands
    [InlineData("shutdown now")]
    [InlineData("reboot")]
    [InlineData("rm -rf /")]
    [InlineData("dd if=/dev/zero of=/dev/sda")]
    // Not in allowlist - common commands excluded from the minimal set
    [InlineData("curl https://example.com")]
    [InlineData("wget https://example.com")]
    [InlineData("python app.py")]
    [InlineData("touch file.txt")]
    [InlineData("echo hello")]
    [InlineData("kill 12345")]
    [InlineData("killall node")]
    // pkill with non-dev processes
    [InlineData("pkill bash")]
    [InlineData("pkill chrome")]
    [InlineData("pkill python")]
    // Shell injection attempts
    [InlineData("$(echo pkill) node")]
    [InlineData("eval \"pkill node\"")]
    [InlineData("bash -c \"pkill node\"")]
    // chmod with disallowed modes
    [InlineData("chmod 777 file.sh")]
    [InlineData("chmod 755 file.sh")]
    [InlineData("chmod +w file.sh")]
    [InlineData("chmod -R +x dir/")]
    // Non-init.sh scripts
    [InlineData("./setup.sh")]
    [InlineData("./malicious.sh")]
    [InlineData("bash script.sh")]
    // Regression case: a tokenizer that splits ";" as its own token even with no surrounding
    // whitespace would let "./init.sh" pass ValidateInitScript's exact-match check while the
    // injected "rm -rf /" hides in a second "segment" - make sure the no-space form is still
    // blocked as a whole (it's blocked because "rm" isn't allowlisted either way, but this
    // guards the injection vector specifically, not just the allowlist).
    [InlineData("./init.sh;rm -rf /")]
    public void ValidateBashCommand_BlocksDangerousCommands(string command)
    {
        var (allowed, _) = Security.ValidateBashCommand(command);
        Assert.False(allowed, $"Expected '{command}' to be blocked");
    }

    [Theory]
    // File inspection
    [InlineData("ls -la")]
    [InlineData("cat README.md")]
    [InlineData("head -100 file.txt")]
    [InlineData("tail -20 log.txt")]
    [InlineData("wc -l file.txt")]
    [InlineData("grep -r pattern src/")]
    // File operations
    [InlineData("cp file1.txt file2.txt")]
    [InlineData("mkdir newdir")]
    [InlineData("mkdir -p path/to/dir")]
    // Directory
    [InlineData("pwd")]
    // Node.js development
    [InlineData("npm install")]
    [InlineData("npm run build")]
    [InlineData("node server.js")]
    // Version control
    [InlineData("git status")]
    [InlineData("git commit -m 'test'")]
    [InlineData("git add . && git commit -m 'msg'")]
    // Process management
    [InlineData("ps aux")]
    [InlineData("lsof -i :3000")]
    [InlineData("sleep 2")]
    // Allowed pkill patterns for dev servers
    [InlineData("pkill node")]
    [InlineData("pkill npm")]
    [InlineData("pkill -f node")]
    [InlineData("pkill -f 'node server.js'")]
    [InlineData("pkill vite")]
    // Chained commands
    [InlineData("npm install && npm run build")]
    [InlineData("ls | grep test")]
    // Full paths
    [InlineData("/usr/local/bin/node app.js")]
    // chmod +x (allowed)
    [InlineData("chmod +x init.sh")]
    [InlineData("chmod +x script.sh")]
    [InlineData("chmod u+x init.sh")]
    [InlineData("chmod a+x init.sh")]
    // init.sh execution (allowed)
    [InlineData("./init.sh")]
    [InlineData("./init.sh --production")]
    [InlineData("/path/to/init.sh")]
    // Combined chmod and init.sh
    [InlineData("chmod +x init.sh && ./init.sh")]
    public void ValidateBashCommand_AllowsSafeCommands(string command)
    {
        var (allowed, reason) = Security.ValidateBashCommand(command);
        Assert.True(allowed, $"Expected '{command}' to be allowed, but got: {reason}");
    }
}
