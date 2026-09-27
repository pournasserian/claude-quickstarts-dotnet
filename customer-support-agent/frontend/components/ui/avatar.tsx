import * as React from "react";
import Image from "next/image";
import { cn } from "@/lib/utils";

export function Avatar({ className, children }: { className?: string; children: React.ReactNode }) {
  return (
    <div className={cn("relative flex shrink-0 overflow-hidden rounded-full bg-muted items-center justify-center", className)}>
      {children}
    </div>
  );
}

export function AvatarImage({ src, alt }: { src: string; alt: string }) {
  return <Image src={src} alt={alt} fill sizes="40px" className="object-cover" />;
}

export function AvatarFallback({ children }: { children: React.ReactNode }) {
  return <span className="text-xs font-medium text-muted-foreground">{children}</span>;
}
