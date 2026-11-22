import React, { useState, useEffect, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { notify } from '../lib/notify';
import { useConfirm } from '../components/common/ConfirmModal';
import { apiFetch } from '../api/client';
import { AdminLayout } from '../components/admin/AdminLayout';

/**
 * Định nghĩa cấu trúc dữ liệu cho một hạng mục trong Bảng xếp chồng giá trị (Value Stack).
 */
export interface ValueStackItem {
  id?: number;
  offerId?: number;
  name: string;
  description?: string;
  type: 'core' | 'bonus';
  originalValue: number;
  sortOrder?: number;
  isHighlighted?: boolean;
}

/**
 * Định nghĩa cấu trúc dữ liệu của Grand Slam Offer trả về từ Backend API.
 */
export interface OfferPricing {
  originalPrice: number;
  offerPrice: number;
  savingsAmount: number;
  savingsPercentage: number;
  depositPercentage: number;
  depositAmount: number;
}

/**
 * Định nghĩa cấu trúc dữ liệu của Grand Slam Offer trả về từ Backend API.
 */
export interface OfferData {
  id: number;
  title: string;
  slug: string;
  tagline?: string;
  targetAudience?: string;
  heroMechanism?: string;
  corePromise?: string;
  pricing: OfferPricing;
  duration?: {
    days: number;
    nights: number;
  };
  scarcityUrgency?: {
    maxSlotsPerWeek: number;
    availableSlots: number;
    urgencyHours: number;
  };
  guarantees?: Array<{ id: string; title: string; description: string }>;
  paymentTerms?: Array<{ phase: number; name: string; percentage: number; description: string }>;
  salesScript?: string;
  status: 'active' | 'draft' | 'archived';
  isFeatured: boolean;
  travelId?: number;
  travel?: {
    id: number;
    title: string;
    code: string;
    country: string;
    priceFrom: number;
    durationDays: number;
    durationNights: number;
  };
  items: ValueStackItem[];
  createdAt?: string;
  updatedAt?: string;
}

/**
 * Thông tin tóm tắt Tour sản phẩm để gắn liên kết vào Offer.
 */
interface TravelOption {
  id: number;
  title: string;
  code: string;
  country: string;
  priceFrom: number;
}

/**
 * Trang Quản Trị Offers (Grand Slam & Value Stack)
 * Tái hiện 100% giao diện và nghiệp vụ từ welcome.blade.php của PHP core.
 */
export const AdminOffersPage: React.FC = () => {
  const confirm = useConfirm();

  // State danh sách
  const [offers, setOffers] = useState<OfferData[]>([]);
  const [travels, setTravels] = useState<TravelOption[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [searchQuery, setSearchQuery] = useState<string>('');

  // State Modal Xem Chi Tiết
  const [viewingOffer, setViewingOffer] = useState<OfferData | null>(null);

  // State Modal Tạo / Sửa Offer
  const [showEditModal, setShowEditModal] = useState<boolean>(false);
  const [editingId, setEditingId] = useState<number | null>(null);

  // Form fields
  const [formTravelId, setFormTravelId] = useState<string>('');
  const [formTitle, setFormTitle] = useState<string>('');
  const [formTagline, setFormTagline] = useState<string>('');
  const [formTarget, setFormTarget] = useState<string>('');
  const [formMechanism, setFormMechanism] = useState<string>('');
  const [formOrigPrice, setFormOrigPrice] = useState<number>(6600000);
  const [formOfferPrice, setFormOfferPrice] = useState<number>(3290000);
  const [formDepositPct, setFormDepositPct] = useState<number>(30);
  const [formMaxSlots, setFormMaxSlots] = useState<number>(4);
  const [formUrgencyHours, setFormUrgencyHours] = useState<number>(24);
  const [formStatus, setFormStatus] = useState<'active' | 'draft' | 'archived'>('active');
  const [formItems, setFormItems] = useState<ValueStackItem[]>([]);

  /**
   * Tải danh sách Offers từ Backend .NET API.
   */
  const fetchOffers = async () => {
    setLoading(true);
    try {
      const res = await apiFetch('/api/v1/offers');
      const json = await res.json();
      if (json.success && json.data) {
        setOffers(json.data);
      }
    } catch (err) {
      console.error('Lỗi khi tải danh sách Offers:', err);
    } finally {
      setLoading(false);
    }
  };

  /**
   * Tải danh sách Tour sản phẩm để làm dropdown lựa chọn.
   */
  const fetchTravels = async () => {
    try {
      const res = await apiFetch('/api/v1/travels');
      const json = await res.json();
      if (json.success && json.data) {
        setTravels(json.data);
      }
    } catch (err) {
      console.error('Lỗi khi tải danh sách Tours:', err);
    }
  };

  useEffect(() => {
    fetchOffers();
    fetchTravels();
    const handleRefresh = () => fetchOffers();
    window.addEventListener('refresh-admin-data', handleRefresh);
    return () => window.removeEventListener('refresh-admin-data', handleRefresh);
  }, []);

  /**
   * Bộ lọc tìm kiếm Client-side theo tiêu đề hoặc khẩu hiệu.
   */
  const filteredOffers = useMemo(() => {
    if (!searchQuery.trim()) return offers;
    const q = searchQuery.toLowerCase();
    return offers.filter(
      o =>
        o.title.toLowerCase().includes(q) ||
        (o.tagline && o.tagline.toLowerCase().includes(q)) ||
        (o.heroMechanism && o.heroMechanism.toLowerCase().includes(q))
    );
  }, [offers, searchQuery]);

  /**
   * Mở modal thêm mới Grand Slam Offer.
   */
  const handleOpenCreateModal = () => {
    setEditingId(null);
    setFormTravelId('');
    setFormTitle('Gói Combo Đặc Quyền Tinh Hoa');
    setFormTagline('Xách vali lên và đi, an tâm tiền cọc 100%');
    setFormTarget('Dân văn phòng 24 – 35 tuổi, cặp đôi, gia đình trẻ muốn có kỳ nghỉ xả stress chuẩn gu');
    setFormMechanism('Hệ Thống Du Lịch 4 Chạm Thảnh Thơi (AI-Human Hybrid Travel Protocol)');
    setFormOrigPrice(6600000);
    setFormOfferPrice(3290000);
    setFormDepositPct(30);
    setFormMaxSlots(4);
    setFormUrgencyHours(24);
    setFormStatus('active');
    setFormItems([
      { name: 'Vé máy bay khứ hồi / Xe VIP Limousine', description: 'Giờ bay đẹp, bao gồm hành lý tiêu chuẩn', type: 'core', originalValue: 2200000, sortOrder: 1 },
      { name: 'Khách sạn 3-4 sao trung tâm view đẹp', description: '2 đêm phòng tiêu chuẩn cao, buffet sáng miễn phí', type: 'core', originalValue: 1800000, sortOrder: 2 },
      { name: 'Xe riêng đưa đón sân bay tận nơi', description: 'Tài xế đón đúng giờ, không lo bị chặt chém', type: 'core', originalValue: 400000, sortOrder: 3 },
      { name: '[BONUS 1] Bản đồ 10 quán ăn ngon bản địa', description: 'Cẩm nang quán ăn chuẩn vị dân địa phương', type: 'bonus', originalValue: 500000, sortOrder: 4, isHighlighted: true },
      { name: '[BONUS 2] Lịch trình May Đo Độc Quyền (PDF)', description: 'Bản lịch trình thiết kế đẹp, chia khung giờ chill', type: 'bonus', originalValue: 500000, sortOrder: 5, isHighlighted: true },
      { name: '[BONUS 3] Trợ lý AI Đồng Hành 24/7', description: 'Giải đáp thắc mắc và cứu nguy sự cố trong 5 phút', type: 'bonus', originalValue: 1000000, sortOrder: 6, isHighlighted: true },
      { name: '[BONUS 4] Bảo hiểm du lịch trọn gói', description: 'Bảo vệ quyền lợi y tế và hành lý toàn diện', type: 'bonus', originalValue: 200000, sortOrder: 7, isHighlighted: true },
    ]);
    setShowEditModal(true);
  };

  /**
   * Mở modal chỉnh sửa Offer hiện có.
   */
  const handleOpenEditModal = (offer: OfferData) => {
    setEditingId(offer.id);
    setFormTravelId(offer.travelId ? String(offer.travelId) : '');
    setFormTitle(offer.title);
    setFormTagline(offer.tagline || '');
    setFormTarget(offer.targetAudience || '');
    setFormMechanism(offer.heroMechanism || '');
    setFormOrigPrice(offer.pricing?.originalPrice || 0);
    setFormOfferPrice(offer.pricing?.offerPrice || 0);
    setFormDepositPct(offer.pricing?.depositPercentage || 30);
    setFormMaxSlots(offer.scarcityUrgency?.maxSlotsPerWeek || 4);
    setFormUrgencyHours(offer.scarcityUrgency?.urgencyHours || 24);
    setFormStatus(offer.status || 'active');
    setFormItems(
      offer.items && offer.items.length > 0
        ? offer.items.map((it, idx) => ({ ...it, originalValue: it.originalValue || 0, sortOrder: idx + 1 }))
        : [
            { name: 'Vé máy bay khứ hồi / Xe VIP Limousine', description: '', type: 'core', originalValue: 2200000 },
            { name: '[BONUS 1] Quà tặng chào mừng', description: '', type: 'bonus', originalValue: 500000 },
          ]
    );
    setShowEditModal(true);
  };

  /**
   * Xử lý khi chọn một Tour du lịch trong Dropdown modal.
   */
  const handleTravelSelectChange = (travelIdStr: string) => {
    setFormTravelId(travelIdStr);
    if (!travelIdStr) return;
    const t = travels.find(tr => tr.id === Number(travelIdStr));
    if (t) {
      if (!formTitle || formTitle === 'Gói Combo Đặc Quyền Tinh Hoa') {
        setFormTitle(`Gói Combo Đặc Quyền: ${t.title}`);
      }
      if (t.priceFrom && formOfferPrice <= 3290000) {
        setFormOfferPrice(t.priceFrom);
      }
    }
  };

  /**
   * Tính tổng giá trị các món trong Value Stack.
   */
  const totalItemsValue = useMemo(() => {
    return formItems.reduce((acc, curr) => acc + (Number(curr.originalValue) || 0), 0);
  }, [formItems]);

  /**
   * Áp dụng tổng Value Stack vào ô Giá gốc.
   */
  const applySumToOriginalPrice = () => {
    if (totalItemsValue > 0) {
      setFormOrigPrice(totalItemsValue);
    }
  };

  /**
   * Thêm một dòng mới vào Bảng xếp chồng giá trị.
   */
  const handleAddValueStackRow = () => {
    setFormItems([
      ...formItems,
      {
        name: '',
        description: '',
        type: 'bonus',
        originalValue: 500000,
        sortOrder: formItems.length + 1,
        isHighlighted: true,
      },
    ]);
  };

  /**
   * Xóa một dòng trong Value Stack.
   */
  const handleRemoveValueStackRow = (index: number) => {
    setFormItems(formItems.filter((_, idx) => idx !== index));
  };

  /**
   * Cập nhật thông tin một dòng trong Value Stack.
   */
  const handleUpdateValueStackRow = (index: number, field: keyof ValueStackItem, value: any) => {
    const updated = [...formItems];
    updated[index] = { ...updated[index], [field]: value };
    if (field === 'type') {
      updated[index].isHighlighted = value === 'bonus';
    }
    setFormItems(updated);
  };

  /**
   * Lưu thông tin Offer (Tạo mới hoặc Cập nhật) gửi về Backend .NET API.
   */
  const handleSubmitOfferForm = async (e: React.FormEvent) => {
    e.preventDefault();

    const payload = {
      travelId: formTravelId ? Number(formTravelId) : null,
      title: formTitle,
      tagline: formTagline,
      targetAudience: formTarget,
      heroMechanism: formMechanism,
      originalPrice: Number(formOrigPrice),
      offerPrice: Number(formOfferPrice),
      depositPercentage: Number(formDepositPct),
      maxSlotsPerWeek: Number(formMaxSlots),
      availableSlots: Number(formMaxSlots),
      urgencyHours: Number(formUrgencyHours),
      status: formStatus,
      isFeatured: false,
      items: formItems.filter(it => it.name.trim()).map((it, idx) => ({
        name: it.name.trim(),
        description: it.description || '',
        type: it.type,
        originalValue: Number(it.originalValue) || 0,
        sortOrder: idx + 1,
        isHighlighted: it.type === 'bonus',
      })),
      guarantees: [
        { id: 'zero_scam', title: 'Cam kết an toàn cọc 100% (Zero-Scam Guarantee)', description: 'Mọi giao dịch đều có Hợp đồng điện tử có Mã số thuế công ty và chuyển khoản trực tiếp vào Số tài khoản ngân hàng doanh nghiệp.' },
        { id: 'no_hidden_fees', title: 'Cam kết minh bạch từng đồng — Không phụ phí (No Hidden Fees)', description: 'Bảng kê chi phí 2 cột rõ ràng. Nếu đến nơi phát sinh bất kỳ khoản phí dịch vụ ẩn nào nằm ngoài thỏa thuận, Tyler đền bù gấp đôi số tiền phát sinh đó.' },
        { id: 'hassle_free', title: 'Cam kết dịch vụ "Êm Ru" (Hassle-Free Guarantee)', description: 'Nếu phòng khách sạn thực tế không đúng hạng phòng và hình ảnh đã cam kết trong hợp đồng, Tyler sẽ đổi ngay khách sạn tương đương hoặc cao cấp hơn hoàn toàn miễn phí trong vòng 60 phút.' }
      ],
      paymentTerms: [
        { phase: 1, name: 'Đợt 1 (Cọc giữ chỗ)', percentage: 30, description: 'Thanh toán 30% để khóa vé máy bay và phòng khách sạn giá tốt nhất.' },
        { phase: 2, name: 'Đợt 2 (Hoàn tất)', percentage: 70, description: 'Thanh toán 70% còn lại trước ngày khởi hành 7 ngày sau khi nhận đầy đủ mã vé và mã phòng.' }
      ]
    };

    try {
      const url = editingId ? `/api/v1/offers/${editingId}` : '/api/v1/offers';
      const method = editingId ? 'PUT' : 'POST';

      const res = await apiFetch(url, {
        method,
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify(payload),
      });

      const result = await res.json();
      if (res.ok && result.success) {
        notify.success(editingId ? 'Cập nhật Offer & Value Stack thành công!' : 'Đã tạo Offer mới thành công!');
        setShowEditModal(false);
        fetchOffers();
      } else {
        notify.error(result.message || 'Không thể lưu Offer');
      }
    } catch (err: unknown) {
      notify.error('Lỗi kết nối máy chủ: ' + (err instanceof Error ? err.message : String(err)));
    }
  };

  /**
   * Xóa một gói Offer khỏi CSDL.
   */
  const handleDeleteOffer = async (id: number, title: string) => {
    const isConfirmed = await confirm({
      title: 'Xác nhận xóa gói Offer',
      message: `Bạn có chắc chắn muốn xóa gói Offer "${title}" không? Toàn bộ các ưu đãi và chính sách kèm theo sẽ bị xóa vĩnh viễn.`,
      confirmText: 'Xóa vĩnh viễn',
      cancelText: 'Hủy bỏ',
      variant: 'danger',
    });
    if (!isConfirmed) return;

    try {
      const res = await apiFetch(`/api/v1/offers/${id}`, {
        method: 'DELETE',
        headers: { Accept: 'application/json' },
      });
      const result = await res.json();
      if (res.ok && result.success) {
        notify.success('Đã xóa Offer thành công!');
        fetchOffers();
      } else {
        notify.error(result.message || 'Không thể xóa');
      }
    } catch (err: unknown) {
      notify.error('Lỗi kết nối máy chủ: ' + (err instanceof Error ? err.message : String(err)));
    }
  };

  const formatCurrency = (val: number): string => {
    return new Intl.NumberFormat('vi-VN').format(val) + ' đ';
  };

  return (
    <AdminLayout
      title="AI Agent Travel — Quản Trị Hệ Thống"
      subtitle="Trụ cột Offer • AI Agent Commander & Value Stack Management"
      badge=".NET 10 Core"
      icon="💎"
      headerActions={
        <>
          <Link
            to="/"
            target="_blank"
            className="px-3 py-1.5 rounded-xl bg-cyan-500/20 hover:bg-cyan-500/30 text-cyan-300 font-semibold text-xs border border-cyan-500/30 transition flex items-center gap-1.5 shrink-0"
          >
            <span>🌐 Xem Landing Page</span>
          </Link>
          <button
            onClick={handleOpenCreateModal}
            className="px-3.5 py-1.5 rounded-xl bg-gradient-to-r from-orange-500 to-rose-500 hover:from-orange-600 hover:to-rose-600 text-white font-bold text-xs shadow-lg shadow-orange-500/20 transition-all flex items-center gap-1.5 shrink-0"
          >
            <span>+ Thêm Offer</span>
          </button>
        </>
      }
    >
      {/* Main Content Area */}
      <main className="flex-1 p-6 lg:p-8 space-y-6">
          {/* Metrics / KPI Cards */}

          {/* Offer Management Table Section */}
          <div className="bg-[#151c2e] border border-[#222f49] rounded-2xl overflow-hidden shadow-xl space-y-4 p-5 sm:p-6">
            {/* Search & Filters */}
            <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
              <div className="w-full sm:w-80 relative">
                <input
                  type="text"
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  placeholder="Tìm kiếm tên offer, tagline, cơ chế..."
                  className="w-full pl-9 pr-4 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-xs text-white placeholder-slate-500 focus:outline-none focus:border-orange-500 transition"
                />
                <svg className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </div>

              <div className="flex items-center space-x-2">
                <button
                  onClick={fetchOffers}
                  className="px-3.5 py-2 rounded-xl bg-[#0b0f19] hover:bg-slate-800 border border-[#222f49] text-xs text-slate-300 font-semibold transition flex items-center gap-1.5"
                >
                  <span>🔄 Làm mới dữ liệu</span>
                </button>
              </div>
            </div>

            {/* Table */}
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-xs">
                <thead>
                  <tr className="border-b border-[#222f49] bg-[#0f1524] text-slate-400 font-semibold uppercase text-[11px] tracking-wider">
                    <th className="py-3 px-4">Tên Offer & Cơ Chế</th>
                    <th className="py-3 px-4">Bảng Giá (Gốc / Ưu Đãi)</th>
                    <th className="py-3 px-4">Tiền Cọc (30%)</th>
                    <th className="py-3 px-4">Khan Hiếm / Hạn Giữ Giá</th>
                    <th className="py-3 px-4">Trạng Thái</th>
                    <th className="py-3 px-4 text-right">Thao Tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#222f49]">
                  {loading ? (
                    <tr>
                      <td colSpan={6} className="py-8 text-center text-slate-400">
                        Đang tải danh sách Grand Slam Offers từ Database...
                      </td>
                    </tr>
                  ) : filteredOffers.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="py-8 text-center text-slate-400">
                        Không tìm thấy Offer nào phù hợp.
                      </td>
                    </tr>
                  ) : (
                    filteredOffers.map(offer => {
                      const origPrice = formatCurrency(offer.pricing?.originalPrice || 0);
                      const offPrice = formatCurrency(offer.pricing?.offerPrice || 0);
                      const depositAmt = formatCurrency(offer.pricing?.depositAmount || 0);
                      const savingsPct = offer.pricing?.savingsPercentage || 0;

                      return (
                        <tr key={offer.id} className="hover:bg-[#151d30] transition-colors">
                          <td className="py-4 px-4">
                            <div className="font-bold text-white text-sm">{offer.title}</div>
                            <div className="text-[11px] text-slate-400 mt-0.5">{offer.tagline || offer.heroMechanism || ''}</div>
                            <div className="flex flex-wrap items-center gap-2 mt-1.5 font-mono text-[10px]">
                              {offer.travel ? (
                                <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-lg bg-sky-500/20 text-sky-300 border border-sky-500/30">
                                  ✈️ Tour: {offer.travel.title}
                                </span>
                              ) : (
                                <span className="text-slate-500 italic">Chưa gắn tour</span>
                              )}
                              <span className="text-amber-400">🎁 {offer.items?.length || 0} mục Value Stack</span>
                            </div>
                          </td>
                          <td className="py-4 px-4 font-mono">
                            <div className="text-sm font-bold text-orange-400">{offPrice}</div>
                            <div className="text-[11px] text-slate-400 line-through">{origPrice}</div>
                            <div className="text-[10px] text-emerald-400 font-sans font-semibold">
                              Tiết kiệm {savingsPct}%
                            </div>
                          </td>
                          <td className="py-4 px-4 font-mono">
                            <div className="text-sm font-bold text-emerald-400">{depositAmt}</div>
                            <div className="text-[10px] text-slate-400 font-sans">
                              ({offer.pricing?.depositPercentage}% đợt 1)
                            </div>
                          </td>
                          <td className="py-4 px-4 text-slate-300">
                            <div>
                              <strong>{offer.scarcityUrgency?.availableSlots || 4}</strong> / {offer.scarcityUrgency?.maxSlotsPerWeek || 4} nhóm/tuần
                            </div>
                            <div className="text-[10px] text-amber-400 mt-0.5">
                              Giữ giá: {offer.scarcityUrgency?.urgencyHours || 24}h
                            </div>
                          </td>
                          <td className="py-4 px-4">
                            {offer.status === 'active' ? (
                              <span className="px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 text-[10px] font-bold uppercase">
                                Active
                              </span>
                            ) : (
                              <span className="px-2 py-0.5 rounded-full bg-slate-700 text-slate-300 text-[10px] font-bold uppercase">
                                {offer.status}
                              </span>
                            )}
                          </td>
                          <td className="py-4 px-4 text-right space-x-1.5 whitespace-nowrap">
                            <button
                              onClick={() => setViewingOffer(offer)}
                              className="px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 font-semibold text-[11px] transition"
                            >
                              👁️ Xem Chi Tiết
                            </button>
                            <button
                              onClick={() => handleOpenEditModal(offer)}
                              className="px-2.5 py-1.5 rounded-lg bg-orange-500/20 hover:bg-orange-500/30 text-orange-300 border border-orange-500/40 font-semibold text-[11px] transition"
                            >
                              ✏️ Sửa & Thêm Bonus
                            </button>
                            <button
                              onClick={() => handleDeleteOffer(offer.id, offer.title)}
                              className="px-2.5 py-1.5 rounded-lg bg-rose-500/20 hover:bg-rose-500/30 text-rose-300 border border-rose-500/40 font-semibold text-[11px] transition"
                            >
                              🗑️ Xóa
                            </button>
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

      {/* MODAL 1: XEM CHI TIẾT OFFER & VALUE STACK */}
      {viewingOffer && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-[#151c2e] border border-[#222f49] rounded-2xl w-full max-w-2xl max-h-[90vh] flex flex-col shadow-2xl overflow-hidden">
            <div className="p-5 border-b border-[#222f49] flex items-center justify-between bg-[#111726]">
              <div>
                <h3 className="text-base font-bold text-white flex items-center gap-2">
                  <span>💎</span>
                  <span>{viewingOffer.title}</span>
                </h3>
                <p className="text-xs text-slate-400 mt-0.5">{viewingOffer.tagline || viewingOffer.heroMechanism}</p>
              </div>
              <button
                onClick={() => setViewingOffer(null)}
                className="w-8 h-8 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 flex items-center justify-center transition"
              >
                ✕
              </button>
            </div>

            <div className="p-6 space-y-6 overflow-y-auto flex-1 text-xs">
              {/* Value Stack Items List */}
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <h4 className="font-bold text-slate-200 uppercase tracking-wider text-[11px]">
                    Bảng Xếp Chồng Giá Trị (Value Stack)
                  </h4>
                  <span className="font-mono text-orange-400 font-bold">
                    Tổng: {formatCurrency(viewingOffer.pricing.originalPrice || 0)}
                  </span>
                </div>

                <div className="space-y-2">
                  {viewingOffer.items && viewingOffer.items.length > 0 ? (
                    viewingOffer.items.map((item, idx) => (
                      <div
                        key={idx}
                        className={`p-3 rounded-xl flex items-start justify-between ${
                          item.type === 'bonus'
                            ? 'bg-amber-500/10 border border-amber-500/30 text-amber-200'
                            : 'bg-[#0b0f19] border border-[#222f49] text-slate-200'
                        }`}
                      >
                        <div>
                          <div className="font-bold flex items-center gap-1.5">
                            <span>{item.type === 'bonus' ? '🎁' : '✅'} {item.name}</span>
                            {item.type === 'bonus' ? (
                              <span className="text-[9px] uppercase px-1.5 py-0.2 bg-amber-400/20 text-amber-300 rounded font-bold">
                                Bonus Quà Tặng
                              </span>
                            ) : (
                              <span className="text-[9px] uppercase px-1.5 py-0.2 bg-emerald-400/20 text-emerald-300 rounded font-bold">
                                Hạng Mục Chính
                              </span>
                            )}
                          </div>
                          {item.description && (
                            <p className="text-[11px] text-slate-400 mt-0.5">{item.description}</p>
                          )}
                        </div>
                        <div className="font-mono font-bold text-xs whitespace-nowrap ml-2">
                          {formatCurrency(item.originalValue || 0)}
                        </div>
                      </div>
                    ))
                  ) : (
                    <p className="text-slate-400">Chưa có hạng mục Value Stack nào.</p>
                  )}
                </div>
              </div>

              {/* Guarantees List */}
              {viewingOffer.guarantees && viewingOffer.guarantees.length > 0 && (
                <div className="space-y-2">
                  <h4 className="font-bold text-slate-200 uppercase tracking-wider text-[11px]">
                    Cam Kết Không Thể Từ Chối (Guarantees)
                  </h4>
                  <div className="space-y-2">
                    {viewingOffer.guarantees.map((g, idx) => (
                      <div key={idx} className="p-3 rounded-xl bg-[#0b0f19] border border-[#222f49]">
                        <strong className="text-emerald-400">{g.title}:</strong>
                        <p className="text-[11px] text-slate-300 mt-0.5">{g.description}</p>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {/* Sales Script */}
              {viewingOffer.salesScript && (
                <div className="space-y-2">
                  <h4 className="font-bold text-slate-200 uppercase tracking-wider text-[11px]">
                    Kịch Bản Tư Vấn & Bán Hàng (Sales Script)
                  </h4>
                  <div className="p-3.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-slate-300 whitespace-pre-line leading-relaxed">
                    {viewingOffer.salesScript}
                  </div>
                </div>
              )}
            </div>

            <div className="p-4 border-t border-[#222f49] flex justify-end space-x-2 bg-[#111726]">
              <button
                onClick={() => {
                  const o = viewingOffer;
                  setViewingOffer(null);
                  handleOpenEditModal(o);
                }}
                className="px-4 py-2 rounded-xl bg-orange-500 hover:bg-orange-600 text-white font-bold text-xs transition"
              >
                ✏️ Chỉnh Sửa Offer Này
              </button>
              <button
                onClick={() => setViewingOffer(null)}
                className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-semibold text-xs transition"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MODAL 2: TẠO MỚI / CHỈNH SỬA OFFER */}
      {showEditModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-[#151c2e] border border-[#222f49] rounded-2xl w-full max-w-3xl max-h-[92vh] flex flex-col shadow-2xl overflow-hidden">
            <div className="p-5 border-b border-[#222f49] flex items-center justify-between bg-[#111726]">
              <h3 className="text-base font-bold text-white flex items-center gap-2">
                <span>💎</span>
                <span>{editingId ? 'Chỉnh Sửa Grand Slam Offer & Value Stack' : 'Tạo Mới Grand Slam Offer (Kèm Value Stack)'}</span>
              </h3>
              <button
                onClick={() => setShowEditModal(false)}
                className="w-8 h-8 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 flex items-center justify-center transition"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleSubmitOfferForm} className="p-6 space-y-5 overflow-y-auto flex-1 text-xs">
              {/* Tour gốc liên kết */}
              <div>
                <label className="block font-semibold text-slate-300 mb-1">Liên kết Tour Sản Phẩm (Travel):</label>
                <select
                  value={formTravelId}
                  onChange={e => handleTravelSelectChange(e.target.value)}
                  className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white text-xs focus:border-orange-500 focus:outline-none"
                >
                  <option value="">-- Không liên kết tour (Offer độc lập) --</option>
                  {travels.map(t => (
                    <option key={t.id} value={t.id}>
                      [{t.code || `TOUR-#${t.id}`}] {t.title} ({t.country}) — Sàn: {formatCurrency(t.priceFrom)}
                    </option>
                  ))}
                </select>
              </div>

              {/* Tiêu đề & Tagline */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block font-semibold text-slate-300 mb-1">Tiêu đề Offer (*):</label>
                  <input
                    type="text"
                    required
                    value={formTitle}
                    onChange={e => setFormTitle(e.target.value)}
                    placeholder="vd: Gói Combo Nghỉ Dưỡng 4 Chạm Thảnh Thơi (3N2Đ)"
                    className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white text-xs focus:border-orange-500 focus:outline-none"
                  />
                </div>

                <div>
                  <label className="block font-semibold text-slate-300 mb-1">Khẩu hiệu (Tagline):</label>
                  <input
                    type="text"
                    value={formTagline}
                    onChange={e => setFormTagline(e.target.value)}
                    placeholder="vd: Xách vali lên và đi, an tâm tiền cọc 100%"
                    className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white text-xs focus:border-orange-500 focus:outline-none"
                  />
                </div>
              </div>

              {/* Target & Mechanism */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block font-semibold text-slate-300 mb-1">Khách hàng mục tiêu (Target Audience):</label>
                  <input
                    type="text"
                    value={formTarget}
                    onChange={e => setFormTarget(e.target.value)}
                    placeholder="vd: Dân văn phòng 24 – 35 tuổi, cặp đôi, gia đình trẻ"
                    className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white text-xs focus:border-orange-500 focus:outline-none"
                  />
                </div>

                <div>
                  <label className="block font-semibold text-slate-300 mb-1">Cơ chế độc quyền (Hero Mechanism):</label>
                  <input
                    type="text"
                    value={formMechanism}
                    onChange={e => setFormMechanism(e.target.value)}
                    placeholder="vd: Hệ Thống Du Lịch 4 Chạm Thảnh Thơi"
                    className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white text-xs focus:border-orange-500 focus:outline-none"
                  />
                </div>
              </div>

              {/* Pricing, Deposit, Scarcity */}
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 p-4 rounded-xl bg-[#0b0f19] border border-[#222f49]">
                <div>
                  <label className="block font-semibold text-slate-300 mb-1">Giá niêm yết gốc (VNĐ):</label>
                  <input
                    type="number"
                    required
                    value={formOrigPrice}
                    onChange={e => setFormOrigPrice(Number(e.target.value))}
                    className="w-full px-2.5 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-white font-mono text-xs focus:border-orange-500 focus:outline-none"
                  />
                </div>

                <div>
                  <label className="block font-semibold text-orange-400 mb-1">Giá ưu đãi Offer (VNĐ):</label>
                  <input
                    type="number"
                    required
                    value={formOfferPrice}
                    onChange={e => setFormOfferPrice(Number(e.target.value))}
                    className="w-full px-2.5 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-orange-300 font-mono text-xs focus:border-orange-500 focus:outline-none"
                  />
                </div>

                <div>
                  <label className="block font-semibold text-emerald-400 mb-1">% Cọc đợt 1:</label>
                  <input
                    type="number"
                    value={formDepositPct}
                    onChange={e => setFormDepositPct(Number(e.target.value))}
                    className="w-full px-2.5 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-emerald-300 font-mono text-xs focus:border-orange-500 focus:outline-none"
                  />
                </div>

                <div>
                  <label className="block font-semibold text-amber-400 mb-1">Slots tuần:</label>
                  <input
                    type="number"
                    value={formMaxSlots}
                    onChange={e => setFormMaxSlots(Number(e.target.value))}
                    className="w-full px-2.5 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-amber-300 font-mono text-xs focus:border-orange-500 focus:outline-none"
                  />
                </div>
              </div>

              {/* Urgency & Status */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block font-semibold text-slate-300 mb-1">Giờ giữ giá khẩn cấp (Urgency Hours):</label>
                  <input
                    type="number"
                    value={formUrgencyHours}
                    onChange={e => setFormUrgencyHours(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white text-xs focus:border-orange-500 focus:outline-none"
                  />
                </div>

                <div>
                  <label className="block font-semibold text-slate-300 mb-1">Trạng thái phát hành:</label>
                  <select
                    value={formStatus}
                    onChange={e => setFormStatus(e.target.value as any)}
                    className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white text-xs focus:border-orange-500 focus:outline-none"
                  >
                    <option value="active">Active (Đang chạy)</option>
                    <option value="draft">Draft (Bản nháp)</option>
                    <option value="archived">Archived (Lưu trữ)</option>
                  </select>
                </div>
              </div>

              {/* Dynamic Value Stack Items */}
              <div className="space-y-3 pt-2 border-t border-[#222f49]">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                  <div>
                    <h4 className="font-bold text-slate-200 uppercase tracking-wider text-xs">
                      Bảng Xếp Chồng Giá Trị (Value Stack)
                    </h4>
                    <p className="text-[11px] text-slate-400">
                      Đóng gói các món chính và quà tặng kèm để tạo cảm giác "Hời" vượt trội.
                    </p>
                  </div>

                  <div className="flex items-center space-x-2">
                    <span className="text-[11px] font-mono text-amber-400">
                      Tổng: {formatCurrency(totalItemsValue)}
                    </span>
                    <button
                      type="button"
                      onClick={applySumToOriginalPrice}
                      className="px-2.5 py-1 rounded-lg bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 border border-amber-500/40 text-[10px] font-bold transition"
                    >
                      Áp dụng vào Giá Gốc ⬆️
                    </button>
                    <button
                      type="button"
                      onClick={handleAddValueStackRow}
                      className="px-2.5 py-1 rounded-lg bg-orange-500 hover:bg-orange-600 text-white font-bold text-xs transition"
                    >
                      + Thêm Món
                    </button>
                  </div>
                </div>

                <div className="space-y-2 max-h-60 overflow-y-auto pr-1">
                  {formItems.map((item, idx) => (
                    <div key={idx} className="p-3 rounded-xl bg-[#0b0f19] border border-[#222f49] space-y-2">
                      <div className="grid grid-cols-12 gap-2 items-center">
                        <div className="col-span-6 sm:col-span-5">
                          <input
                            type="text"
                            placeholder="Tên món (vd: [BONUS] Voucher cafe)"
                            required
                            value={item.name}
                            onChange={e => handleUpdateValueStackRow(idx, 'name', e.target.value)}
                            className="w-full px-2.5 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-white text-xs focus:border-amber-500 focus:outline-none"
                          />
                        </div>
                        <div className="col-span-3 sm:col-span-3">
                          <select
                            value={item.type}
                            onChange={e => handleUpdateValueStackRow(idx, 'type', e.target.value)}
                            className="w-full px-2 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-white text-xs focus:border-amber-500 focus:outline-none"
                          >
                            <option value="core">✅ Hạng mục chính</option>
                            <option value="bonus">🎁 Quà tặng (Bonus)</option>
                          </select>
                        </div>
                        <div className="col-span-2 sm:col-span-3">
                          <input
                            type="number"
                            placeholder="Giá trị (VNĐ)"
                            required
                            value={item.originalValue || ''}
                            onChange={e => handleUpdateValueStackRow(idx, 'originalValue', Number(e.target.value))}
                            className="w-full px-2.5 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-amber-300 font-mono text-xs focus:border-amber-500 focus:outline-none"
                          />
                        </div>
                        <div className="col-span-1 text-right">
                          <button
                            type="button"
                            onClick={() => handleRemoveValueStackRow(idx)}
                            className="p-1.5 rounded-lg bg-rose-500/10 hover:bg-rose-500/20 text-rose-400 transition"
                            title="Xóa món này"
                          >
                            🗑️
                          </button>
                        </div>
                      </div>
                      <div>
                        <input
                          type="text"
                          placeholder="Mô tả ngắn gọn giá trị món..."
                          value={item.description || ''}
                          onChange={e => handleUpdateValueStackRow(idx, 'description', e.target.value)}
                          className="w-full px-2.5 py-1 rounded-lg bg-[#151c2e] border border-[#222f49] text-slate-400 text-[11px] focus:border-slate-500 focus:outline-none"
                        />
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Form buttons */}
              <div className="p-4 border-t border-[#222f49] flex justify-end space-x-2.5 bg-[#111726] -mx-6 -mb-6 mt-4">
                <button
                  type="button"
                  onClick={() => setShowEditModal(false)}
                  className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-semibold text-xs transition"
                >
                  Hủy Bỏ
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 rounded-xl bg-gradient-to-r from-orange-500 to-rose-500 hover:from-orange-600 hover:to-rose-600 text-white font-bold text-xs shadow-lg shadow-orange-500/20 transition"
                >
                  💾 Lưu Gói Offer
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </AdminLayout>
  );
};
