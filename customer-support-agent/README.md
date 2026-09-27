# Customer Support Agent - .NET / SSR rewrite

A rewrite of [`../customer-support-agent`](../customer-support-agent) with an ASP.NET Core backend
and a server-rendered (SSR-only) Next.js frontend. Same feature set - Claude-powered chat, Amazon
Bedrock Knowledge Base RAG, mood/category detection, human-agent redirection, themeable UI - built
on a different stack. The original project is untouched.

## Stack

- **Backend**: ASP.NET Core Minimal API, .NET 10, [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/)'s
  Anthropic provider (`Microsoft.Agents.AI.Anthropic`, currently a prerelease package) for Claude,
  `AWSSDK.BedrockAgentRuntime` for RAG. Conversation state is in-memory, keyed by a session id -
  no database.
- **Frontend**: Next.js 16 (App Router), React 19, Tailwind v4. Rendering is SSR-only: sending a
  message, switching models, and changing the theme are all Server Actions or plain form posts -
  there's no client-side fetch to the backend and no client-managed chat state. The backend is
  never called from the browser, only server-to-server from the Next.js server.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js](https://nodejs.org/) 20+
- An [Anthropic API key](https://console.anthropic.com/)
- (Optional) AWS credentials and a Bedrock Knowledge Base, if you want real RAG retrieval - see the
  original project's [README](../customer-support-agent/README.md#-amazon-bedrock-rag-integration)
  for how to set one up. Without it, the app runs fine and Claude just answers without retrieved
  context, the same graceful fallback the original app has.

## Backend setup

```bash
cd backend
dotnet user-secrets init
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."

# optional, only if you have a real Bedrock Knowledge Base:
dotnet user-secrets set "Aws:AccessKeyId" "..."
dotnet user-secrets set "Aws:SecretAccessKey" "..."
dotnet user-secrets set "Aws:KnowledgeBaseId" "..."

dotnet run --urls http://localhost:5211
```

## Frontend setup

```bash
cd frontend
npm install
npm run dev
```

Open [http://localhost:3000](http://localhost:3000). `.env.local` already points `BACKEND_URL` at
`http://localhost:5211`; change it if you run the backend elsewhere.

Sidebar visibility is controlled the same way as the original, via npm script variants:
`dev` / `dev:left` / `dev:right` / `dev:chat` (and the matching `build:*` scripts).

## What's different from the original

- **No client-side chat state.** Chat history, the "Assistant Thinking" log, and the "Knowledge
  Base History" log all live server-side (in-memory, per session) and are rendered on every
  request - a page refresh no longer loses your conversation, unlike the original.
- **Structured JSON output** comes from Agent Framework's response-format support instead of the
  original's "prefill `{` and regex-sanitize" trick.
- **The knowledge-base and model pickers are plain `<select>` fields** inside the message form,
  not separate client-state dropdowns.
- **The theme toggle and color picker use native `<details>` + Server Actions**, not a client
  dropdown component - so the color/mode swatch menu doesn't auto-close on an outside click the
  way the original's Radix-based one did. Minor, deliberate tradeoff for staying JS-free.
- **The "view full source" modal is an inline `<details>` disclosure**, not a popup dialog -
  same reasoning.
- **No streaming, no optimistic "message appears instantly" UI.** Like the original, a reply is
  a single non-streamed response; unlike the original, the pending state is a spinner on the Send
  button (`useFormStatus`) rather than a placeholder chat bubble.
- Neither app is responsive below the sidebars' combined fixed width (this was already true of
  the original - not something this rewrite changed).

## Verification status

Manually verified end-to-end in a browser: layout, dark/light + all 7 color themes, sending a
message, session persistence across a full page reload, and graceful fallback behavior when
`Anthropic:ApiKey` isn't configured. **Not yet verified with a real Anthropic API key** - that
needs your own key plugged into user-secrets, since none was available in the environment this was
built in.
