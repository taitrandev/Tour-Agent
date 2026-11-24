/**
 * Định nghĩa vai trò (Persona / Scope) của AI Agent trong toàn bộ ứng dụng frontend.
 * Tránh triệt để Magic String khi gọi API hoặc kiểm tra điều kiện.
 */
export const AGENT_ROLES = {
  CONSULTANT: 'consultant',
  COMMANDER: 'commander',
} as const;

export type AgentRole = typeof AGENT_ROLES[keyof typeof AGENT_ROLES];

/**
 * Định nghĩa danh sách đường dẫn API chuẩn xác của AI Agent.
 * Tách biệt giữa luồng Public Consultant và luồng Admin Copilot.
 */
export const AGENT_API_ROUTES = {
  CHAT: '/api/v1/agent/chat',
  CHAT_STREAM: '/api/v1/agent/chat/stream',
  ADMIN_CHAT: '/api/v1/agent/admin/chat',
  ADMIN_CHAT_STREAM: '/api/v1/agent/admin/chat/stream',
} as const;

/**
 * Danh sách tên các công cụ quản trị cập nhật dữ liệu (Write Tools).
 * Dùng để phát sự kiện làm mới (refresh) giao diện quản trị mà không dùng magic string.
 */
export const ADMIN_WRITE_TOOLS = [
  'update_travel_title',
  'update_offer_price',
] as const;

export type AdminWriteTool = typeof ADMIN_WRITE_TOOLS[number];

