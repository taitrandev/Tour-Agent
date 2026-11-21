import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { notify } from '../lib/notify';
import { apiFetch } from '../api/client';
import { AdminLayout } from '../components/admin/AdminLayout';

interface EscalationTicket {
  id: number;
  code: string;
  customer_name?: string;
  customerName?: string;
  customer_phone?: string;
  customerPhone?: string;
  channel: 'telegram' | 'web_widget' | string;
  tour_title?: string;
  tourTitle?: string;
  discount_percent?: number;
  discountPercent?: number;
  group_size?: number;
  groupSize?: number;
  notes: string;
  status: 'pending' | 'contacting' | 'approved' | 'rejected' | 'closed' | string;
  time_ago?: string;
  timeAgo?: string;
  created_at?: string;
  createdAt?: string;
  conversation_id?: number;
  conversationId?: number;
  travel_id?: number;
  travelId?: number;
}

interface EscalationsResponse {
  items: EscalationTicket[];
  total_count?: number;
  totalCount?: number;
  pending_count?: number;
  pendingCount?: number;
  contacting_count?: number;
  contactingCount?: number;
  approved_count?: number;
  approvedCount?: number;
}

/**
 * Trang Bàn Quyết Định (Human-in-the-Loop Escalations Desk).
 * Quản trị tiếp nhận khách nóng, duyệt chiết khấu & hỗ trợ trực tiếp.
 */
export const AdminEscalationsPage: React.FC = () => {
  const [tickets, setTickets] = useState<EscalationTicket[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedTicket, setSelectedTicket] = useState<EscalationTicket | null>(null);

  const [stats, setStats] = useState({
    total: 0,
    pending: 0,
    contacting: 0,
    approved: 0,
  });

  const copyToClipboard = (text: string, label: string) => {
    navigator.clipboard.writeText(text);
    notify.success(`Đã sao chép ${label}!`);
  };

  // Helper getters hỗ trợ cả snake_case (từ backend .NET) và camelCase
  const getCustomerName = (t: EscalationTicket) => t.customer_name || t.customerName || 'Khách quan tâm tour';
  const getCustomerPhone = (t: EscalationTicket) => t.customer_phone || t.customerPhone || 'Chưa có SĐT';
  const getTourTitle = (t: EscalationTicket) => t.tour_title || t.tourTitle || 'Chưa chọn tour cụ thể';
  const getGroupSize = (t: EscalationTicket) => t.group_size ?? t.groupSize ?? 1;
  const getDiscountPercent = (t: EscalationTicket) => t.discount_percent ?? t.discountPercent ?? 0;
  const getTimeAgo = (t: EscalationTicket) => t.time_ago || t.timeAgo || 'Vừa xong';
  const getConversationId = (t: EscalationTicket) => t.conversation_id ?? t.conversationId;
  const getTravelId = (t: EscalationTicket) => t.travel_id ?? t.travelId;
  const getCreatedAt = (t: EscalationTicket) => t.created_at || t.createdAt;

  /**
   * Tải danh sách ticket từ API backend.
   */
  const fetchTickets = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await apiFetch('/api/v1/admin/escalations');
      if (!res.ok) {
        throw new Error(`Lỗi tải dữ liệu: ${res.statusText}`);
      }
      const data: EscalationsResponse = await res.json();
      const items = data.items || [];
      setTickets(items);
      setStats({
        total: data.total_count ?? data.totalCount ?? items.length,
        pending: data.pending_count ?? data.pendingCount ?? 0,
        contacting: data.contacting_count ?? data.contactingCount ?? 0,
        approved: data.approved_count ?? data.approvedCount ?? 0,
      });

      // Nếu đang xem modal chi tiết, cập nhật lại dữ liệu mới nhất
      setSelectedTicket((prev) => {
        if (!prev) return null;
        return items.find((it) => it.id === prev.id) || prev;
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
      const endpoint = `/api/v1/admin/escalations/${id}/${action}`;
      const res = await apiFetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({}),
      });

      if (!res.ok) {
        throw new Error(`Cập nhật trạng thái thất bại: ${res.statusText}`);
      }

      notify.success(
        action === 'contacting'
          ? 'Đã tiếp nhận ticket và bắt đầu tư vấn!'
          : action === 'approve'
          ? 'Đã duyệt ưu đãi thành công!'
          : 'Đã đóng ticket.'
      );

      // Tải lại danh sách sau khi thao tác
      await fetchTickets();
    } catch (err: any) {
      notify.error(err.message || 'Lỗi cập nhật ticket');
    }
  };

  return (
    <AdminLayout
      subtitle="Bàn Quyết Định (Escalations Desk) • Tiếp nhận khách nóng, duyệt chiết khấu & gọi điện tư vấn"
      icon="🛎️"
      headerActions={
        <button
          onClick={fetchTickets}
          className="px-3.5 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-semibold text-xs border border-[#222f49] transition flex items-center gap-1.5 shrink-0"
          title="Làm mới danh sách"
        >
          <span className={loading ? 'animate-spin' : ''}>🔄</span>
          <span>Làm mới</span>
        </button>
      }
    >
      <div className="p-6 space-y-6">
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
              <span className="w-2.5 h-2.5 rounded-full bg-amber-400 animate-ping" />
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
            <div className="flex items-center gap-2.5">
              <span className="text-lg">🛎️</span>
              <h3 className="font-bold text-base text-white">Danh Sách Ticket Cần Quyết Định</h3>
            </div>
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

                {tickets.map((ticket) => {
                  const customerName = getCustomerName(ticket);
                  const customerPhone = getCustomerPhone(ticket);
                  const tourTitle = getTourTitle(ticket);
                  const groupSize = getGroupSize(ticket);
                  const discountPercent = getDiscountPercent(ticket);
                  const timeAgo = getTimeAgo(ticket);
                  const convId = getConversationId(ticket);

                  return (
                    <tr key={ticket.id} className="hover:bg-slate-900/40 transition">
                      {/* Mã vé */}
                      <td className="py-4 px-4 font-mono font-bold text-amber-400">
                        <button
                          type="button"
                          onClick={() => setSelectedTicket(ticket)}
                          className="hover:underline hover:text-amber-300 flex items-center gap-1 group text-left font-bold"
                          title="Bấm để xem chi tiết đầy đủ ticket"
                        >
                          <span>{ticket.code}</span>
                          <span className="text-[10px] opacity-0 group-hover:opacity-100 transition">🔍</span>
                        </button>
                        <div className="text-[10px] text-slate-500 font-sans font-normal">{timeAgo}</div>
                      </td>

                      {/* Khách hàng & SĐT */}
                      <td className="py-4 px-4">
                        <div className="font-bold text-white flex items-center gap-1.5">
                          <span>{ticket.channel === 'telegram' ? '📱' : '🌐'}</span>
                          <span>{customerName}</span>
                        </div>
                        <div className="font-mono text-xs text-emerald-400 mt-0.5 select-all flex items-center gap-1">
                          <span>📞 {customerPhone}</span>
                          {customerPhone !== 'Chưa có SĐT' && (
                            <button
                              type="button"
                              onClick={() => copyToClipboard(customerPhone, 'Số điện thoại')}
                              className="text-[10px] text-slate-400 hover:text-white px-1 py-0.2 rounded hover:bg-slate-800"
                              title="Sao chép SĐT"
                            >
                              📋
                            </button>
                          )}
                        </div>
                        {convId && (
                          <Link
                            to={`/admin/conversations?id=${convId}`}
                            className="text-[10px] text-purple-400 hover:text-purple-300 underline mt-0.5 inline-flex items-center gap-1"
                          >
                            <span>💬 Xem lịch sử chat #{convId}</span>
                            <span>➔</span>
                          </Link>
                        )}
                      </td>

                      {/* Tour quan tâm */}
                      <td className="py-4 px-4">
                        <div className="font-semibold text-slate-200">{tourTitle}</div>
                        <div className="text-[11px] text-slate-400">Đoàn {groupSize} khách</div>
                        {ticket.notes && (
                          <div
                            className="text-[11px] text-slate-400 italic mt-1 max-w-sm truncate cursor-pointer hover:text-slate-300"
                            title={ticket.notes}
                            onClick={() => setSelectedTicket(ticket)}
                          >
                            📝 {ticket.notes}
                          </div>
                        )}
                      </td>

                      {/* Đề xuất giảm giá */}
                      <td className="py-4 px-4 font-mono font-bold text-rose-400">
                        {discountPercent > 0 ? `-${discountPercent}%` : '—'}
                      </td>

                      {/* Trạng thái */}
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

                      {/* Thao tác quyết định */}
                      <td className="py-4 px-4 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          <button
                            type="button"
                            onClick={() => setSelectedTicket(ticket)}
                            className="px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 font-semibold text-xs border border-slate-700 transition flex items-center gap-1"
                            title="Mở bảng chi tiết toàn diện ticket"
                          >
                            <span>👁️</span>
                            <span>Chi Tiết</span>
                          </button>

                          {ticket.status === 'pending' && (
                            <button
                              type="button"
                              onClick={() => updateStatus(ticket.id, 'contacting')}
                              className="px-3 py-1.5 rounded-lg bg-sky-500/20 hover:bg-sky-500/30 text-sky-300 text-xs font-semibold border border-sky-500/40 transition flex items-center gap-1"
                            >
                              <span>📞</span>
                              <span>Nhận Cuộc Gọi</span>
                            </button>
                          )}

                          {ticket.status === 'contacting' && (
                            <button
                              type="button"
                              onClick={() => updateStatus(ticket.id, 'approve')}
                              className="px-3 py-1.5 rounded-lg bg-emerald-500/20 hover:bg-emerald-500/30 text-emerald-300 text-xs font-semibold border border-emerald-500/40 transition flex items-center gap-1"
                            >
                              <span>✓</span>
                              <span>Duyệt Ưu Đãi</span>
                            </button>
                          )}

                          {ticket.status !== 'closed' && (
                            <button
                              type="button"
                              onClick={() => updateStatus(ticket.id, 'close')}
                              className="px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-400 hover:text-white text-xs transition"
                              title="Đóng ticket này"
                            >
                              ✕ Đóng
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      {/* ── CHI TIẾT TICKET MODAL ── */}
      {selectedTicket && (
        <div className="fixed inset-0 bg-black/80 backdrop-blur-sm z-50 flex items-center justify-center p-4 animate-in fade-in duration-200">
          <div className="bg-[#131a2b] border border-[#222f49] rounded-2xl w-full max-w-2xl overflow-hidden shadow-2xl flex flex-col max-h-[90vh]">
            {/* Modal Header */}
            <div className="p-5 border-b border-[#222f49] flex items-center justify-between bg-[#0e1422]">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-amber-500/20 border border-amber-500/30 flex items-center justify-center text-xl">
                  🛎️
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <h3 className="font-bold text-white text-base font-mono">
                      {selectedTicket.code}
                    </h3>
                    <span
                      className={`px-2.5 py-0.5 rounded-full text-[10px] font-bold uppercase ${
                        selectedTicket.status === 'pending'
                          ? 'bg-amber-500/20 text-amber-300 border border-amber-500/30'
                          : selectedTicket.status === 'contacting'
                          ? 'bg-sky-500/20 text-sky-300 border border-sky-500/30'
                          : selectedTicket.status === 'approved'
                          ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30'
                          : 'bg-slate-800 text-slate-400'
                      }`}
                    >
                      {selectedTicket.status === 'pending'
                        ? 'Chờ tiếp nhận'
                        : selectedTicket.status === 'contacting'
                        ? 'Đang tư vấn'
                        : selectedTicket.status === 'approved'
                        ? 'Đã duyệt'
                        : 'Đã đóng'}
                    </span>
                  </div>
                  <p className="text-xs text-slate-400 mt-0.5">
                    Tạo {getTimeAgo(selectedTicket)} • Kênh: {selectedTicket.channel === 'telegram' ? 'Telegram Bot' : 'Web Widget'}
                  </p>
                </div>
              </div>

              <button
                type="button"
                onClick={() => setSelectedTicket(null)}
                className="p-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-400 hover:text-white transition"
              >
                ✕
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-6 space-y-5 overflow-y-auto custom-scrollbar flex-1">
              {/* Grid 2 cột thông tin */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Cột 1: Thông tin khách hàng */}
                <div className="p-4 rounded-xl bg-[#0e1422] border border-[#222f49] space-y-3">
                  <div className="text-xs font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
                    <span>👤</span>
                    <span>Thông Tin Khách Hàng</span>
                  </div>
                  <div className="space-y-2 text-xs">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Họ và tên:</span>
                      <span className="font-bold text-white text-sm">
                        {getCustomerName(selectedTicket)}
                      </span>
                    </div>

                    <div>
                      <span className="text-slate-400 block text-[11px]">Số điện thoại:</span>
                      <div className="flex items-center gap-2 mt-0.5">
                        <span className="font-mono text-emerald-400 font-bold text-sm bg-emerald-500/10 px-2 py-0.5 rounded border border-emerald-500/20 select-all">
                          {getCustomerPhone(selectedTicket)}
                        </span>
                        {getCustomerPhone(selectedTicket) !== 'Chưa có SĐT' && (
                          <>
                            <button
                              type="button"
                              onClick={() => copyToClipboard(getCustomerPhone(selectedTicket), 'Số điện thoại')}
                              className="px-2 py-0.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 text-[11px]"
                              title="Sao chép"
                            >
                              📋
                            </button>
                            <a
                              href={`tel:${getCustomerPhone(selectedTicket)}`}
                              className="px-2 py-0.5 rounded bg-emerald-600 hover:bg-emerald-500 text-white text-[11px] font-semibold flex items-center gap-1"
                            >
                              <span>📞 Gọi</span>
                            </a>
                          </>
                        )}
                      </div>
                    </div>

                    <div>
                      <span className="text-slate-400 block text-[11px]">Kênh liên hệ:</span>
                      <span className="font-semibold text-slate-200">
                        {selectedTicket.channel === 'telegram' ? '📱 Telegram' : '💻 Website'}
                      </span>
                    </div>

                    {getCreatedAt(selectedTicket) && (
                      <div>
                        <span className="text-slate-400 block text-[11px]">Thời điểm tạo:</span>
                        <span className="font-mono text-slate-300 text-[11px]">
                          {new Date(getCreatedAt(selectedTicket)!).toLocaleString('vi-VN')}
                        </span>
                      </div>
                    )}
                  </div>
                </div>

                {/* Cột 2: Thông tin Tour & Đề xuất */}
                <div className="p-4 rounded-xl bg-[#0e1422] border border-[#222f49] space-y-3">
                  <div className="text-xs font-bold text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
                    <span>🗺️</span>
                    <span>Tour & Đề Xuất Deal</span>
                  </div>
                  <div className="space-y-2 text-xs">
                    <div>
                      <span className="text-slate-400 block text-[11px]">Tour quan tâm:</span>
                      <span className="font-bold text-sky-300 text-sm">
                        {getTourTitle(selectedTicket)}
                      </span>
                      {getTravelId(selectedTicket) && (
                        <span className="text-[10px] text-slate-500 font-mono block">
                          Mã Tour ID: #{getTravelId(selectedTicket)}
                        </span>
                      )}
                    </div>

                    <div>
                      <span className="text-slate-400 block text-[11px]">Quy mô đoàn:</span>
                      <span className="font-bold text-white font-mono">
                        {getGroupSize(selectedTicket)} người lớn / khách
                      </span>
                    </div>

                    <div>
                      <span className="text-slate-400 block text-[11px]">Đề xuất chiết khấu:</span>
                      <span className="font-bold font-mono text-rose-400 text-sm">
                        {getDiscountPercent(selectedTicket) > 0
                          ? `-${getDiscountPercent(selectedTicket)}% chiết khấu`
                          : 'Không yêu cầu giảm giá'}
                      </span>
                    </div>

                    <div>
                      <span className="text-slate-400 block text-[11px]">Mã định danh hệ thống:</span>
                      <span className="font-mono text-slate-400 text-[11px]">
                        ID #{selectedTicket.id}
                      </span>
                    </div>
                  </div>
                </div>
              </div>

              {/* Nhu cầu chi tiết & Ghi chú từ AI Agent */}
              <div className="p-4 rounded-xl bg-[#0e1422] border border-[#222f49] space-y-2">
                <div className="text-xs font-bold text-amber-400 uppercase tracking-wider flex items-center gap-1.5">
                  <span>📝</span>
                  <span>Nhu Cầu Chi Tiết & Ghi Chú AI Agent</span>
                </div>
                <div className="p-3.5 rounded-lg bg-[#080c14] border border-[#222f49]/80 text-xs text-slate-200 leading-relaxed whitespace-pre-line font-sans select-text">
                  {selectedTicket.notes || 'Không có ghi chú bổ sung.'}
                </div>
              </div>

              {/* Link mở phiên chat nếu có */}
              {getConversationId(selectedTicket) && (
                <div className="p-4 rounded-xl bg-purple-950/20 border border-purple-500/30 flex items-center justify-between gap-3">
                  <div className="flex items-center gap-2.5">
                    <span className="text-xl">💬</span>
                    <div>
                      <h4 className="text-xs font-bold text-purple-300">
                        Phiên Hội Thoại Gốc #{getConversationId(selectedTicket)}
                      </h4>
                      <p className="text-[11px] text-purple-400/80">
                        Xem toàn bộ lịch sử tin nhắn của khách hàng và các tool AI Agent đã gọi
                      </p>
                    </div>
                  </div>
                  <Link
                    to={`/admin/conversations?id=${getConversationId(selectedTicket)}`}
                    className="px-3.5 py-1.5 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-semibold transition shrink-0 flex items-center gap-1.5 shadow-md shadow-purple-600/30"
                  >
                    <span>Mở Cuộc Chat</span>
                    <span>➔</span>
                  </Link>
                </div>
              )}
            </div>

            {/* Modal Footer Actions */}
            <div className="p-4 border-t border-[#222f49] bg-[#0e1422] flex items-center justify-between gap-3">
              <div className="flex items-center gap-2">
                {selectedTicket.status === 'pending' && (
                  <button
                    type="button"
                    onClick={() => updateStatus(selectedTicket.id, 'contacting')}
                    className="px-4 py-2 rounded-xl bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold transition flex items-center gap-1.5 shadow-md shadow-sky-600/30"
                  >
                    <span>📞</span>
                    <span>Tiếp Nhận & Bắt Đầu Tư Vấn</span>
                  </button>
                )}

                {selectedTicket.status === 'contacting' && (
                  <button
                    type="button"
                    onClick={() => updateStatus(selectedTicket.id, 'approve')}
                    className="px-4 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold transition flex items-center gap-1.5 shadow-md shadow-emerald-600/30"
                  >
                    <span>✓</span>
                    <span>Duyệt Ưu Đãi / Chốt Deal</span>
                  </button>
                )}

                {selectedTicket.status !== 'closed' && (
                  <button
                    type="button"
                    onClick={() => updateStatus(selectedTicket.id, 'close')}
                    className="px-3 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold border border-slate-700 transition"
                  >
                    ✕ Đóng Phiếu
                  </button>
                )}
              </div>

              <button
                type="button"
                onClick={() => setSelectedTicket(null)}
                className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  );
};

export default AdminEscalationsPage;
