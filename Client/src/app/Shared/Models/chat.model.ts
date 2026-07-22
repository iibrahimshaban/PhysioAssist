export type SearchMode = 'smart' | 'exact';
export type MessageRole = 'user' | 'assistant';
export type MessageStatus = 'sending' | 'sent' | 'error';

export interface ChatMessage {
  id: string;
  role: MessageRole;
  content: string;
  timestamp: number;
  status?: MessageStatus;
}

export interface ChatSession {
  id: string;
  title: string;          // derived from the first user message
  mode: SearchMode;
  createdAt: number;
  updatedAt: number;
  messages: ChatMessage[];
}

// export interface AiChatRequest {
//   sessionId: string;
//   mode: SearchMode;
//   message: string;
//   history: { role: MessageRole; content: string }[];
// }

export interface AiChatResponse {
  conversationId: string;
  question: string;
  answer: string;
}

export interface AiClearResponse {
  result: boolean;
  response: string;
}
