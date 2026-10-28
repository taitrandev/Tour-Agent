import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';

/**
 * Interface biểu diễn thông tin khách hàng gắn với đơn hàng.
 */
interface OrderContact {
  id: number;
  name: string | null;
  phone: string | null;
  email: string | null;
  telegram_username: string | null;
  channel: string;
}

/**
 * Interface biểu diễn tour du lịch gắn với đơn hàng.
 */
interface OrderTravel {
  id: number;
  title: string;
  code: string | null;
  price_from: number;
}

/**
 * Interface đại diện cho một Đơn Hàng Tour Du Lịch từ API.
 */
interface TourOrderItem {
  id: number;
  order_code: string;
  contact_id: number | null;
  contact: OrderContact | null;
  travel_id: number | null;
  travel: OrderTravel | null;
  travel_departure_id: number | null;
  conversation_id: number | null;
  channel: string;
  pax_adults: number;
  pax_children: number;
  total_price: number;
  deposit_percent: number;
  deposit_amount: number;
  deposit_status: string;
  deposit_paid_at: string | null;
  remaining_amount: number;
  remaining_status: string;
  remaining_paid_at: string | null;
  payment_account_note: string | null;
  visa_status: string;
  visa_notes: string | null;
  operator_name: string | null;
  operator_booking_ref: string | null;
  departure_date: string | null;
  tour_guide_name: string | null;
  tour_guide_phone: string | null;
  tour_guide_notes: string | null;
  pre_departure_notified_at: string | null;
  status: string;
  ai_summary: string | null;
  notes: string | null;
  created_at: string;
  updated_at: string | null;
}

/**
 * Interface các chỉ số thống kê đường ống đơn hàng thời gian thực.
 */
interface OrderMetrics {
  total: number;
  lead_consulting: number;
  deposit_pending: number;
  visa_processing: number;
  final_payment_pending: number;
  t3_departure: number;
}

const API_BASE_URL = 'http://localhost:5132';

/**
 * Trang Quản Trị Đơn Tour & Tiến Trình Visa (Order & Visa Pipeline Desk).
 * Tái hiện 100% UI Dark Glassmorphism và đầy đủ logic từ orders.blade.php.
 */
export const AdminOrdersPage: React.FC = () => {
  // State dữ liệu danh sách và bộ lọc
  const [orders, setOrders] = useState<TourOrderItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [currentStatus, setCurrentStatus] = useState<string>('');
  const [searchKeyword, setSearchKeyword] = useState<string>('');
  const [metrics, setMetrics] = useState<OrderMetrics>({
    total: 0,
    lead_consulting: 0,
    deposit_pending: 0,
    visa_processing: 0,
    final_payment_pending: 0,
    t3_departure: 0,
  });

  // State Modal Chi Tiết & AI Briefing
  const [detailModalOpen, setDetailModalOpen] = useState<boolean>(false);
  const [selectedOrder, setSelectedOrder] = useState<TourOrderItem | null>(null);

  // State Modal Xác Nhận Cọc 50%
  const [depositModalOpen, setDepositModalOpen] = useState<boolean>(false);
  const [depositOrder, setDepositOrder] = useState<TourOrderItem | null>(null);
  const [depositAccountNote, setDepositAccountNote] = useState<string>('');
  const [submittingDeposit, setSubmittingDeposit] = useState<boolean>(false);

  // State Modal Cập Nhật Visa
  const [visaModalOpen, setVisaModalOpen] = useState<boolean>(false);
  const [visaOrder, setVisaOrder] = useState<TourOrderItem | null>(null);
  const [visaStatusInput, setVisaStatusInput] = useState<string>('awaiting_docs');
  const [visaNotesInput, setVisaNotesInput] = useState<string>('');
  const [operatorNameInput, setOperatorNameInput] = useState<string>('');
  const [operatorBookingRefInput, setOperatorBookingRefInput] = useState<string>('');
  const [submittingVisa, setSubmittingVisa] = useState<boolean>(false);
  const [zaloVisaMessage, setZaloVisaMessage] = useState<string | null>(null);

  // State Modal Thu Nốt 50%
  const [finalPaymentModalOpen, setFinalPaymentModalOpen] = useState<boolean>(false);
  const [finalPaymentOrder, setFinalPaymentOrder] = useState<TourOrderItem | null>(null);
  const [submittingFinalPayment, setSubmittingFinalPayment] = useState<boolean>(false);

  // State Modal Dặn Dò T-3 & HDV
  const [t3ModalOpen, setT3ModalOpen] = useState<boolean>(false);
  const [t3Order, setT3Order] = useState<TourOrderItem | null>(null);
  const [guideNameInput, setGuideNameInput] = useState<string>('');
  const [guidePhoneInput, setGuidePhoneInput] = useState<string>('');
  const [guideNotesInput, setGuideNotesInput] = useState<string>('');
  const [submittingT3, setSubmittingT3] = useState<boolean>(false);
  const [zaloT3Message, setZaloT3Message] = useState<string | null>(null);

  // State Modal Tạo Mới / Sửa Đơn Tour
  const [formModalOpen, setFormModalOpen] = useState<boolean>(false);
  const [formIsEditing, setFormIsEditing] = useState<boolean>(false);
  const [editingOrderId, setEditingOrderId] = useState<number | null>(null);
  const [formPaxAdults, setFormPaxAdults] = useState<number>(1);
  const [formPaxChildren, setFormPaxChildren] = useState<number>(0);
  const [formTotalPrice, setFormTotalPrice] = useState<number>(0);
  const [formDepartureDate, setFormDepartureDate] = useState<string>('');
  const [formPaymentNote, setFormPaymentNote] = useState<string>('');
  const [formNotes, setFormNotes] = useState<string>('');
  const [submittingForm, setSubmittingForm] = useState<boolean>(false);

  // State Toast Notification
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const showToast = useCallback((msg: string) => {
    setToastMessage(msg);
    setTimeout(() => {
      setToastMessage(null);
    }, 4000);
  }, []);

  /**
   * Gọi API tải danh sách đơn hàng tour kèm bộ lọc và chỉ số thống kê.
   */
  const fetchOrders = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (currentStatus) params.append('status', currentStatus);
      if (searchKeyword.trim()) params.append('search', searchKeyword.trim());
      params.append('per_page', '50');

      const res = await fetch(`${API_BASE_URL}/api/v1/admin/orders?${params.toString()}`);
      if (!res.ok) throw new Error(`HTTP error ${res.status}`);
      const json = await res.json();
      if (json.success) {
        setOrders(json.data || []);
        if (json.metrics) {
          setMetrics(json.metrics);
        }
      }
    } catch (err) {
      console.error('Lỗi khi tải danh sách đơn hàng tour:', err);
      showToast('❌ Không thể tải danh sách đơn hàng tour. Hãy thử lại.');
    } finally {
      setLoading(false);
    }
  }, [currentStatus, searchKeyword, showToast]);

  useEffect(() => {
    fetchOrders();
  }, [fetchOrders]);

  /**
   * Định dạng tiền tệ VNĐ.
   */
  const formatCurrency = (val: number | null | undefined): string => {
    if (val === null || val === undefined || isNaN(val)) return '0đ';
    return new Intl.NumberFormat('vi-VN').format(val) + 'đ';
  };

  /**
   * Định dạng ngày tháng hiển thị.
   */
  const formatDate = (dateStr: string | null | undefined): string => {
    if (!dateStr) return '—';
    try {
      const d = new Date(dateStr);
      return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
    } catch {
      return dateStr;
    }
  };

  /**
   * Mở modal chi tiết và deal briefing call summary.
   */
  const openDetailModal = (order: TourOrderItem) => {
    setSelectedOrder(order);
    setDetailModalOpen(true);
  };

  /**
   * Mở modal xác nhận cọc 50%.
   */
  const openDepositModal = (order: TourOrderItem) => {
    setDepositOrder(order);
    setDepositAccountNote(order.payment_account_note || 'STK: 1903688888 - Techcombank (Công ty Du Lịch Travel AI)');
    setDepositModalOpen(true);
  };

  /**
   * Gửi yêu cầu xác nhận cọc 50%.
   */
  const handleConfirmDeposit = async () => {
    if (!depositOrder) return;
    setSubmittingDeposit(true);
    try {
      const res = await fetch(`${API_BASE_URL}/api/v1/admin/orders/${depositOrder.id}/deposit`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ payment_account_note: depositAccountNote }),
      });
      const json = await res.json();
      if (json.success) {
        showToast(`✅ Đã xác nhận cọc 50% thành công cho đơn #${depositOrder.order_code}!`);
        setDepositModalOpen(false);
        fetchOrders();
      } else {
        showToast(`❌ ${json.message || 'Lỗi khi xác nhận cọc'}`);
      }
    } catch (err) {
      console.error(err);
      showToast('❌ Lỗi kết nối khi xác nhận cọc.');
    } finally {
      setSubmittingDeposit(false);
    }
  };

  /**
   * Mở modal cập nhật Visa.
   */
  const openVisaModal = (order: TourOrderItem) => {
    setVisaOrder(order);
    setVisaStatusInput(order.visa_status || 'awaiting_docs');
    setVisaNotesInput(order.visa_notes || '');
    setOperatorNameInput(order.operator_name || '');
    setOperatorBookingRefInput(order.operator_booking_ref || '');
    setZaloVisaMessage(null);
    setVisaModalOpen(true);
  };

  /**
   * Gửi yêu cầu cập nhật Visa.
   */
  const handleConfirmVisa = async () => {
    if (!visaOrder) return;
    setSubmittingVisa(true);
    try {
      const res = await fetch(`${API_BASE_URL}/api/v1/admin/orders/${visaOrder.id}/visa`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          visa_status: visaStatusInput,
          visa_notes: visaNotesInput,
          operator_name: operatorNameInput,
          operator_booking_ref: operatorBookingRefInput,
        }),
      });
      const json = await res.json();
      if (json.success) {
        showToast(`✅ Cập nhật tiến trình Visa thành công cho đơn #${visaOrder.order_code}!`);
        if (json.zalo_message) {
          setZaloVisaMessage(json.zalo_message);
        } else {
          setVisaModalOpen(false);
        }
        fetchOrders();
      } else {
        showToast(`❌ ${json.message || 'Lỗi khi cập nhật visa'}`);
      }
    } catch (err) {
      console.error(err);
      showToast('❌ Lỗi kết nối khi cập nhật visa.');
    } finally {
      setSubmittingVisa(false);
    }
  };

  /**
   * Mở modal xác nhận thu nốt 50%.
   */
  const openFinalPaymentModal = (order: TourOrderItem) => {
    setFinalPaymentOrder(order);
    setFinalPaymentModalOpen(true);
  };

  /**
   * Gửi yêu cầu xác nhận thu nốt 50%.
   */
  const handleConfirmFinalPayment = async () => {
    if (!finalPaymentOrder) return;
    setSubmittingFinalPayment(true);
    try {
      const res = await fetch(`${API_BASE_URL}/api/v1/admin/orders/${finalPaymentOrder.id}/final-payment`, {
        method: 'POST',
      });
      const json = await res.json();
      if (json.success) {
        showToast(`🎉 Đã xác nhận thu đủ 100% cho đơn #${finalPaymentOrder.order_code}! Sẵn sàng khởi hành.`);
        setFinalPaymentModalOpen(false);
        fetchOrders();
      } else {
        showToast(`❌ ${json.message || 'Lỗi khi xác nhận thanh toán'}`);
      }
    } catch (err) {
      console.error(err);
      showToast('❌ Lỗi kết nối khi xác nhận thanh toán.');
    } finally {
      setSubmittingFinalPayment(false);
    }
  };

  /**
   * Mở modal dặn dò T-3 và bàn giao HDV.
   */
  const openT3Modal = (order: TourOrderItem) => {
    setT3Order(order);
    setGuideNameInput(order.tour_guide_name || 'Nguyễn Văn Tuấn');
    setGuidePhoneInput(order.tour_guide_phone || '0912.888.999');
    setGuideNotesInput(order.tour_guide_notes || 'Tập trung tại Cột số 10 - Ga đi Quốc tế sân bay Tân Sơn Nhất / Nội Bài');
    setZaloT3Message(null);
    setT3ModalOpen(true);
  };

  /**
   * Gửi yêu cầu lưu thông tin HDV và dặn dò T-3.
   */
  const handleConfirmT3 = async () => {
    if (!t3Order) return;
    setSubmittingT3(true);
    try {
      const res = await fetch(`${API_BASE_URL}/api/v1/admin/orders/${t3Order.id}/t3-notified`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          tour_guide_name: guideNameInput,
          tour_guide_phone: guidePhoneInput,
          tour_guide_notes: guideNotesInput,
        }),
      });
      const json = await res.json();
      if (json.success) {
        showToast(`✈️ Đã lưu thông tin HDV và đánh dấu dặn dò T-3 cho đơn #${t3Order.order_code}!`);
        if (json.zalo_message) {
          setZaloT3Message(json.zalo_message);
        } else {
          setT3ModalOpen(false);
        }
        fetchOrders();
      } else {
        showToast(`❌ ${json.message || 'Lỗi khi lưu thông tin HDV'}`);
      }
    } catch (err) {
      console.error(err);
      showToast('❌ Lỗi kết nối khi dặn dò T-3.');
    } finally {
      setSubmittingT3(false);
    }
  };

  /**
   * Sao chép văn bản vào Clipboard.
   */
  const copyToClipboard = (text: string, label: string) => {
    navigator.clipboard.writeText(text);
    showToast(`📋 Đã sao chép ${label} vào bộ nhớ tạm!`);
  };

  /**
   * Xóa đơn hàng (soft delete).
   */
  const handleDeleteOrder = async (order: TourOrderItem) => {
    if (!window.confirm(`Bạn có chắc muốn xóa đơn hàng #${order.order_code} không?`)) return;
    try {
      const res = await fetch(`${API_BASE_URL}/api/v1/admin/orders/${order.id}`, {
        method: 'DELETE',
      });
      const json = await res.json();
      if (json.success) {
        showToast(`🗑️ Đã xóa đơn hàng #${order.order_code} thành công.`);
        fetchOrders();
      } else {
        showToast(`❌ ${json.message || 'Lỗi khi xóa đơn hàng'}`);
      }
    } catch (err) {
      console.error(err);
      showToast('❌ Lỗi kết nối khi xóa đơn hàng.');
    }
  };

  /**
   * Mở modal tạo mới đơn thủ công.
   */
  const openCreateModal = () => {
    setFormIsEditing(false);
    setEditingOrderId(null);
    setFormPaxAdults(2);
    setFormPaxChildren(0);
    setFormTotalPrice(59800000);
    setFormDepartureDate('');
    setFormPaymentNote('STK: 1903688888 - Techcombank (Công ty Du Lịch Travel AI)');
    setFormNotes('');
    setFormModalOpen(true);
  };

  /**
   * Mở modal chỉnh sửa đơn hàng.
   */
  const openEditModal = (order: TourOrderItem) => {
    setFormIsEditing(true);
    setEditingOrderId(order.id);
    setFormPaxAdults(order.pax_adults);
    setFormPaxChildren(order.pax_children);
    setFormTotalPrice(order.total_price);
    setFormDepartureDate(order.departure_date || '');
    setFormPaymentNote(order.payment_account_note || '');
    setFormNotes(order.notes || '');
    setFormModalOpen(true);
  };

  /**
   * Submit form tạo / sửa đơn hàng.
   */
  const handleSaveOrderForm = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmittingForm(true);
    try {
      const payload: any = {
        pax_adults: formPaxAdults,
        pax_children: formPaxChildren,
        total_price: formTotalPrice,
        departure_date: formDepartureDate || null,
        payment_account_note: formPaymentNote,
        notes: formNotes,
      };

      let url = `${API_BASE_URL}/api/v1/admin/orders`;
      let method = 'POST';

      if (formIsEditing && editingOrderId) {
        url = `${API_BASE_URL}/api/v1/admin/orders/${editingOrderId}`;
        method = 'PUT';
      }

      const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });
      const json = await res.json();
      if (json.success) {
        showToast(formIsEditing ? '✅ Đã cập nhật đơn hàng thành công!' : '🎉 Đã tạo đơn hàng mới thành công!');
        setFormModalOpen(false);
        fetchOrders();
      } else {
        showToast(`❌ ${json.message || 'Lỗi khi lưu đơn hàng'}`);
      }
    } catch (err) {
      console.error(err);
      showToast('❌ Lỗi kết nối khi lưu đơn hàng.');
    } finally {
      setSubmittingForm(false);
    }
  };

  /**
   * Trả về badge trạng thái pipeline.
   */
  const renderStatusBadge = (status: string) => {
    switch (status) {
      case 'lead_consulting':
        return <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-rose-500/20 text-rose-300 border border-rose-500/30">🔥 Chờ tư vấn</span>;
      case 'deposit_pending':
        return <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30">💵 Chờ cọc 50%</span>;
      case 'visa_processing':
        return <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-sky-500/20 text-sky-300 border border-sky-500/30">🛂 Đang làm Visa</span>;
      case 'final_payment_pending':
        return <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">💰 Cần thu 50%</span>;
      case 'ready_to_depart':
        return <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">🎉 Sẵn sàng bay</span>;
      case 'completed':
        return <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-slate-500/20 text-slate-300 border border-slate-500/30">Hoàn thành</span>;
      case 'cancelled':
        return <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-zinc-500/20 text-zinc-400 border border-zinc-500/30">Đã hủy</span>;
      default:
        return <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-slate-800 text-slate-400">{status}</span>;
    }
  };

  /**
   * Trả về badge tiến trình Visa.
   */
  const renderVisaBadge = (visaStatus: string) => {
    switch (visaStatus) {
      case 'not_required':
        return <span className="text-slate-500 text-[11px]">Không cần</span>;
      case 'awaiting_docs':
        return <span className="px-1.5 py-0.5 rounded text-[10px] font-medium bg-amber-500/20 text-amber-300">Chờ hồ sơ</span>;
      case 'sent_to_operator':
        return <span className="px-1.5 py-0.5 rounded text-[10px] font-medium bg-blue-500/20 text-blue-300">Đã nộp cty đối tác</span>;
      case 'processing':
        return <span className="px-1.5 py-0.5 rounded text-[10px] font-medium bg-sky-500/20 text-sky-300">Đang xét duyệt</span>;
      case 'approved':
        return <span className="px-1.5 py-0.5 rounded text-[10px] font-bold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">✅ ĐẬU VISA</span>;
      case 'rejected':
        return <span className="px-1.5 py-0.5 rounded text-[10px] font-bold bg-rose-500/20 text-rose-400 border border-rose-500/30">❌ RỚT VISA</span>;
      default:
        return <span className="text-slate-400 text-[11px]">{visaStatus}</span>;
    }
  };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 flex flex-col font-sans antialiased selection:bg-sky-500 selection:text-white">
      {/* Toast Notification */}
      {toastMessage && (
        <div className="fixed top-5 right-5 z-50 px-4 py-2.5 rounded-xl bg-slate-900 border border-sky-500/40 text-white shadow-2xl text-xs flex items-center gap-2 animate-bounce">
          <span>{toastMessage}</span>
        </div>
      )}

      {/* Top Navigation Header */}
      <header className="h-16 border-b border-slate-800 bg-slate-900/80 backdrop-blur px-6 flex items-center justify-between shrink-0 z-20 sticky top-0">
        <div className="flex items-center gap-4">
          <Link
            to="/admin/products"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-semibold border border-slate-700/80 transition shadow-sm"
          >
            <span>← Dashboard</span>
          </Link>

          <div className="h-6 w-px bg-slate-800"></div>

          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-sky-500 to-indigo-600 flex items-center justify-center font-bold text-white text-base shadow-lg shadow-sky-500/20">
              📦
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-sm sm:text-base font-bold text-white tracking-tight">
                  Quản Lý Đơn Tour & Tiến Trình Visa
                </h1>
                <span className="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded bg-sky-500/20 text-sky-300 border border-sky-500/30">
                  Mô Hình Agency B2B2C
                </span>
              </div>
              <p className="text-xs text-slate-400">
                Quy trình Cọc 50% ➔ Làm Visa qua Cty Du Lịch ➔ Thu 50% ➔ Điều phối HDV trước 3 ngày
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2.5">
          <Link
            to="/admin/contacts"
            className="px-3 py-1.5 rounded-lg bg-teal-500/10 hover:bg-teal-500/20 text-teal-300 text-xs font-semibold border border-teal-500/30 transition flex items-center gap-1.5"
          >
            <span>👥 Khách Hàng (CRM)</span>
          </Link>

          <Link
            to="/admin/conversations"
            className="px-3 py-1.5 rounded-lg bg-purple-500/10 hover:bg-purple-500/20 text-purple-300 text-xs font-semibold border border-purple-500/30 transition flex items-center gap-1.5"
          >
            <span>💬 Giám Sát Chat</span>
          </Link>

          <Link
            to="/admin/offers"
            className="px-3 py-1.5 rounded-lg bg-amber-500/10 hover:bg-amber-500/20 text-amber-300 text-xs font-semibold border border-amber-500/30 transition flex items-center gap-1.5"
          >
            <span>🎁 Grand Slam Offers</span>
          </Link>

          <button
            onClick={openCreateModal}
            className="px-3 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-semibold transition flex items-center gap-1.5 shadow-sm shadow-sky-600/20"
          >
            <span>+ Thêm Đơn Thủ Công</span>
          </button>

          <button
            onClick={fetchOrders}
            className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium border border-slate-700 transition flex items-center gap-1.5"
          >
            <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            <span className="hidden sm:inline">Làm mới</span>
          </button>
        </div>
      </header>

      {/* Main Content Area */}
      <main className="flex-1 overflow-y-auto p-6 max-w-7xl w-full mx-auto space-y-6">
        {/* Real-time Pipeline Metrics Overview Grid */}
        <div className="grid grid-cols-2 sm:grid-cols-6 gap-3">
          <div
            onClick={() => setCurrentStatus('')}
            className={`border rounded-2xl p-3.5 shadow-sm cursor-pointer transition ${currentStatus === '' ? 'bg-sky-950/40 border-sky-500/60' : 'bg-slate-900/60 border-slate-800 hover:border-slate-700'}`}
          >
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-semibold text-slate-400">Tổng Đơn</span>
              <span className="text-base">📦</span>
            </div>
            <div className="text-xl font-black text-white mt-1.5 font-mono">{metrics.total}</div>
            <div className="text-[10px] text-slate-500">Toàn bộ pipeline</div>
          </div>

          <div
            onClick={() => setCurrentStatus('lead_consulting')}
            className={`border rounded-2xl p-3.5 shadow-sm cursor-pointer transition ${currentStatus === 'lead_consulting' ? 'bg-rose-950/40 border-rose-500/60' : 'bg-slate-900/60 border-slate-800 hover:border-slate-700'}`}
          >
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-semibold text-rose-400">Chờ Tư Vấn</span>
              <span className="text-base">🔥</span>
            </div>
            <div className="text-xl font-black text-rose-400 mt-1.5 font-mono">{metrics.lead_consulting}</div>
            <div className="text-[10px] text-rose-400/80">Sale cần gọi ngay</div>
          </div>

          <div
            onClick={() => setCurrentStatus('deposit_pending')}
            className={`border rounded-2xl p-3.5 shadow-sm cursor-pointer transition ${currentStatus === 'deposit_pending' ? 'bg-amber-950/40 border-amber-500/60' : 'bg-slate-900/60 border-slate-800 hover:border-slate-700'}`}
          >
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-semibold text-amber-400">Chờ Cọc 50%</span>
              <span className="text-base">💵</span>
            </div>
            <div className="text-xl font-black text-amber-400 mt-1.5 font-mono">{metrics.deposit_pending}</div>
            <div className="text-[10px] text-amber-400/80">Đã gửi số tài khoản</div>
          </div>

          <div
            onClick={() => setCurrentStatus('visa_processing')}
            className={`border rounded-2xl p-3.5 shadow-sm cursor-pointer transition ${currentStatus === 'visa_processing' ? 'bg-sky-950/40 border-sky-500/60' : 'bg-slate-900/60 border-slate-800 hover:border-slate-700'}`}
          >
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-semibold text-sky-400">Đang Làm Visa</span>
              <span className="text-base">🛂</span>
            </div>
            <div className="text-xl font-black text-sky-400 mt-1.5 font-mono">{metrics.visa_processing}</div>
            <div className="text-[10px] text-sky-400/80">Chờ cty du lịch báo</div>
          </div>

          <div
            onClick={() => setCurrentStatus('final_payment_pending')}
            className={`border rounded-2xl p-3.5 shadow-sm cursor-pointer transition ${currentStatus === 'final_payment_pending' ? 'bg-emerald-950/40 border-emerald-500/60' : 'bg-slate-900/60 border-slate-800 hover:border-slate-700'}`}
          >
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-semibold text-emerald-400">Cần Thu Nốt 50%</span>
              <span className="text-base">💰</span>
            </div>
            <div className="text-xl font-black text-emerald-400 mt-1.5 font-mono">{metrics.final_payment_pending}</div>
            <div className="text-[10px] text-emerald-400/80">Đã đậu visa</div>
          </div>

          <div
            onClick={() => setCurrentStatus('t3_departure')}
            className={`border rounded-2xl p-3.5 shadow-sm cursor-pointer transition ${currentStatus === 't3_departure' ? 'bg-indigo-950/40 border-indigo-500/60' : 'bg-slate-900/60 border-slate-800 hover:border-slate-700'}`}
          >
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-semibold text-indigo-400">Sắp Bay (T-3)</span>
              <span className="text-base">✈️</span>
            </div>
            <div className="text-xl font-black text-indigo-400 mt-1.5 font-mono">{metrics.t3_departure}</div>
            <div className="text-[10px] text-indigo-400/80">Bàn giao HDV</div>
          </div>
        </div>

        {/* Filter & Search Bar */}
        <div className="bg-slate-900/70 border border-slate-800 rounded-2xl p-4 flex flex-col sm:flex-row gap-3 items-center justify-between">
          <div className="flex flex-wrap items-center gap-1.5 w-full sm:w-auto text-xs">
            <button
              onClick={() => setCurrentStatus('')}
              className={`px-3 py-1.5 rounded-lg font-semibold transition ${currentStatus === '' ? 'bg-sky-600 text-white' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'}`}
            >
              Tất cả
            </button>
            <button
              onClick={() => setCurrentStatus('lead_consulting')}
              className={`px-3 py-1.5 rounded-lg font-semibold transition ${currentStatus === 'lead_consulting' ? 'bg-rose-600 text-white' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'}`}
            >
              🔥 Chờ tư vấn
            </button>
            <button
              onClick={() => setCurrentStatus('deposit_pending')}
              className={`px-3 py-1.5 rounded-lg font-semibold transition ${currentStatus === 'deposit_pending' ? 'bg-amber-600 text-white' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'}`}
            >
              💵 Chờ cọc 50%
            </button>
            <button
              onClick={() => setCurrentStatus('visa_processing')}
              className={`px-3 py-1.5 rounded-lg font-semibold transition ${currentStatus === 'visa_processing' ? 'bg-sky-600 text-white' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'}`}
            >
              🛂 Đang làm Visa
            </button>
            <button
              onClick={() => setCurrentStatus('final_payment_pending')}
              className={`px-3 py-1.5 rounded-lg font-semibold transition ${currentStatus === 'final_payment_pending' ? 'bg-emerald-600 text-white' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'}`}
            >
              💰 Cần thu 50%
            </button>
            <button
              onClick={() => setCurrentStatus('t3_departure')}
              className={`px-3 py-1.5 rounded-lg font-semibold transition ${currentStatus === 't3_departure' ? 'bg-indigo-600 text-white' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'}`}
            >
              ✈️ Sắp bay (T-3)
            </button>
            <button
              onClick={() => setCurrentStatus('ready_to_depart')}
              className={`px-3 py-1.5 rounded-lg font-semibold transition ${currentStatus === 'ready_to_depart' ? 'bg-teal-600 text-white' : 'bg-slate-800 text-slate-300 hover:bg-slate-700'}`}
            >
              🎉 Sẵn sàng bay
            </button>
          </div>

          {/* Search input */}
          <div className="relative w-full sm:w-72">
            <span className="absolute inset-y-0 left-0 flex items-center pl-3 pointer-events-none text-slate-500">
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </span>
            <input
              type="text"
              value={searchKeyword}
              onChange={(e) => setSearchKeyword(e.target.value)}
              placeholder="Mã đơn, tên khách, SĐT, tour..."
              className="w-full pl-9 pr-3 py-1.5 rounded-xl bg-slate-950 border border-slate-700/80 text-white text-xs placeholder-slate-500 focus:outline-none focus:border-sky-500 transition"
            />
          </div>
        </div>

        {/* Orders Table */}
        <div className="bg-slate-900/60 border border-slate-800 rounded-2xl overflow-hidden shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-xs">
              <thead>
                <tr className="border-b border-slate-800 bg-slate-900/80 text-slate-400 font-semibold uppercase text-[10px] tracking-wider">
                  <th className="py-3 px-4">Mã Đơn / Kênh</th>
                  <th className="py-3 px-4">Khách Hàng</th>
                  <th className="py-3 px-4">Tour Du Lịch</th>
                  <th className="py-3 px-4">Tiến Độ Cọc & Tiền</th>
                  <th className="py-3 px-4">Tiến Trình Visa</th>
                  <th className="py-3 px-4">Khởi Hành & HDV (T-3)</th>
                  <th className="py-3 px-4 text-right">Thao Tác Pipeline</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60">
                {loading ? (
                  <tr>
                    <td colSpan={7} className="py-12 text-center text-slate-500 text-xs">
                      <div className="flex flex-col items-center justify-center gap-2">
                        <div className="w-6 h-6 border-2 border-sky-500 border-t-transparent rounded-full animate-spin"></div>
                        <span>Đang tải danh sách đơn hàng...</span>
                      </div>
                    </td>
                  </tr>
                ) : orders.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="py-12 text-center text-slate-500 text-xs">
                      Không tìm thấy đơn hàng nào phù hợp với bộ lọc hiện tại.
                    </td>
                  </tr>
                ) : (
                  orders.map((order) => (
                    <tr key={order.id} className="hover:bg-slate-800/40 transition">
                      {/* Cột 1: Mã đơn & Kênh */}
                      <td className="py-3 px-4">
                        <div className="flex items-center gap-1.5">
                          <button
                            onClick={() => copyToClipboard(order.order_code, 'Mã đơn')}
                            className="font-mono font-bold text-sky-400 hover:text-sky-300 hover:underline"
                            title="Nhấp để sao chép mã"
                          >
                            #{order.order_code}
                          </button>
                        </div>
                        <div className="flex items-center gap-1.5 mt-1">
                          <span className="text-[10px] px-1.5 py-0.2 rounded bg-slate-800 border border-slate-700 text-slate-400 font-mono">
                            {order.channel}
                          </span>
                          {renderStatusBadge(order.status)}
                        </div>
                        <div className="text-[10px] text-slate-500 mt-1 font-mono">
                          {formatDate(order.created_at)}
                        </div>
                      </td>

                      {/* Cột 2: Khách Hàng */}
                      <td className="py-3 px-4">
                        <div className="font-semibold text-white">
                          {order.contact?.name || 'Chưa cập nhật tên'}
                        </div>
                        {order.contact?.phone ? (
                          <div className="flex items-center gap-1.5 mt-0.5">
                            <a
                              href={`tel:${order.contact.phone}`}
                              className="font-mono text-emerald-400 hover:underline font-bold"
                            >
                              📞 {order.contact.phone}
                            </a>
                            <button
                              onClick={() => copyToClipboard(order.contact?.phone || '', 'SĐT')}
                              className="text-slate-400 hover:text-white"
                              title="Sao chép SĐT"
                            >
                              📋
                            </button>
                          </div>
                        ) : (
                          <div className="text-slate-500 italic text-[11px]">Chưa có SĐT</div>
                        )}
                        {order.contact?.telegram_username && (
                          <div className="text-[10px] text-sky-400 font-mono">
                            @{order.contact.telegram_username}
                          </div>
                        )}
                      </td>

                      {/* Cột 3: Tour Du Lịch */}
                      <td className="py-3 px-4 max-w-xs">
                        <div className="font-medium text-slate-200 line-clamp-1" title={order.travel?.title}>
                          {order.travel?.title || order.notes || 'Tour tự do / Land tour'}
                        </div>
                        <div className="flex items-center gap-2 mt-1 text-[11px] text-slate-400">
                          <span>👥 {order.pax_adults} Lớn {order.pax_children > 0 && `+ ${order.pax_children} Trẻ`}</span>
                          {order.travel?.code && (
                            <span className="font-mono text-[10px] text-slate-500">[{order.travel.code}]</span>
                          )}
                        </div>
                      </td>

                      {/* Cột 4: Tiến độ Cọc & Tiền */}
                      <td className="py-3 px-4">
                        <div className="font-mono font-bold text-white text-xs">
                          {formatCurrency(order.total_price)}
                        </div>
                        <div className="space-y-0.5 mt-1 text-[11px]">
                          <div className="flex items-center justify-between gap-2">
                            <span className="text-slate-400">Cọc 50%:</span>
                            <span className={`font-mono ${order.deposit_status === 'paid' ? 'text-emerald-400 font-semibold' : 'text-amber-400'}`}>
                              {formatCurrency(order.deposit_amount)} {order.deposit_status === 'paid' ? '✅' : '⏳'}
                            </span>
                          </div>
                          <div className="flex items-center justify-between gap-2">
                            <span className="text-slate-400">Đợt 2:</span>
                            <span className={`font-mono ${order.remaining_status === 'paid' ? 'text-emerald-400 font-semibold' : 'text-slate-400'}`}>
                              {formatCurrency(order.remaining_amount)} {order.remaining_status === 'paid' ? '✅' : '⏳'}
                            </span>
                          </div>
                        </div>
                      </td>

                      {/* Cột 5: Tiến Trình Visa */}
                      <td className="py-3 px-4">
                        <div>{renderVisaBadge(order.visa_status)}</div>
                        {order.operator_name && (
                          <div className="text-[11px] text-slate-300 mt-1 font-medium">
                            🏢 {order.operator_name}
                          </div>
                        )}
                        {order.operator_booking_ref && (
                          <div className="text-[10px] text-slate-500 font-mono">
                            Ref: {order.operator_booking_ref}
                          </div>
                        )}
                      </td>

                      {/* Cột 6: Khởi Hành & HDV (T-3) */}
                      <td className="py-3 px-4">
                        <div className="font-mono text-sky-400 font-semibold">
                          📅 {formatDate(order.departure_date)}
                        </div>
                        {order.tour_guide_name ? (
                          <div className="mt-1 text-[11px]">
                            <span className="text-slate-300 font-medium">👤 {order.tour_guide_name}</span>
                            {order.tour_guide_phone && (
                              <span className="text-emerald-400 font-mono block text-[10px]">
                                📞 {order.tour_guide_phone}
                              </span>
                            )}
                          </div>
                        ) : (
                          <div className="text-[10px] text-slate-500 mt-1">Chưa phân bổ HDV</div>
                        )}
                        {order.pre_departure_notified_at ? (
                          <span className="inline-block mt-1 text-[9px] px-1.5 py-0.2 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                            Đã dặn dò T-3
                          </span>
                        ) : null}
                      </td>

                      {/* Cột 7: Thao Tác Pipeline */}
                      <td className="py-3 px-4 text-right">
                        <div className="flex items-center justify-end gap-1.5 flex-wrap">
                          {/* Nút Xem chi tiết / AI Briefing */}
                          <button
                            onClick={() => openDetailModal(order)}
                            className="p-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-sky-400 hover:text-white transition"
                            title="Xem chi tiết & Tóm tắt AI"
                          >
                            👁️
                          </button>

                          {/* Nút Xác nhận Cọc 50% (nếu chưa cọc) */}
                          {order.deposit_status !== 'paid' && (
                            <button
                              onClick={() => openDepositModal(order)}
                              className="px-2 py-1 rounded-lg bg-amber-600/20 hover:bg-amber-600/30 text-amber-300 border border-amber-500/40 text-[10px] font-bold transition"
                              title="Xác nhận khách đã cọc 50%"
                            >
                              💵 Cọc
                            </button>
                          )}

                          {/* Nút Cập nhật Visa */}
                          {order.visa_status !== 'not_required' && (
                            <button
                              onClick={() => openVisaModal(order)}
                              className="px-2 py-1 rounded-lg bg-sky-600/20 hover:bg-sky-600/30 text-sky-300 border border-sky-500/40 text-[10px] font-bold transition"
                              title="Cập nhật tiến trình visa"
                            >
                              🛂 Visa
                            </button>
                          )}

                          {/* Nút Thu nốt 50% (nếu đã cọc nhưng chưa thu đủ) */}
                          {order.deposit_status === 'paid' && order.remaining_status !== 'paid' && (
                            <button
                              onClick={() => openFinalPaymentModal(order)}
                              className="px-2 py-1 rounded-lg bg-emerald-600/20 hover:bg-emerald-600/30 text-emerald-300 border border-emerald-500/40 text-[10px] font-bold transition"
                              title="Thu 50% còn lại"
                            >
                              💰 Thu 50%
                            </button>
                          )}

                          {/* Nút Dặn dò T-3 & HDV */}
                          <button
                            onClick={() => openT3Modal(order)}
                            className="px-2 py-1 rounded-lg bg-indigo-600/20 hover:bg-indigo-600/30 text-indigo-300 border border-indigo-500/40 text-[10px] font-bold transition"
                            title="Dặn dò trước bay 3 ngày & HDV"
                          >
                            ✈️ T-3
                          </button>

                          {/* Nút Sửa */}
                          <button
                            onClick={() => openEditModal(order)}
                            className="p-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white transition"
                            title="Chỉnh sửa đơn"
                          >
                            ✏️
                          </button>

                          {/* Nút Xóa */}
                          <button
                            onClick={() => handleDeleteOrder(order)}
                            className="p-1.5 rounded-lg bg-slate-800 hover:bg-rose-900/40 text-slate-400 hover:text-rose-400 transition"
                            title="Xóa đơn hàng"
                          >
                            🗑️
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </main>

      {/* Modal 1: Chi Tiết Đơn Hàng & Deal Briefing Call Summary */}
      {detailModalOpen && selectedOrder && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700/80 rounded-2xl w-full max-w-2xl overflow-hidden shadow-2xl flex flex-col max-h-[90vh]">
            <div className="p-4 border-b border-slate-800 flex items-center justify-between bg-slate-900/90">
              <div className="flex items-center gap-2.5">
                <span className="text-xl">📋</span>
                <div>
                  <h3 className="font-bold text-white text-sm">Hồ Sơ Đơn Hàng #{selectedOrder.order_code}</h3>
                  <p className="text-[11px] text-slate-400 font-mono">Kênh: {selectedOrder.channel} | Tạo lúc: {formatDate(selectedOrder.created_at)}</p>
                </div>
              </div>
              <button
                onClick={() => setDetailModalOpen(false)}
                className="text-slate-400 hover:text-white p-1 rounded-lg hover:bg-slate-800 transition"
              >
                ✕
              </button>
            </div>

            <div className="p-4 overflow-y-auto space-y-4 flex-1 text-xs">
              {/* AI Briefing Call Summary (Điểm mấu chốt cho Sales) */}
              <div className="p-3.5 bg-gradient-to-r from-sky-950/60 to-indigo-950/60 rounded-xl border border-sky-500/30">
                <div className="flex items-center justify-between mb-2">
                  <span className="font-bold text-sky-400 uppercase text-[10px] flex items-center gap-1.5 tracking-wider">
                    <span>🤖 Tóm Tắt Cho Sales (AI Briefing)</span>
                  </span>
                  <span className="text-[10px] text-slate-400">Đọc 30s trước khi gọi khách</span>
                </div>
                <div className="text-slate-200 text-xs leading-relaxed whitespace-pre-line">
                  {selectedOrder.ai_summary || selectedOrder.notes || 'Chưa có bản ghi tóm tắt từ AI.'}
                </div>
              </div>

              {/* Thông tin khách hàng & Tour */}
              <div className="grid grid-cols-2 gap-3">
                <div className="p-3 bg-slate-800/60 rounded-xl border border-slate-700/60 space-y-1">
                  <span className="text-slate-400 block text-[10px] uppercase font-bold">Khách Hàng</span>
                  <div className="font-bold text-white text-sm">
                    {selectedOrder.contact?.name || 'Khách vãng lai'}
                  </div>
                  <div className="font-mono text-emerald-400 font-bold">
                    📞 {selectedOrder.contact?.phone || 'Chưa có SĐT'}
                  </div>
                  <div className="text-[11px] text-slate-400">
                    Kênh: {selectedOrder.contact?.channel || selectedOrder.channel}
                  </div>
                </div>

                <div className="p-3 bg-slate-800/60 rounded-xl border border-slate-700/60 space-y-1">
                  <span className="text-slate-400 block text-[10px] uppercase font-bold">Tour & Ngày Khởi Hành</span>
                  <div className="font-bold text-white text-sm truncate">
                    {selectedOrder.travel?.title || selectedOrder.notes || 'Tour theo yêu cầu'}
                  </div>
                  <div className="font-mono text-sky-400">
                    📅 Khởi hành: {formatDate(selectedOrder.departure_date)}
                  </div>
                  <div className="text-[11px] text-slate-400">
                    Quy mô: {selectedOrder.pax_adults} Người lớn {selectedOrder.pax_children > 0 && `+ ${selectedOrder.pax_children} Trẻ em`}
                  </div>
                </div>
              </div>

              {/* Bảng số tiền cọc 50% và còn lại 50% */}
              <div className="p-3 bg-slate-800/40 rounded-xl border border-slate-700/50 space-y-2">
                <span className="text-slate-400 block text-[10px] uppercase font-bold">Tài Chính & Kế Hoạch Chuyển Khoản</span>
                <div className="grid grid-cols-3 gap-2 text-center">
                  <div className="p-2 bg-slate-900/60 rounded-lg">
                    <span className="text-[10px] text-slate-400 block">Tổng Giá Tour</span>
                    <span className="font-mono font-bold text-white text-sm">{formatCurrency(selectedOrder.total_price)}</span>
                  </div>
                  <div className="p-2 bg-slate-900/60 rounded-lg">
                    <span className="text-[10px] text-amber-400 block">Cọc 50% (Đợt 1)</span>
                    <span className="font-mono font-bold text-amber-400 text-sm">{formatCurrency(selectedOrder.deposit_amount)}</span>
                    <span className="block text-[9px] mt-0.5 text-slate-400">
                      {selectedOrder.deposit_status === 'paid' ? '✅ Đã nhận cọc' : '⏳ Chờ chuyển khoản'}
                    </span>
                  </div>
                  <div className="p-2 bg-slate-900/60 rounded-lg">
                    <span className="text-[10px] text-emerald-400 block">Còn Lại 50% (Đợt 2)</span>
                    <span className="font-mono font-bold text-emerald-400 text-sm">{formatCurrency(selectedOrder.remaining_amount)}</span>
                    <span className="block text-[9px] mt-0.5 text-slate-400">
                      {selectedOrder.remaining_status === 'paid' ? '✅ Đã thanh toán đủ' : '⏳ Thu trước ngày bay'}
                    </span>
                  </div>
                </div>
                {selectedOrder.payment_account_note && (
                  <div className="text-[11px] text-slate-400 italic mt-1">
                    📌 STK chuyển khoản: {selectedOrder.payment_account_note}
                  </div>
                )}
              </div>

              {/* Tiến trình Visa & Công ty Du Lịch Đối Tác */}
              <div className="p-3 bg-slate-800/40 rounded-xl border border-slate-700/50 space-y-1.5">
                <span className="text-slate-400 block text-[10px] uppercase font-bold">Đối Tác & Hồ Sơ Visa</span>
                <div className="flex items-center justify-between">
                  <span className="text-slate-400">Trạng thái Visa:</span>
                  <div>{renderVisaBadge(selectedOrder.visa_status)}</div>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-slate-400">Công ty du lịch đối tác:</span>
                  <span className="font-medium text-white">{selectedOrder.operator_name || 'Chưa cập nhật'}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-slate-400">Mã Booking đối tác:</span>
                  <span className="font-mono text-slate-300">{selectedOrder.operator_booking_ref || 'Chưa có'}</span>
                </div>
                {selectedOrder.visa_notes && (
                  <div className="text-[11px] text-slate-300 mt-1">
                    Ghi chú Visa: {selectedOrder.visa_notes}
                  </div>
                )}
              </div>

              {/* Điều phối HDV trước 3 ngày */}
              <div className="p-3 bg-slate-800/40 rounded-xl border border-slate-700/50 space-y-1.5">
                <span className="text-slate-400 block text-[10px] uppercase font-bold">Điều Phối Trước Khởi Hành (T-3)</span>
                <div className="flex items-center justify-between">
                  <span className="text-slate-400">Hướng Dẫn Viên:</span>
                  <span className="font-bold text-white">{selectedOrder.tour_guide_name || 'Đang cập nhật'}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-slate-400">SĐT Hướng Dẫn Viên:</span>
                  <span className="font-mono text-emerald-400 font-bold">{selectedOrder.tour_guide_phone || 'Đang cập nhật'}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-slate-400">Thời gian dặn dò khách:</span>
                  <span className="font-mono text-slate-400">{formatDate(selectedOrder.pre_departure_notified_at)}</span>
                </div>
              </div>
            </div>

            <div className="p-3 bg-slate-900 border-t border-slate-800 flex justify-end">
              <button
                onClick={() => setDetailModalOpen(false)}
                className="px-4 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-white text-xs font-semibold"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal 2: Xác Nhận Cọc 50% */}
      {depositModalOpen && depositOrder && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-md overflow-hidden shadow-2xl p-5 space-y-4">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3">
              <div className="flex items-center gap-2">
                <span className="text-lg">💵</span>
                <h3 className="font-bold text-white text-sm">Xác Nhận Cọc 50% — Đơn #{depositOrder.order_code}</h3>
              </div>
              <button onClick={() => setDepositModalOpen(false)} className="text-slate-400 hover:text-white">✕</button>
            </div>

            <div className="space-y-3 text-xs">
              <div className="p-3 bg-amber-950/30 border border-amber-500/30 rounded-xl space-y-1">
                <div className="text-slate-400">Số tiền đặt cọc cần thu:</div>
                <div className="text-lg font-black text-amber-400 font-mono">
                  {formatCurrency(depositOrder.deposit_amount)}
                </div>
                <div className="text-[11px] text-slate-400">
                  (50% của tổng đơn giá {formatCurrency(depositOrder.total_price)})
                </div>
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">
                  Thông tin đối soát chuyển khoản / Mã Ref ngân hàng:
                </label>
                <input
                  type="text"
                  value={depositAccountNote}
                  onChange={(e) => setDepositAccountNote(e.target.value)}
                  placeholder="VD: MBVCB.6291823 - Techcombank Tran Van A"
                  className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-amber-500 outline-none"
                />
              </div>

              <p className="text-[11px] text-slate-400 italic">
                * Sau khi xác nhận, đơn hàng sẽ tự động chuyển sang trạng thái <strong>Đang làm Visa</strong> hoặc <strong>Chờ thu nốt 50%</strong>, đồng thời nâng phân hạng khách trong CRM lên <strong>Hot Lead</strong>.
              </p>
            </div>

            <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-800">
              <button
                type="button"
                onClick={() => setDepositModalOpen(false)}
                className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleConfirmDeposit}
                disabled={submittingDeposit}
                className="px-4 py-1.5 rounded-lg bg-amber-600 hover:bg-amber-500 text-white text-xs font-bold shadow-md shadow-amber-600/30 transition disabled:opacity-50"
              >
                {submittingDeposit ? 'Đang lưu...' : 'Xác Nhận Đã Nhận Cọc 50%'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal 3: Cập Nhật Tiến Trình Visa */}
      {visaModalOpen && visaOrder && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-lg overflow-hidden shadow-2xl p-5 space-y-4">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3">
              <div className="flex items-center gap-2">
                <span className="text-lg">🛂</span>
                <h3 className="font-bold text-white text-sm">Tiến Trình Visa — Đơn #{visaOrder.order_code}</h3>
              </div>
              <button onClick={() => setVisaModalOpen(false)} className="text-slate-400 hover:text-white">✕</button>
            </div>

            {/* Nếu có mẫu tin nhắn Zalo gửi chúc mừng */}
            {zaloVisaMessage ? (
              <div className="space-y-3 text-xs">
                <div className="p-3 bg-emerald-950/40 border border-emerald-500/40 rounded-xl space-y-2">
                  <div className="font-bold text-emerald-400 flex items-center gap-1.5">
                    <span>🎉 ĐÃ CẬP NHẬT ĐẬU VISA THÀNH CÔNG!</span>
                  </div>
                  <p className="text-slate-300 text-[11px]">
                    Hệ thống đã tự động soạn mẫu tin nhắn chúc mừng và nhắc thu 50% còn lại. Bạn có thể sao chép để gửi cho khách hàng qua Zalo / Telegram:
                  </p>
                  <pre className="p-3 bg-slate-950/90 rounded-lg border border-slate-800 text-slate-200 text-xs whitespace-pre-wrap font-sans leading-relaxed">
                    {zaloVisaMessage}
                  </pre>
                  <button
                    type="button"
                    onClick={() => copyToClipboard(zaloVisaMessage, 'Tin nhắn Zalo chúc mừng')}
                    className="w-full py-2 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-xs transition"
                  >
                    📋 Sao Chép Tin Nhắn Gửi Khách
                  </button>
                </div>

                <div className="flex justify-end">
                  <button
                    type="button"
                    onClick={() => setVisaModalOpen(false)}
                    className="px-4 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-white text-xs font-semibold"
                  >
                    Đóng
                  </button>
                </div>
              </div>
            ) : (
              <div className="space-y-3 text-xs">
                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Trạng thái Visa:</label>
                  <select
                    value={visaStatusInput}
                    onChange={(e) => setVisaStatusInput(e.target.value)}
                    className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-sky-500 outline-none"
                  >
                    <option value="not_required">Không yêu cầu Visa</option>
                    <option value="awaiting_docs">⏳ Chờ khách nộp hồ sơ</option>
                    <option value="sent_to_operator">📦 Đã chuyển hồ sơ sang Cty Du Lịch đối tác</option>
                    <option value="processing">🔍 Đang xét duyệt tại Đại sứ quán / Lãnh sự quán</option>
                    <option value="approved">✅ ĐÃ ĐẬU VISA (APPROVED)</option>
                    <option value="rejected">❌ BỊ TỪ CHỐI VISA (REJECTED)</option>
                  </select>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-slate-300 font-semibold mb-1">Cty Du Lịch Đối Tác:</label>
                    <input
                      type="text"
                      value={operatorNameInput}
                      onChange={(e) => setOperatorNameInput(e.target.value)}
                      placeholder="VD: Saigontourist B2B"
                      className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-sky-500 outline-none"
                    />
                  </div>

                  <div>
                    <label className="block text-slate-300 font-semibold mb-1">Mã Booking Đối Tác:</label>
                    <input
                      type="text"
                      value={operatorBookingRefInput}
                      onChange={(e) => setOperatorBookingRefInput(e.target.value)}
                      placeholder="VD: SGT-2026-NRT99"
                      className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-sky-500 outline-none"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Ghi chú tiến trình / Hạn trả kết quả:</label>
                  <textarea
                    rows={2}
                    value={visaNotesInput}
                    onChange={(e) => setVisaNotesInput(e.target.value)}
                    placeholder="Ghi chú hẹn ngày lấy kết quả hoặc bổ sung tài chính..."
                    className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-sky-500 outline-none resize-none"
                  ></textarea>
                </div>

                <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-800">
                  <button
                    type="button"
                    onClick={() => setVisaModalOpen(false)}
                    className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
                  >
                    Hủy
                  </button>
                  <button
                    type="button"
                    onClick={handleConfirmVisa}
                    disabled={submittingVisa}
                    className="px-4 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold shadow-md shadow-sky-600/30 transition disabled:opacity-50"
                  >
                    {submittingVisa ? 'Đang lưu...' : 'Lưu Tiến Trình Visa'}
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Modal 4: Xác Nhận Thu Nốt 50% */}
      {finalPaymentModalOpen && finalPaymentOrder && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-md overflow-hidden shadow-2xl p-5 space-y-4">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3">
              <div className="flex items-center gap-2">
                <span className="text-lg">💰</span>
                <h3 className="font-bold text-white text-sm">Thu Nốt 50% — Đơn #{finalPaymentOrder.order_code}</h3>
              </div>
              <button onClick={() => setFinalPaymentModalOpen(false)} className="text-slate-400 hover:text-white">✕</button>
            </div>

            <div className="space-y-3 text-xs">
              <div className="p-3 bg-emerald-950/30 border border-emerald-500/30 rounded-xl space-y-1">
                <div className="text-slate-400">Số tiền đợt 2 cần thanh toán:</div>
                <div className="text-lg font-black text-emerald-400 font-mono">
                  {formatCurrency(finalPaymentOrder.remaining_amount)}
                </div>
                <div className="text-[11px] text-slate-400">
                  (Hoàn tất 100% tổng giá tour {formatCurrency(finalPaymentOrder.total_price)})
                </div>
              </div>

              <p className="text-[11px] text-slate-300">
                Xác nhận khách hàng đã hoàn tất chuyển khoản 50% còn lại. Đơn hàng sẽ được chuyển sang trạng thái <strong>Sẵn sàng khởi hành</strong> và nâng hạng khách hàng trong CRM lên <strong>Customer</strong> (Đã mua thành công).
              </p>
            </div>

            <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-800">
              <button
                type="button"
                onClick={() => setFinalPaymentModalOpen(false)}
                className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleConfirmFinalPayment}
                disabled={submittingFinalPayment}
                className="px-4 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold shadow-md shadow-emerald-600/30 transition disabled:opacity-50"
              >
                {submittingFinalPayment ? 'Đang xác nhận...' : 'Xác Nhận Đã Thu Đủ 100%'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Modal 5: Dặn Dò T-3 & Bàn Giao Hướng Dẫn Viên */}
      {t3ModalOpen && t3Order && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-lg overflow-hidden shadow-2xl p-5 space-y-4">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3">
              <div className="flex items-center gap-2">
                <span className="text-lg">✈️</span>
                <h3 className="font-bold text-white text-sm">Dặn Dò T-3 & HDV — Đơn #{t3Order.order_code}</h3>
              </div>
              <button onClick={() => setT3ModalOpen(false)} className="text-slate-400 hover:text-white">✕</button>
            </div>

            {/* Nếu có mẫu tin nhắn Zalo gửi dặn dò */}
            {zaloT3Message ? (
              <div className="space-y-3 text-xs">
                <div className="p-3 bg-indigo-950/40 border border-indigo-500/40 rounded-xl space-y-2">
                  <div className="font-bold text-indigo-400 flex items-center gap-1.5">
                    <span>✈️ ĐÃ LƯU THÔNG TIN HDV & DẶN DÒ T-3!</span>
                  </div>
                  <p className="text-slate-300 text-[11px]">
                    Mẫu tin nhắn dặn dò chuyến bay trước 3 ngày đã được tạo. Bạn hãy sao chép để gửi khách hàng qua Zalo:
                  </p>
                  <pre className="p-3 bg-slate-950/90 rounded-lg border border-slate-800 text-slate-200 text-xs whitespace-pre-wrap font-sans leading-relaxed">
                    {zaloT3Message}
                  </pre>
                  <button
                    type="button"
                    onClick={() => copyToClipboard(zaloT3Message, 'Tin nhắn dặn dò T-3')}
                    className="w-full py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white font-bold text-xs transition"
                  >
                    📋 Sao Chép Tin Nhắn Dặn Dò
                  </button>
                </div>

                <div className="flex justify-end">
                  <button
                    type="button"
                    onClick={() => setT3ModalOpen(false)}
                    className="px-4 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-white text-xs font-semibold"
                  >
                    Đóng
                  </button>
                </div>
              </div>
            ) : (
              <div className="space-y-3 text-xs">
                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="block text-slate-300 font-semibold mb-1">Tên Hướng Dẫn Viên:</label>
                    <input
                      type="text"
                      value={guideNameInput}
                      onChange={(e) => setGuideNameInput(e.target.value)}
                      placeholder="VD: Nguyễn Văn Tuấn"
                      className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-indigo-500 outline-none"
                    />
                  </div>

                  <div>
                    <label className="block text-slate-300 font-semibold mb-1">SĐT Hướng Dẫn Viên:</label>
                    <input
                      type="text"
                      value={guidePhoneInput}
                      onChange={(e) => setGuidePhoneInput(e.target.value)}
                      placeholder="VD: 0912.888.999"
                      className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-indigo-500 outline-none"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Điểm tập trung & Thời gian dặn dò:</label>
                  <textarea
                    rows={2}
                    value={guideNotesInput}
                    onChange={(e) => setGuideNotesInput(e.target.value)}
                    placeholder="Tập trung lúc 05:30 sáng tại Cột số 10 - Ga đi Quốc tế..."
                    className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-indigo-500 outline-none resize-none"
                  ></textarea>
                </div>

                <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-800">
                  <button
                    type="button"
                    onClick={() => setT3ModalOpen(false)}
                    className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
                  >
                    Hủy
                  </button>
                  <button
                    type="button"
                    onClick={handleConfirmT3}
                    disabled={submittingT3}
                    className="px-4 py-1.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-bold shadow-md shadow-indigo-600/30 transition disabled:opacity-50"
                  >
                    {submittingT3 ? 'Đang lưu...' : 'Lưu Thông Tin & Tạo Tin Nhắn T-3'}
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Modal 6: Tạo Mới / Sửa Đơn Tour */}
      {formModalOpen && (
        <div className="fixed inset-0 bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <form
            onSubmit={handleSaveOrderForm}
            className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-lg overflow-hidden shadow-2xl p-5 space-y-4"
          >
            <div className="flex items-center justify-between border-b border-slate-800 pb-3">
              <div className="flex items-center gap-2">
                <span className="text-lg">{formIsEditing ? '✏️' : '📦'}</span>
                <h3 className="font-bold text-white text-sm">
                  {formIsEditing ? `Chỉnh Sửa Đơn Tour #${editingOrderId}` : 'Thêm Đơn Hàng Tour Mới'}
                </h3>
              </div>
              <button
                type="button"
                onClick={() => setFormModalOpen(false)}
                className="text-slate-400 hover:text-white"
              >
                ✕
              </button>
            </div>

            <div className="space-y-3 text-xs">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Số khách người lớn:</label>
                  <input
                    type="number"
                    min={1}
                    value={formPaxAdults}
                    onChange={(e) => setFormPaxAdults(parseInt(e.target.value) || 1)}
                    className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-sky-500 outline-none"
                    required
                  />
                </div>

                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Số khách trẻ em:</label>
                  <input
                    type="number"
                    min={0}
                    value={formPaxChildren}
                    onChange={(e) => setFormPaxChildren(parseInt(e.target.value) || 0)}
                    className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-sky-500 outline-none"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Tổng giá tour (VNĐ):</label>
                  <input
                    type="number"
                    min={0}
                    step={100000}
                    value={formTotalPrice}
                    onChange={(e) => setFormTotalPrice(parseFloat(e.target.value) || 0)}
                    className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-sky-500 outline-none font-mono"
                    required
                  />
                  <span className="text-[10px] text-slate-500">
                    Cọc 50%: {formatCurrency(formTotalPrice * 0.5)}
                  </span>
                </div>

                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Ngày khởi hành:</label>
                  <input
                    type="date"
                    value={formDepartureDate}
                    onChange={(e) => setFormDepartureDate(e.target.value)}
                    className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-sky-500 outline-none font-mono"
                  />
                </div>
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Ghi chú số tài khoản cọc:</label>
                <input
                  type="text"
                  value={formPaymentNote}
                  onChange={(e) => setFormPaymentNote(e.target.value)}
                  placeholder="STK: 1903688888 - Techcombank (Công ty Du Lịch Travel AI)"
                  className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-sky-500 outline-none"
                />
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Ghi chú nội bộ / Nhu cầu khách:</label>
                <textarea
                  rows={2}
                  value={formNotes}
                  onChange={(e) => setFormNotes(e.target.value)}
                  placeholder="Ghi chú sở thích, yêu cầu phòng, ăn uống hoặc visa..."
                  className="w-full px-3 py-2 rounded-xl bg-slate-950 border border-slate-700 text-white text-xs focus:border-sky-500 outline-none resize-none"
                ></textarea>
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-800">
              <button
                type="button"
                onClick={() => setFormModalOpen(false)}
                className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={submittingForm}
                className="px-4 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold shadow-md shadow-sky-600/30 transition disabled:opacity-50"
              >
                {submittingForm ? 'Đang lưu...' : formIsEditing ? 'Cập Nhật Đơn Hàng' : 'Tạo Đơn Hàng Mới'}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
};

export default AdminOrdersPage;
