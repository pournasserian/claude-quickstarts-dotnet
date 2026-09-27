# claude-quickstarts-dotnet

Personal .NET ports of two quickstarts from Anthropic's [claude-quickstarts](https://github.com/anthropics/anthropic-quickstarts) repository. Not affiliated with or endorsed by Anthropic — just an exploration of the same demos on a different stack.

## Projects

### [`customer-support-agent/`](customer-support-agent/)

A rewrite of the [customer-support-agent](https://github.com/anthropics/anthropic-quickstarts/tree/main/customer-support-agent) quickstart: an ASP.NET Core backend (Minimal API, Microsoft Agent Framework, Claude, optional AWS Bedrock RAG) plus a server-rendered, SSR-only Next.js frontend, in place of the original's single combined Next.js app.

### [`autonomous-coding/`](autonomous-coding/)

A .NET console port of the [autonomous-coding](https://github.com/anthropics/anthropic-quickstarts/tree/main/autonomous-coding) quickstart's long-running, two-agent autonomous coding harness. Rebuilt on Microsoft Agent Framework's direct-model-inference path with hand-written file/bash tools, since there's no C# equivalent of the Python Claude Agent SDK the original depends on. **See that project's README for an important security note** — this port does not carry over Claude Code's OS-level sandbox.

## License

MIT — see [LICENSE](LICENSE).
