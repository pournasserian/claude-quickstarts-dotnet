import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { FileIcon, MessageCircleIcon } from "lucide-react";
import type { RagHistoryEntryDto } from "@/lib/backend";

const truncateSnippet = (text: string): string => (text?.length > 150 ? `${text.slice(0, 100)}...` : text || "");

const getScoreColor = (score: number): string => {
  if (score > 0.6) return "bg-green-100 text-green-800";
  if (score > 0.4) return "bg-yellow-100 text-yellow-800";
  return "bg-red-100 text-red-800";
};

export default function RightSidebar({ history }: { history: RagHistoryEntryDto[] }) {
  return (
    <aside className="w-[380px] pr-4 overflow-hidden pb-4">
      <Card className="h-full overflow-hidden animate-fade-in-up">
        <CardHeader>
          <CardTitle className="text-sm font-medium leading-none">Knowledge Base History</CardTitle>
        </CardHeader>
        <CardContent className="overflow-y-auto h-[calc(100%-45px)]">
          {history.length === 0 && (
            <div className="text-sm text-muted-foreground">
              The assistant will display sources here once finding them
            </div>
          )}
          {history.map((item) => (
            <div key={item.timestamp} className="mb-6 animate-fade-in-up">
              <div className="flex items-center text-xs text-muted-foreground mb-2 gap-1">
                <MessageCircleIcon size={14} className="text-muted-foreground" />
                <span>{item.query}</span>
              </div>
              {item.sources.map((source) => (
                <Card key={source.id} className="mb-2 animate-fade-in-up">
                  <CardContent className="py-4">
                    <p className="text-sm text-muted-foreground">{truncateSnippet(source.snippet)}</p>
                    <div className="flex flex-col gap-2">
                      <div
                        className={`${getScoreColor(source.score)} px-2 py-1 mt-4 rounded-full text-xs inline-block w-fit`}
                      >
                        {(source.score * 100).toFixed(0)}% match
                      </div>
                      <details className="group">
                        <summary className="inline-flex items-center mr-2 mt-2 text-muted-foreground text-xs py-0 cursor-pointer hover:text-foreground list-none">
                          <FileIcon className="w-4 h-4 min-w-[12px] min-h-[12px] mr-2" />
                          <span className="text-xs underline">{truncateSnippet(source.fileName || "Unnamed")}</span>
                        </summary>
                        <div className="mt-2 rounded-md border border-border bg-muted/40 p-3 text-xs">
                          <div className="mb-2 font-medium">
                            Score: {(source.score * 100).toFixed(2)}%
                          </div>
                          <p className="whitespace-pre-wrap text-muted-foreground">{source.snippet}</p>
                        </div>
                      </details>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          ))}
        </CardContent>
      </Card>
    </aside>
  );
}
