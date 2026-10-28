/**
 * Trạng thái kết nối của Kênh Telegram Webhook.
 */
export interface TelegramStatus {
  status: 'online' | 'offline';
  channel: string;
  timestamp: string;
}

/**
 * Thống kê tổng quan cho Admin Dashboard.
 */
export interface AdminOverviewStats {
  totalTours: number;
  openDepartures: number;
  totalConversations: number;
  totalTokensConsumed: number;
  estimatedLlmCostUsd: number;
}

/**
 * Bản ghi phiên hội thoại tóm tắt để quản trị viên giám sát.
 */
export interface ConversationAuditItem {
  id: number;
  channel: string;
  channelUserId: string;
  status: 'bot_active' | 'human_active' | 'resolved';
  lastMessageAt: string;
  lastMessageSnippet: string;
  totalLlmCalls: number;
  isEscalated: boolean;
  metadata?: {
    customerPhone?: string;
    customerName?: string;
    escalationReason?: string;
  };
}
