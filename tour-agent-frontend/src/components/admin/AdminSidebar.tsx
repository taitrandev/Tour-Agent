import React from 'react';
import { Link, useLocation } from 'react-router-dom';

interface AdminSidebarProps {
  onOpenAgentDrawer?: () => void;
}

/**
 * Sidebar Điều Hướng Chuẩn Dùng Chung Cho Toàn Bộ Phân Hệ Quản Trị (Admin Portal).
 * Tự động nhận diện đường dẫn (useLocation) để kích hoạt trạng thái Active tương ứng.
 */
export const AdminSidebar: React.FC<AdminSidebarProps> = ({ onOpenAgentDrawer }) => {
  const location = useLocation();
  const path = location.pathname;

  // Kiểm tra đường dẫn đang kích hoạt
  const isOffersActive = path.startsWith('/admin/offers');
  const isProductsActive = path.startsWith('/admin/products');
  const isEscalationsActive = path.startsWith('/admin/escalations');
  const isContactsActive = path.startsWith('/admin/contacts');
  const isOrdersActive = path.startsWith('/admin/orders');
  const isLlmActive = path.startsWith('/admin/settings/llm') || path.startsWith('/admin/llm');
  const isTelegramActive = path.startsWith('/admin/channels/telegram') || path.startsWith('/admin/telegram');
  const isConversationsActive = path.startsWith('/admin/conversations');

  return (
    <aside className="w-full lg:w-64 bg-[#0e1422] border-b lg:border-b-0 lg:border-r border-[#222f49] p-4 space-y-6 shrink-0">
      <div className="space-y-1">
        <div className="text-[11px] font-bold text-slate-400 uppercase tracking-wider px-3 mb-2">
          Trụ cột Nghiệp Vụ
        </div>

        {/* 1. Quản lý Offers */}
        <Link
          to="/admin/offers"
          className={`w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl font-semibold text-xs transition text-left ${
            isOffersActive
              ? 'bg-orange-500/10 border border-orange-500/30 text-orange-400'
              : 'text-slate-400 hover:bg-slate-800/60 hover:text-slate-200 border border-transparent'
          }`}
        >
          <span className="text-base">💎</span>
          <span className="flex-1">Quản lý Offers (Grand Slam)</span>
        </Link>

        {/* 2. Quản lý Sản Phẩm (Tours) */}
        <Link
          to="/admin/products"
          className={`w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl font-semibold text-xs transition text-left ${
            isProductsActive
              ? 'bg-blue-500/10 border border-blue-500/30 text-blue-400'
              : 'text-slate-400 hover:bg-slate-800/60 hover:text-slate-200 border border-transparent'
          }`}
        >
          <span className="text-base">📦</span>
          <span className="flex-1">Quản lý Sản Phẩm (Tours)</span>
          <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-blue-500/20 text-blue-300 font-bold">
            Catalog
          </span>
        </Link>

        {/* 3. Bàn Quyết Định (Escalations Desk) */}
        <Link
          to="/admin/escalations"
          className={`w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl font-semibold text-xs transition text-left ${
            isEscalationsActive
              ? 'bg-amber-500/15 border border-amber-500/40 text-amber-300'
              : 'text-amber-400/80 hover:bg-amber-500/10 hover:text-amber-300 border border-amber-500/20'
          }`}
        >
          <span className="text-base">🛎️</span>
          <span className="flex-1">Bàn Quyết Định</span>
          <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-300 font-bold">
            Deal Desk
          </span>
        </Link>

        {/* 4. Quản Lý Khách Hàng (CRM) */}
        <Link
          to="/admin/contacts"
          className={`w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl font-semibold text-xs transition text-left ${
            isContactsActive
              ? 'bg-teal-500/15 border border-teal-500/40 text-teal-300'
              : 'text-teal-400/80 hover:bg-teal-500/10 hover:text-teal-300 border border-teal-500/20'
          }`}
        >
          <span className="text-base">👥</span>
          <span className="flex-1">Quản Lý Khách Hàng (CRM)</span>
          <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-teal-500/20 text-teal-300 font-bold">
            Leads
          </span>
        </Link>

        {/* 5. Đơn Tour & Visa */}
        <Link
          to="/admin/orders"
          className={`w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl font-semibold text-xs transition text-left ${
            isOrdersActive
              ? 'bg-sky-500/15 border border-sky-500/40 text-sky-300'
              : 'text-sky-400/80 hover:bg-sky-500/10 hover:text-sky-300 border border-sky-500/20'
          }`}
        >
          <span className="text-base">📦</span>
          <span className="flex-1">Đơn Tour & Visa</span>
          <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-sky-500/20 text-sky-300 font-bold">
            Pipeline
          </span>
        </Link>

        {/* 6. AI Agent Commander */}
        {onOpenAgentDrawer ? (
          <button
            type="button"
            onClick={onOpenAgentDrawer}
            className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-emerald-400 hover:bg-emerald-500/10 border border-emerald-500/20 font-semibold text-xs transition text-left"
          >
            <span className="text-base">🤖</span>
            <span className="flex-1">AI Agent Commander</span>
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
          </button>
        ) : (
          <Link
            to="/admin/conversations"
            className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-emerald-400 hover:bg-emerald-500/10 border border-emerald-500/20 font-semibold text-xs transition text-left"
          >
            <span className="text-base">🤖</span>
            <span className="flex-1">AI Agent Commander</span>
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
          </Link>
        )}

        {/* 7. Cấu Hình LLM Gateway */}
        <Link
          to="/admin/settings/llm"
          className={`w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl font-semibold text-xs transition text-left ${
            isLlmActive
              ? 'bg-purple-500/15 border border-purple-500/40 text-purple-300'
              : 'text-indigo-400/80 hover:bg-indigo-500/10 hover:text-indigo-300 border border-indigo-500/20'
          }`}
        >
          <span className="text-base">⚙️</span>
          <span className="flex-1">Cấu Hình LLM Gateway</span>
          <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-indigo-500/20 text-indigo-300 font-bold">
            Gateway
          </span>
        </Link>

        {/* 8. Quản Trị Kênh Telegram */}
        <Link
          to="/admin/channels/telegram"
          className={`w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl font-semibold text-xs transition text-left ${
            isTelegramActive
              ? 'bg-sky-500/15 border border-sky-500/40 text-sky-300'
              : 'text-sky-400/80 hover:bg-sky-500/10 hover:text-sky-300 border border-sky-500/20'
          }`}
        >
          <span className="text-base">📱</span>
          <span className="flex-1">Quản Trị Kênh Telegram</span>
          <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 border border-slate-700">
            Bot Hub
          </span>
        </Link>

        {/* 9. Giám Sát Hội Thoại & LLM */}
        <Link
          to="/admin/conversations"
          className={`w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl font-semibold text-xs transition text-left ${
            isConversationsActive
              ? 'bg-purple-500/15 border border-purple-500/40 text-purple-300'
              : 'text-purple-400/80 hover:bg-purple-500/10 hover:text-purple-300 border border-purple-500/20'
          }`}
        >
          <span className="text-base">💬</span>
          <span className="flex-1">Giám Sát Hội Thoại & LLM</span>
          <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-purple-500/20 text-purple-300 font-bold">
            Live
          </span>
        </Link>

        {/* 10. API Product Catalog */}
        <a
          href="/api/v1/travels"
          target="_blank"
          rel="noreferrer"
          className="flex items-center space-x-3 px-3 py-2.5 rounded-xl text-slate-400 hover:bg-slate-800/60 hover:text-slate-200 text-xs transition"
        >
          <span className="text-base">🗺️</span>
          <span className="flex-1">API Product Catalog</span>
          <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 border border-slate-700">
            REST
          </span>
        </a>
      </div>

      {/* Framework Architecture Card */}
      <div className="p-3.5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1.5 text-xs">
        <div className="font-bold text-slate-200 flex items-center gap-1.5">
          <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
          <span>Core AI Architecture</span>
        </div>
        <p className="text-[11px] text-slate-400 leading-relaxed">
          .NET 10 Clean Architecture • CQRS • Postgres • Claude & DeepSeek Gateway
        </p>
      </div>
    </aside>
  );
};
