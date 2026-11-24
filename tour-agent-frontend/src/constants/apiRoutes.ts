/**
 * Định nghĩa tập trung toàn bộ API Endpoints của hệ thống TourAgent (Client-side Routing Constants).
 * Đóng vai trò Single Source of Truth, triệt tiêu hoàn toàn Magic Strings trong toàn bộ ứng dụng frontend.
 */
export const API_ROUTES = {
  // 1. Phân hệ AI Agent & Trò chuyện
  AGENT: {
    CHAT: '/api/v1/agent/chat',
    CHAT_STREAM: '/api/v1/agent/chat/stream',
    ADMIN_CHAT: '/api/v1/agent/admin/chat',
    ADMIN_CHAT_STREAM: '/api/v1/agent/admin/chat/stream',
    TELEGRAM: {
      STATUS: '/api/v1/agent/telegram/status',
      SAVE_TOKEN: '/api/v1/agent/telegram/save-token',
      SET_WEBHOOK: '/api/v1/agent/telegram/set-webhook',
      DELETE_WEBHOOK: '/api/v1/agent/telegram/delete-webhook',
      SAVE_ADMIN_CHAT_ID: '/api/v1/agent/telegram/save-admin-chat-id',
      TEST_ESCALATION: '/api/v1/agent/telegram/test-escalation',
      TEST_MESSAGE: '/api/v1/agent/telegram/test-message',
    },
    CONVERSATIONS: {
      BASE: '/api/v1/agent/consultant/conversations',
      BY_ID: (id: number | string) => `/api/v1/agent/consultant/conversations/${id}`,
    },
  },

  // 2. Phân hệ Tour Du Lịch & Sản Phẩm (Travels)
  TRAVELS: {
    BASE: '/api/v1/travels',
    SEARCH: '/api/v1/travels/search',
    BY_ID: (id: number | string) => `/api/v1/travels/${id}`,
    BY_SLUG: (slug: string) => `/api/v1/travels/slug/${encodeURIComponent(slug)}`,
    REVIEWS: (travelId: number | string) => `/api/v1/travels/${travelId}/reviews`,
    FAQS: (travelId: number | string) => `/api/v1/travels/${travelId}/faqs`,
  },

  // 3. Phân hệ Grand Slam Offers
  OFFERS: {
    BASE: '/api/v1/offers',
    BY_ID: (id: number | string) => `/api/v1/offers/${id}`,
  },

  // 4. Phân hệ Quản Trị Vận Hành (Admin Operations)
  ADMIN: {
    CONTACTS: {
      BASE: '/api/v1/admin/contacts',
      BY_ID: (id: number | string) => `/api/v1/admin/contacts/${id}`,
    },
    ORDERS: {
      BASE: '/api/v1/admin/orders',
      BY_ID: (id: number | string) => `/api/v1/admin/orders/${id}`,
      DEPOSIT: (id: number | string) => `/api/v1/admin/orders/${id}/deposit`,
      VISA: (id: number | string) => `/api/v1/admin/orders/${id}/visa`,
      FINAL_PAYMENT: (id: number | string) => `/api/v1/admin/orders/${id}/final-payment`,
      T3_NOTIFIED: (id: number | string) => `/api/v1/admin/orders/${id}/t3-notified`,
      ZALO_TEMPLATE: (id: number | string, type: string) => `/api/v1/admin/orders/${id}/zalo-template/${type}`,
    },
    ESCALATIONS: {
      BASE: '/api/v1/admin/escalations',
      BY_ID: (id: number | string) => `/api/v1/admin/escalations/${id}`,
      ACTION: (id: number | string, action: string) => `/api/v1/admin/escalations/${id}/${action}`,
      STATUS: (id: number | string) => `/api/v1/admin/escalations/${id}/status`,
    },
    LLM_SETTINGS: {
      BASE: '/api/v1/llm-setting',
      TEST: '/api/v1/llm-setting/test',
    },
  },

  // 5. Phân hệ Webhooks (Inbound Integrations)
  WEBHOOKS: {
    TELEGRAM: '/api/v1/webhooks/telegram',
    TELEGRAM_STATUS: '/api/v1/webhooks/telegram/status',
  },
} as const;

export type ApiRoutesType = typeof API_ROUTES;
