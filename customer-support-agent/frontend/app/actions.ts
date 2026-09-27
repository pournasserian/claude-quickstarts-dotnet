"use server";

import { revalidatePath } from "next/cache";
import { sendChatMessage } from "@/lib/backend";
import { getSessionId, saveSessionId } from "@/lib/session";
import { THEME_COLOR_COOKIE, THEME_MODE_COOKIE, themeNames, type ThemeName, type ThemeMode } from "@/lib/theme";
import { cookies } from "next/headers";

export async function sendMessage(formData: FormData): Promise<void> {
  const message = String(formData.get("message") ?? "").trim();
  const model = String(formData.get("model") ?? "");
  const knowledgeBaseId = String(formData.get("knowledgeBaseId") ?? "");

  if (!message) {
    return;
  }

  const sessionId = await getSessionId();
  const response = await sendChatMessage({ sessionId, message, model, knowledgeBaseId });
  await saveSessionId(response.sessionId);

  revalidatePath("/");
}

export async function setThemeMode(mode: ThemeMode): Promise<void> {
  const store = await cookies();
  store.set(THEME_MODE_COOKIE, mode, { sameSite: "lax", path: "/" });
  revalidatePath("/");
}

export async function setThemeColor(color: ThemeName): Promise<void> {
  if (!themeNames.includes(color)) return;
  const store = await cookies();
  store.set(THEME_COLOR_COOKIE, color, { sameSite: "lax", path: "/" });
  revalidatePath("/");
}
