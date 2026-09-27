import type { ComponentType } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { User, DollarSign, Zap, Building2, Scale, CircleHelp, Wrench, ChartBarBig } from "lucide-react";
import type { ThinkingEntryDto } from "@/lib/backend";

const categoryIcons: Record<string, ComponentType<{ className?: string }>> = {
  account: User,
  billing: DollarSign,
  feature: Zap,
  internal: Building2,
  legal: Scale,
  other: CircleHelp,
  technical: Wrench,
  usage: ChartBarBig,
};

const moodColors: Record<string, string> = {
  positive: "bg-green-100 text-green-800",
  negative: "bg-red-100 text-red-800",
  curious: "bg-blue-100 text-blue-800",
  frustrated: "bg-orange-100 text-orange-800",
  confused: "bg-yellow-100 text-yellow-800",
  neutral: "bg-gray-100 text-gray-800",
};

export default function LeftSidebar({ entries }: { entries: ThinkingEntryDto[] }) {
  return (
    <aside className="w-[380px] pl-4 overflow-hidden pb-4">
      <Card className="h-full overflow-hidden">
        <CardHeader>
          <CardTitle className="text-sm font-medium leading-none">Assistant Thinking</CardTitle>
        </CardHeader>
        <CardContent className="overflow-y-auto h-[calc(100%-45px)]">
          {entries.length === 0 ? (
            <div className="text-sm text-muted-foreground">
              The assistant inner dialogue will appear here for you to debug it
            </div>
          ) : (
            entries.map((entry) => (
              <Card key={entry.id} className="mb-4 animate-fade-in-up">
                <CardContent className="py-4">
                  <div className="text-sm text-muted-foreground">{entry.content}</div>
                  <div className="flex items-center space-x-2 mt-4 text-xs">
                    <span className={`px-2 py-1 rounded-full ${moodColors[entry.userMood?.toLowerCase()] ?? moodColors.neutral}`}>
                      {entry.userMood ? entry.userMood.charAt(0).toUpperCase() + entry.userMood.slice(1) : "Neutral"}
                    </span>
                    <span
                      className={`px-2 py-1 rounded-full border ${
                        entry.contextUsed
                          ? "bg-green-100 text-green-800 border-green-300"
                          : "bg-yellow-100 text-yellow-800 border-yellow-300"
                      }`}
                    >
                      Context: {entry.contextUsed ? "✅" : "❌"}
                    </span>
                  </div>
                  {entry.matchedCategories.length > 0 && (
                    <div className="mt-2">
                      {entry.matchedCategories.map((category) => {
                        const Icon = categoryIcons[category];
                        return (
                          <div
                            key={category}
                            className="inline-flex items-center mr-2 mt-2 text-muted-foreground text-xs py-0"
                          >
                            {Icon && <Icon className="w-3 h-3 mr-1" />}
                            {category
                              .split("_")
                              .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
                              .join(" ")}
                          </div>
                        );
                      })}
                    </div>
                  )}
                </CardContent>
              </Card>
            ))
          )}
        </CardContent>
      </Card>
    </aside>
  );
}
