import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Bot } from 'lucide-react';
import { AdminSidebar } from './AdminSidebar';
import { AdminCommanderDrawer } from './AdminCommanderDrawer';

interface AdminLayoutProps {
  title?: string;
  subtitle?: string;
  badge?: string;
  icon?: string;
  headerActions?: React.ReactNode;
  children: React.ReactNode;
  contentClassName?: string;
}

/**
 * Khung Giao Diện Quản Trị Chuẩn Nhất Quán (Unified Admin Layout).
 * Duy trì thanh Header và thanh Sidebar bên trái cố định xuyên suốt tất cả các trang,
 * ngăn chặn hoàn toàn hiện tượng layout bị giật, nhảy trang hay mất sidebar khi chuyển menu.
 */
export const AdminLayout: React.FC<AdminLayoutProps> = ({
  title = 'AI Agent Travel — Quản Trị Hệ Thống',
  subtitle = 'Catalog Base & 4 Bộ Chính Sách Cam Kết',
  badge = 'Backend Core .NET 10',
  icon = '💎',
  headerActions,
  children,
  contentClassName,
}) => {
  // State quản lý mở/đóng AI Agent Commander toàn cục
  const [isCommanderOpen, setIsCommanderOpen] = useState(false);

  useEffect(() => {
    const handleOpenEvent = () => setIsCommanderOpen(true);
    window.addEventListener('open-agent-commander', handleOpenEvent);
    return () => window.removeEventListener('open-agent-commander', handleOpenEvent);
  }, []);

  const handleCloseCommander = () => setIsCommanderOpen(false);

  return (
    <div className="h-screen bg-[#0b0f19] text-slate-100 flex flex-col font-sans overflow-hidden relative">
      {/* ── TOP UNIFIED HEADER ── */}
      <header className="h-16 border-b border-[#222f49] bg-[#0e1422]/90 backdrop-blur px-6 flex items-center justify-between shrink-0 z-20 sticky top-0 shadow-md">
        {/* Left: Brand & Page Subtitle */}
        <div className="flex items-center space-x-4">
          <Link to="/admin" className="flex items-center space-x-3 group">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-orange-500 to-rose-500 flex items-center justify-center shadow-lg shadow-orange-500/20 font-bold text-white text-lg group-hover:scale-105 transition-transform">
              {icon}
            </div>
            <div>
              <span className="font-extrabold text-white text-base tracking-tight flex items-center gap-2">
                {title}
                {badge && (
                  <span className="text-[10px] font-bold uppercase px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                    {badge}
                  </span>
                )}
              </span>
              <p className="text-xs text-slate-400">{subtitle}</p>
            </div>
          </Link>
        </div>

        {/* Right: Quick Action Nav Pills & Page Actions */}
        <div className="flex items-center space-x-2.5 overflow-x-auto py-1">
          <Link
            to="/admin/escalations"
            className="px-3 py-1.5 rounded-xl bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 font-semibold text-xs border border-amber-500/30 transition flex items-center gap-1.5 shadow-sm shadow-amber-500/10 shrink-0"
          >
            <span>🛎️ Bàn Quyết Định</span>
          </Link>

          <Link
            to="/admin/contacts"
            className="px-3 py-1.5 rounded-xl bg-teal-500/20 hover:bg-teal-500/30 text-teal-300 font-semibold text-xs border border-teal-500/30 transition flex items-center gap-1.5 shadow-sm shadow-teal-500/10 shrink-0"
          >
            <span>👥 Khách Hàng (CRM)</span>
          </Link>

          <Link
            to="/admin/orders"
            className="px-3 py-1.5 rounded-xl bg-sky-500/20 hover:bg-sky-500/30 text-sky-300 font-semibold text-xs border border-sky-500/30 transition flex items-center gap-1.5 shadow-sm shadow-sky-500/10 shrink-0"
          >
            <span>📦 Đơn Tour & Visa</span>
          </Link>

          <Link
            to="/admin/settings/llm"
            className="px-3 py-1.5 rounded-xl bg-purple-500/20 hover:bg-purple-500/30 text-purple-300 font-semibold text-xs border border-purple-500/30 transition flex items-center gap-1.5 shadow-sm shadow-purple-500/10 shrink-0"
          >
            <span>⚙️ Cấu Hình LLM</span>
          </Link>

          <Link
            to="/admin/conversations"
            className="px-3 py-1.5 rounded-xl bg-purple-500/20 hover:bg-purple-500/30 text-purple-300 font-semibold text-xs border border-purple-500/30 transition flex items-center gap-1.5 shrink-0"
          >
            <span>💬 Giám Sát Hội Thoại</span>
          </Link>

          <Link
            to="/admin/channels/telegram"
            className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-semibold text-xs border border-[#222f49] transition flex items-center gap-1.5 shrink-0"
          >
            <span>📱 Telegram</span>
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
          </Link>

          {headerActions}
        </div>
      </header>

      {/* ── MAIN WORKSPACE CONTAINER ── */}
      <div className="flex-1 flex flex-col lg:flex-row min-w-0 overflow-hidden">
        {/* Shared Persistent Sidebar */}
        <AdminSidebar />

        {/* Content Area */}
        <div className={`flex-1 flex flex-col min-w-0 h-full ${contentClassName ?? 'overflow-y-auto'}`}>
          {children}
        </div>
      </div>

      {/* ── GLOBAL AI AGENT COMMANDER COPILOT DRAWER ── */}
      <AdminCommanderDrawer isOpen={isCommanderOpen} onClose={handleCloseCommander} />

      {/* Floating Action Button mở nhanh AI Commander ở góc phải dưới */}
      <button
        type="button"
        onClick={() => setIsCommanderOpen((prev) => !prev)}
        className="fixed bottom-6 right-6 z-40 w-13 h-13 rounded-2xl bg-gradient-to-tr from-emerald-500 to-teal-500 hover:scale-105 active:scale-95 text-white flex items-center justify-center shadow-xl shadow-emerald-500/30 border border-emerald-400/40 transition-all group"
        title="Mở AI Agent Commander"
      >
        <Bot className="w-6 h-6 group-hover:rotate-12 transition-transform" />
        <span className="absolute -top-1 -right-1 w-3.5 h-3.5 rounded-full bg-emerald-400 border-2 border-[#0b0f19] animate-pulse" />
      </button>
    </div>
  );
};

