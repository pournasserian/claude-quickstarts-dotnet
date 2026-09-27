import type { Metadata } from "next";
import type { CSSProperties } from "react";
import { Inter } from "next/font/google";
import "./globals.css";
import { resolveTheme, themeCssVars } from "@/lib/theme";

const inter = Inter({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: "AI Chat Assistant",
  description: "Chat with an AI assistant powered by Anthropic",
};

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const { mode, vars } = await resolveTheme();
  const style = themeCssVars(vars) as CSSProperties;

  return (
    <html lang="en" className={mode === "dark" ? "dark" : undefined} style={style}>
      <body className={`${inter.className} flex flex-col h-full min-h-screen`}>{children}</body>
    </html>
  );
}
