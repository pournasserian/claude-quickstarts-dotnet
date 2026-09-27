# Autonomous Coding Agent Demo - .NET port

A .NET port of [`../autonomous-coding`](../autonomous-coding). Same two-agent pattern (initializer +
coding agent), same prompts, same bash-command allowlist - built as a .NET console app instead of a
Python script. The original project is untouched.

## Why this isn't a 1:1 port

The original drives the **Claude Agent SDK** (`claude_code_sdk`), which wraps Claude Code's own
managed runtime: sessions, permissions, an OS-level sandbox, and built-in file/shell tools. That
integration is [explicitly Python-only](https://learn.microsoft.com/agent-framework/integrations/by-component/agent-services/)
in Microsoft Agent Framework - there's no C# equivalent.

So this port rebuilds the tool-calling loop directly on Agent Framework's other integration path
(the same one `../DotNet/backend` uses): `Microsoft.Agents.AI.Anthropic`'s direct model inference,
with hand-written function tools (`Tools/FileTools.cs`, `Tools/BashTool.cs`) standing in for Claude
Code's built-in Read/Write/Edit/Glob/Grep/Bash tools.

**What carries over:** the bash-command allowlist (`Security.cs`, ported line-for-line in behavior
from `security.py`, with `test_security.py`'s full test suite ported to `AutonomousCoding.Tests`),
project-directory restriction on every file tool, and the Puppeteer MCP server for browser-based
verification (via the official MCP C# SDK).

**What doesn't:** Claude Code's **OS-level sandbox**. The original's security model is defense in
depth across three layers (sandbox, permissions, allowlist); this port only has the allowlist and
a directory-restriction check we implement ourselves in each tool. That's a real reduction in
depth-of-defense, not just a cosmetic difference - worth knowing before pointing this at anything
you don't fully trust the model's tool calls around.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An [Anthropic API key](https://console.anthropic.com/)
- **Git Bash** (`bash` on PATH) - the Bash tool shells out to it, since the allowlist (`ls`, `cat`,
  `pkill`, ...) assumes a POSIX shell. On Windows this is what Git for Windows installs.
- Node.js + `npx`, for the Puppeteer MCP server the coding prompt expects for browser verification.
  If `npx` isn't available, the agent still runs - it just won't have browser automation tools for
  that session (a warning is printed).

## Setup

```bash
export ANTHROPIC_API_KEY='your-api-key-here'   # bash
$env:ANTHROPIC_API_KEY = 'your-api-key-here'    # PowerShell
```

## Usage

Same CLI shape as the original:

```bash
dotnet run --project autonomous-coding-dotnet -- --project-dir ./my_project
dotnet run --project autonomous-coding-dotnet -- --project-dir ./my_project --max-iterations 3
dotnet run --project autonomous-coding-dotnet -- --project-dir ./my_project --model claude-sonnet-4-5-20250929
```

Relative `--project-dir` values are placed under `generations/`, same as the original. Expect the
same timing: the first (initializer) session can take 10-20+ minutes generating 200 test cases;
each coding session after that, 5-15 minutes.

## Running the tests

```bash
dotnet test
```

101 tests: the full `test_security.py` suite ported to xUnit (90 cases, including one regression
case - see below), plus new coverage for `FileTools`' path restriction and `BashTool`'s real
subprocess execution (18 cases; `FileTools` and `BashTool` are new code with no original test
suite to port from).

### A real bug the port caught

While porting `security.py`'s command parsing, I initially wrote a "smarter" shell tokenizer that
split `;`, `|`, and `&&` into their own tokens unconditionally. That's more correct as general
shell parsing, but it's *not* what Python's `shlex.split()` - which the original's parsing relies
on - actually does: shlex only treats those as separators when they're whitespace-separated,
otherwise they're glued onto the adjacent word. That difference is security-relevant:
`./init.sh;rm -rf /` needs to tokenize as one dirty token that fails the `== "./init.sh"` check,
not as a clean `"./init.sh"` followed by a hidden `rm`. The ported test suite caught this
immediately (`test_security.py` has this exact case). Fixed in `ShellTokenizer.cs`, which now
matches shlex's actual behavior. This is exactly the kind of subtle bug the "review later" you
chose for this security code should specifically go looking for.

## Verification status

Built and fully unit-tested (101/101 passing), including live subprocess execution through the
real allowlist. **Not run end-to-end against the live Anthropic API or a real coding session** - no
API key was available in the environment this was built in, and a real run takes hours by design.
Before trusting this with a real project directory, I'd suggest a short run with
`--max-iterations 1` first.
