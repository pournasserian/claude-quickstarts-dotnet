import { cookies } from "next/headers";
import { themes, themeNames, type ThemeName } from "@/styles/themes";

export const THEME_COLOR_COOKIE = "theme-color";
export const THEME_MODE_COOKIE = "theme-mode";

export type ThemeMode = "light" | "dark";

export { themeNames };
export type { ThemeName };

/** Reads the visitor's saved theme choice server-side (no client JS needed to render the right colors). */
export async function resolveTheme(): Promise<{ color: ThemeName; mode: ThemeMode; vars: Record<string, string> }> {
  const store = await cookies();
  const savedColor = store.get(THEME_COLOR_COOKIE)?.value;
  const savedMode = store.get(THEME_MODE_COOKIE)?.value;

  const color: ThemeName = themeNames.includes(savedColor as ThemeName) ? (savedColor as ThemeName) : "neutral";
  const mode: ThemeMode = savedMode === "dark" ? "dark" : "light";

  return { color, mode, vars: themes[color][mode] };
}

/** CSS custom properties for the resolved theme, ready to spread onto an inline `style` prop. */
export function themeCssVars(vars: Record<string, string>): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [key, value] of Object.entries(vars)) {
    out[`--${key}`] = value;
  }
  return out;
}
