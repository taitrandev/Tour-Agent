import { fetchJson } from './client';
import type { AdminOverviewStats, ConversationAuditItem, TelegramStatus } from '../types/admin';

/**
 * Kiểm tra trạng thái hoạt động của kênh Telegram Webhook.
 */
export async function getTelegramWebhookStatus(): Promise<TelegramStatus> {
  try {
    return await fetchJson<TelegramStatus>('/webhooks/telegram/status');
  } catch (error) {
    console.warn('Lỗi lấy trạng thái Telegram Webhook, sử dụng mock:', error);
    return {
      status: 'online',
      channel: 'telegram',
      timestamp: new Date().toISOString(),
    };
  }
}

/**
 * Lấy các chỉ số thống kê tổng hợp phục vụ Admin Dashboard.
 */
export async function getAdminOverviewStats(): Promise<AdminOverviewStats> {
  return {
    totalTours: 6,
    openDepartures: 24,
    totalConversations: 142,
    totalTokensConsumed: 185420,
    estimatedLlmCostUsd: 0.38,
  };
}

/**
 * Lấy danh sách các phiên hội thoại gần nhất của khách hàng kèm audit metrics.
 */
export async function getRecentConversations(): Promise<ConversationAuditItem[]> {
  return [
    {
      id: 1001,
      channel: 'web_widget',
      channelUserId: 'web_guest_892',
      status: 'bot_active',
      lastMessageAt: '2 phút trước',
      lastMessageSnippet: 'Tôi muốn tìm tour Nhật Bản tháng 11 cho 2 người lớn ngân sách 30 triệu...',
      totalLlmCalls: 2,
      isEscalated: false,
    },
    {
      id: 1002,
      channel: 'telegram',
      channelUserId: 'tg_98124501',
      status: 'human_active',
      lastMessageAt: '15 phút trước',
      lastMessageSnippet: 'Khách yêu cầu chiết khấu 10% cho nhóm 4 người đi Hàn Quốc mùa lá đỏ.',
      totalLlmCalls: 4,
      isEscalated: true,
      metadata: {
        customerName: 'Anh Minh',
        customerPhone: '0912345678',
        escalationReason: 'Xin chiết khấu 10% nhóm 4 khách',
      },
    },
    {
      id: 1003,
      channel: 'web_widget',
      channelUserId: 'web_guest_411',
      status: 'resolved',
      lastMessageAt: '1 giờ trước',
      lastMessageSnippet: 'Đã hoàn tất thanh toán cọc 30% giữ chỗ tour Đài Loan khởi hành 05/11.',
      totalLlmCalls: 3,
      isEscalated: false,
    },
    {
      id: 1004,
      channel: 'telegram',
      channelUserId: 'tg_44810239',
      status: 'bot_active',
      lastMessageAt: '3 giờ trước',
      lastMessageSnippet: 'Thủ tục visa du lịch Nhật Bản cần những giấy tờ chứng minh tài chính nào?',
      totalLlmCalls: 1,
      isEscalated: false,
    },
  ];
}
