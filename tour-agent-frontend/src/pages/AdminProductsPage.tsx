import React, { useState, useEffect, useMemo } from 'react';
import { Link } from 'react-router-dom';
import type { TravelProduct, TourDeparture, ItineraryDay, TravelReview, TravelFaq } from '../types/travel';

/**
 * Trang Quản lý Danh mục Sản Phẩm Tour Du Lịch (Core Product Base).
 * Tái hiện 100% giao diện, layout và logic từ tab-products trong welcome.blade.php của PHP.
 */
export const AdminProductsPage: React.FC = () => {
  // State danh sách tour và tìm kiếm
  const [products, setProducts] = useState<TravelProduct[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [searchQuery, setSearchQuery] = useState<string>('');

  // State các Modal
  const [selectedProduct, setSelectedProduct] = useState<TravelProduct | null>(null);
  const [showItineraryModal, setShowItineraryModal] = useState<boolean>(false);
  const [showPoliciesModal, setShowPoliciesModal] = useState<boolean>(false);
  const [showReviewsModal, setShowReviewsModal] = useState<boolean>(false);
  const [showFaqsModal, setShowFaqsModal] = useState<boolean>(false);
  const [showEntryModal, setShowEntryModal] = useState<boolean>(false);

  // State Reviews modal
  const [reviews, setReviews] = useState<TravelReview[]>([]);
  const [showAddReviewForm, setShowAddReviewForm] = useState<boolean>(false);
  const [reviewName, setReviewName] = useState<string>('');
  const [reviewDate, setReviewDate] = useState<string>('');
  const [reviewRating, setReviewRating] = useState<number>(5);
  const [reviewContent, setReviewContent] = useState<string>('');
  const [reviewVisible, setReviewVisible] = useState<boolean>(true);

  // State FAQs modal
  const [faqs, setFaqs] = useState<TravelFaq[]>([]);
  const [showAddFaqForm, setShowAddFaqForm] = useState<boolean>(false);
  const [faqQuestion, setFaqQuestion] = useState<string>('');
  const [faqAnswer, setFaqAnswer] = useState<string>('');
  const [faqSort, setFaqSort] = useState<number>(0);
  const [faqVisible, setFaqVisible] = useState<boolean>(true);

  // State Form Create / Edit Tour
  const [editingId, setEditingId] = useState<number | null>(null);
  const [entryTitle, setEntryTitle] = useState<string>('');
  const [entryCode, setEntryCode] = useState<string>('');
  const [entryCountry, setEntryCountry] = useState<string>('Nhật Bản');
  const [entryDepartureCity, setEntryDepartureCity] = useState<string>('TP.HCM');
  const [entryDays, setEntryDays] = useState<number>(6);
  const [entryNights, setEntryNights] = useState<number>(5);
  const [entryDestinations, setEntryDestinations] = useState<string>('Osaka, Kyoto, Gifu, Takayama, Shirakawago, Núi Phú Sĩ, Tokyo');
  const [entryRoute, setEntryRoute] = useState<string>('TP.HCM – Osaka – Kyoto – Tokyo – TP.HCM');
  const [entryTags, setEntryTags] = useState<string[]>(['cao-cap', 'gia-dinh']);
  const [entryStatus, setEntryStatus] = useState<'published' | 'draft'>('published');
  const [entryIsFeatured, setEntryIsFeatured] = useState<boolean>(false);
  const [entryDepartures, setEntryDepartures] = useState<TourDeparture[]>([
    { departure_date: '2025-10-15', price_adult: 35990000, price_child: 30990000, single_supplement: 5000000, status: 'open', notes: 'Mùa thu lá đỏ' }
  ]);
  const [entryItinerary, setEntryItinerary] = useState<ItineraryDay[]>([
    { day: 1, title: 'NGÀY 1: TP.HCM – OSAKA', route: 'Kansai – Osaka', content: 'Sáng: Tập trung tại sân bay Tân Sơn Nhất.\nTrưa: Đáp sân bay Kansai, dùng bữa trưa.\nTối: Nhận phòng khách sạn 4 sao tại Osaka, tự do dạo phố Dotonbori.', images: [] }
  ]);
  const [entryInclusions, setEntryInclusions] = useState<string>('Vé máy bay khứ hồi Vietjet Air\nKhách sạn 3-4 sao có tắm khoáng nóng Onsen\nBữa ăn chất lượng theo chương trình\nBảo hiểm du lịch quốc tế');
  const [entryExclusions, setEntryExclusions] = useState<string>('Tiền tip hướng dẫn viên (1.000.000đ/khách)\nLệ phí visa (1.000.000đ/khách)\nPhụ thu phòng đơn\nChi phí cá nhân');
  const [entryChildPolicy, setEntryChildPolicy] = useState<string>('• Trẻ dưới 2 tuổi: 35% giá tour người lớn\n• Trẻ 2 - dưới 11 tuổi không giường: 85% giá tour\n• Trẻ từ 11 tuổi trở lên: 100% giá người lớn');
  const [entryPaymentPolicy, setEntryPaymentPolicy] = useState<string>('• Đợt 1: Đặt cọc 30% khi đăng ký giữ chỗ\n• Đợt 2: Thanh toán 70% còn lại trước ngày khởi hành 7 ngày');
  const [entryCancellationPolicy, setEntryCancellationPolicy] = useState<string>('• Hủy sau khi cọc: mất cọc\n• Cam kết rớt Visa: Hoàn 100% tiền tour');
  const [entryVisaPolicy, setEntryVisaPolicy] = useState<string>('Hộ chiếu gốc còn hạn trên 6 tháng; 02 ảnh 3.5x4.5 nền trắng; CCCD; Hợp đồng lao động & sao kê');
  const [entryNotes, setEntryNotes] = useState<string>('1. Hạn chót nộp hồ sơ trước 15 ngày.\n2. Giá có thể thay đổi tùy đợt bay.');

  // State AI Commander Drawer
  const [showAgentDrawer, setShowAgentDrawer] = useState<boolean>(false);
  const [agentMode, setAgentMode] = useState<'consultant' | 'commander'>('consultant');
  const [agentMessages, setAgentMessages] = useState<Array<{ sender: 'user' | 'agent'; text: string }>>([
    { sender: 'agent', text: 'Dạ em chào anh/chị! Em là Chuyên viên AI Tư Vấn Du Lịch 24/7. Em có thể hỗ trợ tra cứu danh sách tour, lịch trình, ngày bay và chính sách hoàn hủy.' }
  ]);
  const [agentInput, setAgentInput] = useState<string>('');
  const [isAgentTyping, setIsAgentTyping] = useState<boolean>(false);

  /**
   * Tải danh mục tour trực tiếp từ Backend API.
   */
  const fetchProducts = async () => {
    setLoading(true);
    try {
      const res = await fetch('/api/v1/travels');
      const json = await res.json();
      if (json.success && json.data) {
        setProducts(json.data);
      }
    } catch (err) {
      console.error('Lỗi khi tải danh sách tour:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  /**
   * Bộ lọc tìm kiếm Client-side theo tiêu chí mã tour, tên, quốc gia, điểm đến.
   */
  const filteredProducts = useMemo(() => {
    if (!searchQuery.trim()) return products;
    const q = searchQuery.toLowerCase();
    return products.filter(
      p =>
        p.title.toLowerCase().includes(q) ||
        p.code.toLowerCase().includes(q) ||
        p.country.toLowerCase().includes(q) ||
        (p.route && p.route.toLowerCase().includes(q)) ||
        (p.destinations && p.destinations.some(d => d.toLowerCase().includes(q)))
    );
  }, [products, searchQuery]);

  /**
   * Mở modal thêm tour mới.
   */
  const handleOpenCreateModal = () => {
    setEditingId(null);
    setEntryTitle('');
    setEntryCode(`TOUR-JP-${Date.now().toString().slice(-4)}`);
    setEntryCountry('Nhật Bản');
    setEntryDepartureCity('TP.HCM');
    setEntryDays(6);
    setEntryNights(5);
    setEntryDestinations('Osaka, Kyoto, Gifu, Takayama, Shirakawago, Núi Phú Sĩ, Tokyo');
    setEntryRoute('TP.HCM – Osaka – Kyoto – Tokyo – TP.HCM');
    setEntryTags(['cao-cap', 'gia-dinh']);
    setEntryStatus('published');
    setEntryIsFeatured(false);
    setEntryDepartures([
      { departure_date: '2025-10-15', price_adult: 35990000, price_child: 30990000, single_supplement: 5000000, status: 'open', notes: 'Mùa thu lá đỏ' }
    ]);
    setEntryItinerary([
      { day: 1, title: 'NGÀY 1: TP.HCM – OSAKA', route: 'Kansai – Osaka', content: 'Sáng: Tập trung tại sân bay Tân Sơn Nhất.\nTrưa: Đáp sân bay Kansai, dùng bữa trưa.\nTối: Nhận phòng khách sạn 4 sao tại Osaka, tự do dạo phố Dotonbori.', images: [] }
    ]);
    setShowEntryModal(true);
  };

  /**
   * Mở modal sửa thông tin tour hiện có.
   */
  const handleOpenEditModal = (product: TravelProduct) => {
    setEditingId(product.id);
    setEntryTitle(product.title);
    setEntryCode(product.code);
    setEntryCountry(product.country || 'Nhật Bản');
    setEntryDepartureCity(product.departure_city || 'TP.HCM');
    setEntryDays(product.duration_days || 6);
    setEntryNights(product.duration_nights || 5);
    setEntryDestinations(Array.isArray(product.destinations) ? product.destinations.join(', ') : '');
    setEntryRoute(product.route || '');
    setEntryTags(Array.isArray(product.tags) ? product.tags : []);
    setEntryStatus(product.status || 'published');
    setEntryIsFeatured(Boolean(product.is_featured));
    setEntryDepartures(product.departures && product.departures.length > 0 ? product.departures : [
      { departure_date: '', price_adult: product.price_from || 35990000, status: 'open' }
    ]);
    setEntryItinerary(product.itinerary && product.itinerary.length > 0 ? product.itinerary : [
      { day: 1, title: 'NGÀY 1: KHỞI HÀNH', content: '', images: [] }
    ]);
    setEntryInclusions(Array.isArray(product.inclusions) ? product.inclusions.join('\n') : (product.inclusions || ''));
    setEntryExclusions(Array.isArray(product.exclusions) ? product.exclusions.join('\n') : (product.exclusions || ''));
    setEntryChildPolicy(product.child_policy || '');
    setEntryPaymentPolicy(product.payment_policy || '');
    setEntryCancellationPolicy(product.cancellation_policy || '');
    setEntryVisaPolicy(product.visa_policy || '');
    setEntryNotes(product.notes || '');
    setShowEntryModal(true);
  };

  /**
   * Lưu dữ liệu tour (Tạo mới hoặc Cập nhật) gửi về Backend .NET API.
   */
  const handleSubmitTourForm = async (e: React.FormEvent) => {
    e.preventDefault();
    const destinationsArray = entryDestinations.split(',').map(s => s.trim()).filter(Boolean);
    const inclusionsArray = entryInclusions.split('\n').map(s => s.trim()).filter(Boolean);
    const exclusionsArray = entryExclusions.split('\n').map(s => s.trim()).filter(Boolean);

    const payload = {
      title: entryTitle,
      code: entryCode,
      country: entryCountry,
      departure_city: entryDepartureCity,
      duration_days: Number(entryDays),
      duration_nights: Number(entryNights),
      route: entryRoute,
      destinations: destinationsArray,
      tags: entryTags,
      status: entryStatus,
      is_featured: entryIsFeatured,
      departures: entryDepartures.filter(d => d.departure_date && d.price_adult > 0),
      itinerary: entryItinerary,
      inclusions: inclusionsArray,
      exclusions: exclusionsArray,
      child_policy: entryChildPolicy,
      payment_policy: entryPaymentPolicy,
      cancellation_policy: entryCancellationPolicy,
      visa_policy: entryVisaPolicy,
      notes: entryNotes,
    };

    try {
      const url = editingId ? `/api/v1/travels/${editingId}` : '/api/v1/travels';
      const method = editingId ? 'PUT' : 'POST';

      const res = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify(payload),
      });

      const result = await res.json();
      if (res.ok && result.success) {
        alert(editingId ? '✅ Cập nhật Tour thành công!' : '🎉 Đã tạo Tour mới thành công!');
        setShowEntryModal(false);
        fetchProducts();
      } else {
        alert('❌ Lỗi: ' + (result.message || 'Không thể lưu Tour'));
      }
    } catch (err: unknown) {
      alert('❌ Lỗi kết nối máy chủ: ' + (err instanceof Error ? err.message : String(err)));
    }
  };

  /**
   * Xóa một Tour khỏi CSDL.
   */
  const handleDeleteProduct = async (id: number, title: string) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa tour "${title}" không?`)) return;

    try {
      const res = await fetch(`/api/v1/travels/${id}`, {
        method: 'DELETE',
        headers: { Accept: 'application/json' },
      });
      const result = await res.json();
      if (res.ok && result.success) {
        alert('Đã xóa Tour thành công!');
        fetchProducts();
      } else {
        alert('Lỗi: ' + (result.message || 'Không thể xóa'));
      }
    } catch (err: unknown) {
      alert('Lỗi: ' + (err instanceof Error ? err.message : String(err)));
    }
  };

  /**
   * Mở modal đánh giá Reviews của tour.
   */
  const handleOpenReviewsModal = async (product: TravelProduct) => {
    setSelectedProduct(product);
    setShowReviewsModal(true);
    setShowAddReviewForm(false);
    try {
      const res = await fetch(`/api/v1/travels/${product.id}/reviews`);
      const json = await res.json();
      if (json.success && json.data) {
        setReviews(json.data);
      }
    } catch {
      setReviews([]);
    }
  };

  /**
   * Thêm Review mới cho Tour.
   */
  const handleAddReview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProduct) return;

    try {
      const res = await fetch(`/api/v1/travels/${selectedProduct.id}/reviews`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({
          reviewer_name: reviewName,
          travel_date: reviewDate,
          rating: Number(reviewRating),
          content: reviewContent,
          is_visible: reviewVisible,
        }),
      });
      const json = await res.json();
      if (res.ok && json.success) {
        setShowAddReviewForm(false);
        setReviewName('');
        setReviewDate('');
        setReviewContent('');
        handleOpenReviewsModal(selectedProduct);
      } else {
        alert(json.message || 'Lỗi lưu review');
      }
    } catch (err: unknown) {
      alert('Lỗi: ' + (err instanceof Error ? err.message : String(err)));
    }
  };

  /**
   * Mở modal câu hỏi thường gặp FAQs của tour.
   */
  const handleOpenFaqsModal = async (product: TravelProduct) => {
    setSelectedProduct(product);
    setShowFaqsModal(true);
    setShowAddFaqForm(false);
    try {
      const res = await fetch(`/api/v1/travels/${product.id}/faqs`);
      const json = await res.json();
      if (json.success && json.data) {
        setFaqs(json.data);
      }
    } catch {
      setFaqs([]);
    }
  };

  /**
   * Thêm FAQ mới cho Tour.
   */
  const handleAddFaq = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedProduct) return;

    try {
      const res = await fetch(`/api/v1/travels/${selectedProduct.id}/faqs`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({
          question: faqQuestion,
          answer: faqAnswer,
          sort_order: Number(faqSort),
          is_visible: faqVisible,
        }),
      });
      const json = await res.json();
      if (res.ok && json.success) {
        setShowAddFaqForm(false);
        setFaqQuestion('');
        setFaqAnswer('');
        handleOpenFaqsModal(selectedProduct);
      } else {
        alert(json.message || 'Lỗi lưu FAQ');
      }
    } catch (err: unknown) {
      alert('Lỗi: ' + (err instanceof Error ? err.message : String(err)));
    }
  };

  /**
   * Gửi tin nhắn chat đến AI Agent.
   */
  const handleAgentChat = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!agentInput.trim()) return;

    const userText = agentInput.trim();
    setAgentMessages(prev => [...prev, { sender: 'user', text: userText }]);
    setAgentInput('');
    setIsAgentTyping(true);

    try {
      const res = await fetch('/api/v1/agent/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({
          message: userText,
          conversation_id: 'admin_session',
        }),
      });
      const data = await res.json();
      if (data.reply || (data.data && data.data.reply)) {
        setAgentMessages(prev => [
          ...prev,
          { sender: 'agent', text: data.reply || data.data.reply },
        ]);
      } else {
        setAgentMessages(prev => [
          ...prev,
          { sender: 'agent', text: 'Dạ hệ thống đã nhận thông tin và đang xử lý.' },
        ]);
      }
    } catch {
      setAgentMessages(prev => [
        ...prev,
        { sender: 'agent', text: '⚠️ Lỗi kết nối dịch vụ AI. Vui lòng thử lại sau.' },
      ]);
    } finally {
      setIsAgentTyping(false);
    }
  };

  return (
    <div className="bg-[#0b0f19] text-slate-100 min-h-screen flex flex-col font-sans selection:bg-orange-500 selection:text-white">
      {/* ── TOP NAVIGATION BAR ── */}
      <header className="bg-[#111726] border-b border-[#222f49] px-6 py-3.5 flex items-center justify-between sticky top-0 z-30 shadow-md">
        <div className="flex items-center space-x-3">
          <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-orange-500 to-rose-500 flex items-center justify-center shadow-lg shadow-orange-500/20 font-bold text-white text-lg">
            💎
          </div>
          <div>
            <span className="font-extrabold text-white text-base tracking-tight flex items-center gap-2">
              AI Agent Travel — Quản Trị Hệ Thống
              <span className="text-[10px] font-bold uppercase px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                Backend Core .NET 10
              </span>
            </span>
            <p className="text-xs text-slate-400">Trụ cột Sản Phẩm (Tours) • Catalog Base & 4 Bộ Chính Sách Cam Kết</p>
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
            to="/admin/settings/llm"
            className="px-3 py-2 rounded-xl bg-indigo-500/20 hover:bg-indigo-500/30 text-indigo-300 font-semibold text-xs border border-indigo-500/30 transition flex items-center gap-1.5 shadow-sm shadow-indigo-500/10"
          >
            <span>⚙️ Cấu Hình LLM</span>
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
            <span className="w-2 h-2 rounded-full bg-emerald-400"></span>
          </Link>
          <button
            onClick={handleOpenCreateModal}
            className="px-3.5 py-2 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-bold text-xs shadow-lg shadow-blue-500/20 transition-all flex items-center gap-1.5"
          >
            <span>➕ Thêm Tour Mới</span>
          </button>
        </div>
      </header>

      {/* ── MAIN WORKSPACE CONTAINER ── */}
      <div className="flex-1 flex flex-col lg:flex-row">
        {/* SIDEBAR NAVIGATION */}
        <aside className="w-full lg:w-64 bg-[#0e1422] border-b lg:border-b-0 lg:border-r border-[#222f49] p-4 space-y-6">
          <div className="space-y-1">
            <div className="text-[11px] font-bold text-slate-400 uppercase tracking-wider px-3 mb-2">
              Trụ cột Nghiệp Vụ
            </div>

            <Link
              to="/admin/offers"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-slate-400 hover:bg-slate-800/60 hover:text-slate-200 border border-transparent font-semibold text-xs transition text-left"
            >
              <span className="text-base">💎</span>
              <span className="flex-1">Quản lý Offers (Grand Slam)</span>
            </Link>

            <Link
              to="/admin/products"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl bg-blue-500/10 border border-blue-500/30 text-blue-400 font-semibold text-xs transition text-left"
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
              <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
            </button>

            <Link
              to="/admin/settings/llm"
              className="w-full flex items-center space-x-3 px-3 py-2.5 rounded-xl text-indigo-400 hover:bg-indigo-500/10 border border-indigo-500/20 font-semibold text-xs transition text-left"
            >
              <span className="text-base">⚙️</span>
              <span className="flex-1">Cấu Hình LLM DeepSeek</span>
              <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-indigo-500/20 text-indigo-300 font-bold">
                Gateway
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

            <a
              href="/api/v1/travels"
              target="_blank"
              rel="noreferrer"
              className="flex items-center space-x-3 px-3 py-2.5 rounded-xl text-slate-400 hover:bg-slate-800/60 hover:text-slate-200 text-xs transition"
            >
              <span className="text-base">🗺️</span>
              <span>API Product Catalog</span>
            </a>
          </div>

          {/* Framework Info Card */}
          <div className="p-3.5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-2 text-xs">
            <div className="font-bold text-slate-200 flex items-center gap-1.5">
              <span className="w-2 h-2 rounded-full bg-emerald-400"></span>
              <span>ASSP 05 — Product Base</span>
            </div>
            <p className="text-[11px] text-slate-400 leading-relaxed">
              Cơ sở dữ liệu sản phẩm chuẩn hóa (Docx Catalog gốc). Cung cấp dữ liệu kỹ thuật và chính sách cho AI Agent
              tra cứu qua 5 Tools.
            </p>
            <div className="pt-1 text-[11px] font-mono text-slate-400">
              Database: <strong className="text-emerald-400">PostgreSQL (tour_agent_db)</strong>
            </div>
          </div>
        </aside>

        {/* MAIN WORKSPACE CONTENT AREA */}
        <main className="flex-1 p-6 lg:p-8 space-y-6 overflow-y-auto">
          {/* KPI CARDS */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
              <span className="text-xs text-slate-400 font-semibold uppercase">Tổng Tour Trong Catalog</span>
              <div className="text-2xl font-extrabold text-white font-mono">{products.length}</div>
              <span className="text-[11px] text-blue-400 font-medium">● Đã đồng bộ từ Docx gốc</span>
            </div>

            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
              <span className="text-xs text-slate-400 font-semibold uppercase">Thời Lượng Tiêu Chuẩn</span>
              <div className="text-2xl font-extrabold text-blue-400 font-mono">6N5Đ (Bay Thẳng)</div>
              <span className="text-[11px] text-slate-400">Osaka - Kyoto - Shirakawago - Tokyo</span>
            </div>

            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
              <span className="text-xs text-slate-400 font-semibold uppercase">Hệ Thống Nghỉ Dưỡng</span>
              <div className="text-2xl font-extrabold text-emerald-400 font-mono">3–4★ Có Onsen</div>
              <span className="text-[11px] text-emerald-400 font-medium">♨️ Tắm khoáng nóng thiên nhiên</span>
            </div>

            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-1">
              <span className="text-xs text-slate-400 font-semibold uppercase">Mô hình Phân Tách</span>
              <div className="text-2xl font-extrabold text-amber-400 font-mono">1 Product ──&lt; N Offers</div>
              <span className="text-[11px] text-slate-400">1 Tour đóng gói thành nhiều Offer</span>
            </div>
          </div>

          {/* PRODUCT CATALOG TABLE SECTION */}
          <div className="bg-[#151c2e] border border-[#222f49] rounded-2xl overflow-hidden shadow-xl space-y-4 p-5 sm:p-6">
            {/* Search & Actions Header */}
            <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
              <div className="flex items-center space-x-3">
                <div className="w-9 h-9 rounded-xl bg-blue-500/20 text-blue-400 flex items-center justify-center font-bold text-lg">
                  🗺️
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white">Danh Mục Tour Sản Phẩm (Core Product Base)</h3>
                  <p className="text-xs text-slate-400">
                    Chứa thông số kỹ thuật, lịch trình chi tiết từng ngày, thông tin bay, khách sạn & 4 bộ chính sách.
                  </p>
                </div>
              </div>

              <div className="flex items-center space-x-2 w-full sm:w-auto">
                <div className="relative flex-1 sm:w-72">
                  <input
                    type="text"
                    value={searchQuery}
                    onChange={e => setSearchQuery(e.target.value)}
                    placeholder="Tìm mã tour, lộ trình, địa danh..."
                    className="w-full pl-9 pr-4 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-xs text-white placeholder-slate-500 focus:outline-none focus:border-blue-500 transition"
                  />
                  <svg
                    className="w-4 h-4 text-slate-400 absolute left-3 top-2.5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth="2"
                      d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                    />
                  </svg>
                </div>
                <button
                  onClick={fetchProducts}
                  className="px-3.5 py-2 rounded-xl bg-[#0b0f19] hover:bg-slate-800 border border-[#222f49] text-xs text-slate-300 font-semibold transition flex items-center gap-1.5 whitespace-nowrap"
                >
                  <span>🔄 Làm mới</span>
                </button>
                <button
                  onClick={handleOpenCreateModal}
                  className="px-3.5 py-2 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-semibold text-xs transition flex items-center gap-1.5 shadow-lg shadow-blue-500/20 whitespace-nowrap"
                >
                  <span>➕ Thêm Tour Mới</span>
                </button>
              </div>
            </div>

            {/* Table */}
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-xs">
                <thead>
                  <tr className="border-b border-[#222f49] bg-[#0f1524] text-slate-400 font-semibold uppercase text-[11px] tracking-wider">
                    <th className="py-3 px-4">Mã & Tên Tour Sản Phẩm</th>
                    <th className="py-3 px-4">Tuyến Đi & Thời Lượng</th>
                    <th className="py-3 px-4">Giá Niêm Yết Gốc</th>
                    <th className="py-3 px-4">Offers Liên Kết</th>
                    <th className="py-3 px-4">Trạng Thái</th>
                    <th className="py-3 px-4 text-right">Chi Tiết Kỹ Thuật</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#222f49]">
                  {loading ? (
                    <tr>
                      <td colSpan={6} className="py-8 text-center text-slate-400">
                        Đang tải danh mục Tour từ Database...
                      </td>
                    </tr>
                  ) : filteredProducts.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="py-8 text-center text-slate-400">
                        Không tìm thấy tour nào.
                      </td>
                    </tr>
                  ) : (
                    filteredProducts.map(t => {
                      const origPrice = Number(t.price_from || t.original_price || 0).toLocaleString('vi-VN');
                      const depCount = t.departures ? t.departures.length : 0;
                      const offersCount = t.offers_count || 0;
                      const statusBadge =
                        t.status === 'published' ? (
                          <span className="px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 text-[10px] font-bold uppercase">
                            Published
                          </span>
                        ) : (
                          <span className="px-2 py-0.5 rounded-full bg-slate-700 text-slate-300 text-[10px] font-bold uppercase">
                            {t.status}
                          </span>
                        );

                      return (
                        <tr key={t.id} className="hover:bg-[#151d30] transition-colors">
                          <td className="py-4 px-4">
                            <div className="flex items-center gap-2">
                              <span className="font-mono text-[11px] text-blue-400 font-bold">
                                {t.code || `TOUR-#${t.id}`}
                              </span>
                              {t.country && (
                                <span className="px-2 py-0.5 rounded-full bg-blue-500/10 text-blue-300 border border-blue-500/20 text-[9px] font-semibold">
                                  📍 {t.country}
                                </span>
                              )}
                              {t.is_featured && (
                                <span className="text-amber-400 text-xs" title="Tour nổi bật">
                                  ⭐
                                </span>
                              )}
                            </div>
                            <div
                              onClick={() => {
                                setSelectedProduct(t);
                                setShowItineraryModal(true);
                              }}
                              className="font-bold text-white hover:text-blue-400 cursor-pointer text-sm mt-1 max-w-md line-clamp-2 transition"
                              title="Nhấp để xem lịch trình"
                            >
                              {t.title}
                            </div>
                            <div className="flex flex-wrap items-center gap-1 mt-1.5">
                              {Array.isArray(t.destinations) &&
                                t.destinations.slice(0, 4).map((d, i) => (
                                  <span
                                    key={i}
                                    className="px-1.5 py-0.5 rounded bg-[#0b0f19] text-slate-300 border border-[#222f49] text-[9px]"
                                  >
                                    {d}
                                  </span>
                                ))}
                            </div>
                          </td>

                          <td className="py-4 px-4">
                            <div className="text-xs font-semibold text-slate-200">
                              {t.duration_days || 1} Ngày {t.duration_nights || 0} Đêm
                            </div>
                            <div className="text-[11px] text-slate-400 max-w-xs truncate mt-0.5" title={t.route || ''}>
                              🗺️ {t.route || 'Lộ trình chi tiết'}
                            </div>
                            <div className="text-[10px] text-emerald-400 font-mono mt-0.5">
                              🛫 Khởi hành: {t.departure_city || 'TP.HCM'}
                            </div>
                          </td>

                          <td className="py-4 px-4 font-mono">
                            <div className="text-sm font-bold text-slate-200">Từ {origPrice} đ</div>
                            <div className="text-[10px] text-slate-400 font-sans mt-0.5">
                              {depCount > 0 ? (
                                <span className="text-amber-400 font-semibold">📅 {depCount} ngày đi</span>
                              ) : (
                                'Chưa có lịch bay'
                              )}
                            </div>
                          </td>

                          <td className="py-4 px-4">
                            <div className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-xl bg-orange-500/10 border border-orange-500/20 text-orange-300 font-mono font-bold">
                              <span>💎 {offersCount}</span>
                              <span className="text-[10px] font-sans font-normal text-slate-400">Offer live</span>
                            </div>
                          </td>

                          <td className="py-4 px-4">{statusBadge}</td>

                          <td className="py-4 px-4 text-right space-x-1.5 whitespace-nowrap">
                            <button
                              onClick={() => {
                                setSelectedProduct(t);
                                setShowItineraryModal(true);
                              }}
                              className="px-2.5 py-1.5 rounded-lg bg-blue-500/20 hover:bg-blue-500/30 text-blue-300 border border-blue-500/40 font-semibold text-[11px] transition"
                              title="Xem lịch trình"
                            >
                              📅 Lịch Trình
                            </button>
                            <button
                              onClick={() => {
                                setSelectedProduct(t);
                                setShowPoliciesModal(true);
                              }}
                              className="px-2.5 py-1.5 rounded-lg bg-purple-500/20 hover:bg-purple-500/30 text-purple-300 border border-purple-500/40 font-semibold text-[11px] transition"
                              title="Xem chính sách"
                            >
                              📋 Chính Sách
                            </button>
                            <button
                              onClick={() => handleOpenReviewsModal(t)}
                              className="px-2 py-1.5 rounded-lg bg-amber-500/15 hover:bg-amber-500/25 text-amber-300 border border-amber-500/30 font-semibold text-[11px] transition inline-flex items-center gap-1"
                              title="Quản lý đánh giá khách hàng (Reviews)"
                            >
                              <span>⭐ Reviews</span>
                              <span className="px-1.5 py-0.2 rounded-full bg-amber-500/30 text-[10px] font-mono font-bold">
                                {t.reviews_count || 0}
                              </span>
                            </button>
                            <button
                              onClick={() => handleOpenFaqsModal(t)}
                              className="px-2 py-1.5 rounded-lg bg-emerald-500/15 hover:bg-emerald-500/25 text-emerald-300 border border-emerald-500/30 font-semibold text-[11px] transition inline-flex items-center gap-1"
                              title="Quản lý câu hỏi thường gặp (FAQs)"
                            >
                              <span>❓ FAQs</span>
                              <span className="px-1.5 py-0.2 rounded-full bg-emerald-500/30 text-[10px] font-mono font-bold">
                                {t.faqs_count || 0}
                              </span>
                            </button>
                            <button
                              onClick={() => handleOpenEditModal(t)}
                              className="px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-amber-300 border border-[#222f49] font-semibold text-[11px] transition"
                              title="Chỉnh sửa thông tin tour"
                            >
                              ✏️ Sửa
                            </button>
                            <button
                              onClick={() => handleDeleteProduct(t.id, t.title)}
                              className="px-2 py-1.5 rounded-lg bg-rose-500/10 hover:bg-rose-500/20 text-rose-400 border border-rose-500/30 transition text-[11px]"
                              title="Xóa tour"
                            >
                              🗑️
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

      {/* ── MODAL: PRODUCT ITINERARY TIMELINE ── */}
      {showItineraryModal && selectedProduct && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-[#151c2e] border border-[#222f49] max-w-4xl w-full max-h-[92vh] rounded-3xl p-6 sm:p-8 space-y-6 shadow-2xl flex flex-col overflow-hidden">
            <div className="flex items-start justify-between border-b border-[#222f49] pb-4">
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <span className="text-[10px] uppercase font-mono font-bold px-2 py-0.5 rounded bg-blue-500/20 text-blue-300 border border-blue-500/30">
                    {selectedProduct.code}
                  </span>
                  <span className="text-[10px] uppercase font-bold px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                    {selectedProduct.duration_days} Ngày {selectedProduct.duration_nights} Đêm
                  </span>
                </div>
                <h3 className="text-lg sm:text-xl font-bold text-white">Lịch Trình Chi Tiết: {selectedProduct.title}</h3>
                <p className="text-xs text-slate-400">{selectedProduct.route}</p>
              </div>
              <button
                onClick={() => setShowItineraryModal(false)}
                className="text-slate-400 hover:text-white text-2xl font-bold leading-none"
              >
                &times;
              </button>
            </div>

            <div className="flex-1 overflow-y-auto space-y-4 pr-1 text-xs">
              {selectedProduct.itinerary && selectedProduct.itinerary.length > 0 ? (
                selectedProduct.itinerary.map((day, idx) => (
                  <div key={idx} className="p-4 rounded-2xl bg-[#0b0f19] border border-[#222f49] space-y-2.5 relative">
                    <div className="flex flex-wrap items-center justify-between gap-2 border-b border-[#222f49]/60 pb-2">
                      <div className="flex items-center gap-2">
                        <span className="w-6 h-6 rounded-full bg-blue-500 text-white flex items-center justify-center font-bold text-xs">
                          {day.day || idx + 1}
                        </span>
                        <h4 className="font-bold text-white text-xs sm:text-sm">{day.title}</h4>
                      </div>
                      {day.route && (
                        <span className="text-[11px] font-mono px-2 py-0.5 rounded bg-slate-800 text-amber-300 font-semibold">
                          📍 {day.route}
                        </span>
                      )}
                    </div>
                    <p className="text-slate-300 leading-relaxed text-xs whitespace-pre-line bg-[#151c2e] p-3 rounded-xl border border-[#222f49]/40">
                      {day.content}
                    </p>
                  </div>
                ))
              ) : (
                <div className="p-6 text-center text-slate-400">Chưa có thông tin lịch trình chi tiết.</div>
              )}
            </div>

            <div className="border-t border-[#222f49] pt-4 flex justify-end">
              <button
                onClick={() => setShowItineraryModal(false)}
                className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-white font-semibold text-xs transition"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── MODAL: PRODUCT POLICIES ── */}
      {showPoliciesModal && selectedProduct && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-[#151c2e] border border-[#222f49] max-w-3xl w-full max-h-[92vh] rounded-3xl p-6 sm:p-8 space-y-6 shadow-2xl flex flex-col overflow-hidden">
            <div className="flex items-start justify-between border-b border-[#222f49] pb-4">
              <div className="space-y-1">
                <span className="text-[10px] uppercase font-bold px-2 py-0.5 rounded bg-blue-500/20 text-blue-300 border border-blue-500/30">
                  Chính Sách & Điều Khoản Cam Kết
                </span>
                <h3 className="text-lg font-bold text-white">{selectedProduct.title}</h3>
                <p className="text-xs text-slate-400">Bao gồm, không bao gồm, giá trẻ em, quy trình thanh toán và điều kiện hủy tour</p>
              </div>
              <button
                onClick={() => setShowPoliciesModal(false)}
                className="text-slate-400 hover:text-white text-2xl font-bold leading-none"
              >
                &times;
              </button>
            </div>

            <div className="flex-1 overflow-y-auto space-y-4 text-xs pr-1">
              <div className="p-4 rounded-2xl bg-[#0b0f19] border border-[#222f49] space-y-2">
                <h4 className="font-bold text-emerald-400 flex items-center gap-1.5">
                  <span>✅</span> 1. Giá Tour Bao Gồm
                </h4>
                <div className="text-slate-300 whitespace-pre-line leading-relaxed bg-[#151c2e] p-3 rounded-xl">
                  {Array.isArray(selectedProduct.inclusions) ? selectedProduct.inclusions.join('\n') : (selectedProduct.inclusions || 'Đang cập nhật')}
                </div>
              </div>

              <div className="p-4 rounded-2xl bg-[#0b0f19] border border-[#222f49] space-y-2">
                <h4 className="font-bold text-rose-400 flex items-center gap-1.5">
                  <span>❌</span> 2. Giá Tour Không Bao Gồm
                </h4>
                <div className="text-slate-300 whitespace-pre-line leading-relaxed bg-[#151c2e] p-3 rounded-xl">
                  {Array.isArray(selectedProduct.exclusions) ? selectedProduct.exclusions.join('\n') : (selectedProduct.exclusions || 'Đang cập nhật')}
                </div>
              </div>

              <div className="p-4 rounded-2xl bg-[#0b0f19] border border-[#222f49] space-y-2">
                <h4 className="font-bold text-amber-400 flex items-center gap-1.5">
                  <span>👶</span> 3. Chính Sách Giá Tour Trẻ Em
                </h4>
                <div className="text-slate-300 whitespace-pre-line leading-relaxed bg-[#151c2e] p-3 rounded-xl">
                  {selectedProduct.child_policy || 'Đang cập nhật'}
                </div>
              </div>

              <div className="p-4 rounded-2xl bg-[#0b0f19] border border-[#222f49] space-y-2">
                <h4 className="font-bold text-blue-400 flex items-center gap-1.5">
                  <span>💳</span> 4. Quy Trình Thanh Toán & Cọc
                </h4>
                <div className="text-slate-300 whitespace-pre-line leading-relaxed bg-[#151c2e] p-3 rounded-xl">
                  {selectedProduct.payment_policy || 'Đang cập nhật'}
                </div>
              </div>

              <div className="p-4 rounded-2xl bg-[#0b0f19] border border-[#222f49] space-y-2">
                <h4 className="font-bold text-orange-400 flex items-center gap-1.5">
                  <span>🛡️</span> 5. Điều Kiện Hủy Tour & Cam Kết Visa
                </h4>
                <div className="text-slate-300 whitespace-pre-line leading-relaxed bg-[#151c2e] p-3 rounded-xl">
                  {selectedProduct.cancellation_policy || 'Đang cập nhật'}
                </div>
              </div>
            </div>

            <div className="border-t border-[#222f49] pt-4 flex justify-end">
              <button
                onClick={() => setShowPoliciesModal(false)}
                className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-white font-semibold text-xs transition"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── MODAL: TRAVEL REVIEWS ── */}
      {showReviewsModal && selectedProduct && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-[#151c2e] border border-[#222f49] max-w-4xl w-full max-h-[92vh] rounded-3xl p-6 sm:p-8 space-y-5 shadow-2xl flex flex-col overflow-hidden">
            <div className="flex items-start justify-between border-b border-[#222f49] pb-4">
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <span className="text-[10px] uppercase font-mono font-bold px-2 py-0.5 rounded bg-amber-500/20 text-amber-300 border border-amber-500/30">
                    {selectedProduct.code}
                  </span>
                  <span className="text-[10px] uppercase font-bold px-2 py-0.5 rounded bg-blue-500/20 text-blue-300 border border-blue-500/30">
                    Social Proof
                  </span>
                </div>
                <h3 className="text-lg sm:text-xl font-bold text-white">⭐ Quản Lý Đánh Giá Khách Hàng: {selectedProduct.title}</h3>
                <p className="text-xs text-slate-400">Các review ở trạng thái "Hiển thị" sẽ tự động xuất hiện trên Landing Page để tăng tỷ lệ chốt cọc.</p>
              </div>
              <button
                onClick={() => setShowReviewsModal(false)}
                className="text-slate-400 hover:text-white text-2xl font-bold leading-none"
              >
                &times;
              </button>
            </div>

            <div className="flex items-center justify-between gap-3">
              <div className="text-xs text-slate-300">
                Danh sách đánh giá: <span className="font-bold text-amber-400">{reviews.length}</span> đánh giá
              </div>
              <button
                onClick={() => setShowAddReviewForm(!showAddReviewForm)}
                className="px-3.5 py-1.5 rounded-xl bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 border border-amber-500/40 text-xs font-bold transition flex items-center gap-1.5"
              >
                <span>{showAddReviewForm ? '✕ Đóng Form Thêm' : '➕ Thêm Đánh Giá Mới'}</span>
              </button>
            </div>

            {showAddReviewForm && (
              <form onSubmit={handleAddReview} className="p-4 rounded-2xl bg-[#0e1422] border border-amber-500/30 space-y-3 text-xs">
                <div className="font-bold text-amber-300 text-xs uppercase tracking-wider flex items-center gap-1.5">
                  <span>✍️</span> Nhập Thông Tin Đánh Giá Của Khách Hàng
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                  <div>
                    <label className="block font-semibold text-slate-300 mb-1">Tên khách hàng *</label>
                    <input
                      type="text"
                      required
                      value={reviewName}
                      onChange={e => setReviewName(e.target.value)}
                      placeholder="Ví dụ: Chị Mai Phương (TP.HCM)"
                      className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-amber-500 focus:outline-none"
                    />
                  </div>
                  <div>
                    <label className="block font-semibold text-slate-300 mb-1">Thời điểm khởi hành</label>
                    <input
                      type="text"
                      value={reviewDate}
                      onChange={e => setReviewDate(e.target.value)}
                      placeholder="Ví dụ: 10/2025 hoặc Tháng 11"
                      className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-amber-500 focus:outline-none"
                    />
                  </div>
                  <div>
                    <label className="block font-semibold text-slate-300 mb-1">Số sao đánh giá *</label>
                    <select
                      value={reviewRating}
                      onChange={e => setReviewRating(Number(e.target.value))}
                      className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-amber-300 font-bold focus:border-amber-500 focus:outline-none"
                    >
                      <option value={5}>★★★★★ (5 Sao - Xuất sắc)</option>
                      <option value={4}>★★★★☆ (4 Sao - Rất tốt)</option>
                      <option value={3}>★★★☆☆ (3 Sao - Hài lòng)</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label className="block font-semibold text-slate-300 mb-1">Nội dung cảm nhận / review thực tế *</label>
                  <textarea
                    rows={3}
                    required
                    value={reviewContent}
                    onChange={e => setReviewContent(e.target.value)}
                    placeholder="Cảm nhận thực tế của khách về chuyến đi..."
                    className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-amber-500 focus:outline-none"
                  />
                </div>
                <div className="flex items-center justify-between pt-1">
                  <label className="flex items-center gap-2 cursor-pointer text-slate-300">
                    <input
                      type="checkbox"
                      checked={reviewVisible}
                      onChange={e => setReviewVisible(e.target.checked)}
                      className="rounded bg-[#0b0f19] border-[#222f49] text-amber-500"
                    />
                    <span>Hiển thị ngay trên Landing Page</span>
                  </label>
                  <button
                    type="submit"
                    className="px-4 py-1.5 rounded-xl bg-amber-500 text-slate-900 font-bold hover:brightness-110 transition"
                  >
                    Lưu Đánh Giá
                  </button>
                </div>
              </form>
            )}

            <div className="flex-1 overflow-y-auto space-y-3 pr-1 text-xs">
              {reviews.length === 0 ? (
                <div className="p-8 text-center text-slate-500 rounded-2xl bg-[#0b0f19] border border-[#222f49]">
                  Chưa có đánh giá nào cho tour này.<br />
                  <span className="text-xs text-amber-400/80">Hãy bấm "+ Thêm Đánh Giá Mới" để tạo đánh giá thực tế.</span>
                </div>
              ) : (
                reviews.map(r => (
                  <div key={r.id} className="p-4 rounded-2xl bg-[#0b0f19] border border-[#222f49] space-y-2">
                    <div className="flex flex-wrap items-center justify-between gap-2 border-b border-[#222f49]/60 pb-2">
                      <div className="flex items-center gap-2.5">
                        <div className="w-8 h-8 rounded-full bg-amber-500/20 text-amber-400 font-bold flex items-center justify-center text-xs">
                          {r.reviewer_name ? r.reviewer_name.charAt(0) : 'K'}
                        </div>
                        <div>
                          <div className="font-bold text-white text-xs">{r.reviewer_name}</div>
                          <div className="text-[10px] text-slate-400">Khởi hành: {r.travel_date || 'Gần đây'}</div>
                        </div>
                      </div>
                      <div className="text-amber-400 text-xs font-bold tracking-wider">
                        {'★'.repeat(r.rating || 5)}{'☆'.repeat(5 - (r.rating || 5))}
                      </div>
                    </div>
                    <p className="text-slate-300 text-xs leading-relaxed whitespace-pre-line">{r.content}</p>
                  </div>
                ))
              )}
            </div>

            <div className="border-t border-[#222f49] pt-4 flex justify-end">
              <button
                onClick={() => setShowReviewsModal(false)}
                className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-white font-semibold text-xs transition"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── MODAL: TRAVEL FAQS ── */}
      {showFaqsModal && selectedProduct && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-[#151c2e] border border-[#222f49] max-w-4xl w-full max-h-[92vh] rounded-3xl p-6 sm:p-8 space-y-5 shadow-2xl flex flex-col overflow-hidden">
            <div className="flex items-start justify-between border-b border-[#222f49] pb-4">
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <span className="text-[10px] uppercase font-mono font-bold px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                    {selectedProduct.code}
                  </span>
                  <span className="text-[10px] uppercase font-bold px-2 py-0.5 rounded bg-purple-500/20 text-purple-300 border border-purple-500/30">
                    Tour FAQ
                  </span>
                </div>
                <h3 className="text-lg sm:text-xl font-bold text-white">❓ Quản Lý Câu Hỏi Thường Gặp: {selectedProduct.title}</h3>
                <p className="text-xs text-slate-400">Các câu hỏi giải đáp thắc mắc về visa, ăn ở, ghép phòng để khách đọc & AI Agent tra cứu.</p>
              </div>
              <button
                onClick={() => setShowFaqsModal(false)}
                className="text-slate-400 hover:text-white text-2xl font-bold leading-none"
              >
                &times;
              </button>
            </div>

            <div className="flex items-center justify-between gap-3">
              <div className="text-xs text-slate-300">
                Danh sách câu hỏi: <span className="font-bold text-emerald-400">{faqs.length}</span> câu hỏi
              </div>
              <button
                onClick={() => setShowAddFaqForm(!showAddFaqForm)}
                className="px-3.5 py-1.5 rounded-xl bg-emerald-500/20 hover:bg-emerald-500/30 text-emerald-300 border border-emerald-500/40 text-xs font-bold transition flex items-center gap-1.5"
              >
                <span>{showAddFaqForm ? '✕ Đóng Form Thêm' : '➕ Thêm Câu Hỏi Mới'}</span>
              </button>
            </div>

            {showAddFaqForm && (
              <form onSubmit={handleAddFaq} className="p-4 rounded-2xl bg-[#0e1422] border border-emerald-500/30 space-y-3 text-xs">
                <div className="font-bold text-emerald-300 text-xs uppercase tracking-wider flex items-center gap-1.5">
                  <span>💡</span> Nhập Câu Hỏi & Trả Lời Mới
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
                  <div className="sm:col-span-3">
                    <label className="block font-semibold text-slate-300 mb-1">Câu hỏi (Question) *</label>
                    <input
                      type="text"
                      required
                      value={faqQuestion}
                      onChange={e => setFaqQuestion(e.target.value)}
                      placeholder="Ví dụ: Thủ tục xin visa Nhật Bản cần những giấy tờ gì?"
                      className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-emerald-500 focus:outline-none"
                    />
                  </div>
                  <div>
                    <label className="block font-semibold text-slate-300 mb-1">Thứ tự (sort)</label>
                    <input
                      type="number"
                      value={faqSort}
                      onChange={e => setFaqSort(Number(e.target.value))}
                      className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white font-mono focus:border-emerald-500 focus:outline-none"
                    />
                  </div>
                </div>
                <div>
                  <label className="block font-semibold text-slate-300 mb-1">Câu trả lời chi tiết *</label>
                  <textarea
                    rows={3}
                    required
                    value={faqAnswer}
                    onChange={e => setFaqAnswer(e.target.value)}
                    placeholder="Nội dung giải đáp rõ ràng, chi tiết, minh bạch..."
                    className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-emerald-500 focus:outline-none"
                  />
                </div>
                <div className="flex items-center justify-between pt-1">
                  <label className="flex items-center gap-2 cursor-pointer text-slate-300">
                    <input
                      type="checkbox"
                      checked={faqVisible}
                      onChange={e => setFaqVisible(e.target.checked)}
                      className="rounded bg-[#0b0f19] border-[#222f49] text-emerald-500"
                    />
                    <span>Hiển thị ngay trên Landing Page</span>
                  </label>
                  <button
                    type="submit"
                    className="px-4 py-1.5 rounded-xl bg-emerald-500 text-slate-900 font-bold hover:brightness-110 transition"
                  >
                    Lưu Câu Hỏi
                  </button>
                </div>
              </form>
            )}

            <div className="flex-1 overflow-y-auto space-y-3 pr-1 text-xs">
              {faqs.length === 0 ? (
                <div className="p-8 text-center text-slate-500 rounded-2xl bg-[#0b0f19] border border-[#222f49]">
                  Chưa có câu hỏi FAQ nào cho tour này.<br />
                  <span className="text-xs text-emerald-400/80">Bấm "+ Thêm Câu Hỏi Mới" để tạo câu hỏi thường gặp.</span>
                </div>
              ) : (
                faqs.map(f => (
                  <div key={f.id} className="p-4 rounded-2xl bg-[#0b0f19] border border-[#222f49] space-y-2">
                    <div className="flex items-center justify-between border-b border-[#222f49]/60 pb-2">
                      <h4 className="font-bold text-white text-xs sm:text-sm">❓ {f.question}</h4>
                      <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-800 text-emerald-300">
                        #{f.sort_order}
                      </span>
                    </div>
                    <p className="text-slate-300 text-xs leading-relaxed whitespace-pre-line bg-[#151c2e] p-3 rounded-xl">
                      {f.answer}
                    </p>
                  </div>
                ))
              )}
            </div>

            <div className="border-t border-[#222f49] pt-4 flex justify-end">
              <button
                onClick={() => setShowFaqsModal(false)}
                className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-white font-semibold text-xs transition"
              >
                Đóng
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── MODAL: MANUAL ENTRY TOUR (CREATE & EDIT) ── */}
      {showEntryModal && (
        <div className="fixed inset-0 z-50 bg-black/85 backdrop-blur-sm flex items-center justify-center p-3 sm:p-4">
          <div className="bg-[#151c2e] border border-[#222f49] max-w-5xl w-full max-h-[95vh] rounded-3xl p-5 sm:p-7 space-y-5 shadow-2xl flex flex-col overflow-hidden">
            {/* Header */}
            <div className="flex items-start justify-between border-b border-[#222f49] pb-3">
              <div className="space-y-0.5">
                <div className="flex items-center gap-2">
                  <span className="text-[10px] uppercase font-mono font-bold px-2 py-0.5 rounded bg-blue-500/20 text-blue-300 border border-blue-500/30">
                    Manual Entry
                  </span>
                  <span className="text-[10px] uppercase font-bold px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                    Core Product Base
                  </span>
                </div>
                <h3 className="text-lg font-bold text-white">
                  {editingId ? `✏️ Cập Nhật Tour: ${entryCode}` : '➕ Thêm Tour Sản Phẩm Mới (Thủ Công)'}
                </h3>
                <p className="text-xs text-slate-400">
                  Nhập đầy đủ thông tin chuẩn hóa, lịch trình chi tiết, lịch khởi hành theo ngày & chính sách để AI Agent
                  tra cứu.
                </p>
              </div>
              <button
                onClick={() => setShowEntryModal(false)}
                className="text-slate-400 hover:text-white text-2xl font-bold leading-none"
              >
                &times;
              </button>
            </div>

            {/* Form Body with Scroll */}
            <form onSubmit={handleSubmitTourForm} className="flex-1 flex flex-col space-y-5 overflow-y-auto pr-2 text-xs">
              {/* 1. Thông Tin Cơ Bản */}
              <div className="p-4 rounded-2xl bg-[#0e1422] border border-[#222f49] space-y-3">
                <div className="text-[11px] font-bold text-blue-400 uppercase tracking-wider flex items-center gap-1.5">
                  <span>🗺️</span> 1. Thông Tin Cơ Bản & Định Tuyến Cho AI
                </div>

                <div className="space-y-1">
                  <label className="block font-semibold text-slate-300">
                    Tên Tour Sản Phẩm <span className="text-rose-400">*</span>
                  </label>
                  <input
                    type="text"
                    required
                    value={entryTitle}
                    onChange={e => setEntryTitle(e.target.value)}
                    placeholder="Ví dụ: Cung Đường Vàng Nhật Bản 6N5Đ: Osaka - Kyoto - Phú Sĩ - Tokyo"
                    className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none"
                  />
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">Mã Tour (Code)</label>
                    <input
                      type="text"
                      value={entryCode}
                      onChange={e => setEntryCode(e.target.value)}
                      placeholder="TOUR-JP-6N5D"
                      className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white font-mono focus:border-blue-500 focus:outline-none"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">
                      Quốc Gia <span className="text-rose-400">*</span>
                    </label>
                    <input
                      type="text"
                      required
                      value={entryCountry}
                      onChange={e => setEntryCountry(e.target.value)}
                      placeholder="Nhật Bản, Trung Quốc, Singapore..."
                      className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">Nơi Khởi Hành</label>
                    <select
                      value={entryDepartureCity}
                      onChange={e => setEntryDepartureCity(e.target.value)}
                      className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none"
                    >
                      <option value="TP.HCM">TP.HCM (SGN)</option>
                      <option value="Hà Nội">Hà Nội (HAN)</option>
                      <option value="Đà Nẵng">Đà Nẵng (DAD)</option>
                      <option value="Cần Thơ">Cần Thơ (VCA)</option>
                    </select>
                  </div>
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">Thời Lượng (Ngày / Đêm)</label>
                    <div className="flex gap-2">
                      <input
                        type="number"
                        min={1}
                        max={60}
                        value={entryDays}
                        onChange={e => setEntryDays(Number(e.target.value))}
                        className="w-1/2 px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white font-mono focus:border-blue-500 focus:outline-none"
                        title="Số Ngày"
                      />
                      <input
                        type="number"
                        min={0}
                        max={60}
                        value={entryNights}
                        onChange={e => setEntryNights(Number(e.target.value))}
                        className="w-1/2 px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white font-mono focus:border-blue-500 focus:outline-none"
                        title="Số Đêm"
                      />
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">
                      Danh Sách Điểm Đến (cách nhau bằng dấu phẩy) *
                    </label>
                    <input
                      type="text"
                      required
                      value={entryDestinations}
                      onChange={e => setEntryDestinations(e.target.value)}
                      placeholder="Osaka, Kyoto, Gifu, Takayama, Shirakawago, Núi Phú Sĩ, Tokyo"
                      className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">Lộ Trình Tóm Tắt (Route)</label>
                    <input
                      type="text"
                      value={entryRoute}
                      onChange={e => setEntryRoute(e.target.value)}
                      placeholder="TP.HCM – Osaka – Kyoto – Tokyo – TP.HCM"
                      className="w-full px-3 py-2 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none"
                    />
                  </div>
                </div>

                {/* Tags & Status */}
                <div className="grid grid-cols-1 sm:grid-cols-12 gap-3 pt-1 border-t border-[#222f49]/60 items-center">
                  <div className="sm:col-span-8 space-y-1.5">
                    <label className="block font-semibold text-slate-300">Thẻ Phân Loại Nhu Cầu (Tags):</label>
                    <div className="flex flex-wrap gap-3">
                      {['gia-dinh', 'le-tet', 'cao-cap', 'tiet-kiem', 'kham-pha', 'nghi-duong'].map(tag => (
                        <label key={tag} className="inline-flex items-center gap-1.5 cursor-pointer text-slate-300 hover:text-white">
                          <input
                            type="checkbox"
                            checked={entryTags.includes(tag)}
                            onChange={e => {
                              if (e.target.checked) setEntryTags([...entryTags, tag]);
                              else setEntryTags(entryTags.filter(t => t !== tag));
                            }}
                            className="rounded bg-[#0b0f19] border-slate-600 text-blue-500"
                          />
                          <span>{tag}</span>
                        </label>
                      ))}
                    </div>
                  </div>

                  <div className="sm:col-span-2 space-y-1">
                    <label className="block font-semibold text-slate-300">Trạng Thái</label>
                    <select
                      value={entryStatus}
                      onChange={e => setEntryStatus(e.target.value as 'published' | 'draft')}
                      className="w-full px-3 py-1.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none"
                    >
                      <option value="published">Published</option>
                      <option value="draft">Draft</option>
                    </select>
                  </div>

                  <div className="sm:col-span-2 pt-4">
                    <label className="inline-flex items-center gap-2 cursor-pointer text-slate-200 hover:text-white">
                      <input
                        type="checkbox"
                        checked={entryIsFeatured}
                        onChange={e => setEntryIsFeatured(e.target.checked)}
                        className="rounded bg-[#0b0f19] border-slate-600 text-blue-500"
                      />
                      <span className="font-semibold">⭐ Tour Nổi Bật</span>
                    </label>
                  </div>
                </div>
              </div>

              {/* 2. Lịch Khởi Hành & Giá Theo Ngày */}
              <div className="p-4 rounded-2xl bg-[#0e1422] border border-[#222f49] space-y-3">
                <div className="flex items-center justify-between">
                  <div className="text-[11px] font-bold text-amber-400 uppercase tracking-wider flex items-center gap-1.5">
                    <span>📅</span> 2. Lịch Khởi Hành & Bảng Giá Từng Đợt (Departures)
                  </div>
                  <button
                    type="button"
                    onClick={() =>
                      setEntryDepartures([
                        ...entryDepartures,
                        { departure_date: '', price_adult: 35990000, status: 'open' }
                      ])
                    }
                    className="px-3 py-1.5 rounded-xl bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 border border-amber-500/30 font-semibold text-xs transition flex items-center gap-1"
                  >
                    <span>+ Thêm Ngày Khởi Hành</span>
                  </button>
                </div>

                <div className="space-y-2.5">
                  {entryDepartures.map((dep, idx) => (
                    <div key={idx} className="p-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] flex flex-wrap items-center gap-2 text-xs">
                      <div className="w-36">
                        <label className="block text-[10px] text-slate-400 font-semibold mb-0.5">Ngày Khởi Hành *</label>
                        <input
                          type="date"
                          value={dep.departure_date}
                          onChange={e => {
                            const newDeps = [...entryDepartures];
                            newDeps[idx].departure_date = e.target.value;
                            setEntryDepartures(newDeps);
                          }}
                          className="w-full px-2 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-white font-mono text-xs focus:border-amber-500 focus:outline-none"
                          required
                        />
                      </div>
                      <div className="w-32">
                        <label className="block text-[10px] text-slate-400 font-semibold mb-0.5">Giá Người Lớn (đ) *</label>
                        <input
                          type="number"
                          value={dep.price_adult}
                          onChange={e => {
                            const newDeps = [...entryDepartures];
                            newDeps[idx].price_adult = Number(e.target.value);
                            setEntryDepartures(newDeps);
                          }}
                          className="w-full px-2 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-amber-300 font-mono font-bold text-xs focus:border-amber-500 focus:outline-none"
                          required
                        />
                      </div>
                      <div className="w-24">
                        <label className="block text-[10px] text-slate-400 font-semibold mb-0.5">Trạng Thái</label>
                        <select
                          value={dep.status}
                          onChange={e => {
                            const newDeps = [...entryDepartures];
                            newDeps[idx].status = e.target.value as 'open' | 'guaranteed' | 'full' | 'closed';
                            setEntryDepartures(newDeps);
                          }}
                          className="w-full px-1.5 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-slate-200 text-xs focus:border-amber-500 focus:outline-none"
                        >
                          <option value="open">Open</option>
                          <option value="guaranteed">Chắc chắn đi</option>
                          <option value="full">Full chỗ</option>
                          <option value="closed">Đóng</option>
                        </select>
                      </div>
                      <div className="flex-1 min-w-[140px]">
                        <label className="block text-[10px] text-slate-400 font-semibold mb-0.5">Ghi Chú Đợt Bay</label>
                        <input
                          type="text"
                          value={dep.notes || ''}
                          onChange={e => {
                            const newDeps = [...entryDepartures];
                            newDeps[idx].notes = e.target.value;
                            setEntryDepartures(newDeps);
                          }}
                          placeholder="VD: Cao điểm lá vàng, tặng sim..."
                          className="w-full px-2 py-1.5 rounded-lg bg-[#151c2e] border border-[#222f49] text-slate-300 text-xs focus:border-amber-500 focus:outline-none"
                        />
                      </div>
                      <div className="pt-4">
                        <button
                          type="button"
                          onClick={() => setEntryDepartures(entryDepartures.filter((_, i) => i !== idx))}
                          className="p-1.5 rounded-lg text-rose-400 hover:bg-rose-500/20 transition font-bold"
                          title="Xóa đợt khởi hành"
                        >
                          ✕
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* 3. Lịch Trình Chi Tiết Từng Ngày */}
              <div className="p-4 rounded-2xl bg-[#0e1422] border border-[#222f49] space-y-3">
                <div className="flex items-center justify-between">
                  <div className="text-[11px] font-bold text-emerald-400 uppercase tracking-wider flex items-center gap-1.5">
                    <span>📝</span> 3. Lịch Trình Chi Tiết Từng Ngày (Itinerary Builder)
                  </div>
                  <button
                    type="button"
                    onClick={() =>
                      setEntryItinerary([
                        ...entryItinerary,
                        { day: entryItinerary.length + 1, title: `NGÀY ${entryItinerary.length + 1}: `, content: '', images: [] }
                      ])
                    }
                    className="px-3 py-1.5 rounded-xl bg-emerald-500/20 hover:bg-emerald-500/30 text-emerald-300 border border-emerald-500/30 font-semibold text-xs transition flex items-center gap-1"
                  >
                    <span>+ Thêm Ngày Lịch Trình</span>
                  </button>
                </div>

                <div className="space-y-3">
                  {entryItinerary.map((day, idx) => (
                    <div key={idx} className="p-3.5 rounded-2xl bg-[#0b0f19] border border-[#222f49] space-y-2.5 text-xs">
                      <div className="flex items-center justify-between border-b border-[#222f49]/60 pb-2">
                        <div className="flex items-center gap-2 flex-1 mr-2">
                          <span className="w-6 h-6 rounded-full bg-emerald-500 text-white flex items-center justify-center font-bold text-xs">
                            {idx + 1}
                          </span>
                          <input
                            type="text"
                            value={day.title}
                            onChange={e => {
                              const newItin = [...entryItinerary];
                              newItin[idx].title = e.target.value;
                              setEntryItinerary(newItin);
                            }}
                            placeholder={`NGÀY ${idx + 1}: TIÊU ĐỀ HÀNH TRÌNH`}
                            className="flex-1 px-2.5 py-1.5 rounded-xl bg-[#151c2e] border border-[#222f49] text-white font-bold text-xs focus:border-emerald-500 focus:outline-none"
                            required
                          />
                        </div>
                        <button
                          type="button"
                          onClick={() => setEntryItinerary(entryItinerary.filter((_, i) => i !== idx))}
                          className="px-2 py-1 rounded-lg text-rose-400 hover:bg-rose-500/20 text-xs font-semibold transition"
                        >
                          ✕ Xóa Ngày
                        </button>
                      </div>
                      <div>
                        <textarea
                          rows={3}
                          value={day.content}
                          onChange={e => {
                            const newItin = [...entryItinerary];
                            newItin[idx].content = e.target.value;
                            setEntryItinerary(newItin);
                          }}
                          placeholder="Sáng: Đoàn dùng bữa sáng...\nTrưa: Tham quan...\nTối: ..."
                          className="w-full p-2.5 rounded-xl bg-[#151c2e] border border-[#222f49] text-slate-200 focus:border-emerald-500 focus:outline-none text-[11px] leading-relaxed"
                          required
                        />
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* 4. 4 Bộ Chính Sách */}
              <div className="p-4 rounded-2xl bg-[#0e1422] border border-[#222f49] space-y-3">
                <div className="text-[11px] font-bold text-purple-400 uppercase tracking-wider flex items-center gap-1.5">
                  <span>📜</span> 4. Bộ Chính Sách, Hồ Sơ Visa & Lưu Ý Quan Trọng
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">Giá Tour Bao Gồm (mỗi dòng 1 mục)</label>
                    <textarea
                      rows={3}
                      value={entryInclusions}
                      onChange={e => setEntryInclusions(e.target.value)}
                      className="w-full p-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none text-[11px]"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">Giá Tour Không Bao Gồm (mỗi dòng 1 mục)</label>
                    <textarea
                      rows={3}
                      value={entryExclusions}
                      onChange={e => setEntryExclusions(e.target.value)}
                      className="w-full p-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none text-[11px]"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">Chính Sách Trẻ Em</label>
                    <textarea
                      rows={2}
                      value={entryChildPolicy}
                      onChange={e => setEntryChildPolicy(e.target.value)}
                      className="w-full p-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none text-[11px]"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">Quy Trình Thanh Toán & Cọc</label>
                    <textarea
                      rows={2}
                      value={entryPaymentPolicy}
                      onChange={e => setEntryPaymentPolicy(e.target.value)}
                      className="w-full p-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none text-[11px]"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">Điều Kiện Hủy Tour & Cam Kết Visa</label>
                    <textarea
                      rows={2}
                      value={entryCancellationPolicy}
                      onChange={e => setEntryCancellationPolicy(e.target.value)}
                      className="w-full p-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none text-[11px]"
                    />
                  </div>
                  <div className="space-y-1">
                    <label className="block font-semibold text-slate-300">Hồ Sơ Xin Visa</label>
                    <textarea
                      rows={2}
                      value={entryVisaPolicy}
                      onChange={e => setEntryVisaPolicy(e.target.value)}
                      className="w-full p-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white focus:border-blue-500 focus:outline-none text-[11px]"
                    />
                  </div>
                </div>
              </div>

              {/* Footer Actions */}
              <div className="pt-3 flex items-center justify-end gap-3 sticky bottom-0 bg-[#151c2e] py-2 border-t border-[#222f49]">
                <button
                  type="button"
                  onClick={() => setShowEntryModal(false)}
                  className="px-5 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-white font-semibold text-xs transition"
                >
                  Hủy Bỏ
                </button>
                <button
                  type="submit"
                  className="px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white font-bold text-xs shadow-lg shadow-blue-500/20 transition flex items-center gap-2"
                >
                  <span>💾 Lưu Tour Vào Hệ Thống</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── AI COMMANDER FLOATING CHAT DRAWER ── */}
      <div className="fixed bottom-6 right-6 z-40">
        <button
          onClick={() => setShowAgentDrawer(!showAgentDrawer)}
          className="w-14 h-14 rounded-2xl bg-gradient-to-tr from-emerald-500 to-teal-500 text-white shadow-2xl shadow-emerald-500/40 flex items-center justify-center hover:scale-110 transition-all font-bold text-2xl"
          title="Mở AI Agent Commander"
        >
          🤖
        </button>

        {showAgentDrawer && (
          <div className="absolute bottom-16 right-0 w-[380px] sm:w-[480px] h-[600px] rounded-3xl bg-[#111726] border border-[#222f49] shadow-2xl flex flex-col overflow-hidden">
            {/* Header */}
            <div className="px-4 py-3 bg-[#0e1422] border-b border-[#222f49] flex items-center justify-between">
              <div className="flex items-center space-x-2.5">
                <div className="w-9 h-9 rounded-xl bg-blue-500/20 text-blue-400 border border-blue-500/40 flex items-center justify-center text-lg">
                  🏖️
                </div>
                <div>
                  <div className="text-xs font-bold text-white flex items-center gap-1.5">
                    AI Tư Vấn Du Lịch 24/7
                    <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
                  </div>
                  <div className="text-[10px] text-emerald-400 font-mono flex items-center gap-1">
                    <span>⚡ DeepSeek V3 • 5 Travel Tools</span>
                  </div>
                </div>
              </div>
              <button
                onClick={() => setShowAgentDrawer(false)}
                className="text-slate-400 hover:text-white text-2xl font-bold leading-none"
              >
                &times;
              </button>
            </div>

            {/* Mode Switcher */}
            <div className="px-3 py-1.5 bg-[#090d19] border-b border-[#222f49] flex gap-1.5 text-[11px]">
              <button
                type="button"
                onClick={() => setAgentMode('consultant')}
                className={`flex-1 py-1.5 px-2 rounded-xl font-bold transition flex items-center justify-center gap-1.5 ${
                  agentMode === 'consultant'
                    ? 'bg-blue-600/30 text-blue-300 border border-blue-500/40'
                    : 'bg-[#151c2e]/40 text-slate-400 hover:text-slate-200 border border-[#222f49]'
                }`}
              >
                <span>🏖️ Tư Vấn Tour</span>
              </button>
              <button
                type="button"
                onClick={() => setAgentMode('commander')}
                className={`flex-1 py-1.5 px-2 rounded-xl font-bold transition flex items-center justify-center gap-1.5 ${
                  agentMode === 'commander'
                    ? 'bg-emerald-600/30 text-emerald-300 border border-emerald-500/40'
                    : 'bg-[#151c2e]/40 text-slate-400 hover:text-slate-200 border border-[#222f49]'
                }`}
              >
                <span>⚡ Tyler Commander</span>
              </button>
            </div>

            {/* Message Stream */}
            <div className="flex-1 p-4 overflow-y-auto space-y-3 text-xs">
              {agentMessages.map((msg, i) => (
                <div
                  key={i}
                  className={`flex items-start space-x-2 ${msg.sender === 'user' ? 'justify-end' : ''}`}
                >
                  {msg.sender === 'agent' && (
                    <div className="w-7 h-7 rounded-lg bg-blue-500/20 text-blue-400 flex items-center justify-center text-xs shrink-0">
                      🏖️
                    </div>
                  )}
                  <div
                    className={`p-3.5 rounded-2xl ${
                      msg.sender === 'user'
                        ? 'bg-blue-600 text-white rounded-tr-none max-w-[85%]'
                        : 'bg-[#151c2e] text-slate-200 rounded-tl-none border border-[#222f49] max-w-[90%]'
                    } leading-relaxed whitespace-pre-line`}
                  >
                    {msg.text}
                  </div>
                </div>
              ))}
              {isAgentTyping && (
                <div className="flex items-start space-x-2">
                  <div className="w-7 h-7 rounded-lg bg-blue-500/20 text-blue-400 flex items-center justify-center text-xs shrink-0">
                    🏖️
                  </div>
                  <div className="p-3 rounded-2xl rounded-tl-none bg-[#151c2e] text-slate-400 text-[11px] flex items-center gap-1 border border-[#222f49]">
                    <span>Đang suy nghĩ & tra cứu</span>
                    <span className="animate-bounce">.</span>
                    <span className="animate-bounce delay-100">.</span>
                    <span className="animate-bounce delay-200">.</span>
                  </div>
                </div>
              )}
            </div>

            {/* Quick Pills */}
            <div className="px-3 py-2 bg-[#0a0f1d] border-t border-[#222f49] flex gap-1.5 overflow-x-auto text-[11px]">
              <button
                type="button"
                onClick={() => setAgentInput('Hiện tại có bao nhiêu tour?')}
                className="px-2.5 py-1 rounded-full bg-[#151c2e] hover:bg-slate-800 text-slate-300 border border-[#222f49] whitespace-nowrap"
              >
                Có bao nhiêu tour?
              </button>
              <button
                type="button"
                onClick={() => setAgentInput('Tour Nhật Bản có những ngày nào đi?')}
                className="px-2.5 py-1 rounded-full bg-[#151c2e] hover:bg-slate-800 text-slate-300 border border-[#222f49] whitespace-nowrap"
              >
                Lịch bay tour Nhật
              </button>
              <button
                type="button"
                onClick={() => setAgentInput('Lịch trình ngày 2 tour Nhật Bản đi đâu?')}
                className="px-2.5 py-1 rounded-full bg-[#151c2e] hover:bg-slate-800 text-slate-300 border border-[#222f49] whitespace-nowrap"
              >
                Lịch trình Ngày 2
              </button>
            </div>

            {/* Input Bar */}
            <form onSubmit={handleAgentChat} className="p-3 bg-[#0e1422] border-t border-[#222f49] flex items-center gap-2">
              <input
                type="text"
                value={agentInput}
                onChange={e => setAgentInput(e.target.value)}
                placeholder="Hỏi AI về tour (vd: có bao nhiêu tour? lịch bay tháng 10?)..."
                className="flex-1 px-3.5 py-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white text-xs focus:border-blue-500 focus:outline-none"
              />
              <button
                type="submit"
                className="px-4 py-2.5 rounded-xl bg-blue-500 hover:bg-blue-600 text-white text-xs font-bold transition shadow-lg shadow-blue-500/20"
              >
                Gửi
              </button>
            </form>
          </div>
        )}
      </div>
    </div>
  );
};
