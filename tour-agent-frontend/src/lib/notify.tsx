import React from 'react';
import { toast, Toaster } from 'sonner';

/**
 * Tùy chọn cấu hình thông báo độc lập của riêng dự án (Project-owned Types).
 * Tuyệt đối không tái sử dụng hay để lộ type trực tiếp của thư viện bên ngoài (Tránh Leaky Abstraction).
 */
export interface NotifyOptions {
  /** Chi tiết mô tả bổ sung bên dưới tiêu đề thông báo */
  description?: string;
  /** Thời gian hiển thị tính bằng mili-giây (Mặc định: 3000ms) */
  duration?: number;
  /** Nút hành động tương tác nhanh nếu có */
  action?: {
    label: string;
    onClick: () => void;
  };
}

/**
 * Lớp trừu tượng thông báo (Facade / Adapter Pattern).
 * Đây là điểm truy cập DUY NHẤT cho toàn bộ thông báo (Toast) trong toàn bộ dự án.
 * Nếu sau này thay đổi thư viện UI, chỉ cần cập nhật bên trong file này.
 */
export const notify = {
  success: (message: string, options?: NotifyOptions) => {
    return toast.success(message, options);
  },
  error: (message: string, options?: NotifyOptions) => {
    return toast.error(message, options);
  },
  warning: (message: string, options?: NotifyOptions) => {
    return toast.warning(message, options);
  },
  info: (message: string, options?: NotifyOptions) => {
    return toast.info(message, options);
  },
  /** Đóng thông báo cụ thể hoặc toàn bộ */
  dismiss: (id?: string | number) => {
    toast.dismiss(id);
  },
};

/**
 * Component hiển thị thông báo dùng chung của hệ thống.
 * Đóng gói toàn bộ cấu hình hiển thị (Vị trí, Dark Theme, Rich Colors) tại một nơi duy nhất.
 */
export const NotificationContainer: React.FC = () => {
  return (
    <Toaster
      position="top-right"
      richColors
      theme="dark"
      closeButton
      duration={3000}
    />
  );
};

