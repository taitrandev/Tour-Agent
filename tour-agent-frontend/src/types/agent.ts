/**
 * Yêu cầu gửi câu hỏi tới AI Agent tư vấn du lịch.
 */
export interface ChatRequest {
  conversationId?: number;
  message: string;
  currentTravelId?: number;
  currentTravelTitle?: string;
  country?: string;
}

/**
 * Kết quả trả về sau chu trình xử lý đồng bộ của AI Agent.
 */
export interface AgentChatResult {
  responseText: string;
  toolsExecuted: string[];
  totalTokens: number;
  estimatedCostUsd?: number;
  totalLatencyMs: number;
  turnsCount: number;
  isEscalated: boolean;
}

/**
 * Định nghĩa loại sự kiện truyền phát SSE từ AI Agent.
 */
export type StreamEventType = 'tool_start' | 'tool_end' | 'token' | 'completed' | 'error';

/**
 * Cấu trúc gói tin sự kiện Server-Sent Events (SSE) từ /api/v1/agent/chat/stream.
 */
export interface AgentStreamEvent {
  eventType: StreamEventType;
  content?: string | null;
  toolName?: string | null;
}

/**
 * Cấu trúc tin nhắn hiển thị trên khung chat người dùng.
 */
export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: Date;
  toolsExecuted?: string[];
  activeTool?: string | null;
  isStreaming?: boolean;
  tokensCount?: number;
  latencyMs?: number;
  isEscalated?: boolean;
}
