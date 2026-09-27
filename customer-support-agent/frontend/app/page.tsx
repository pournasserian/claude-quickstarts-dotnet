import TopNavBar from "@/components/TopNavBar";
import ChatArea from "@/components/ChatArea";
import LeftSidebar from "@/components/LeftSidebar";
import RightSidebar from "@/components/RightSidebar";
import config from "@/config";
import { getConfig, getSessionState } from "@/lib/backend";
import { getSessionId } from "@/lib/session";

export default async function Home() {
  const sessionId = await getSessionId();
  const [backendConfig, state] = await Promise.all([getConfig(), getSessionState(sessionId)]);

  const selectedModel = state.lastModel ?? backendConfig.models[0]?.id ?? "";
  const selectedKnowledgeBaseId = state.lastKnowledgeBaseId ?? backendConfig.knowledgeBases[0]?.id ?? "";

  return (
    <div className="flex flex-col h-screen w-full">
      <TopNavBar />
      <div className="flex flex-1 overflow-hidden h-screen w-full">
        {config.includeLeftSidebar && <LeftSidebar entries={state.thinkingHistory} />}
        <ChatArea
          messages={state.messages}
          models={backendConfig.models}
          knowledgeBases={backendConfig.knowledgeBases}
          selectedModel={selectedModel}
          selectedKnowledgeBaseId={selectedKnowledgeBaseId}
        />
        {config.includeRightSidebar && <RightSidebar history={state.ragHistory} />}
      </div>
    </div>
  );
}
