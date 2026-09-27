import { cookies } from "next/headers";

export const SESSION_COOKIE = "csa_session";

/** Reads the visitor's session id, if the backend has already assigned one. */
export async function getSessionId(): Promise<string | undefined> {
  const store = await cookies();
  return store.get(SESSION_COOKIE)?.value;
}

/** Persists the session id the backend assigned, so later requests reuse the same conversation. */
export async function saveSessionId(sessionId: string): Promise<void> {
  const store = await cookies();
  store.set(SESSION_COOKIE, sessionId, {
    httpOnly: true,
    sameSite: "lax",
    path: "/",
  });
}
