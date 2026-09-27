const BACKEND_URL = process.env.BACKEND_URL ?? "http://localhost:5211";

export interface ModelOption {
  id: string;
  name: string;
}

export interface KnowledgeBaseOption {
  id: string;
  name: string;
}

export interface ConfigResponse {
  models: ModelOption[];
  knowledgeBases: KnowledgeBaseOption[];
}

export interface ChatMessageDto {
  id: string;
  role: "user" | "assistant";
  content: string;
  suggestedQuestions?: string[] | null;
  redirectToAgent?: RedirectToAgentDto | null;
}

export interface RedirectToAgentDto {
  shouldRedirect: boolean;
  reason?: string | null;
}

export interface RagSourceDto {
  id: string;
  fileName: string;
  snippet: string;
  score: number;
}

export interface ThinkingEntryDto {
  id: string;
  content: string;
  userMood: string;
  contextUsed: boolean;
  matchedCategories: string[];
  timestamp: string;
}

export interface RagHistoryEntryDto {
  query: string;
  timestamp: string;
  sources: RagSourceDto[];
}

export interface ChatResponse {
  sessionId: string;
  id: string;
  response: string;
  userMood: string;
  suggestedQuestions: string[];
  matchedCategories: string[];
  redirectToAgent: RedirectToAgentDto | null;
  debug: { contextUsed: boolean };
  messages: ChatMessageDto[];
  thinkingHistory: ThinkingEntryDto[];
  ragHistory: RagHistoryEntryDto[];
  lastModel: string | null;
  lastKnowledgeBaseId: string | null;
}

export interface SessionState {
  sessionId: string;
  messages: ChatMessageDto[];
  thinkingHistory: ThinkingEntryDto[];
  ragHistory: RagHistoryEntryDto[];
  lastModel: string | null;
  lastKnowledgeBaseId: string | null;
}

export async function getConfig(): Promise<ConfigResponse> {
  const res = await fetch(`${BACKEND_URL}/api/config`, { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Backend /api/config failed with status ${res.status}`);
  }
  return res.json();
}

export async function getSessionState(sessionId?: string): Promise<SessionState> {
  const url = new URL(`${BACKEND_URL}/api/session`);
  if (sessionId) url.searchParams.set("sessionId", sessionId);

  const res = await fetch(url, { cache: "no-store" });
  if (!res.ok) {
    throw new Error(`Backend /api/session failed with status ${res.status}`);
  }
  return res.json();
}

export async function sendChatMessage(params: {
  sessionId?: string;
  message: string;
  model: string;
  knowledgeBaseId?: string;
}): Promise<ChatResponse> {
  const res = await fetch(`${BACKEND_URL}/api/chat`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(params),
    cache: "no-store",
  });

  if (!res.ok) {
    throw new Error(`Backend /api/chat failed with status ${res.status}`);
  }

  return res.json();
}
