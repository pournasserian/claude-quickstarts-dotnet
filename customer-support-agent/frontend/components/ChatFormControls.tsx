"use client";

import { useFormStatus } from "react-dom";
import { useRef } from "react";
import { Send } from "lucide-react";
import { Button } from "@/components/ui/button";

/**
 * The only client-side interactivity in the chat form: a pending spinner (useFormStatus needs to
 * live in a client component) and Enter-to-submit on the textarea. Everything else - sending the
 * message, rendering the reply, updating both sidebars - happens via the sendMessage Server Action
 * and a normal server re-render.
 */
export function SubmitButton() {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" disabled={pending} size="sm" className="gap-2">
      {pending ? (
        <span className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent" />
      ) : (
        <>
          Send Message
          <Send className="h-4 w-4" />
        </>
      )}
    </Button>
  );
}

export function MessageTextarea({ defaultValue }: { defaultValue?: string }) {
  const ref = useRef<HTMLTextAreaElement>(null);

  return (
    <textarea
      ref={ref}
      name="message"
      defaultValue={defaultValue}
      placeholder="Type your message here..."
      rows={1}
      className="resize-none min-h-[44px] max-h-[300px] bg-background border-0 p-3 rounded-xl shadow-none focus:outline-none focus:ring-0 w-full"
      onKeyDown={(event) => {
        if (event.key === "Enter" && !event.shiftKey) {
          event.preventDefault();
          const textarea = event.currentTarget;
          if (textarea.value.trim() !== "") {
            textarea.form?.requestSubmit();
          }
        }
      }}
      onInput={(event) => {
        const textarea = event.currentTarget;
        textarea.style.height = "auto";
        textarea.style.height = `${Math.min(textarea.scrollHeight, 300)}px`;
      }}
    />
  );
}
