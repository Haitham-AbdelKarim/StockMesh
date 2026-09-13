export interface ConversationSummary {
  id: string;
  title: string;
  lastActiveAt: string;
}

export interface ConversationTurn {
  question: string;
  toolCalls: string;
  answer: string;
  createdAt: string;
}

export interface ConversationDetail {
  id: string;
  title: string;
  turns: ConversationTurn[];
}

export interface ToolCallTrace {
  name: string;
  args: Record<string, unknown>;
}

export interface AssistantDone {
  answer: string;
  toolCalls: ToolCallTrace[];
  modelVersion: string;
  conversationId: string;
}
