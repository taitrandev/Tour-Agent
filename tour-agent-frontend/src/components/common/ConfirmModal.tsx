import React, { createContext, useContext, useState, useCallback, useRef } from 'react';
import { AlertCircle, AlertTriangle, Info, X } from 'lucide-react';

/**
 * Cấu hình tùy chọn cho hộp thoại xác nhận (Confirmation Dialog Options).
 */
export interface ConfirmOptions {
  /** Tiêu đề hộp thoại */
  title?: string;
  /** Nội dung thông điệp cần người dùng xác nhận */
  message: string;
  /** Nhãn nút đồng ý (Mặc định: 'Xác nhận') */
  confirmText?: string;
  /** Nhãn nút hủy bỏ (Mặc định: 'Hủy bỏ') */
  cancelText?: string;
  /** Mức độ cảnh báo: danger (nguy hiểm/xóa), warning (cảnh báo), info (thông tin) */
  variant?: 'danger' | 'warning' | 'info';
}

interface ConfirmContextType {
  /**
   * Gọi hộp thoại xác nhận dạng Promise.
   * Trả về true nếu người dùng bấm Xác nhận, false nếu Hủy hoặc click ra ngoài.
   */
  confirm: (options: ConfirmOptions | string) => Promise<boolean>;
}

const ConfirmContext = createContext<ConfirmContextType | null>(null);

/**
 * Provider quản lý trạng thái modal xác nhận dùng chung trên toàn hệ thống.
 * Đặt tại App.tsx bọc lấy toàn bộ Router.
 */
export const ConfirmProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isOpen, setIsOpen] = useState(false);
  const [options, setOptions] = useState<ConfirmOptions>({
    title: 'Xác nhận thao tác',
    message: '',
    confirmText: 'Xác nhận',
    cancelText: 'Hủy bỏ',
    variant: 'danger',
  });

  const resolverRef = useRef<((value: boolean) => void) | null>(null);

  const confirm = useCallback((opts: ConfirmOptions | string): Promise<boolean> => {
    return new Promise<boolean>((resolve) => {
      resolverRef.current = resolve;
      if (typeof opts === 'string') {
        setOptions({
          title: 'Xác nhận thao tác',
          message: opts,
          confirmText: 'Xác nhận',
          cancelText: 'Hủy bỏ',
          variant: 'danger',
        });
      } else {
        setOptions({
          title: opts.title || 'Xác nhận thao tác',
          message: opts.message,
          confirmText: opts.confirmText || 'Xác nhận',
          cancelText: opts.cancelText || 'Hủy bỏ',
          variant: opts.variant || 'danger',
        });
      }
      setIsOpen(true);
    });
  }, []);

  const handleConfirm = () => {
    setIsOpen(false);
    if (resolverRef.current) {
      resolverRef.current(true);
      resolverRef.current = null;
    }
  };

  const handleCancel = () => {
    setIsOpen(false);
    if (resolverRef.current) {
      resolverRef.current(false);
      resolverRef.current = null;
    }
  };

  const variant = options.variant || 'danger';

  return (
    <ConfirmContext.Provider value={{ confirm }}>
      {children}

      {isOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm animate-in fade-in duration-200"
          onClick={handleCancel}
        >
          <div
            className="w-full max-w-md rounded-2xl bg-[#111827] border border-[#1f2937] shadow-2xl p-6 space-y-4 text-slate-100 transform transition-all animate-in zoom-in-95 duration-200"
            onClick={(e) => e.stopPropagation()}
            role="dialog"
            aria-modal="true"
          >
            {/* Header with Icon, Title and Close Button */}
            <div className="flex items-start gap-3.5">
              <div
                className={`w-10 h-10 rounded-xl flex items-center justify-center shrink-0 border ${
                  variant === 'danger'
                    ? 'bg-rose-500/15 border-rose-500/30 text-rose-400'
                    : variant === 'warning'
                    ? 'bg-amber-500/15 border-amber-500/30 text-amber-400'
                    : 'bg-sky-500/15 border-sky-500/30 text-sky-400'
                }`}
              >
                {variant === 'danger' && <AlertCircle className="w-5 h-5" />}
                {variant === 'warning' && <AlertTriangle className="w-5 h-5" />}
                {variant === 'info' && <Info className="w-5 h-5" />}
              </div>

              <div className="flex-1 min-w-0">
                <h3 className="text-sm font-bold text-white tracking-tight">
                  {options.title}
                </h3>
                <p className="text-xs text-slate-400 mt-1 leading-relaxed whitespace-pre-wrap">
                  {options.message}
                </p>
              </div>

              <button
                onClick={handleCancel}
                className="text-slate-500 hover:text-slate-300 p-1 rounded-lg hover:bg-slate-800 transition -mr-1 -mt-1"
                aria-label="Đóng"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Actions Footer */}
            <div className="flex items-center justify-end gap-2.5 pt-3 border-t border-[#1f2937]">
              <button
                type="button"
                onClick={handleCancel}
                className="px-4 py-2 rounded-xl text-xs font-semibold bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 transition"
              >
                {options.cancelText}
              </button>
              <button
                type="button"
                autoFocus
                onClick={handleConfirm}
                className={`px-4 py-2 rounded-xl text-xs font-bold transition shadow-lg ${
                  variant === 'danger'
                    ? 'bg-rose-600 hover:bg-rose-500 text-white shadow-rose-600/30 active:scale-95'
                    : variant === 'warning'
                    ? 'bg-amber-500 hover:bg-amber-400 text-slate-950 shadow-amber-500/30 active:scale-95'
                    : 'bg-sky-600 hover:bg-sky-500 text-white shadow-sky-600/30 active:scale-95'
                }`}
              >
                {options.confirmText}
              </button>
            </div>
          </div>
        </div>
      )}
    </ConfirmContext.Provider>
  );
};

/**
 * Hook sử dụng modal xác nhận ở bất kỳ component nào trong cây React.
 * @example
 * const confirm = useConfirm();
 * const ok = await confirm({
 *   title: 'Xóa Tour',
 *   message: 'Bạn có chắc chắn muốn xóa tour này?',
 *   confirmText: 'Xóa vĩnh viễn',
 *   variant: 'danger',
 * });
 * if (ok) { ... }
 */
export const useConfirm = () => {
  const context = useContext(ConfirmContext);
  if (!context) {
    throw new Error('useConfirm must be used within a ConfirmProvider');
  }
  return context.confirm;
};

