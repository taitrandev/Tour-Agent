import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';

interface EscalationTicket {
  id: number;
  code: string;
  customerName: string;
  customerPhone: string;
  channel: 'telegram' | 'web_widget';
  tourTitle: string;
  discountPercent: number;
  groupSize: number;
  notes: string;
  status: 'pending' | 'contacting' | 'approved' | 'closed';
  timeAgo: string;
  conversationId?: number;
  travelId?: number;
}

interface EscalationsResponse {
  items: EscalationTicket[];
  totalCount: number;
  pendingCount: number;
  contactingCount: number;
  approvedCount: number;
}

/**
/// <summary>
/// Trang Bàn Quyết Định (Human-in-the-Loop Escalations Desk).
/// Kết nối API thực tế /api/v1/admin/escalations để hiển thị các ticket cần xử lý từ AI Agent.
/// </summary>
*/
export const AdminEscalationsPage: React.FC = () => {
  const [tickets, setTickets] = useState<EscalationTicket[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [stats, setStats] = useState({
    total: 0,
    pending: 0,
    contacting: 0,
    approved: 0,
  });

  /**
   * Tải danh sách ticket từ API backend.
   */
  const fetchTickets = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await fetch('/api/v1/admin/escalations');
      if (!res.ok) {
        throw new Error(`Lỗi tải dữ liệu: ${res.statusText}`);
      }
      const data: EscalationsResponse = await res.json();
      setTickets(data.items);
      setStats({
        total: data.totalCount,
        pending: data.pendingCount,
        contacting: data.contactingCount,
        approved: data.approvedCount,
      });
    } catch (err: any) {
      console.error('Lỗi khi fetch escalations:', err);
      setError(err.message || 'Không thể tải danh sách ticket');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchTickets();
  }, [fetchTickets]);

  /**
   * Cập nhật trạng thái của ticket khi chuyên viên thao tác.
   */
  const updateStatus = async (id: number, action: 'contacting' | 'approve' | 'close') => {
    try {
      let endpoint = `/api/v1/admin/escalations/${id}/${action}`;
      const res = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({}),
      });

      if (!res.ok) {
        throw new Error(`Cập nhật trạng thái thất bại: ${res.statusText}`);
      }

      // Tải lại danh sách sau khi thao tác
      await fetchTickets();
    } catch (err: any) {
      alert(err.message || 'Lỗi cập nhật ticket');
    }
  };

  return (
    <div className="bg-[#0b0f19] text-slate-100 min-h-screen flex flex-col font-sans selection:bg-amber-500 selection:text-black">
      {/* Top Navigation Header */}
      <header className="h-16 border-b border-slate-800 bg-slate-900/80 backdrop-blur px-6 flex items-center justify-between shrink-0 z-20">
        <div className="flex items-center gap-4">
          <Link
            to="/admin"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-semibold border border-slate-700/80 transition shadow-sm"
          >
            <span>← Dashboard Quản Trị</span>
          </Link>

          <div className="h-6 w-px bg-slate-800" />

          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-amber-500 to-rose-500 flex items-center justify-center font-bold text-white text-base shadow-lg shadow-amber-500/20">
              🛎️
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-sm sm:text-base font-bold text-white tracking-tight">
                  Bàn Quyết Định (Human-in-the-Loop)
                </h1>
                <span className="text-[10px] font-semibold tracking-wider uppercase px-2 py-0.5 rounded-full bg-amber-500/20 text-amber-300 border border-amber-500/30">
                  Escalations Desk
                </span>
              </div>
              <p className="text-xs text-slate-400">
                Tiếp nhận khách nóng (Hot Lead), duyệt chiết khấu và gọi điện tư vấn trực tiếp
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={fetchTickets}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-semibold border border-slate-700/80 transition"
            title="Làm mới danh sách"
          >
            <span>🔄 Làm mới</span>
          </button>
          <Link
            to="/admin/channels/telegram"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-sky-500/15 hover:bg-sky-500/25 text-sky-300 text-xs font-semibold border border-sky-500/30 transition"
          >
            <span>📱 Kênh Telegram</span>
          </Link>
          <Link
            to="/admin/conversations"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-purple-500/15 hover:bg-purple-500/25 text-purple-300 text-xs font-semibold border border-purple-500/30 transition"
          >
            <span>💬 Giám Sát Chat</span>
          </Link>
        </div>
      </header>

      {/* Main Workspace */}
      <main className="flex-1 overflow-y-auto p-6 max-w-7xl w-full mx-auto space-y-6">
        {/* 4 KPI Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="p-5 rounded-2xl bg-[#131a2b] border border-[#222f49] space-y-1 shadow-lg">
            <span className="text-xs text-slate-400 font-semibold uppercase">Tổng Số Ticket</span>
            <div className="text-2xl font-extrabold text-white font-mono">{stats.total}</div>
            <span className="text-[11px] text-slate-500">Tất cả yêu cầu chuyển tiếp từ AI</span>
          </div>

          <div className="p-5 rounded-2xl bg-[#131a2b] border border-amber-500/30 space-y-1 shadow-lg relative overflow-hidden">
            <div className="flex items-center justify-between">
              <span className="text-xs text-amber-400 font-semibold uppercase">Chờ Tiếp Nhận</span>
              <span className="w-2.5 h-2.5 rounded-full bg-amber-400 glow-hot" />
            </div>
            <div className="text-2xl font-extrabold text-amber-400 font-mono">{stats.pending}</div>
            <span className="text-[11px] text-amber-300/80">Khách đang đợi phản hồi</span>
          </div>

          <div className="p-5 rounded-2xl bg-[#131a2b] border border-sky-500/30 space-y-1 shadow-lg relative overflow-hidden">
            <div className="flex items-center justify-between">
              <span className="text-xs text-sky-400 font-semibold uppercase">Đang Tư Vấn</span>
              <span className="w-2.5 h-2.5 rounded-full bg-sky-400 animate-pulse" />
            </div>
            <div className="text-2xl font-extrabold text-sky-400 font-mono">{stats.contacting}</div>
            <span className="text-[11px] text-sky-300/80">Sales đang gọi điện 1:1</span>
          </div>

          <div className="p-5 rounded-2xl bg-[#131a2b] border border-emerald-500/30 space-y-1 shadow-lg">
            <span className="text-xs text-emerald-400 font-semibold uppercase">Đã Duyệt Deal</span>
            <div className="text-2xl font-extrabold text-emerald-400 font-mono">{stats.approved}</div>
            <span className="text-[11px] text-emerald-300/80">Chốt sale thành công</span>
          </div>
        </div>

        {/* Tickets Table */}
        <div className="p-6 rounded-2xl bg-[#131a2b] border border-[#222f49] shadow-xl space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="font-bold text-base text-white">Danh Sách Ticket Cần Quyết Định</h3>
            <span className="text-xs text-slate-400 font-mono">
              {loading ? 'Đang tải...' : `Dữ liệu thời gian thực (${tickets.length} tickets)`}
            </span>
          </div>

          {error && (
            <div className="p-4 rounded-xl bg-rose-500/10 border border-rose-500/30 text-rose-300 text-xs">
              ⚠️ {error}
            </div>
          )}

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs sm:text-sm">
              <thead className="bg-[#0e1422] text-slate-400 uppercase text-[11px] font-mono border-b border-[#222f49]">
                <tr>
                  <th className="py-3 px-4">Mã Vé</th>
                  <th className="py-3 px-4">Khách Hàng & SĐT</th>
                  <th className="py-3 px-4">Tour Quan Tâm</th>
                  <th className="py-3 px-4">Đề Xuất Giảm</th>
                  <th className="py-3 px-4">Trạng Thái</th>
                  <th className="py-3 px-4 text-right">Thao Tác Quyết Định</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#222f49]">
                {tickets.length === 0 && !loading && (
                  <tr>
                    <td colSpan={6} className="py-8 text-center text-slate-500 text-xs">
                      Chưa có phiếu chuyển tiếp nào cần xử lý.
                    </td>
                  </tr>
                )}

                {tickets.map((ticket) => (
                  <tr key={ticket.id} className="hover:bg-slate-900/40 transition">
                    <td className="py-4 px-4 font-mono font-bold text-amber-400">
                      <div>{ticket.code}</div>
                      <div className="text-[10px] text-slate-500 font-sans font-normal">{ticket.timeAgo}</div>
                    </td>
                    <td className="py-4 px-4">
                      <div className="font-bold text-white flex items-center gap-1.5">
                        <span>{ticket.channel === 'telegram' ? '📱' : '🌐'}</span>
                        <span>{ticket.customerName}</span>
                      </div>
                      <div className="font-mono text-xs text-emerald-400 mt-0.5 select-all">
                        📞 {ticket.customerPhone}
                      </div>
                      {ticket.conversationId && (
                        <Link
                          to={`/admin/conversations`}
                          className="text-[10px] text-purple-400 hover:text-purple-300 underline mt-0.5 block"
                        >
                          Xem lịch sử chat #{ticket.conversationId}
                        </Link>
                      )}
                    </td>
                    <td className="py-4 px-4">
                      <div className="font-semibold text-slate-200">{ticket.tourTitle}</div>
                      <div className="text-[11px] text-slate-400">Đoàn {ticket.groupSize} khách</div>
                      {ticket.notes && (
                        <div className="text-[11px] text-slate-400 italic mt-1 max-w-xs truncate" title={ticket.notes}>
                          📝 {ticket.notes}
                        </div>
                      )}
                    </td>
                    <td className="py-4 px-4 font-mono font-bold text-rose-400">
                      {ticket.discountPercent > 0 ? `-${ticket.discountPercent}%` : '—'}
                    </td>
                    <td className="py-4 px-4">
                      <span
                        className={`px-2.5 py-1 rounded-full text-[10px] font-bold uppercase ${
                          ticket.status === 'pending'
                            ? 'bg-amber-500/20 text-amber-300 border border-amber-500/30'
                            : ticket.status === 'contacting'
                            ? 'bg-sky-500/20 text-sky-300 border border-sky-500/30'
                            : ticket.status === 'approved'
                            ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30'
                            : 'bg-slate-800 text-slate-400'
                        }`}
                      >
                        {ticket.status === 'pending'
                          ? 'Chờ tiếp nhận'
                          : ticket.status === 'contacting'
                          ? 'Đang gọi tư vấn'
                          : ticket.status === 'approved'
                          ? 'Đã duyệt giảm giá'
                          : 'Đã đóng'}
                      </span>
                    </td>
                    <td className="py-4 px-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        {ticket.status === 'pending' && (
                          <button
                            onClick={() => updateStatus(ticket.id, 'contacting')}
                            className="px-3 py-1.5 rounded-lg bg-sky-500/20 hover:bg-sky-500/30 text-sky-300 text-xs font-semibold border border-sky-500/40 transition"
                          >
                            📞 Nhận Cuộc Gọi
                          </button>
                        )}
                        {ticket.status === 'contacting' && (
                          <button
                            onClick={() => updateStatus(ticket.id, 'approve')}
                            className="px-3 py-1.5 rounded-lg bg-emerald-500/20 hover:bg-emerald-500/30 text-emerald-300 text-xs font-semibold border border-emerald-500/40 transition"
                          >
                            ✓ Duyệt Ưu Đãi
                          </button>
                        )}
                        <button
                          onClick={() => updateStatus(ticket.id, 'close')}
                          className="px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-400 hover:text-white text-xs transition"
                        >
                          ✕ Đóng
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </main>
    </div>
  );
};
