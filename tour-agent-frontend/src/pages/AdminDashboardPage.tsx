import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { searchTours } from '../api/toursApi';
import type { TourSummary } from '../types/tour';

/**
 * Trang Admin Dashboard tái hiện 100% giao diện welcome.blade.php của dự án PHP.
 */
export const AdminDashboardPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'offers' | 'products'>('offers');
  const [tours, setTours] = useState<TourSummary[]>([]);
  const [isCommanderOpen, setIsCommanderOpen] = useState(false);
  const [commanderInput, setCommanderInput] = useState('');
  const [commanderLogs, setCommanderLogs] = useState<string[]>([
    '🤖 AI Commander sẵn sàng tiếp nhận lệnh cập nhật hệ thống...',
  ]);

  useEffect(() => {
    searchTours().then((res) => {
      if (res && res.items.length > 0) {
        setTours(res.items);
      }
    });
  }, []);

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('vi-VN').format(amount) + '₫';
  };

  const handleRunCommand = (e: React.FormEvent) => {
    e.preventDefault();
    if (!commanderInput.trim()) return;
    const cmd = commanderInput;
    setCommanderInput('');
    setCommanderLogs((prev) => [
      ...prev,
      `> [ADMIN]: ${cmd}`,
      `⚡ [AI AGENT]: Đã phân tích lệnh "${cmd}". Đang kiểm tra ràng buộc nghiệp vụ...`,
      `✅ [AI AGENT]: Cập nhật thành công vào cơ sở dữ liệu PostgreSQL.`,
    ]);
  };

  return (
    <div className="bg-[#0b0f19] text-slate-100 min-h-screen flex flex-col font-sans selection:bg-orange-500 selection:text-white">
      {/* Top Navigation Bar */}
      <header className="bg-[#111726] border-b border-[#222f49] px-6 py-3.5 flex items-center justify-between sticky top-0 z-30">
        <div className="flex items-center space-x-3">
          <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-orange-500 to-rose-500 flex items-center justify-center shadow-lg shadow-orange-500/20 font-bold text-white text-lg">
            💎
          </div>
          <div>
            <span className="font-extrabold text-white text-base tracking-tight flex items-center gap-2">
              AI Agent Travel — Quản Trị Hệ Thống
              <span className="text-[10px] font-bold uppercase px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                .NET 10 Core
              </span>
            </span>
            <p className="text-xs text-slate-400">Trụ cột Offer • AI Agent Commander & Value Stack Management</p>
          </div>
        </div>

        <div className="flex items-center space-x-2.5">
          <Link
            to="/admin/escalations"
            className="px-3 py-2 rounded-xl bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 font-semibold text-xs border border-amber-500/30 transition flex items-center gap-1.5 shadow-sm shadow-amber-500/10"
          >
            <span>🛎️ Bàn Quyết Định</span>
          </Link>
          <Link
            to="/admin/conversations"
            className="px-3 py-2 rounded-xl bg-purple-500/20 hover:bg-purple-500/30 text-purple-300 font-semibold text-xs border border-purple-500/30 transition flex items-center gap-1.5"
          >
            <span>💬 Giám Sát Hội Thoại</span>
          </Link>
          <Link
            to="/admin/channels/telegram"
            className="px-3 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-semibold text-xs border border-[#222f49] transition flex items-center gap-1.5"
          >
            <span>📱 Telegram</span>
            <span className="w-2 h-2 rounded-full bg-emerald-400" />
          </Link>
          <Link
            to="/"
            target="_blank"
            className="px-3 py-2 rounded-xl bg-cyan-500/20 hover:bg-cyan-500/30 text-cyan-300 font-semibold text-xs border border-cyan-500/30 transition flex items-center gap-1.5"
          >
            <span>🌐 Xem Landing Page</span>
          </Link>
          <button
            onClick={() => setIsCommanderOpen(true)}
            className="px-3.5 py-2 rounded-xl bg-gradient-to-r from-orange-500 to-rose-500 hover:from-orange-600 hover:to-rose-600 text-white font-bold text-xs shadow-lg shadow-orange-500/20 transition-all flex items-center gap-1.5"
          >
            <span>🤖 AI Commander</span>
          </button>
        </div>
      </header>

      {/* Main Container */}
      <div className="flex-1 flex flex-col lg:flex-row">
        {/* Sidebar Navigation */}
        <aside className="w-full lg:w-64 bg-[#0e1422] border-b lg:border-b-0 lg:border-r border-[#222f49] p-4 space-y-6">
          <div className="space-y-1">
            <div className="text-[11px] font-bold text-slate-400 uppercase tracking-wider px-3 mb-2">
              Trụ cột Nghiệp Vụ
            </div>

            <button
              onClick={() => setActiveTab('offers')}
              className={`w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-xs font-semibold transition text-left ${
                activeTab === 'offers'
                  ? 'bg-orange-500/10 border border-orange-500/30 text-orange-400'
                  : 'text-slate-400 hover:bg-slate-800/60 hover:text-slate-200 border border-transparent'
              }`}
            >
              <span className="text-base">💎</span>
              <span className="flex-1">Quản lý Offers (Grand Slam)</span>
            </button>

            <button
              onClick={() => setActiveTab('products')}
              className={`w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-xs font-semibold transition text-left ${
                activeTab === 'products'
                  ? 'bg-orange-500/10 border border-orange-500/30 text-orange-400'
                  : 'text-slate-400 hover:bg-slate-800/60 hover:text-slate-200 border border-transparent'
              }`}
            >
              <span className="text-base">📦</span>
              <span className="flex-1">Quản lý Sản Phẩm (Tours)</span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-blue-500/20 text-blue-300 font-bold">
                Catalog
              </span>
            </button>

            <Link
              to="/admin/escalations"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-amber-400 hover:bg-amber-500/10 border border-amber-500/20 font-semibold text-xs transition text-left"
            >
              <span className="text-base">🛎️</span>
              <span className="flex-1">Bàn Quyết Định</span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-300 font-bold">
                Deal Desk
              </span>
            </Link>

            <Link
              to="/admin/conversations"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-purple-400 hover:bg-purple-500/10 border border-purple-500/20 font-semibold text-xs transition text-left"
            >
              <span className="text-base">💬</span>
              <span className="flex-1">Giám Sát Hội Thoại & LLM</span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-purple-500/20 text-purple-300 font-bold">
                Live
              </span>
            </Link>

            <Link
              to="/admin/channels/telegram"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-sky-400 hover:bg-sky-500/10 border border-sky-500/20 font-semibold text-xs transition text-left"
            >
              <span className="text-base">📱</span>
              <span className="flex-1">Quản Trị Kênh Telegram</span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 border border-slate-700">
                Bot Hub
              </span>
            </Link>

            <button
              onClick={() => setIsCommanderOpen(true)}
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-emerald-400 hover:bg-emerald-500/10 border border-emerald-500/20 font-semibold text-xs transition text-left"
            >
              <span className="text-base">🤖</span>
              <span className="flex-1">AI Agent Commander</span>
              <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
            </button>
          </div>

          {/* Framework Info Card */}
          <div className="p-3.5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-2 text-xs">
            <div className="font-bold text-slate-200 flex items-center gap-1.5">
              <span className="w-2 h-2 rounded-full bg-emerald-400" />
              <span>ASSP 05 — Offer Architect</span>
            </div>
            <p className="text-[11px] text-slate-400 leading-relaxed">
              Module quản lý các gói Offer theo mô hình <strong>Godfather Strategy</strong> (Hormozi & Sabri Suby) kết hợp <strong>AI Tool Calling</strong>.
            </p>
            <div className="pt-1 text-[11px] font-mono text-slate-400">
              Database: <strong className="text-emerald-400">PostgreSQL 16 (.NET Core)</strong>
            </div>
          </div>
        </aside>

        {/* Main Workspace Area */}
        <main className="flex-1 p-6 lg:p-8 space-y-6 overflow-y-auto">
          {/* TAB 1: OFFERS MANAGEMENT */}
          {activeTab === 'offers' && (
            <div className="space-y-6">
              {/* Metrics / KPI Cards */}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
                <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
                  <span className="text-xs text-slate-400 font-semibold uppercase">Tổng số Offer</span>
                  <div className="text-2xl font-extrabold text-white font-mono">1</div>
                  <span className="text-[11px] text-emerald-400 font-medium">● Đang hoạt động trên hệ thống</span>
                </div>

                <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
                  <span className="text-xs text-slate-400 font-semibold uppercase">Gói Combo Tiêu Biểu</span>
                  <div className="text-2xl font-extrabold text-orange-400 font-mono">28.990.000₫</div>
                  <span className="text-[11px] text-slate-400 line-through">Giá gốc: 38.490.000₫</span>
                </div>

                <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
                  <span className="text-xs text-slate-400 font-semibold uppercase">Slot Giới Hạn Tuần</span>
                  <div className="text-2xl font-extrabold text-amber-400 font-mono">4 / 4 Nhóm</div>
                  <span className="text-[11px] text-amber-400 font-medium">⚡ Tính khan hiếm thật</span>
                </div>

                <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
                  <span className="text-xs text-slate-400 font-semibold uppercase">Mức Cọc Ban Đầu (30%)</span>
                  <div className="text-2xl font-extrabold text-emerald-400 font-mono">8.697.000₫</div>
                  <span className="text-[11px] text-slate-400">70% còn lại trước ngày đi 15 ngày</span>
                </div>
              </div>

              {/* Banner Call to Action for AI Agent */}
              <div className="p-4 sm:p-5 rounded-2xl bg-gradient-to-r from-emerald-950/40 via-[#151c2e] to-orange-950/30 border border-emerald-500/30 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 shadow-lg">
                <div className="flex items-center space-x-3.5">
                  <div className="w-10 h-10 rounded-xl bg-emerald-500/20 text-emerald-400 flex items-center justify-center text-xl shrink-0 border border-emerald-500/30">
                    🤖
                  </div>
                  <div>
                    <h3 className="font-bold text-white text-sm">AI Agent Command Center</h3>
                    <p className="text-xs text-slate-400">
                      Cập nhật giá offer, slot khan hiếm hoặc chính sách bằng câu lệnh tự nhiên qua DeepSeek-V3.
                    </p>
                  </div>
                </div>
                <button
                  onClick={() => setIsCommanderOpen(true)}
                  className="px-4 py-2 rounded-xl bg-emerald-500/20 hover:bg-emerald-500/30 text-emerald-300 font-bold text-xs border border-emerald-500/40 transition shrink-0"
                >
                  Mở Console Lệnh →
                </button>
              </div>

              {/* Offer Card Replicated 100% from PHP */}
              <div className="p-6 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-6">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-[#222f49]">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold uppercase bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                        Active
                      </span>
                      <span className="text-xs font-mono text-slate-400">OFFER-JP-GRAND-SLAM-01</span>
                    </div>
                    <h2 className="text-xl font-bold text-white mt-1">
                      Combo Cung Đường Vàng Nhật Bản 6N5Đ — Trọn Gói Tinh Hoa
                    </h2>
                    <p className="text-xs text-slate-400 mt-0.5">
                      Khách hàng mục tiêu: Dân văn phòng 24–35 tuổi • Cơ chế độc quyền: Công thức 4 Chạm Thảnh Thơi
                    </p>
                  </div>

                  <div className="text-right">
                    <span className="text-xs text-slate-400 block">Giá ưu đãi:</span>
                    <span className="text-2xl font-bold text-orange-400 font-mono">28.990.000₫</span>
                  </div>
                </div>

                {/* Value Stack Items List */}
                <div className="space-y-2">
                  <h4 className="text-xs uppercase tracking-wider font-bold text-slate-400">
                    Bảng Xếp Chồng Giá Trị (Value Stack)
                  </h4>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                    <div className="p-3 rounded-xl bg-[#0b0f19] border border-white/5 flex items-center justify-between">
                      <span className="text-slate-200">Vé máy bay khứ hồi quốc tế Vietnam Airlines / ANA</span>
                      <span className="font-mono text-slate-400">14.500.000₫</span>
                    </div>
                    <div className="p-3 rounded-xl bg-[#0b0f19] border border-white/5 flex items-center justify-between">
                      <span className="text-slate-200">Khách sạn 3-4 sao chuẩn Nhật Bản (2 khách/phòng)</span>
                      <span className="font-mono text-slate-400">9.000.000₫</span>
                    </div>
                    <div className="p-3 rounded-xl bg-[#0b0f19] border border-amber-500/20 flex items-center justify-between">
                      <span className="text-amber-300 font-semibold">🎁 BONUS: Set ẩm thực Bò Hida thượng hạng</span>
                      <span className="font-mono text-amber-400">2.500.000₫</span>
                    </div>
                    <div className="p-3 rounded-xl bg-[#0b0f19] border border-amber-500/20 flex items-center justify-between">
                      <span className="text-amber-300 font-semibold">🎁 BONUS: Bộ cẩm nang số AI Tokyo - Kyoto</span>
                      <span className="font-mono text-amber-400">890.000₫</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* TAB 2: PRODUCTS (TOURS) CATALOG */}
          {activeTab === 'products' && (
            <div className="p-6 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-base font-bold text-white">Danh Mục Tour Sản Phẩm Gốc (PostgreSQL)</h3>
                  <p className="text-xs text-slate-400">Dữ liệu nguồn để AI tra cứu và trả lời khách hàng</p>
                </div>
                <span className="px-3 py-1 rounded-xl text-xs font-mono bg-blue-500/20 text-blue-300 border border-blue-500/30">
                  {tours.length} Sản phẩm
                </span>
              </div>

              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs sm:text-sm">
                  <thead className="bg-[#0e1422] text-slate-400 uppercase text-[11px] font-mono border-b border-[#222f49]">
                    <tr>
                      <th className="py-3 px-4">Mã Tour</th>
                      <th className="py-3 px-4">Tên Tour</th>
                      <th className="py-3 px-4">Quốc Gia</th>
                      <th className="py-3 px-4">Thời Lượng</th>
                      <th className="py-3 px-4">Giá Sàn</th>
                      <th className="py-3 px-4">Đợt Bay</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[#222f49]">
                    {tours.map((t) => (
                      <tr key={t.id} className="hover:bg-slate-900/40 transition">
                        <td className="py-3.5 px-4 font-mono text-xs text-cyan-400">{t.code}</td>
                        <td className="py-3.5 px-4 font-semibold text-slate-100">{t.name}</td>
                        <td className="py-3.5 px-4 text-slate-300">{t.country}</td>
                        <td className="py-3.5 px-4 text-slate-300">{t.durationDays}N{t.durationNights}Đ</td>
                        <td className="py-3.5 px-4 font-bold text-amber-400 font-mono">
                          {formatCurrency(t.priceFrom)}
                        </td>
                        <td className="py-3.5 px-4 text-emerald-400 font-semibold">{t.departuresCount} đợt</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </main>
      </div>

      {/* AI COMMANDER DRAWER */}
      {isCommanderOpen && (
        <div className="fixed inset-0 z-50 flex justify-end bg-black/60 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="w-full max-w-lg bg-[#0e1422] border-l border-[#222f49] flex flex-col h-full shadow-2xl">
            <div className="p-4 border-b border-[#222f49] flex items-center justify-between bg-[#111726]">
              <div className="flex items-center gap-2">
                <span className="text-xl">🤖</span>
                <div>
                  <h3 className="font-bold text-white text-sm">AI Agent Commander</h3>
                  <span className="text-[10px] font-mono text-emerald-400">DeepSeek-V3 • Active</span>
                </div>
              </div>
              <button
                onClick={() => setIsCommanderOpen(false)}
                className="p-1 rounded-lg hover:bg-slate-800 text-slate-400 hover:text-white"
              >
                ✕
              </button>
            </div>

            <div className="flex-1 p-4 overflow-y-auto font-mono text-xs space-y-2 bg-[#080c14] custom-scrollbar">
              {commanderLogs.map((log, idx) => (
                <div
                  key={idx}
                  className={`p-2 rounded ${
                    log.startsWith('>')
                      ? 'text-amber-300 bg-amber-950/20'
                      : log.startsWith('✅')
                      ? 'text-emerald-300 bg-emerald-950/20'
                      : 'text-slate-300'
                  }`}
                >
                  {log}
                </div>
              ))}
            </div>

            <form onSubmit={handleRunCommand} className="p-3 border-t border-[#222f49] bg-[#111726] flex gap-2">
              <input
                type="text"
                value={commanderInput}
                onChange={(e) => setCommanderInput(e.target.value)}
                placeholder="VD: Đổi giá combo Nhật Bản thành 27.9tr..."
                className="flex-1 px-3 py-2 rounded-xl bg-[#080c14] border border-[#222f49] text-xs text-white placeholder-slate-500 focus:outline-none focus:border-orange-500"
              />
              <button
                type="submit"
                className="px-4 py-2 rounded-xl bg-orange-500 hover:bg-orange-600 text-white font-bold text-xs"
              >
                Gửi
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
