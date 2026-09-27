import Image from "next/image";
import Link from "next/link";
import { Check, Moon, Sun } from "lucide-react";
import { resolveTheme, themeNames, type ThemeName } from "@/lib/theme";
import { setThemeColor, setThemeMode } from "@/app/actions";

const themeColors: Record<ThemeName, string> = {
  neutral: "#000000",
  red: "#EF4444",
  violet: "#8B5CF6",
  blue: "#3B82F6",
  tangerine: "#F97316",
  emerald: "#10B981",
  amber: "#F59E0B",
};

export default async function TopNavBar() {
  const { color, mode } = await resolveTheme();

  return (
    <nav className="text-foreground p-4 flex justify-between items-center">
      <div className="font-bold text-xl flex gap-2 items-center">
        <Image
          src={mode === "dark" ? "/wordmark-dark.svg" : "/wordmark.svg"}
          alt="Company Wordmark"
          width={112}
          height={20}
        />
      </div>
      <div className="flex items-center gap-2">
        <details className="relative">
          <summary className="list-none cursor-pointer inline-flex h-10 items-center justify-center rounded-md border border-border px-3">
            <span
              className="relative flex h-4 w-4 items-center justify-center rounded-full border border-border"
              style={{ backgroundColor: themeColors[color] }}
            />
          </summary>
          <div className="absolute right-0 mt-2 w-40 rounded-md border border-border bg-popover text-popover-foreground p-1 shadow-md z-10">
            {themeNames.map((name) => (
              <form key={name} action={setThemeColor.bind(null, name)}>
                <button
                  type="submit"
                  className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm hover:bg-accent hover:text-accent-foreground"
                >
                  <span
                    className="relative flex h-4 w-4 shrink-0 items-center justify-center rounded-full border border-border"
                    style={{ backgroundColor: themeColors[name] }}
                  >
                    {name === color && <Check className="text-white" size={12} />}
                  </span>
                  {name.charAt(0).toUpperCase() + name.slice(1)}
                </button>
              </form>
            ))}
          </div>
        </details>

        <form action={setThemeMode.bind(null, mode === "dark" ? "light" : "dark")}>
          <button
            type="submit"
            aria-label="Toggle theme"
            className="inline-flex h-10 w-10 items-center justify-center rounded-md border border-border hover:bg-accent hover:text-accent-foreground"
          >
            {mode === "dark" ? <Sun className="h-[1.2rem] w-[1.2rem]" /> : <Moon className="h-[1.2rem] w-[1.2rem]" />}
          </button>
        </form>

        <Link href="https://github.com/anthropics/anthropic-quickstarts" target="_blank" rel="noopener noreferrer">
          <span className="inline-flex h-10 items-center justify-center rounded-md border border-border px-4 text-sm hover:bg-accent hover:text-accent-foreground">
            Deploy your own
          </span>
        </Link>
      </div>
    </nav>
  );
}
