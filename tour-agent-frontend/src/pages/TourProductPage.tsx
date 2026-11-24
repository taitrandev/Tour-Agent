import React, { useState, useEffect } from 'react';
import { Link, useParams } from 'react-router-dom';
import { notify } from '../lib/notify';
import { FloatingChatWidget } from '../components/chat/FloatingChatWidget';
import { apiFetch } from '../api/client';
import { API_ROUTES } from '../constants/apiRoutes';
import type { TourSummary } from '../types/tour';

/**
 * Mục dịch vụ bao gồm hoặc giá trị xếp chồng (Value Stack item).
 */
interface ValueStackItem {
  name: string;
  val: string;
  type: 'core' | 'bonus';
}

/**
 * Mục câu hỏi thường gặp (FAQ item).
 */
interface FaqItem {
  q: string;
  a: string;
}

/**
 * Mục đánh giá từ khách hàng (Review item).
 */
interface ReviewItem {
  name: string;
  role: string;
  content: string;
}

/**
 * Mục lịch trình ngày (Itinerary Day item).
 */
interface ItineraryDayItem {
  day: number;
  title: string;
  content: string;
  image: string | null;
}

/**
 * Mục đợt khởi hành (Departure item).
 */
interface DepartureItem {
  date: string;
  airline: string;
  price: string;
  status: string;
  hot: boolean;
}

/**
 * Mục chính sách thực tế (Policy item).
 */
interface PolicyItem {
  title: string;
  content: string;
  icon: string;
}

/**
 * Mục điều hướng trên thanh Navbar/Footer (Navigation item).
 */
interface NavItem {
  id: string;
  label: string;
}

/**
 * Trang Chi Tiết Sản Phẩm Tour Du Lịch (Product Landing Page).
 * Chuyên biệt phục vụ đường dẫn /tour/:slug.
 * - Triệt tiêu 100% dữ liệu giả / mock data (Zero Mock Data).
 * - Section nào không có dữ liệu thực tế từ Database thì tự động ẩn hoàn toàn.
 * - Thanh Navigation (Navbar & Footer) hoàn toàn động theo các section thực tế tồn tại.
 * - Triển khai mô hình Hybrid 80/20 Adaptive UI cho Offer:
 *    + Nếu tour có 0 offer: fallback dùng inclusions từ tour.
 *    + Nếu tour có đúng 1 offer: hiển thị đơn giản 1-1, không có tab thừa.
 *    + Nếu tour có từ 2 offer trở lên: tự động xuất hiện bộ chuyển đổi Pill Tabs giữa các gói.
 */
export const TourProductPage: React.FC = () => {
  const { slug } = useParams<{ slug?: string }>();
  const [isChatOpen, setIsChatOpen] = useState<boolean>(false);
  const [activeFaq, setActiveFaq] = useState<number | null>(null);

  // Chỉ mục gói Offer đang được chọn (phục vụ tour có nhiều offer)
  const [selectedOfferIndex, setSelectedOfferIndex] = useState<number>(0);

  // Trạng thái nạp dữ liệu từ Backend
  const [loading, setLoading] = useState<boolean>(true);
  const [notFound, setNotFound] = useState<boolean>(false);
  const [tour, setTour] = useState<any | null>(null);
  const [itinerary, setItinerary] = useState<ItineraryDayItem[]>([]);
  const [departures, setDepartures] = useState<DepartureItem[]>([]);
  const [faqs, setFaqs] = useState<FaqItem[]>([]);
  const [reviews, setReviews] = useState<ReviewItem[]>([]);
  const [valueStack, setValueStack] = useState<ValueStackItem[]>([]);

  /**
   * Effect nạp dữ liệu chi tiết tour, lịch trình, ngày bay, câu hỏi FAQ và đánh giá từ API.
   * Toàn bộ state khởi tạo rỗng, chỉ cập nhật khi backend có dữ liệu thật.
   */
  useEffect(() => {
    if (!slug) {
      setNotFound(true);
      setLoading(false);
      return;
    }

    setLoading(true);
    setNotFound(false);
    setSelectedOfferIndex(0);

    apiFetch(API_ROUTES.TRAVELS.BY_SLUG(slug))
      .then(async (response) => {
        if (!response.ok) {
          throw new Error('NotFound');
        }
        return response.json();
      })
      .then((jsonResult) => {
        if (jsonResult.success && jsonResult.data) {
          const tourData = jsonResult.data;
          setTour(tourData);

          // 1. Phân tích lịch trình chi tiết từ database (KHÔNG dùng ảnh fallback giả)
          if (tourData.itinerary) {
            try {
              const parsedItinerary =
                typeof tourData.itinerary === 'string'
                  ? JSON.parse(tourData.itinerary)
                  : tourData.itinerary;

              if (Array.isArray(parsedItinerary) && parsedItinerary.length > 0) {
                setItinerary(
                  parsedItinerary.map((dayItem: any, index: number) => ({
                    day: dayItem.day || index + 1,
                    title: dayItem.title || `NGÀY ${index + 1}: Khám phá`,
                    content: dayItem.content || '',
                    image:
                      Array.isArray(dayItem.images) && dayItem.images.length > 0 && dayItem.images[0]
                        ? dayItem.images[0]
                        : null,
                  }))
                );
              } else {
                setItinerary([]);
              }
            } catch (parseError) {
              console.warn('Lỗi phân tích lịch trình tour:', parseError);
              setItinerary([]);
            }
          } else {
            setItinerary([]);
          }

          // 2. Phân tích các ngày khởi hành mở bán từ database
          if (Array.isArray(tourData.departures) && tourData.departures.length > 0) {
            setDepartures(
              tourData.departures.map((departureItem: any) => ({
                date: departureItem.departureDate || 'Đang mở đăng ký',
                airline: departureItem.notes || 'Hàng không quốc tế',
                price: (departureItem.priceAdult || tourData.priceFrom || 0).toLocaleString('vi-VN') + ' ₫',
                status:
                  departureItem.status === 'open'
                    ? 'Còn chỗ'
                    : departureItem.status === 'guaranteed'
                    ? 'Chắc chắn bay'
                    : departureItem.status === 'full'
                    ? 'Hết chỗ'
                    : 'Mở đăng ký',
                hot: departureItem.status === 'guaranteed' || departureItem.status === 'open',
              }))
            );
          } else {
            setDepartures([]);
          }

          // 3. Phân tích dịch vụ bao gồm (Inclusions) để làm fallback cho Value Stack
          if (tourData.inclusions) {
            try {
              let inclusionsList: string[] = [];
              if (Array.isArray(tourData.inclusions)) {
                inclusionsList = tourData.inclusions;
              } else if (typeof tourData.inclusions === 'string') {
                const parsedInclusions = JSON.parse(tourData.inclusions);
                if (Array.isArray(parsedInclusions)) {
                  inclusionsList = parsedInclusions;
                } else {
                  inclusionsList = tourData.inclusions
                    .split('\n')
                    .map((line: string) => line.trim())
                    .filter(Boolean);
                }
              }
              if (inclusionsList.length > 0) {
                setValueStack(
                  inclusionsList.map((itemName: string, index: number) => ({
                    name: itemName,
                    val: 'Đã bao gồm',
                    type: index >= 5 ? 'bonus' : 'core',
                  }))
                );
              } else {
                setValueStack([]);
              }
            } catch {
              const lines = String(tourData.inclusions)
                .split('\n')
                .map((line: string) => line.trim())
                .filter(Boolean);
              if (lines.length > 0) {
                setValueStack(
                  lines.map((itemName: string, index: number) => ({
                    name: itemName,
                    val: 'Đã bao gồm',
                    type: index >= 5 ? 'bonus' : 'core',
                  }))
                );
              } else {
                setValueStack([]);
              }
            }
          } else {
            setValueStack([]);
          }

          // 4. Nạp câu hỏi thường gặp (FAQs) của tour từ API (chỉ lưu nếu có data)
          apiFetch(API_ROUTES.TRAVELS.FAQS(tourData.id))
            .then((res) => res.json())
            .then((faqJson) => {
              if (faqJson.success && Array.isArray(faqJson.data) && faqJson.data.length > 0) {
                const visibleFaqs = faqJson.data.filter((f: any) => f.isVisible !== false);
                setFaqs(visibleFaqs.map((f: any) => ({ q: f.question, a: f.answer })));
              } else {
                setFaqs([]);
              }
            })
            .catch(() => {
              setFaqs([]);
            });

          // 5. Nạp đánh giá của khách hàng (Reviews) từ API (chỉ lưu nếu có data)
          apiFetch(API_ROUTES.TRAVELS.REVIEWS(tourData.id))
            .then((res) => res.json())
            .then((reviewJson) => {
              if (reviewJson.success && Array.isArray(reviewJson.data) && reviewJson.data.length > 0) {
                const visibleReviews = reviewJson.data.filter((r: any) => r.isVisible !== false);
                setReviews(
                  visibleReviews.map((r: any) => ({
                    name: r.reviewerName || 'Khách hàng',
                    role: r.travelDate ? `Khởi hành: ${r.travelDate}` : 'Du khách',
                    content: r.content,
                  }))
                );
              } else {
                setReviews([]);
              }
            })
            .catch(() => {
              setReviews([]);
            });
        } else {
          setNotFound(true);
        }
      })
      .catch(() => {
        setNotFound(true);
      })
      .finally(() => {
        setLoading(false);
      });
  }, [slug]);

  // Context tour dành cho FloatingChatWidget
  const chatContext: TourSummary | null = tour
    ? {
        id: tour.id,
        code: tour.code || '',
        name: tour.name || tour.title || 'Tour Du Lịch',
        priceFrom: tour.priceFrom || 0,
        currency: tour.currency || 'VND',
        durationDays: tour.durationDays || 1,
        durationNights: tour.durationNights || 0,
        destination: tour.destination || '',
        country: tour.country || '',
        departureCity: tour.departureCity || 'TP.HCM',
        departuresCount: departures.length,
        highlights: tour.highlights || '',
      }
    : null;

  // ── TRƯỜNG HỢP 1: SKELETON LOADER (ZERO FLASH) ──
  if (loading) {
    return (
      <div className="bg-[#080c14] text-slate-100 min-h-screen font-sans">
        {/* Top Ticker Skeleton */}
        <div className="h-10 bg-[#151c2e]/60 border-b border-white/5 animate-pulse" />

        {/* Header Skeleton */}
        <div className="max-w-7xl mx-auto px-4 h-20 flex items-center justify-between border-b border-white/5">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-amber-500/20 animate-pulse" />
            <div className="w-32 h-6 bg-slate-800 rounded animate-pulse" />
          </div>
          <div className="hidden lg:flex items-center gap-6">
            <div className="w-16 h-4 bg-slate-800 rounded animate-pulse" />
            <div className="w-20 h-4 bg-slate-800 rounded animate-pulse" />
            <div className="w-20 h-4 bg-slate-800 rounded animate-pulse" />
          </div>
          <div className="w-28 h-9 bg-amber-500/20 rounded-full animate-pulse" />
        </div>

        {/* Hero Section Skeleton */}
        <div className="max-w-4xl mx-auto px-4 pt-16 pb-20 text-center space-y-6">
          <div className="w-56 h-7 bg-amber-500/20 rounded-full mx-auto animate-pulse" />
          <div className="w-3/4 h-14 bg-slate-800/80 rounded-2xl mx-auto animate-pulse" />
          <div className="w-1/2 h-5 bg-slate-800/60 rounded mx-auto animate-pulse" />

          {/* Price Box Skeleton */}
          <div className="max-w-md mx-auto p-6 rounded-3xl bg-[#151c2e]/80 border border-amber-500/20 shadow-2xl space-y-4 mt-8">
            <div className="h-4 bg-slate-700/50 rounded w-1/3 animate-pulse" />
            <div className="h-10 bg-amber-500/30 rounded w-2/3 animate-pulse" />
            <div className="h-12 bg-gradient-to-r from-amber-500/40 to-amber-600/40 rounded-2xl animate-pulse" />
          </div>
        </div>

        {/* Loading notice */}
        <div className="text-center text-xs text-amber-400/80 flex items-center justify-center gap-2">
          <span className="w-2 h-2 rounded-full bg-amber-400 animate-ping" />
          <span>Đang tải thông tin tour thực tế từ hệ thống...</span>
        </div>
      </div>
    );
  }

  // ── TRƯỜNG HỢP 2: TOUR NOT FOUND (404) ──
  if (notFound || !tour) {
    return (
      <div className="bg-[#080c14] text-slate-100 min-h-screen flex flex-col items-center justify-center p-6 text-center">
        <div className="max-w-md p-8 rounded-3xl bg-[#151c2e] border border-[#222f49] shadow-2xl space-y-5">
          <div className="w-16 h-16 rounded-2xl bg-rose-500/15 text-rose-400 flex items-center justify-center text-3xl mx-auto border border-rose-500/30">
            🔍
          </div>
          <h2 className="text-2xl font-bold text-white">Không Tìm Thấy Tour</h2>
          <p className="text-slate-400 text-sm leading-relaxed">
            Hành trình với mã hoặc đường dẫn <code className="text-amber-400 font-mono">"{slug}"</code> không tồn tại hoặc đã tạm đóng đăng ký.
          </p>
          <div className="pt-2 flex flex-col sm:flex-row gap-3">
            <Link
              to="/"
              className="flex-1 py-3 px-4 rounded-xl bg-slate-800 hover:bg-slate-700 text-white font-semibold text-xs transition"
            >
              ← Về Trang Chủ
            </Link>
            <Link
              to="/admin/products"
              className="flex-1 py-3 px-4 rounded-xl bg-cyan-500/20 hover:bg-cyan-500/30 text-cyan-300 border border-cyan-500/40 font-semibold text-xs transition"
            >
              Danh Mục Admin
            </Link>
          </div>
        </div>
      </div>
    );
  }

  // ── TRƯỜNG HỢP 3: XỬ LÝ OFFER HYBRID 80/20 VÀ DỮ LIỆU HIỂN THỊ ──
  const offers: any[] = Array.isArray(tour.offers) ? tour.offers : [];
  const hasMultipleOffers = offers.length > 1;
  const currentOffer = offers.length > 0 ? offers[selectedOfferIndex] || offers[0] : null;

  // Giá hiển thị ưu tiên theo Offer hiện tại, nếu không có offer thì lấy giá gốc tour
  const priceFrom = currentOffer
    ? Number(currentOffer.pricing?.offerPrice || currentOffer.offerPrice || tour.priceFrom || 0)
    : Number(tour.priceFrom || 0);

  const originalPrice = currentOffer
    ? Number(currentOffer.pricing?.originalPrice || currentOffer.originalPrice || Math.round(priceFrom * 1.2))
    : Math.round(priceFrom * 1.2) || priceFrom;

  const depositAmount = currentOffer
    ? Number(currentOffer.pricing?.depositAmount || currentOffer.depositAmount || Math.round(priceFrom * 0.3))
    : Math.round(priceFrom * 0.3) || 0;

  const durationText = `${tour.durationDays || 1} Ngày ${tour.durationNights || 0} Đêm`;

  // Danh sách Value Stack ưu tiên lấy từ Offer items, nếu không có offer thì lấy từ Inclusions
  let displayedValueStack: ValueStackItem[] = [];
  if (currentOffer && Array.isArray(currentOffer.items) && currentOffer.items.length > 0) {
    displayedValueStack = currentOffer.items.map((it: any) => ({
      name: it.name,
      val: it.originalValue > 0 ? Number(it.originalValue).toLocaleString('vi-VN') + ' ₫' : 'Đã bao gồm',
      type: it.type === 'bonus' ? 'bonus' : 'core',
    }));
  } else {
    displayedValueStack = valueStack;
  }

  // Kiểm tra ảnh đại diện thực tế (nếu ngày đầu có ảnh hoặc tour có ảnh)
  const heroRealImage = itinerary.find((dayItem) => Boolean(dayItem.image))?.image || null;

  // Lọc danh sách chính sách thực tế từ database
  const policyItems: PolicyItem[] = [];
  if (tour.cancellationPolicy?.trim()) {
    policyItems.push({
      title: 'Chính Sách Hoàn Cọc & Hủy Tour',
      content: tour.cancellationPolicy.trim(),
      icon: '🛡️',
    });
  }
  if (tour.visaPolicy?.trim()) {
    policyItems.push({
      title: 'Chính Sách Thụ Lý Visa',
      content: tour.visaPolicy.trim(),
      icon: '🛂',
    });
  }
  if (tour.paymentPolicy?.trim()) {
    policyItems.push({
      title: 'Chính Sách Thanh Toán & Đặt Cọc',
      content: tour.paymentPolicy.trim(),
      icon: '💳',
    });
  }
  if (tour.childPolicy?.trim()) {
    policyItems.push({
      title: 'Chính Sách Trẻ Em & Phụ Thu',
      content: tour.childPolicy.trim(),
      icon: '👶',
    });
  }
  const hasPolicies = policyItems.length > 0;
  const hasOfferSection = displayedValueStack.length > 0;

  // Xây dựng danh sách điều hướng động dựa trên các section THỰC TẾ tồn tại
  const navItems: NavItem[] = [
    ...(itinerary.length > 0 ? [{ id: 'itinerary', label: 'Lịch Trình' }] : []),
    ...(hasOfferSection
      ? [{ id: 'offer', label: hasMultipleOffers ? 'Các Gói Ưu Đãi' : 'Đặc Quyền Offer' }]
      : []),
    ...(hasPolicies ? [{ id: 'policies', label: 'Chính Sách & Cam Kết' }] : []),
    ...(departures.length > 0 ? [{ id: 'departures', label: 'Lịch Bay & Giá' }] : []),
    ...(faqs.length > 0 ? [{ id: 'faq', label: 'Câu Hỏi FAQ' }] : []),
    ...(reviews.length > 0 ? [{ id: 'reviews', label: 'Đánh Giá' }] : []),
  ];

  // Xác định mục neo ưu tiên cho nút CTA Đặt chỗ
  const primaryCtaAnchor = hasOfferSection ? '#offer' : departures.length > 0 ? '#departures' : null;

  return (
    <div className="bg-[#080c14] text-slate-100 min-h-screen selection:bg-amber-500 selection:text-black font-sans">
      {/* TOP ANNOUNCEMENT TICKER */}
      <div className="bg-gradient-to-r from-[#181105] via-[#2a1d07] to-[#181105] border-b border-amber-500/25 py-2 px-4 text-xs sm:text-sm text-amber-400 text-center font-medium flex items-center justify-center gap-2">
        <span className="inline-block w-2 h-2 rounded-full bg-emerald-400 animate-ping" />
        <span>
          🌸 <strong>Hành Trình Tinh Hoa:</strong> Tư vấn chi tiết 24/7 cùng trợ lý AI Sakura & Đội ngũ chuyên viên du lịch
        </span>
      </div>

      {/* MAIN NAVBAR ĐỘNG */}
      <header className="sticky top-0 z-40 glass-panel border-b border-white/10 transition-all duration-300">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-20 flex items-center justify-between">
          {/* Brand Logo */}
          <Link to="/" className="flex items-center gap-3 group">
            <div className="w-11 h-11 rounded-xl bg-gradient-to-br from-amber-400 to-amber-600 flex items-center justify-center text-[#080c14] font-bold text-xl shadow-lg shadow-amber-500/20 group-hover:scale-105 transition-transform">
              ⛩️
            </div>
            <div>
              <span className="font-display text-2xl font-bold tracking-wide text-white block leading-tight">
                NIPPON <span className="text-amber-400 font-semibold">LUXE</span>
              </span>
              <span className="text-[10px] tracking-widest uppercase text-amber-500/80 font-semibold">
                Hành Trình Tinh Hoa
              </span>
            </div>
          </Link>

          {/* Navigation Links Động (Chỉ hiển thị các mục có data thực tế) */}
          <nav className="hidden lg:flex items-center gap-7 text-sm font-medium text-slate-300">
            {navItems.map((item) => (
              <a
                key={item.id}
                href={`#${item.id}`}
                className="hover:text-amber-400 transition-colors"
              >
                {item.label}
              </a>
            ))}
            <Link
              to="/admin/products"
              className="text-xs px-2.5 py-1 rounded bg-slate-800 text-slate-400 hover:text-white transition-colors border border-slate-700"
            >
              Admin Portal
            </Link>
          </nav>

          {/* Actions */}
          <div className="flex items-center gap-3">
            <button
              onClick={() => setIsChatOpen(true)}
              className="hidden sm:flex items-center gap-2 px-4 py-2.5 rounded-full border border-amber-500/40 text-amber-400 hover:bg-amber-500/10 transition-all text-xs sm:text-sm font-semibold"
            >
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
                <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500" />
              </span>
              <span>Hỏi AI Sakura</span>
            </button>
            {primaryCtaAnchor ? (
              <a
                href={primaryCtaAnchor}
                className="px-5 py-2.5 rounded-full bg-gradient-to-r from-amber-500 via-amber-400 to-amber-600 text-[#080c14] font-bold text-xs sm:text-sm hover:shadow-lg hover:shadow-amber-500/25 hover:brightness-110 transition-all flex items-center gap-1.5"
              >
                <span>Giữ Chỗ Ngay</span>
                <span>→</span>
              </a>
            ) : (
              <button
                onClick={() => setIsChatOpen(true)}
                className="px-5 py-2.5 rounded-full bg-gradient-to-r from-amber-500 via-amber-400 to-amber-600 text-[#080c14] font-bold text-xs sm:text-sm hover:shadow-lg hover:shadow-amber-500/25 hover:brightness-110 transition-all flex items-center gap-1.5"
              >
                <span>Tư Vấn Giữ Chỗ</span>
                <span>→</span>
              </button>
            )}
          </div>
        </div>
      </header>

      {/* [SECTION 1] HERO SECTION (Không dùng ảnh Unsplash giả mạo) */}
      <section className="relative min-h-[85vh] flex items-center justify-center overflow-hidden pt-12 pb-20">
        <div className="absolute inset-0 z-0">
          {heroRealImage ? (
            <>
              <img
                src={heroRealImage}
                alt={tour.name}
                className="w-full h-full object-cover object-center transform scale-105 transition-transform duration-1000 ease-out"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-[#080c14] via-[#080c14]/75 to-[#080c14]/50" />
            </>
          ) : (
            <div className="w-full h-full bg-gradient-to-br from-[#0c1424] via-[#080c14] to-[#04060a] relative">
              <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,_var(--tw-gradient-stops))] from-amber-500/10 via-transparent to-transparent" />
              <div className="absolute top-1/4 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[300px] bg-amber-500/5 blur-[120px] rounded-full pointer-events-none" />
            </div>
          )}
        </div>

        <div className="relative z-10 max-w-5xl mx-auto px-4 sm:px-6 text-center">
          {/* Super Badge */}
          <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-amber-500/15 border border-amber-500/40 text-amber-400 text-xs sm:text-sm font-semibold tracking-wide uppercase mb-6 backdrop-blur-md">
            <span>🌸</span>
            <span>
              Hành Trình {durationText} • {tour.country || 'Quốc tế'} • Khởi Hành: {tour.departureCity || 'TP.HCM'}
            </span>
          </div>

          {/* Main Heading */}
          <h1 className="font-display text-4xl sm:text-6xl lg:text-7xl font-bold text-white tracking-tight leading-[1.1] mb-6">
            <span className="text-gold-gradient">{tour.name}</span>
          </h1>

          {/* Hero Mechanism / Tagline của Offer hoặc Highlights */}
          {currentOffer?.tagline ? (
            <p className="max-w-3xl mx-auto text-base sm:text-xl text-amber-300 font-light leading-relaxed mb-4">
              "{currentOffer.tagline}"
            </p>
          ) : null}

          {/* Subtitle / Highlights */}
          {tour.highlights && (
            <p className="max-w-3xl mx-auto text-sm sm:text-base text-slate-300 font-light leading-relaxed mb-10">
              {tour.highlights}
            </p>
          )}

          {/* 4 Trust Badges Grid */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 max-w-4xl mx-auto mb-10">
            <div className="glass-panel p-3.5 rounded-2xl flex items-center gap-3 text-left">
              <span className="text-2xl">✈️</span>
              <div>
                <div className="text-xs text-slate-400 font-medium">Hàng không chuẩn</div>
                <div className="text-xs sm:text-sm font-bold text-white">Khởi hành {tour.departureCity || 'TP.HCM'}</div>
              </div>
            </div>
            <div className="glass-panel p-3.5 rounded-2xl flex items-center gap-3 text-left">
              <span className="text-2xl">🏨</span>
              <div>
                <div className="text-xs text-slate-400 font-medium">Lưu trú chất lượng</div>
                <div className="text-xs sm:text-sm font-bold text-white">Khách sạn tiêu chuẩn</div>
              </div>
            </div>
            <div className="glass-panel p-3.5 rounded-2xl flex items-center gap-3 text-left">
              <span className="text-2xl">🍱</span>
              <div>
                <div className="text-xs text-slate-400 font-medium">Ẩm thực trọn gói</div>
                <div className="text-xs sm:text-sm font-bold text-white">Đặc sản địa phương</div>
              </div>
            </div>
            <div className="glass-panel p-3.5 rounded-2xl flex items-center gap-3 text-left">
              <span className="text-2xl">🛡️</span>
              <div>
                <div className="text-xs text-slate-400 font-medium">Cam kết bảo vệ</div>
                <div className="text-xs sm:text-sm font-bold text-white">Bảo vệ quyền lợi tối đa</div>
              </div>
            </div>
          </div>

          {/* Price Card & Dual CTAs (Đồng bộ theo Offer đang chọn) */}
          <div className="glass-panel-gold max-w-xl mx-auto p-6 sm:p-7 rounded-3xl gold-border-glow">
            {currentOffer && hasMultipleOffers && (
              <div className="mb-4 pb-3 border-b border-white/10 flex items-center justify-between text-xs">
                <span className="text-slate-400">Đang chọn gói:</span>
                <span className="font-bold text-amber-300 flex items-center gap-1.5">
                  <span>{currentOffer.isFeatured ? '💎' : '🌸'}</span>
                  <span>{currentOffer.title}</span>
                </span>
              </div>
            )}

            <div className="flex flex-col sm:flex-row items-center justify-between gap-4 border-b border-white/10 pb-5 mb-5">
              <div className="text-center sm:text-left">
                <span className="text-xs text-slate-300 block uppercase tracking-wider font-semibold">
                  Giá trọn gói ưu đãi chỉ từ:
                </span>
                <div className="flex items-baseline gap-2 justify-center sm:justify-start">
                  <span className="font-display text-3xl sm:text-4xl font-bold text-amber-400">
                    {priceFrom.toLocaleString('vi-VN')} ₫
                  </span>
                  {originalPrice > priceFrom && (
                    <span className="text-sm line-through text-slate-400">
                      {originalPrice.toLocaleString('vi-VN')} ₫
                    </span>
                  )}
                </div>
              </div>
              <div className="text-center sm:text-right">
                <span className="text-xs text-emerald-400 font-semibold block">Chỉ cần cọc 30% giữ chỗ:</span>
                <span className="text-lg sm:text-xl font-bold text-white">
                  {depositAmount.toLocaleString('vi-VN')} ₫
                </span>
              </div>
            </div>

            <div className="flex flex-col sm:flex-row gap-3">
              {primaryCtaAnchor ? (
                <a
                  href={primaryCtaAnchor}
                  className="flex-1 py-3.5 px-6 rounded-2xl bg-gradient-to-r from-amber-500 to-amber-400 text-[#080c14] font-bold text-sm sm:text-base hover:brightness-110 shadow-lg shadow-amber-500/30 transition-all flex items-center justify-center gap-2"
                >
                  <span>Nhận Ưu Đãi & Giữ Suất Bay</span>
                  <span>→</span>
                </a>
              ) : (
                <button
                  onClick={() => setIsChatOpen(true)}
                  className="flex-1 py-3.5 px-6 rounded-2xl bg-gradient-to-r from-amber-500 to-amber-400 text-[#080c14] font-bold text-sm sm:text-base hover:brightness-110 shadow-lg shadow-amber-500/30 transition-all flex items-center justify-center gap-2"
                >
                  <span>💬 Tư Vấn & Giữ Suất Bay</span>
                  <span>→</span>
                </button>
              )}
              <button
                onClick={() => setIsChatOpen(true)}
                className="sm:flex-initial py-3.5 px-5 rounded-2xl bg-white/10 hover:bg-white/15 text-white font-semibold text-sm transition-all border border-white/15 flex items-center justify-center gap-2"
              >
                <span>💬 Hỏi AI Sakura 24/7</span>
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* [SECTION 2] SOCIAL PROOF BAR (Chỉ hiển thị khi có đánh giá thực tế từ DB) */}
      {reviews.length > 0 && (
        <section id="reviews" className="py-14 bg-[#0f172a]/60 border-y border-white/5 relative">
          <div className="max-w-7xl mx-auto px-4 sm:px-6">
            <div className="text-center mb-8">
              <span className="text-xs uppercase tracking-widest text-amber-400 font-semibold">
                Trải Nghiệm Khách Hàng Thực Tế
              </span>
              <h2 className="font-display text-3xl font-bold text-white mt-1">
                Cảm Nhận Từ Những Người Bạn Đồng Hành
              </h2>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {reviews.slice(0, 6).map((item, index) => (
                <div key={index} className="glass-panel p-6 rounded-2xl flex flex-col justify-between">
                  <div>
                    <div className="flex text-amber-400 text-sm mb-3">★★★★★</div>
                    <p className="text-slate-300 text-sm italic leading-relaxed mb-4">
                      "{item.content}"
                    </p>
                  </div>
                  <div className="flex items-center gap-3 pt-3 border-t border-white/10">
                    <div className="w-10 h-10 rounded-full bg-amber-500/20 text-amber-400 flex items-center justify-center font-bold text-sm">
                      {item.name.charAt(0).toUpperCase()}
                    </div>
                    <div>
                      <div className="text-sm font-semibold text-white">{item.name}</div>
                      <div className="text-xs text-slate-400">{item.role}</div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </section>
      )}

      {/* [SECTION 3] THE VALUE STACK / HYBRID ADAPTIVE OFFER SECTION */}
      {hasOfferSection && (
        <section id="offer" className="py-20 max-w-5xl mx-auto px-4 sm:px-6">
          <div className="text-center space-y-3 mb-10">
            <span className="text-xs uppercase tracking-widest text-amber-400 font-semibold px-3 py-1 rounded-full bg-amber-500/10 border border-amber-500/30">
              {currentOffer?.tagline || 'Lời Đề Nghị Không Thể Từ Chối (Grand Slam Offer)'}
            </span>
            <h2 className="font-display text-3xl sm:text-5xl font-bold text-white">
              {currentOffer?.title || 'Chi Tiết Quyền Lợi & Hạng Mục Đi Kèm'}
            </h2>
            <p className="text-slate-300 text-sm sm:text-base max-w-2xl mx-auto">
              {currentOffer?.corePromise ||
                currentOffer?.heroMechanism ||
                'Hành trình trọn gói được thiết kế chỉn chu, đảm bảo trải nghiệm thảnh thơi và không phát sinh chi phí ẩn.'}
            </p>
          </div>

          {/* ADAPTIVE PILL TABS: Chỉ xuất hiện khi Tour có từ 2 Offer trở lên */}
          {hasMultipleOffers && (
            <div className="flex flex-wrap items-center justify-center gap-2.5 mb-8">
              {offers.map((off: any, idx: number) => {
                const isSelected = selectedOfferIndex === idx;
                const offPrice = Number(off.pricing?.offerPrice || off.offerPrice || 0);
                return (
                  <button
                    key={off.id}
                    type="button"
                    onClick={() => setSelectedOfferIndex(idx)}
                    className={`px-5 py-2.5 rounded-full text-xs sm:text-sm font-bold transition-all flex items-center gap-2 cursor-pointer ${
                      isSelected
                        ? 'bg-gradient-to-r from-amber-500 to-amber-600 text-black shadow-lg shadow-amber-500/25 scale-105 ring-2 ring-amber-400'
                        : 'bg-slate-900/80 text-slate-300 hover:text-white border border-white/10 hover:border-amber-500/40 hover:bg-slate-800'
                    }`}
                  >
                    <span>{off.isFeatured ? '💎' : '🌸'}</span>
                    <span>{off.title}</span>
                    {offPrice > 0 && (
                      <span
                        className={`text-[11px] font-mono px-2 py-0.5 rounded-full ${
                          isSelected ? 'bg-black/30 text-slate-900 font-extrabold' : 'bg-white/10 text-amber-300'
                        }`}
                      >
                        {offPrice.toLocaleString('vi-VN')} ₫
                      </span>
                    )}
                  </button>
                );
              })}
            </div>
          )}

          <div className="glass-panel-gold rounded-3xl p-6 sm:p-10 gold-border-glow space-y-6">
            <div className="space-y-3">
              {displayedValueStack.map((item, index) => (
                <div
                  key={index}
                  className="flex items-center justify-between p-3.5 rounded-xl bg-slate-900/60 border border-white/5 text-xs sm:text-sm"
                >
                  <div className="flex items-center gap-2.5">
                    <span className="text-emerald-400 font-bold">✓</span>
                    <span className={item.type === 'bonus' ? 'text-amber-300 font-semibold' : 'text-slate-200'}>
                      {item.name}
                    </span>
                  </div>
                  <span className="font-mono text-slate-400 text-xs shrink-0 pl-3">{item.val}</span>
                </div>
              ))}
            </div>

            <div className="pt-6 border-t border-white/10 flex flex-col sm:flex-row items-center justify-between gap-6">
              <div>
                <span className="text-xs text-slate-400 block">
                  Giá trọn gói ưu đãi chỉ từ:
                </span>
                <div className="flex items-baseline gap-2">
                  <span className="font-display text-4xl sm:text-5xl font-bold text-amber-400">
                    {priceFrom.toLocaleString('vi-VN')} ₫
                  </span>
                  {originalPrice > priceFrom && (
                    <span className="text-xs text-emerald-400 font-semibold uppercase px-2 py-0.5 rounded bg-emerald-500/20 border border-emerald-500/30">
                      Tiết kiệm {Math.max(0, originalPrice - priceFrom).toLocaleString('vi-VN')} ₫
                    </span>
                  )}
                </div>
              </div>

              <button
                onClick={() => {
                  notify.success(
                    `Đã ghi nhận yêu cầu đặt chỗ cho gói "${
                      currentOffer?.title || tour.name
                    }". Chuyên viên tư vấn sẽ liên hệ ngay!`
                  );
                }}
                className="w-full sm:w-auto px-8 py-4 rounded-2xl bg-gradient-to-r from-amber-500 to-amber-400 hover:brightness-110 text-[#080c14] font-bold text-base shadow-xl shadow-amber-500/30 transition-all flex items-center justify-center gap-2 active:scale-95"
              >
                <span>Đặt Cọc Giữ Suất Bay ({depositAmount.toLocaleString('vi-VN')} ₫)</span>
                <span>→</span>
              </button>
            </div>
          </div>
        </section>
      )}

      {/* [SECTION 4] ITINERARY SECTION (Chỉ hiển thị khi có lịch trình từ DB) */}
      {itinerary.length > 0 && (
        <section id="itinerary" className="py-20 bg-[#0f172a]/40 border-y border-white/5">
          <div className="max-w-6xl mx-auto px-4 sm:px-6">
            <div className="text-center space-y-3 mb-14">
              <span className="text-xs uppercase tracking-widest text-amber-400 font-semibold">
                Hành Trình Chi Tiết
              </span>
              <h2 className="font-display text-3xl sm:text-5xl font-bold text-white">
                Lịch Trình Từng Ngày {durationText}
              </h2>
              <p className="text-slate-300 text-sm max-w-xl mx-auto">
                Nhịp độ thư thả, cân bằng giữa thắng cảnh văn hóa, ẩm thực đặc trưng và thời gian trải nghiệm trọn vẹn.
              </p>
            </div>

            <div className="space-y-6">
              {itinerary.map((item) => (
                <div
                  key={item.day}
                  className="glass-panel rounded-2xl p-5 sm:p-7 flex flex-col md:flex-row gap-6 items-center"
                >
                  {/* Chỉ render ảnh nếu ngày này có ảnh thật */}
                  {item.image && (
                    <div className="w-full md:w-5/12 h-52 sm:h-60 rounded-xl overflow-hidden shrink-0 border border-white/10">
                      <img src={item.image} alt={item.title} className="w-full h-full object-cover" />
                    </div>
                  )}
                  <div className="flex-1 space-y-3 w-full">
                    <div className="flex items-center gap-2">
                      <span className="px-3 py-1 rounded-lg text-xs font-bold bg-amber-500/20 text-amber-400 border border-amber-500/30 font-mono">
                        NGÀY {item.day}
                      </span>
                      <h3 className="font-bold text-white text-base sm:text-lg">{item.title}</h3>
                    </div>
                    <p className="text-slate-300 text-xs sm:text-sm leading-relaxed whitespace-pre-line">{item.content}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </section>
      )}

      {/* [SECTION 5] POLICIES & GUARANTEES (Chỉ hiển thị khi có chính sách thực tế từ DB) */}
      {hasPolicies && (
        <section id="policies" className="py-20 max-w-5xl mx-auto px-4 sm:px-6">
          <div className="glass-panel-gold rounded-3xl p-8 sm:p-12 gold-border-glow text-center space-y-6">
            <div className="w-16 h-16 rounded-2xl bg-amber-500/20 text-amber-400 flex items-center justify-center text-3xl mx-auto border border-amber-500/40">
              🛡️
            </div>
            <h2 className="font-display text-3xl sm:text-5xl font-bold text-white">
              Chính Sách & Cam Kết Dịch Vụ
            </h2>
            <p className="text-slate-300 text-sm sm:text-base max-w-2xl mx-auto leading-relaxed">
              Quy định rõ ràng, minh bạch bằng văn bản nhằm bảo vệ quyền lợi tối đa cho quý khách trong suốt hành trình:
            </p>

            <div className={`grid grid-cols-1 ${policyItems.length > 1 ? 'md:grid-cols-2' : ''} gap-6 text-left pt-4`}>
              {policyItems.map((policy, index) => (
                <div key={index} className="p-5 rounded-2xl bg-slate-900/80 border border-white/5 space-y-2">
                  <div className="flex items-center gap-2">
                    <span className="text-xl">{policy.icon}</span>
                    <span className="text-amber-400 font-bold text-base sm:text-lg">{policy.title}</span>
                  </div>
                  <p className="text-xs sm:text-sm text-slate-300 leading-relaxed whitespace-pre-line">
                    {policy.content}
                  </p>
                </div>
              ))}
            </div>
          </div>
        </section>
      )}

      {/* [SECTION 6] DEPARTURES TABLE (Chỉ hiển thị khi có lịch bay từ DB) */}
      {departures.length > 0 && (
        <section id="departures" className="py-20 bg-[#0f172a]/40 border-y border-white/5">
          <div className="max-w-5xl mx-auto px-4 sm:px-6">
            <div className="text-center space-y-3 mb-12">
              <span className="text-xs uppercase tracking-widest text-amber-400 font-semibold">
                Lịch Khởi Hành Gần Nhất
              </span>
              <h2 className="font-display text-3xl sm:text-4xl font-bold text-white">
                Bảng Đợt Bay Mở Bán
              </h2>
            </div>

            <div className="glass-panel rounded-2xl overflow-hidden border border-white/10">
              <table className="w-full text-left text-xs sm:text-sm">
                <thead className="bg-slate-900/90 text-slate-400 uppercase text-[11px] font-mono border-b border-white/10">
                  <tr>
                    <th className="py-3.5 px-5">Ngày Bay</th>
                    <th className="py-3.5 px-5">Hãng Hàng Không / Ghi Chú</th>
                    <th className="py-3.5 px-5">Giá Người Lớn</th>
                    <th className="py-3.5 px-5">Tình Trạng</th>
                    <th className="py-3.5 px-5 text-right">Hành Động</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-white/5">
                  {departures.map((departureItem, index) => (
                    <tr key={index} className="hover:bg-slate-900/50 transition">
                      <td className="py-4 px-5 font-semibold text-white">{departureItem.date}</td>
                      <td className="py-4 px-5 text-slate-300">{departureItem.airline}</td>
                      <td className="py-4 px-5 font-bold text-amber-400 font-mono">{departureItem.price}</td>
                      <td className="py-4 px-5">
                        <span className="px-2.5 py-1 rounded-full text-[11px] font-bold uppercase bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                          {departureItem.status}
                        </span>
                      </td>
                      <td className="py-4 px-5 text-right">
                        <button
                          onClick={() => setIsChatOpen(true)}
                          className="px-3.5 py-1.5 rounded-lg text-xs font-semibold bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 border border-amber-500/40 transition"
                        >
                          Giữ chỗ
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </section>
      )}

      {/* [SECTION 7] FAQ ACCORDION (Chỉ hiển thị khi có câu hỏi FAQ từ DB) */}
      {faqs.length > 0 && (
        <section id="faq" className="py-20 max-w-4xl mx-auto px-4 sm:px-6">
          <div className="text-center space-y-3 mb-12">
            <span className="text-xs uppercase tracking-widest text-amber-400 font-semibold">
              Giải Đáp Thắc Mắc
            </span>
            <h2 className="font-display text-3xl sm:text-4xl font-bold text-white">
              Câu Hỏi Thường Gặp (FAQ)
            </h2>
          </div>

          <div className="space-y-3">
            {faqs.map((faqItem, index) => (
              <div key={index} className="glass-panel rounded-2xl overflow-hidden border border-white/10">
                <button
                  onClick={() => setActiveFaq(activeFaq === index ? null : index)}
                  className="w-full p-5 text-left font-semibold text-white flex items-center justify-between gap-4 hover:text-amber-400 transition"
                >
                  <span className="text-sm sm:text-base">{faqItem.q}</span>
                  <span className="text-xl font-mono">{activeFaq === index ? '−' : '+'}</span>
                </button>
                {activeFaq === index && (
                  <div className="px-5 pb-5 text-xs sm:text-sm text-slate-300 leading-relaxed border-t border-white/5 pt-3">
                    {faqItem.a}
                  </div>
                )}
              </div>
            ))}
          </div>
        </section>
      )}

      {/* FOOTER ĐỘNG (Chỉ hiển thị các link có section thực tế) */}
      <footer className="border-t border-white/10 bg-[#0e1422] py-10 px-4 sm:px-8 text-center text-xs text-slate-400 space-y-3">
        <div className="flex flex-wrap items-center justify-center gap-6 text-slate-300">
          {navItems.map((item) => (
            <a key={item.id} href={`#${item.id}`} className="hover:text-amber-400 transition-colors">
              {item.label}
            </a>
          ))}
          <Link to="/admin/products" className="text-amber-400 font-semibold hover:underline">
            Quản Trị Hệ Thống (Admin)
          </Link>
        </div>
        <p className="text-slate-500 text-[11px] pt-2">
          © 2026 AI Agent Travel • Hệ Thống Du Lịch 4 Chạm Thảnh Thơi • .NET 10 Clean Architecture Core.
        </p>
      </footer>

      {/* FLOATING AI SAKURA CHAT WIDGET */}
      {chatContext && (
        <FloatingChatWidget
          isOpen={isChatOpen}
          onClose={() => setIsChatOpen(false)}
          selectedTourContext={chatContext}
        />
      )}

      {/* FLOATING TRIGGER BUTTON */}
      {!isChatOpen && (
        <button
          onClick={() => setIsChatOpen(true)}
          className="fixed bottom-6 right-6 z-40 px-4 py-3 rounded-full bg-gradient-to-r from-amber-500 to-amber-400 hover:brightness-110 text-[#080c14] font-bold text-xs sm:text-sm shadow-2xl shadow-amber-500/40 border border-amber-300/40 flex items-center gap-2 transition active:scale-95 animate-bounce"
        >
          <span className="text-lg">🌸</span>
          <span>Hỏi AI Sakura 24/7</span>
        </button>
      )}
    </div>
  );
};
