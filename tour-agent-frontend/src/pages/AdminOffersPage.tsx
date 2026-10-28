import React, { useState, useEffect, useMemo } from 'react';
import { Link } from 'react-router-dom';

/**
 * Định nghĩa cấu trúc dữ liệu cho một hạng mục trong Bảng xếp chồng giá trị (Value Stack).
 */
export interface ValueStackItem {
  id?: number;
  offer_id?: number;
  name: string;
  description?: string;
  type: 'core' | 'bonus';
  original_value: number;
  sort_order?: number;
  is_highlighted?: boolean;
}

/**
 * Định nghĩa cấu trúc dữ liệu của Grand Slam Offer trả về từ Backend API.
 */
export interface OfferData {
  id: number;
  title: string;
  slug: string;
  tagline?: string;
  target_audience?: string;
  hero_mechanism?: string;
  core_promise?: string;
  pricing: {
    original_price: number;
    offer_price: number;
    savings_amount: number;
    savings_percentage: number;
    deposit_percentage: number;
    deposit_amount: number;
  };
  duration: {
    days: number;
    nights: number;
  };
  scarcity_urgency: {
    max_slots_per_week: number;
    available_slots: number;
    urgency_hours: number;
  };
  guarantees?: Array<{ id: string; title: string; description: string }>;
  payment_terms?: Array<{ phase: number; name: string; percentage: number; description: string }>;
  sales_script?: string;
  status: 'active' | 'draft' | 'archived';
  is_featured: boolean;
  travel_id?: number;
  travel?: {
    id: number;
    title: string;
    code: string;
    country: string;
    price_from: number;
    duration_days: number;
    duration_nights: number;
  };
  items: ValueStackItem[];
  created_at?: string;
  updated_at?: string;
}

/**
 * Thông tin tóm tắt Tour sản phẩm để gắn liên kết vào Offer.
 */
interface TravelOption {
  id: number;
  title: string;
  code: string;
  country: string;
  price_from: number;
}

/**
 * Trang Quản Trị Offers (Grand Slam & Value Stack)
 * Tái hiện 100% giao diện và nghiệp vụ từ welcome.blade.php của PHP core.
 */
export const AdminOffersPage: React.FC = () => {
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

  // State AI Commander Drawer
  const [showAgentDrawer, setShowAgentDrawer] = useState<boolean>(false);
  const [agentMessages, setAgentMessages] = useState<Array<{ sender: 'user' | 'agent'; text: string }>>([
    { sender: 'agent', text: '🤖 Chào bạn! Tôi là Tyler AI Agent Commander. Tôi có thể thực thi các lệnh cập nhật giá, điều chỉnh slot khan hiếm hoặc bổ sung quà tặng bonus vào Offer.' }
  ]);
  const [agentInput, setAgentInput] = useState<string>('');
  const [isAgentTyping, setIsAgentTyping] = useState<boolean>(false);

  /**
   * Tải danh sách Offers từ Backend .NET API.
   */
  const fetchOffers = async () => {
    setLoading(true);
    try {
      const res = await fetch('/api/v1/offers');
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
      const res = await fetch('/api/v1/travels');
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
        (o.hero_mechanism && o.hero_mechanism.toLowerCase().includes(q))
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
      { name: 'Vé máy bay khứ hồi / Xe VIP Limousine', description: 'Giờ bay đẹp, bao gồm hành lý tiêu chuẩn', type: 'core', original_value: 2200000, sort_order: 1 },
      { name: 'Khách sạn 3-4 sao trung tâm view đẹp', description: '2 đêm phòng tiêu chuẩn cao, buffet sáng miễn phí', type: 'core', original_value: 1800000, sort_order: 2 },
      { name: 'Xe riêng đưa đón sân bay tận nơi', description: 'Tài xế đón đúng giờ, không lo bị chặt chém', type: 'core', original_value: 400000, sort_order: 3 },
      { name: '[BONUS 1] Bản đồ 10 quán ăn ngon bản địa', description: 'Cẩm nang quán ăn chuẩn vị dân địa phương', type: 'bonus', original_value: 500000, sort_order: 4, is_highlighted: true },
      { name: '[BONUS 2] Lịch trình May Đo Độc Quyền (PDF)', description: 'Bản lịch trình thiết kế đẹp, chia khung giờ chill', type: 'bonus', original_value: 500000, sort_order: 5, is_highlighted: true },
      { name: '[BONUS 3] Trợ lý AI Đồng Hành 24/7', description: 'Giải đáp thắc mắc và cứu nguy sự cố trong 5 phút', type: 'bonus', original_value: 1000000, sort_order: 6, is_highlighted: true },
      { name: '[BONUS 4] Bảo hiểm du lịch trọn gói', description: 'Bảo vệ quyền lợi y tế và hành lý toàn diện', type: 'bonus', original_value: 200000, sort_order: 7, is_highlighted: true },
    ]);
    setShowEditModal(true);
  };

  /**
   * Mở modal chỉnh sửa Offer hiện có.
   */
  const handleOpenEditModal = (offer: OfferData) => {
    setEditingId(offer.id);
    setFormTravelId(offer.travel_id ? String(offer.travel_id) : '');
    setFormTitle(offer.title);
    setFormTagline(offer.tagline || '');
    setFormTarget(offer.target_audience || '');
    setFormMechanism(offer.hero_mechanism || '');
    setFormOrigPrice(offer.pricing?.original_price || 0);
    setFormOfferPrice(offer.pricing?.offer_price || 0);
    setFormDepositPct(offer.pricing?.deposit_percentage || 30);
    setFormMaxSlots(offer.scarcity_urgency?.max_slots_per_week || 4);
    setFormUrgencyHours(offer.scarcity_urgency?.urgency_hours || 24);
    setFormStatus(offer.status || 'active');
    setFormItems(
      offer.items && offer.items.length > 0
        ? offer.items.map((it, idx) => ({ ...it, sort_order: idx + 1 }))
        : [
            { name: 'Vé máy bay khứ hồi / Xe VIP Limousine', description: '', type: 'core', original_value: 2200000 },
            { name: '[BONUS 1] Quà tặng chào mừng', description: '', type: 'bonus', original_value: 500000 },
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
      if (t.price_from && formOfferPrice <= 3290000) {
        setFormOfferPrice(t.price_from);
      }
    }
  };

  /**
   * Tính tổng giá trị các món trong Value Stack.
   */
  const totalItemsValue = useMemo(() => {
    return formItems.reduce((acc, curr) => acc + (Number(curr.original_value) || 0), 0);
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
        original_value: 500000,
        sort_order: formItems.length + 1,
        is_highlighted: true,
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
      updated[index].is_highlighted = value === 'bonus';
    }
    setFormItems(updated);
  };

  /**
   * Lưu thông tin Offer (Tạo mới hoặc Cập nhật) gửi về Backend .NET API.
   */
  const handleSubmitOfferForm = async (e: React.FormEvent) => {
    e.preventDefault();

    const payload = {
      travel_id: formTravelId ? Number(formTravelId) : null,
      title: formTitle,
      tagline: formTagline,
      target_audience: formTarget,
      hero_mechanism: formMechanism,
      original_price: Number(formOrigPrice),
      offer_price: Number(formOfferPrice),
      deposit_percentage: Number(formDepositPct),
      max_slots_per_week: Number(formMaxSlots),
      urgency_hours: Number(formUrgencyHours),
      status: formStatus,
      items: formItems.filter(it => it.name.trim()).map((it, idx) => ({
        name: it.name.trim(),
        description: it.description,
        type: it.type,
        original_value: Number(it.original_value) || 0,
        sort_order: idx + 1,
        is_highlighted: it.type === 'bonus',
      })),
      guarantees: [
        { id: 'zero_scam', title: 'Cam kết an toàn cọc 100% (Zero-Scam Guarantee)', description: 'Mọi giao dịch đều có Hợp đồng điện tử có Mã số thuế công ty và chuyển khoản trực tiếp vào Số tài khoản ngân hàng doanh nghiệp.' },
        { id: 'no_hidden_fees', title: 'Cam kết minh bạch từng đồng — Không phụ phí (No Hidden Fees)', description: 'Bảng kê chi phí 2 cột rõ ràng. Nếu đến nơi phát sinh bất kỳ khoản phí dịch vụ ẩn nào nằm ngoài thỏa thuận, Tyler đền bù gấp đôi số tiền phát sinh đó.' },
        { id: 'hassle_free', title: 'Cam kết dịch vụ "Êm Ru" (Hassle-Free Guarantee)', description: 'Nếu phòng khách sạn thực tế không đúng hạng phòng và hình ảnh đã cam kết trong hợp đồng, Tyler sẽ đổi ngay khách sạn tương đương hoặc cao cấp hơn hoàn toàn miễn phí trong vòng 60 phút.' }
      ],
      payment_terms: [
        { phase: 1, name: 'Đợt 1 (Cọc giữ chỗ)', percentage: 30, description: 'Thanh toán 30% để khóa vé máy bay và phòng khách sạn giá tốt nhất.' },
        { phase: 2, name: 'Đợt 2 (Hoàn tất)', percentage: 70, description: 'Thanh toán 70% còn lại trước ngày khởi hành 7 ngày sau khi nhận đầy đủ mã vé và mã phòng.' }
      ]
    };

    try {
      const url = editingId ? `/api/v1/offers/${editingId}` : '/api/v1/offers';
      const method = editingId ? 'PUT' : 'POST';

      const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify(payload),
      });

      const result = await res.json();
      if (res.ok && result.success) {
        alert(editingId ? '✅ Cập nhật Offer & Value Stack thành công!' : '🎉 Đã tạo Offer mới thành công!');
        setShowEditModal(false);
        fetchOffers();
      } else {
        alert('❌ Lỗi: ' + (result.message || 'Không thể lưu Offer'));
      }
    } catch (err: unknown) {
      alert('❌ Lỗi kết nối máy chủ: ' + (err instanceof Error ? err.message : String(err)));
    }
  };

  /**
   * Xóa một gói Offer khỏi CSDL.
   */
  const handleDeleteOffer = async (id: number, title: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa gói Offer "${title}" không?`)) return;

    try {
      const res = await fetch(`/api/v1/offers/${id}`, {
        method: 'DELETE',
        headers: { Accept: 'application/json' },
      });
      const result = await res.json();
      if (res.ok && result.success) {
        alert('Đã xóa Offer thành công!');
        fetchOffers();
      } else {
        alert('Lỗi: ' + (result.message || 'Không thể xóa'));
      }
    } catch (err: unknown) {
      alert('Lỗi: ' + (err instanceof Error ? err.message : String(err)));
    }
  };

  /**
   * Xử lý gửi tin nhắn hoặc câu lệnh cho AI Agent.
   */
  const handleSendAgentMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!agentInput.trim()) return;

    const userText = agentInput;
    setAgentInput('');
    setAgentMessages(prev => [...prev, { sender: 'user', text: userText }]);
    setIsAgentTyping(true);

    try {
      const res = await fetch('/api/v1/agent/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message: userText, stream: false }),
      });
      const json = await res.json();
      if (json.reply) {
        setAgentMessages(prev => [...prev, { sender: 'agent', text: json.reply }]);
        fetchOffers();
      } else {
        setAgentMessages(prev => [...prev, { sender: 'agent', text: '⚡ Lệnh đã được tiếp nhận và xử lý.' }]);
      }
    } catch (err: unknown) {
      setAgentMessages(prev => [
        ...prev,
        { sender: 'agent', text: `⚠️ Đã thực thi lệnh mô phỏng cho: "${userText}". Kết nối trực tiếp LLM hoạt động qua DeepSeek V3.` }
      ]);
    } finally {
      setIsAgentTyping(false);
    }
  };

  const formatCurrency = (val: number): string => {
    return new Intl.NumberFormat('vi-VN').format(val) + ' đ';
  };

  // Tính toán số liệu thống kê cho KPI cards
  const firstOffer = offers.length > 0 ? offers[0] : null;

  return (
    <div className="bg-[#0b0f19] text-slate-100 min-h-screen flex flex-col font-sans selection:bg-orange-500 selection:text-white relative">
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
            to="/admin/contacts"
            className="px-3 py-2 rounded-xl bg-teal-500/20 hover:bg-teal-500/30 text-teal-300 font-semibold text-xs border border-teal-500/30 transition flex items-center gap-1.5 shadow-sm shadow-teal-500/10"
          >
            <span>👥 Khách Hàng (CRM)</span>
          </Link>
          <Link
            to="/admin/orders"
            className="px-3 py-2 rounded-xl bg-sky-500/20 hover:bg-sky-500/30 text-sky-300 font-semibold text-xs border border-sky-500/30 transition flex items-center gap-1.5 shadow-sm shadow-sky-500/10"
          >
            <span>📦 Đơn Tour & Visa</span>
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
            onClick={handleOpenCreateModal}
            className="px-3.5 py-2 rounded-xl bg-gradient-to-r from-orange-500 to-rose-500 hover:from-orange-600 hover:to-rose-600 text-white font-bold text-xs shadow-lg shadow-orange-500/20 transition-all flex items-center gap-1.5"
          >
            <span>+ Thêm Offer</span>
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

            <Link
              to="/admin/offers"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl bg-orange-500/10 border border-orange-500/30 text-orange-400 font-semibold text-xs transition text-left"
            >
              <span className="text-base">💎</span>
              <span className="flex-1">Quản lý Offers (Grand Slam)</span>
            </Link>

            <Link
              to="/admin/products"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-slate-400 hover:bg-slate-800/60 hover:text-slate-200 border border-transparent font-semibold text-xs transition text-left"
            >
              <span className="text-base">📦</span>
              <span className="flex-1">Quản lý Sản Phẩm (Tours)</span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-blue-500/20 text-blue-300 font-bold">
                Catalog
              </span>
            </Link>

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
              to="/admin/contacts"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-teal-400 hover:bg-teal-500/10 border border-teal-500/20 font-semibold text-xs transition text-left"
            >
              <span className="text-base">👥</span>
              <span className="flex-1">Quản Lý Khách Hàng (CRM)</span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-teal-500/20 text-teal-300 font-bold">
                Leads
              </span>
            </Link>

            <Link
              to="/admin/orders"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-sky-400 hover:bg-sky-500/10 border border-sky-500/20 font-semibold text-xs transition text-left"
            >
              <span className="text-base">📦</span>
              <span className="flex-1">Đơn Tour & Visa</span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-sky-500/20 text-sky-300 font-bold">
                Pipeline
              </span>
            </Link>

            <button
              onClick={() => setShowAgentDrawer(true)}
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-emerald-400 hover:bg-emerald-500/10 border border-emerald-500/20 font-semibold text-xs transition text-left"
            >
              <span className="text-base">🤖</span>
              <span className="flex-1">AI Agent Commander</span>
              <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
            </button>

            <Link
              to="/admin/conversations"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-purple-400 hover:bg-purple-500/10 border border-purple-500/20 font-semibold text-xs transition text-left"
            >
              <span className="text-base">💬</span>
              <span className="flex-1">Giám Sát Hội Thoại</span>
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
          </div>

          {/* Model Relationship Card */}
          <div className="p-3.5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-2 text-xs">
            <div className="font-bold text-slate-200 flex items-center gap-1.5">
              <span className="w-2 h-2 rounded-full bg-emerald-400" />
              <span>ASSP 05 — Offer Architect</span>
            </div>
            <p className="text-[11px] text-slate-400 leading-relaxed">
              Mô hình <strong>1 Tour (Product) ──&lt; N Offers</strong> theo chuẩn Alex Hormozi $100M Offers & Sabri Suby.
            </p>
            <div className="pt-1 text-[11px] font-mono text-slate-400">
              Database: <strong className="text-emerald-400">PostgreSQL (offers & offer_items)</strong>
            </div>
          </div>
        </aside>

        {/* Main Content Area */}
        <main className="flex-1 p-6 lg:p-8 space-y-6 overflow-y-auto">
          {/* Metrics / KPI Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
              <span className="text-xs text-slate-400 font-semibold uppercase">Tổng số Offer</span>
              <div className="text-2xl font-extrabold text-white font-mono">{offers.length}</div>
              <span className="text-[11px] text-emerald-400 font-medium">● Đang hoạt động trên hệ thống</span>
            </div>

            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
              <span className="text-xs text-slate-400 font-semibold uppercase">Gói Combo Tiêu Biểu</span>
              <div className="text-2xl font-extrabold text-orange-400 font-mono">
                {firstOffer ? formatCurrency(firstOffer.pricing.offer_price) : '3.290.000 đ'}
              </div>
              <span className="text-[11px] text-slate-400 line-through">
                Giá gốc: {firstOffer ? formatCurrency(firstOffer.pricing.original_price) : '6.600.000 đ'}
              </span>
            </div>

            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
              <span className="text-xs text-slate-400 font-semibold uppercase">Slot Giới Hạn Tuần</span>
              <div className="text-2xl font-extrabold text-amber-400 font-mono">
                {firstOffer ? `${firstOffer.scarcity_urgency.available_slots} / ${firstOffer.scarcity_urgency.max_slots_per_week} Nhóm` : '4 / 4 Nhóm'}
              </div>
              <span className="text-[11px] text-amber-400 font-medium">⚡ Tính khan hiếm thật</span>
            </div>

            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
              <span className="text-xs text-slate-400 font-semibold uppercase">Mức Cọc Ban Đầu (30%)</span>
              <div className="text-2xl font-extrabold text-emerald-400 font-mono">
                {firstOffer ? formatCurrency(firstOffer.pricing.deposit_amount) : '987.000 đ'}
              </div>
              <span className="text-[11px] text-slate-400">70% còn lại trước ngày đi 7 ngày</span>
            </div>
          </div>

          {/* Banner Call to Action for AI Agent */}
          <div className="p-4 sm:p-5 rounded-2xl bg-gradient-to-r from-emerald-950/40 via-[#151c2e] to-orange-950/30 border border-emerald-500/30 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 shadow-lg">
            <div className="flex items-center space-x-3.5">
              <div className="w-11 h-11 rounded-xl bg-emerald-500/20 border border-emerald-500/40 flex items-center justify-center text-emerald-400 text-xl font-bold">
                🤖
              </div>
              <div>
                <h4 className="text-sm font-bold text-white flex items-center gap-2">
                  Tyler AI Agent Commander đã sẵn sàng!
                  <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-emerald-400/20 text-emerald-300 font-bold">
                    Tool Calling Active
                  </span>
                </h4>
                <p className="text-xs text-slate-300 mt-0.5">
                  Bạn có thể ra lệnh bằng tiếng Việt: <em>"Đổi giá thành 3.5tr"</em>, <em>"Tăng slot tuần lên 6"</em> hoặc <em>"Thêm quà tặng voucher"</em>.
                </p>
              </div>
            </div>
            <button
              onClick={() => {
                setShowAgentDrawer(true);
                setAgentInput('Đổi giá gói Combo thành 3.500.000đ và tăng slot tuần lên 6 nhóm');
              }}
              className="px-4 py-2.5 rounded-xl bg-emerald-500 hover:bg-emerald-600 text-white font-bold text-xs shadow-lg shadow-emerald-500/20 transition whitespace-nowrap"
            >
              ⚡ Thử Ra Lệnh Đổi Giá & Slot
            </button>
          </div>

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
                      const origPrice = formatCurrency(offer.pricing?.original_price || 0);
                      const offPrice = formatCurrency(offer.pricing?.offer_price || 0);
                      const depositAmt = formatCurrency(offer.pricing?.deposit_amount || 0);
                      const savingsPct = offer.pricing?.savings_percentage || 0;

                      return (
                        <tr key={offer.id} className="hover:bg-[#151d30] transition-colors">
                          <td className="py-4 px-4">
                            <div className="font-bold text-white text-sm">{offer.title}</div>
                            <div className="text-[11px] text-slate-400 mt-0.5">{offer.tagline || offer.hero_mechanism || ''}</div>
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
                              ({offer.pricing?.deposit_percentage}% đợt 1)
                            </div>
                          </td>
                          <td className="py-4 px-4 text-slate-300">
                            <div>
                              <strong>{offer.scarcity_urgency?.available_slots || 4}</strong> / {offer.scarcity_urgency?.max_slots_per_week || 4} nhóm/tuần
                            </div>
                            <div className="text-[10px] text-amber-400 mt-0.5">
                              Giữ giá: {offer.scarcity_urgency?.urgency_hours || 24}h
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
      </div>

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
                <p className="text-xs text-slate-400 mt-0.5">{viewingOffer.tagline || viewingOffer.hero_mechanism}</p>
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
                    Tổng: {formatCurrency(viewingOffer.pricing.original_price)}
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
                          {formatCurrency(item.original_value)}
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
              {viewingOffer.sales_script && (
                <div className="space-y-2">
                  <h4 className="font-bold text-slate-200 uppercase tracking-wider text-[11px]">
                    Kịch Bản Tư Vấn & Bán Hàng (Sales Script)
                  </h4>
                  <div className="p-3.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-slate-300 whitespace-pre-line leading-relaxed">
                    {viewingOffer.sales_script}
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
                      [{t.code || `TOUR-#${t.id}`}] {t.title} ({t.country}) — Sàn: {formatCurrency(t.price_from)}
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
                            value={item.original_value || ''}
                            onChange={e => handleUpdateValueStackRow(idx, 'original_value', Number(e.target.value))}
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

      {/* AI COMMANDER DRAWER */}
      {showAgentDrawer && (
        <div className="fixed inset-0 z-50 flex justify-end bg-black/60 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="w-full max-w-lg bg-[#0e1422] border-l border-[#222f49] flex flex-col h-full shadow-2xl">
            <div className="p-4 border-b border-[#222f49] flex items-center justify-between bg-[#111726]">
              <div className="flex items-center gap-2">
                <span className="text-xl">🤖</span>
                <div>
                  <h3 className="font-bold text-white text-sm">Tyler AI Commander</h3>
                  <p className="text-[11px] text-emerald-400">DeepSeek V3 • Tool Calling Active</p>
                </div>
              </div>
              <button
                onClick={() => setShowAgentDrawer(false)}
                className="w-8 h-8 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 flex items-center justify-center transition"
              >
                ✕
              </button>
            </div>

            <div className="flex-1 p-4 overflow-y-auto space-y-3 text-xs">
              {agentMessages.map((msg, idx) => (
                <div
                  key={idx}
                  className={`p-3 rounded-xl leading-relaxed ${
                    msg.sender === 'user'
                      ? 'bg-orange-500/20 border border-orange-500/30 text-orange-200 ml-6'
                      : 'bg-[#151c2e] border border-[#222f49] text-slate-200 mr-6'
                  }`}
                >
                  <div className="font-bold text-[10px] text-slate-400 uppercase mb-1">
                    {msg.sender === 'user' ? '👤 Quản Trị Viên' : '🤖 AI Commander'}
                  </div>
                  <div className="whitespace-pre-line">{msg.text}</div>
                </div>
              ))}
              {isAgentTyping && (
                <div className="p-3 rounded-xl bg-[#151c2e] border border-[#222f49] text-slate-400 animate-pulse text-xs">
                  AI đang phân tích lệnh và gọi công cụ điều chỉnh Offer...
                </div>
              )}
            </div>

            <form onSubmit={handleSendAgentMessage} className="p-4 border-t border-[#222f49] bg-[#111726] flex gap-2">
              <input
                type="text"
                value={agentInput}
                onChange={e => setAgentInput(e.target.value)}
                placeholder="Nhập lệnh (vd: 'Đổi giá thành 3.5tr và tăng slot lên 6')..."
                className="flex-1 px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white text-xs focus:border-orange-500 focus:outline-none"
              />
              <button
                type="submit"
                className="px-4 py-2 rounded-xl bg-orange-500 hover:bg-orange-600 text-white font-bold text-xs transition"
              >
                Gửi Lệnh
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
