import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { notify } from '../lib/notify';
import { useConfirm } from '../components/common/ConfirmModal';
import { apiFetch } from '../api/client';
import { AdminLayout } from '../components/admin/AdminLayout';
import { API_ROUTES } from '../constants/apiRoutes';

/**
 * Định nghĩa cấu trúc dữ liệu một bản ghi khách hàng CRM.
 */
interface CrmContactItem {
  id: number;
  name: string | null;
  phone: string | null;
  email: string | null;
  telegramChatId: string | null;
  telegramUsername: string | null;
  zaloId: string | null;
  channel: string;
  stage: 'prospect' | 'warm' | 'hot' | 'customer' | 'lost' | string;
  tags?: string[] | null;
  notes: string | null;
  lastActiveAt: string | null;
  conversationsCount: number;
  createdAt: string | null;
  updatedAt: string | null;
}

/**
 * Định nghĩa cấu trúc phiên hội thoại liên kết của khách hàng.
 */
interface ContactConversationItem {
  id: number;
  channel: string;
  channelUserId: string;
  status: string;
  lastMessageAt: string | null;
  messagesCount: number;
}

/**
 * Định nghĩa chi tiết hồ sơ 360° của khách hàng trả về từ API.
 */
interface ContactDetailData extends CrmContactItem {
  conversations: ContactConversationItem[];
}

/**
 * Trang Quản Trị Khách Hàng Tập Trung (Core CRM Contacts 360°).
 * Giao diện Glassmorphism & Logic tương tác quản lý khách hàng.
 */
export const AdminContactsPage: React.FC = () => {
  const confirm = useConfirm();

  // State danh sách khách hàng và trạng thái tải
  const [contacts, setContacts] = useState<CrmContactItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [currentStage, setCurrentStage] = useState<string>('');
  const [searchKeyword, setSearchKeyword] = useState<string>('');

  // State Modal Chi tiết 360°
  const [detailModalOpen, setDetailModalOpen] = useState<boolean>(false);
  const [loadingDetail, setLoadingDetail] = useState<boolean>(false);
  const [detailData, setDetailData] = useState<ContactDetailData | null>(null);

  // State Modal Tạo mới khách hàng
  const [createModalOpen, setCreateModalOpen] = useState<boolean>(false);
  const [createForm, setCreateForm] = useState({
    name: '',
    phone: '',
    email: '',
    channel: 'manual',
    stage: 'hot',
    notes: '',
  });

  // State Modal Sửa thông tin khách hàng
  const [editModalOpen, setEditModalOpen] = useState<boolean>(false);
  const [editForm, setEditForm] = useState({
    id: 0,
    name: '',
    phone: '',
    email: '',
    stage: 'prospect',
    notes: '',
  });

  /**
   * Tải danh sách khách hàng từ API backend kèm các tham số lọc stage và tìm kiếm.
   */
  const fetchContacts = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      params.append('perPage', '100');
      if (currentStage) params.append('stage', currentStage);
      if (searchKeyword.trim()) params.append('search', searchKeyword.trim());

      const res = await apiFetch(`${API_ROUTES.ADMIN.CONTACTS.BASE}?${params.toString()}`);
      const json = await res.json();

      if (json.success && Array.isArray(json.data)) {
        setContacts(json.data);
      } else {
        setContacts([]);
        notify.error(json.message || 'Không thể tải danh bạ khách hàng.');
      }
    } catch (err: any) {
      console.error('Lỗi kết nối API contacts:', err);
      notify.error('Lỗi kết nối máy chủ.');
      setContacts([]);
    } finally {
      setLoading(false);
    }
  }, [currentStage, searchKeyword]);

  // Tự động tải danh sách khi đổi stage hoặc từ khóa tìm kiếm
  useEffect(() => {
    fetchContacts();
  }, [fetchContacts]);

  /**
   * Tính toán các chỉ số thống kê trên thẻ tổng quan (Metrics Overview).
   */
  const metrics = useMemo(() => {
    const total = contacts.length;
    const hot = contacts.filter(c => c.stage === 'hot').length;
    const telegram = contacts.filter(c => c.channel === 'telegram').length;
    const web = contacts.filter(c => c.channel === 'web_widget' || c.channel === 'web').length;
    return { total, hot, telegram, web };
  }, [contacts]);

  /**
   * Mở modal xem hồ sơ 360° chi tiết và lịch sử các phiên chat của khách hàng.
   * @param contactId ID khách hàng cần xem
   */
  const handleOpenDetailModal = async (contactId: number) => {
    setDetailModalOpen(true);
    setLoadingDetail(true);
    setDetailData(null);

    try {
      const res = await apiFetch(API_ROUTES.ADMIN.CONTACTS.BY_ID(contactId));
      const json = await res.json();
      if (json.success && json.data) {
        setDetailData(json.data);
      } else {
        notify.error(json.message || 'Không tìm thấy hồ sơ khách hàng.');
        setDetailModalOpen(false);
      }
    } catch (err: any) {
      console.error(err);
      notify.error('Lỗi khi tải chi tiết khách hàng.');
      setDetailModalOpen(false);
    } finally {
      setLoadingDetail(false);
    }
  };

  /**
   * Đóng modal xem chi tiết.
   */
  const handleCloseDetailModal = () => {
    setDetailModalOpen(false);
    setDetailData(null);
  };

  /**
   * Mở modal cập nhật thông tin khách hàng với dữ liệu hiện tại.
   * @param contact Đối tượng khách hàng cần chỉnh sửa
   */
  const handleOpenEditModal = (contact: CrmContactItem) => {
    setEditForm({
      id: contact.id,
      name: contact.name || '',
      phone: contact.phone || '',
      email: contact.email || '',
      stage: contact.stage || 'prospect',
      notes: contact.notes || '',
    });
    setEditModalOpen(true);
  };

  /**
   * Gửi request cập nhật thông tin khách hàng lên API backend.
   * @param e Sự kiện submit form
   */
  const handleSubmitUpdate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const payload = {
        name: editForm.name.trim() || null,
        phone: editForm.phone.trim() || null,
        email: editForm.email.trim() || null,
        stage: editForm.stage,
        notes: editForm.notes.trim() || null,
      };

      const res = await apiFetch(API_ROUTES.ADMIN.CONTACTS.BY_ID(editForm.id), {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      const json = await res.json();
      if (json.success) {
        notify.success('Cập nhật hồ sơ khách hàng thành công!');
        setEditModalOpen(false);
        fetchContacts();
        if (detailModalOpen && detailData?.id === editForm.id) {
          handleOpenDetailModal(editForm.id);
        }
      } else {
        notify.error(json.message || 'Không thể cập nhật hồ sơ.');
      }
    } catch (err: any) {
      notify.error('Lỗi kết nối khi cập nhật: ' + err.message);
    }
  };

  /**
   * Mở modal tạo mới hồ sơ khách hàng thủ công.
   */
  const handleOpenCreateModal = () => {
    setCreateForm({
      name: '',
      phone: '',
      email: '',
      channel: 'manual',
      stage: 'hot',
      notes: '',
    });
    setCreateModalOpen(true);
  };

  /**
   * Gửi request tạo mới hồ sơ khách hàng lên API backend.
   * @param e Sự kiện submit form
   */
  const handleSubmitCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!createForm.phone.trim()) {
      notify.warning('Vui lòng nhập số điện thoại khách hàng.');
      return;
    }

    try {
      const payload = {
        name: createForm.name.trim() || null,
        phone: createForm.phone.trim() || null,
        email: createForm.email.trim() || null,
        channel: createForm.channel,
        stage: createForm.stage,
        notes: createForm.notes.trim() || null,
      };

      const res = await apiFetch(API_ROUTES.ADMIN.CONTACTS.BASE, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      const json = await res.json();
      if (json.success) {
        notify.success('Tạo hồ sơ khách hàng mới thành công!');
        setCreateModalOpen(false);
        fetchContacts();
      } else {
        notify.error(json.message || 'Không thể tạo mới khách hàng.');
      }
    } catch (err: any) {
      notify.error('Lỗi kết nối khi tạo mới: ' + err.message);
    }
  };

  /**
   * Xác nhận và gửi request xóa mềm hồ sơ khách hàng.
   * @param contactId ID khách hàng cần xóa
   * @param contactName Tên khách hàng
   */
  const handleDeleteContact = async (contactId: number, contactName: string) => {
    const isConfirmed = await confirm({
      title: 'Xác nhận xóa hồ sơ khách hàng',
      message: `Bạn có chắc chắn muốn xóa hồ sơ khách hàng "${contactName}" (ID #${contactId}) không? Mọi thông tin liên hệ và lịch sử của khách hàng sẽ bị gỡ bỏ.`,
      confirmText: 'Xóa hồ sơ',
      cancelText: 'Hủy bỏ',
      variant: 'danger',
    });
    if (!isConfirmed) return;

    try {
      const res = await apiFetch(API_ROUTES.ADMIN.CONTACTS.BY_ID(contactId), {
        method: 'DELETE',
      });
      const json = await res.json();

      if (json.success) {
        notify.success(`Đã xóa khách hàng #${contactId} thành công!`);
        fetchContacts();
      } else {
        notify.error(json.message || 'Không thể xóa hồ sơ.');
      }
    } catch (err: any) {
      notify.error('Lỗi khi xóa: ' + err.message);
    }
  };

  /**
   * Render nhãn định danh kênh tiếp cận theo màu sắc thương hiệu.
   * @param channel Tên kênh
   */
  const renderChannelBadge = (channel: string) => {
    const ch = (channel || '').toLowerCase();
    if (ch === 'telegram') {
      return <span className="px-2 py-0.5 rounded bg-sky-500/20 text-sky-300 font-semibold text-[10px]">📱 Telegram</span>;
    }
    if (ch === 'zalo' || ch === 'zalo_personal') {
      return <span className="px-2 py-0.5 rounded bg-blue-500/20 text-blue-300 font-semibold text-[10px]">💬 Zalo</span>;
    }
    if (ch === 'web_widget' || ch === 'web') {
      return <span className="px-2 py-0.5 rounded bg-purple-500/20 text-purple-300 font-semibold text-[10px]">💻 Web</span>;
    }
    return <span className="px-2 py-0.5 rounded bg-amber-500/20 text-amber-300 font-semibold text-[10px]">📞 Trực tiếp</span>;
  };

  /**
   * Render nhãn phân hạng vòng đời khách hàng (Stage Badge).
   * @param stage Tên stage
   */
  const renderStageBadge = (stage: string) => {
    const s = (stage || '').toLowerCase();
    if (s === 'hot') {
      return (
        <span className="px-2 py-0.5 rounded bg-rose-500/20 text-rose-400 border border-rose-500/30 font-bold text-[10px] animate-pulse inline-flex items-center gap-1">
          <span>🔥</span> HOT LEAD
        </span>
      );
    }
    if (s === 'warm') {
      return <span className="px-2 py-0.5 rounded bg-amber-500/20 text-amber-300 font-semibold text-[10px]">Warm</span>;
    }
    if (s === 'customer') {
      return <span className="px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-300 font-bold text-[10px]">Đã mua</span>;
    }
    if (s === 'lost') {
      return <span className="px-2 py-0.5 rounded bg-slate-800 text-slate-500 font-medium text-[10px]">Hủy / Mất</span>;
    }
    return <span className="px-2 py-0.5 rounded bg-slate-800 text-slate-400 font-medium text-[10px]">Prospect</span>;
  };

  return (
    <AdminLayout
      title="AI Agent Travel — Quản Trị Hệ Thống"
      subtitle="Hồ Sơ Khách Hàng Tập Trung (Core CRM Contacts 360°)"
      icon="👥"
      headerActions={
        <>
          <button
            onClick={handleOpenCreateModal}
            className="px-3.5 py-1.5 rounded-xl bg-teal-600 hover:bg-teal-500 text-white text-xs font-semibold transition flex items-center gap-1.5 shadow-sm shadow-teal-600/20 shrink-0"
          >
            <span>+ Thêm Khách Hàng</span>
          </button>

          <button
            onClick={fetchContacts}
            className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium border border-slate-700 transition flex items-center gap-1.5 shrink-0"
            title="Làm mới danh sách"
          >
            <svg className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-teal-400' : ''}`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            <span className="hidden sm:inline">Làm mới</span>
          </button>
        </>
      }
    >
      {/* Main Content Area */}
      <main className="flex-1 p-6 max-w-7xl w-full mx-auto space-y-6">
        {/* Metrics Overview Grid */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 shadow-sm hover:border-slate-700 transition">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-400">Tổng Khách Hàng</span>
              <span className="text-lg">👥</span>
            </div>
            <div className="text-2xl font-black text-white mt-2 font-mono">{metrics.total}</div>
            <div className="text-[11px] text-slate-500 mt-0.5">Tất cả các kênh</div>
          </div>

          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 shadow-sm hover:border-rose-500/40 transition">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-rose-400">Hot Leads (Có SĐT)</span>
              <span className="text-lg">🔥</span>
            </div>
            <div className="text-2xl font-black text-rose-400 mt-2 font-mono">{metrics.hot}</div>
            <div className="text-[11px] text-rose-400/80 mt-0.5">Sẵn sàng chốt sale</div>
          </div>

          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 shadow-sm hover:border-sky-500/40 transition">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-sky-400">Kênh Telegram</span>
              <span className="text-lg">📱</span>
            </div>
            <div className="text-2xl font-black text-sky-400 mt-2 font-mono">{metrics.telegram}</div>
            <div className="text-[11px] text-slate-500 mt-0.5">Qua Telegram Bot</div>
          </div>

          <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 shadow-sm hover:border-purple-500/40 transition">
            <div className="flex items-center justify-between">
              <span className="text-xs font-semibold text-purple-400">Kênh Web Chat</span>
              <span className="text-lg">💻</span>
            </div>
            <div className="text-2xl font-black text-purple-400 mt-2 font-mono">{metrics.web}</div>
            <div className="text-[11px] text-slate-500 mt-0.5">Trực tiếp trên Website</div>
          </div>
        </div>

        {/* Filter & Search Controls */}
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-4 flex flex-col sm:flex-row gap-3 items-center justify-between">
          <div className="flex flex-wrap items-center gap-2 w-full sm:w-auto">
            <button
              onClick={() => setCurrentStage('')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition ${
                currentStage === '' ? 'bg-teal-600 text-white shadow-sm' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
              }`}
            >
              Tất cả
            </button>
            <button
              onClick={() => setCurrentStage('hot')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition flex items-center gap-1 ${
                currentStage === 'hot' ? 'bg-rose-600 text-white shadow-sm' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
              }`}
            >
              <span>🔥 Hot Lead</span>
            </button>
            <button
              onClick={() => setCurrentStage('warm')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition ${
                currentStage === 'warm' ? 'bg-amber-600 text-white shadow-sm' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
              }`}
            >
              Warm
            </button>
            <button
              onClick={() => setCurrentStage('prospect')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition ${
                currentStage === 'prospect' ? 'bg-teal-600 text-white shadow-sm' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
              }`}
            >
              Prospect
            </button>
            <button
              onClick={() => setCurrentStage('customer')}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition ${
                currentStage === 'customer' ? 'bg-emerald-600 text-white shadow-sm' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'
              }`}
            >
              Khách Hàng
            </button>
          </div>

          <div className="w-full sm:w-80 relative">
            <input
              type="text"
              value={searchKeyword}
              onChange={e => setSearchKeyword(e.target.value)}
              placeholder="Tìm kiếm theo Tên, SĐT, @username..."
              className="w-full bg-slate-950 border border-slate-700/80 rounded-xl px-3.5 py-2 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-teal-500 pl-9"
            />
            <svg className="w-4 h-4 absolute left-3 top-2.5 text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </div>
        </div>

        {/* Contacts Table */}
        <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-950/60 border-b border-slate-800 text-slate-400 font-semibold uppercase tracking-wider text-[11px]">
                <tr>
                  <th className="py-3 px-4">Khách Hàng</th>
                  <th className="py-3 px-4">Số Điện Thoại</th>
                  <th className="py-3 px-4">Kênh Tiếp Cận</th>
                  <th className="py-3 px-4">Phân Hạng (Stage)</th>
                  <th className="py-3 px-4">Số Phiên Chat</th>
                  <th className="py-3 px-4">Hoạt Động Cuối</th>
                  <th className="py-3 px-4 text-right">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60 text-slate-300">
                {loading ? (
                  <tr>
                    <td colSpan={7} className="py-12 text-center text-slate-500">
                      <div className="flex items-center justify-center gap-2">
                        <svg className="animate-spin w-4 h-4 text-teal-500" fill="none" viewBox="0 0 24 24">
                          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        <span>Đang tải danh bạ khách hàng...</span>
                      </div>
                    </td>
                  </tr>
                ) : contacts.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="py-12 text-center text-slate-500">
                      Không tìm thấy hồ sơ khách hàng nào phù hợp với bộ lọc.
                    </td>
                  </tr>
                ) : (
                  contacts.map(c => {
                    const displayName = c.name || (c.telegramUsername ? `@${c.telegramUsername}` : 'Khách vãng lai');
                    const lastActive = c.lastActiveAt ? new Date(c.lastActiveAt).toLocaleString('vi-VN') : '—';

                    return (
                      <tr key={c.id} className="hover:bg-slate-800/40 transition">
                        <td className="py-3 px-4">
                          <div className="font-bold text-white">{displayName}</div>
                          {c.telegramUsername && (
                            <div className="text-[10px] text-slate-500 font-mono">@{c.telegramUsername}</div>
                          )}
                        </td>
                        <td className="py-3 px-4">
                          {c.phone ? (
                            <span className="font-mono text-emerald-400 font-bold bg-emerald-500/10 px-2 py-0.5 rounded border border-emerald-500/20">
                              {c.phone}
                            </span>
                          ) : (
                            <span className="text-slate-500 italic">Chưa có</span>
                          )}
                        </td>
                        <td className="py-3 px-4">{renderChannelBadge(c.channel)}</td>
                        <td className="py-3 px-4">{renderStageBadge(c.stage)}</td>
                        <td className="py-3 px-4 font-mono font-bold text-slate-300">
                          {c.conversationsCount || 0} phiên
                        </td>
                        <td className="py-3 px-4 text-slate-400 text-[11px] font-mono">{lastActive}</td>
                        <td className="py-3 px-4 text-right">
                          <div className="flex items-center justify-end gap-1.5">
                            <button
                              onClick={() => handleOpenDetailModal(c.id)}
                              className="px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white font-semibold text-xs transition border border-slate-700"
                              title="Xem chi tiết 360°"
                            >
                              🔍 Xem
                            </button>
                            <button
                              onClick={() => handleOpenEditModal(c)}
                              className="px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-teal-400 hover:text-teal-300 text-xs transition border border-slate-700"
                              title="Sửa thông tin"
                            >
                              ✏️ Sửa
                            </button>
                            <button
                              onClick={() => handleDeleteContact(c.id, displayName)}
                              className="px-2.5 py-1 rounded-lg bg-rose-500/10 hover:bg-rose-500/20 text-rose-400 hover:text-rose-300 text-xs transition border border-rose-500/20"
                              title="Xóa hồ sơ"
                            >
                              🗑️
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>
      </main>

      {/* Detail & Chat History 360° Modal */}
      {detailModalOpen && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700/80 rounded-2xl w-full max-w-2xl overflow-hidden shadow-2xl flex flex-col max-h-[85vh]">
            <div className="p-4 border-b border-slate-800 flex items-center justify-between bg-slate-900/90">
              <div className="flex items-center gap-2.5">
                <span className="text-lg">👤</span>
                <div>
                  <h3 className="font-bold text-white text-sm">
                    {detailData?.name || (detailData?.telegramUsername ? `@${detailData.telegramUsername}` : 'Khách vãng lai')}
                  </h3>
                  <p className="text-[11px] text-slate-400 font-mono">
                    ID #{detailData?.id} • Kênh: {detailData?.channel} • Stage: {detailData?.stage}
                  </p>
                </div>
              </div>
              <button
                onClick={handleCloseDetailModal}
                className="text-slate-400 hover:text-white p-1 rounded-lg hover:bg-slate-800 transition"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div className="p-4 overflow-y-auto space-y-4 flex-1">
              {loadingDetail ? (
                <div className="py-12 text-center text-slate-500 text-xs">Đang tải chi tiết hồ sơ...</div>
              ) : detailData ? (
                <>
                  {/* Info Cards */}
                  <div className="grid grid-cols-2 gap-3 text-xs">
                    <div className="p-3 bg-slate-800/60 rounded-xl border border-slate-700/60">
                      <span className="text-slate-400 block text-[10px] uppercase font-bold">Số Điện Thoại</span>
                      <span className="font-mono text-emerald-400 font-bold text-sm">
                        {detailData.phone || '—'}
                      </span>
                    </div>
                    <div className="p-3 bg-slate-800/60 rounded-xl border border-slate-700/60">
                      <span className="text-slate-400 block text-[10px] uppercase font-bold">Email</span>
                      <span className="text-slate-200 text-sm">
                        {detailData.email || '—'}
                      </span>
                    </div>
                  </div>

                  {/* Notes / Ghi chú */}
                  <div className="p-3 bg-slate-800/40 rounded-xl border border-slate-700/50 text-xs">
                    <span className="text-slate-400 block text-[10px] uppercase font-bold mb-1">Ghi Chú & Nhu Cầu</span>
                    <p className="text-slate-300 italic">
                      {detailData.notes || 'Chưa có ghi chú thêm về nhu cầu của khách.'}
                    </p>
                  </div>

                  {/* Conversations History */}
                  <div>
                    <h4 className="text-xs font-bold text-slate-300 uppercase tracking-wider mb-2 flex items-center gap-1.5">
                      <span>💬 Lịch Sử Các Phiên Chat</span>
                      <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-slate-800 text-slate-400">
                        {detailData.conversations?.length || 0}
                      </span>
                    </h4>

                    {(!detailData.conversations || detailData.conversations.length === 0) ? (
                      <p className="text-slate-500 text-xs italic">Chưa có phiên chat nào với khách hàng này.</p>
                    ) : (
                      <div className="space-y-2">
                        {detailData.conversations.map(conv => (
                          <Link
                            key={conv.id}
                            to={`/admin/conversations?id=${conv.id}`}
                            className="p-3 bg-slate-800/80 hover:bg-slate-700/80 rounded-xl border border-slate-700 hover:border-purple-500/50 flex items-center justify-between text-xs transition group cursor-pointer block"
                          >
                            <div>
                              <div className="font-bold text-white group-hover:text-purple-300 transition flex items-center gap-2">
                                <span>Phiên #{conv.id} ({conv.channel})</span>
                                <span className="text-[10px] text-purple-400 opacity-0 group-hover:opacity-100 transition">
                                  ↗ Mở xem
                                </span>
                              </div>
                              <span className="text-slate-400 text-[11px] block font-mono">
                                Tin nhắn cuối: {conv.lastMessageAt ? new Date(conv.lastMessageAt).toLocaleString('vi-VN') : '—'}
                              </span>
                            </div>
                            <span className="px-2 py-1 rounded bg-slate-700 group-hover:bg-purple-500/20 text-slate-300 group-hover:text-purple-300 font-mono text-[10px] transition">
                              {conv.messagesCount || 0} tin nhắn
                            </span>
                          </Link>
                        ))}
                      </div>
                    )}
                  </div>
                </>
              ) : null}
            </div>

            <div className="p-3 border-t border-slate-800 bg-slate-900/90 flex justify-end gap-2">
              {detailData && (
                <button
                  onClick={() => {
                    handleCloseDetailModal();
                    handleOpenEditModal(detailData);
                  }}
                  className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-teal-400 hover:text-teal-300 text-xs font-semibold transition border border-slate-700 flex items-center gap-1"
                >
                  ✏️ Sửa Thông Tin
                </button>
              )}
              <button
                onClick={handleCloseDetailModal}
                className="px-3 py-1.5 rounded-lg bg-slate-800 text-slate-400 hover:text-white text-xs transition"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Edit Contact Modal */}
      {editModalOpen && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700/80 rounded-2xl w-full max-w-lg overflow-hidden shadow-2xl">
            <div className="p-4 border-b border-slate-800 flex items-center justify-between bg-slate-900/90">
              <h3 className="font-bold text-white text-sm flex items-center gap-2">
                <span>✏️ Cập Nhật Hồ Sơ Khách Hàng</span>
                <span className="text-[10px] font-mono text-slate-400">ID #{editForm.id}</span>
              </h3>
              <button
                onClick={() => setEditModalOpen(false)}
                className="text-slate-400 hover:text-white text-lg"
              >
                &times;
              </button>
            </div>

            <form onSubmit={handleSubmitUpdate} className="p-4 space-y-3.5 text-xs">
              <div>
                <label className="block text-slate-400 mb-1 font-semibold">Họ Tên Khách Hàng</label>
                <input
                  type="text"
                  value={editForm.name}
                  onChange={e => setEditForm(prev => ({ ...prev, name: e.target.value }))}
                  placeholder="Ví dụ: Trần Văn A"
                  className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-teal-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-slate-400 mb-1 font-semibold">Số Điện Thoại</label>
                  <input
                    type="text"
                    value={editForm.phone}
                    onChange={e => setEditForm(prev => ({ ...prev, phone: e.target.value }))}
                    placeholder="0909xxxxxx"
                    className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 text-white font-mono focus:outline-none focus:border-teal-500"
                  />
                </div>
                <div>
                  <label className="block text-slate-400 mb-1 font-semibold">Email</label>
                  <input
                    type="email"
                    value={editForm.email}
                    onChange={e => setEditForm(prev => ({ ...prev, email: e.target.value }))}
                    placeholder="example@gmail.com"
                    className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-teal-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-slate-400 mb-1 font-semibold">Giai Đoạn Phân Hạng (Stage)</label>
                <select
                  value={editForm.stage}
                  onChange={e => setEditForm(prev => ({ ...prev, stage: e.target.value }))}
                  className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-teal-500"
                >
                  <option value="prospect">Prospect (Mới tiếp cận)</option>
                  <option value="warm">Warm (Có tương tác)</option>
                  <option value="hot">🔥 Hot Lead (Đã để lại SĐT / Sẵn sàng mua)</option>
                  <option value="customer">Customer (Đã thanh toán / Chốt đơn)</option>
                  <option value="lost">Lost (Không có nhu cầu / Hủy)</option>
                </select>
              </div>

              <div>
                <label className="block text-slate-400 mb-1 font-semibold">Ghi Chú Nhu Cầu & Đàm Phán</label>
                <textarea
                  rows={3}
                  value={editForm.notes}
                  onChange={e => setEditForm(prev => ({ ...prev, notes: e.target.value }))}
                  placeholder="Nhập ghi chú yêu cầu của khách (ví dụ: đi 4 người lớn, cần visa gấp...)"
                  className="w-full bg-slate-950 border border-slate-700 rounded-xl p-3 text-white focus:outline-none focus:border-teal-500"
                />
              </div>

              <div className="pt-2 flex justify-end gap-2 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setEditModalOpen(false)}
                  className="px-3.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 transition"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 rounded-lg bg-teal-600 hover:bg-teal-500 text-white font-semibold transition shadow-sm"
                >
                  Lưu Cập Nhật
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Create Contact Modal */}
      {createModalOpen && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700/80 rounded-2xl w-full max-w-lg overflow-hidden shadow-2xl">
            <div className="p-4 border-b border-slate-800 flex items-center justify-between bg-slate-900/90">
              <h3 className="font-bold text-white text-sm flex items-center gap-2">
                <span>➕ Thêm Mới Hồ Sơ Khách Hàng</span>
              </h3>
              <button
                onClick={() => setCreateModalOpen(false)}
                className="text-slate-400 hover:text-white text-lg"
              >
                &times;
              </button>
            </div>

            <form onSubmit={handleSubmitCreate} className="p-4 space-y-3.5 text-xs">
              <div>
                <label className="block text-slate-400 mb-1 font-semibold">Họ Tên Khách Hàng</label>
                <input
                  type="text"
                  value={createForm.name}
                  onChange={e => setCreateForm(prev => ({ ...prev, name: e.target.value }))}
                  placeholder="Ví dụ: Nguyễn Văn B"
                  className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-teal-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-slate-400 mb-1 font-semibold">
                    Số Điện Thoại <span className="text-rose-400">*</span>
                  </label>
                  <input
                    type="text"
                    required
                    value={createForm.phone}
                    onChange={e => setCreateForm(prev => ({ ...prev, phone: e.target.value }))}
                    placeholder="0909xxxxxx"
                    className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 text-white font-mono focus:outline-none focus:border-teal-500"
                  />
                </div>
                <div>
                  <label className="block text-slate-400 mb-1 font-semibold">Kênh Tiếp Cận</label>
                  <select
                    value={createForm.channel}
                    onChange={e => setCreateForm(prev => ({ ...prev, channel: e.target.value }))}
                    className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-teal-500"
                  >
                    <option value="manual">📞 Trực Tiếp / Gọi Điện</option>
                    <option value="telegram">📱 Telegram</option>
                    <option value="zalo">💬 Zalo</option>
                    <option value="web_widget">💻 Website</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-slate-400 mb-1 font-semibold">Email</label>
                  <input
                    type="email"
                    value={createForm.email}
                    onChange={e => setCreateForm(prev => ({ ...prev, email: e.target.value }))}
                    placeholder="example@gmail.com"
                    className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-teal-500"
                  />
                </div>
                <div>
                  <label className="block text-slate-400 mb-1 font-semibold">Phân Hạng (Stage)</label>
                  <select
                    value={createForm.stage}
                    onChange={e => setCreateForm(prev => ({ ...prev, stage: e.target.value }))}
                    className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3 py-2 text-white focus:outline-none focus:border-teal-500"
                  >
                    <option value="hot">🔥 Hot Lead (Có SĐT)</option>
                    <option value="warm">Warm (Quan tâm)</option>
                    <option value="prospect">Prospect</option>
                    <option value="customer">Customer (Đã mua)</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-slate-400 mb-1 font-semibold">Ghi Chú Ban Đầu</label>
                <textarea
                  rows={3}
                  value={createForm.notes}
                  onChange={e => setCreateForm(prev => ({ ...prev, notes: e.target.value }))}
                  placeholder="Ghi chú nhu cầu tour, số lượng người, thời gian khởi hành..."
                  className="w-full bg-slate-950 border border-slate-700 rounded-xl p-3 text-white focus:outline-none focus:border-teal-500"
                />
              </div>

              <div className="pt-2 flex justify-end gap-2 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setCreateModalOpen(false)}
                  className="px-3.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 transition"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 rounded-lg bg-teal-600 hover:bg-teal-500 text-white font-semibold transition shadow-sm"
                >
                  Tạo Khách Hàng
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal tạo khách hàng kết thúc */}
    </AdminLayout>
  );
};
