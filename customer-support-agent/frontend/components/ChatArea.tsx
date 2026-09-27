import ReactMarkdown from "react-markdown";
import rehypeHighlight from "rehype-highlight";
import rehypeRaw from "rehype-raw";
import Image from "next/image";
import { HandHelping, WandSparkles, BookOpenText, LifeBuoyIcon, ChevronDown } from "lucide-react";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Card, CardContent, CardFooter } from "@/components/ui/card";
import { MessageTextarea, SubmitButton } from "@/components/ChatFormControls";
import { sendMessage } from "@/app/actions";
import type { ChatMessageDto, ModelOption, KnowledgeBaseOption } from "@/lib/backend";

interface ChatAreaProps {
  messages: ChatMessageDto[];
  models: ModelOption[];
  knowledgeBases: KnowledgeBaseOption[];
  selectedModel: string;
  selectedKnowledgeBaseId: string;
}

export default function ChatArea({
  messages,
  models,
  knowledgeBases,
  selectedModel,
  selectedKnowledgeBaseId,
}: ChatAreaProps) {
  const showAvatar = messages.length > 0;

  return (
    <Card className="flex-1 flex flex-col mb-4 mr-4 ml-4">
      <CardContent className="flex-1 flex flex-col overflow-hidden pt-4 px-4 pb-0">
        <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between pb-2 animate-fade-in">
          <div className="flex items-center space-x-4 mb-2 sm:mb-0">
            {showAvatar && (
              <>
                <Avatar className="w-10 h-10 border border-border">
                  <AvatarImage src="/ant-logo.svg" alt="AI Assistant Avatar" />
                  <AvatarFallback>AI</AvatarFallback>
                </Avatar>
                <div>
                  <h3 className="text-sm font-medium leading-none">AI Agent</h3>
                  <p className="text-sm text-muted-foreground">Customer support</p>
                </div>
              </>
            )}
          </div>
        </div>

        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          {messages.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full animate-fade-in-up">
              <Avatar className="w-10 h-10 mb-4 border border-border">
                <AvatarImage src="/ant-logo.svg" alt="AI Assistant Avatar" />
              </Avatar>
              <h2 className="text-2xl font-semibold mb-8">Here&apos;s how I can help</h2>
              <div className="space-y-4 text-sm">
                <div className="flex items-center gap-3">
                  <HandHelping className="text-muted-foreground" />
                  <p className="text-muted-foreground">
                    Need guidance? I&apos;ll help navigate tasks using internal resources.
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <WandSparkles className="text-muted-foreground" />
                  <p className="text-muted-foreground">
                    I&apos;m a whiz at finding information! I can dig through your knowledge base.
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <BookOpenText className="text-muted-foreground" />
                  <p className="text-muted-foreground">
                    I&apos;m always learning! The more you share, the better I can assist you.
                  </p>
                </div>
              </div>
            </div>
          ) : (
            <div className="space-y-4">
              {messages.map((message) => (
                <div key={message.id}>
                  <div className={`flex items-start ${message.role === "user" ? "justify-end" : ""}`}>
                    {message.role === "assistant" && (
                      <Avatar className="w-8 h-8 mr-2 border border-border">
                        <AvatarImage src="/ant-logo.svg" alt="AI Assistant Avatar" />
                        <AvatarFallback>AI</AvatarFallback>
                      </Avatar>
                    )}
                    <div
                      className={`p-3 rounded-md text-sm max-w-[65%] ${
                        message.role === "user" ? "bg-primary text-primary-foreground" : "bg-muted border border-border"
                      }`}
                    >
                      <div className="prose prose-sm dark:prose-invert max-w-none">
                        <ReactMarkdown rehypePlugins={[rehypeRaw, rehypeHighlight]}>{message.content}</ReactMarkdown>
                      </div>
                      {message.redirectToAgent?.shouldRedirect && (
                        <span className="mt-2 inline-flex items-center gap-2 rounded-md border border-border px-3 py-1.5 text-sm">
                          <LifeBuoyIcon className="w-4 h-4" />
                          Talk to a human
                        </span>
                      )}
                    </div>
                  </div>
                  {message.role === "assistant" && message.suggestedQuestions && message.suggestedQuestions.length > 0 && (
                    <div className="mt-2 pl-10 flex flex-wrap gap-2">
                      {message.suggestedQuestions.map((question) => (
                        <form key={question} action={sendMessage}>
                          <input type="hidden" name="message" value={question} />
                          <input type="hidden" name="model" value={selectedModel} />
                          <input type="hidden" name="knowledgeBaseId" value={selectedKnowledgeBaseId} />
                          <button
                            type="submit"
                            className="text-sm mb-2 text-muted-foreground shadow-sm border border-border rounded-md px-3 py-1.5 hover:bg-accent hover:text-accent-foreground"
                          >
                            {question}
                          </button>
                        </form>
                      ))}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </CardContent>

      <CardFooter className="p-4 pt-0">
        <form
          action={sendMessage}
          className="flex flex-col w-full relative bg-background border border-border rounded-xl focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-2"
        >
          <MessageTextarea />
          <div className="flex justify-between items-center p-3 flex-wrap gap-2">
            <div className="flex items-center gap-2">
              <Image src="/claude-icon.svg" alt="Claude Icon" width={0} height={14} className="w-auto h-[14px]" />
              <div className="relative">
                <select
                  name="model"
                  defaultValue={selectedModel}
                  className="appearance-none rounded-md border border-border bg-background text-muted-foreground text-sm pl-3 pr-7 py-1.5"
                >
                  {models.map((model) => (
                    <option key={model.id} value={model.id}>
                      {model.name}
                    </option>
                  ))}
                </select>
                <ChevronDown className="pointer-events-none absolute right-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
              </div>
              <div className="relative">
                <select
                  name="knowledgeBaseId"
                  defaultValue={selectedKnowledgeBaseId}
                  className="appearance-none rounded-md border border-border bg-background text-muted-foreground text-sm pl-3 pr-7 py-1.5"
                >
                  {knowledgeBases.map((kb) => (
                    <option key={kb.id} value={kb.id}>
                      {kb.name}
                    </option>
                  ))}
                </select>
                <ChevronDown className="pointer-events-none absolute right-2 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground" />
              </div>
            </div>
            <SubmitButton />
          </div>
        </form>
      </CardFooter>
    </Card>
  );
}
