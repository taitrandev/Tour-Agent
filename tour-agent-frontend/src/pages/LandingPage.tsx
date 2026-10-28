import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { FloatingChatWidget } from '../components/chat/FloatingChatWidget';
import { searchTours } from '../api/toursApi';
import type { TourSummary } from '../types/tour';

/**
 * Trang Landing Page du lịch tinh hoa tái hiện 100% giao diện landing.blade.php của dự án PHP.
 */
export const LandingPage: React.FC = () => {
  const [isChatOpen, setIsChatOpen] = useState(false);
  const [tours, setTours] = useState<TourSummary[]>([]);
  const [activeFaq, setActiveFaq] = useState<number | null>(null);

  useEffect(() => {
    searchTours().then((res) => {
      if (res && res.items.length > 0) {
        setTours(res.items);
      }
    });
  }, []);

  const currentTour = tours[0] || {
    id: 1,
    title: 'Khám Phá Cung Đường Vàng Nhật Bản: Osaka - Kyoto - Phú Sĩ - Tokyo 6N5Đ',
    name: 'Khám Phá Cung Đường Vàng Nhật Bản: Osaka - Kyoto - Phú Sĩ - Tokyo 6N5Đ',
    priceFrom: 28990000,
    originalPrice: 35000000,
    depositAmount: 8697000,
    durationDays: 6,
    durationNights: 5,
  };

  const itineraryDays = [
    {
      day: 1,
      title: 'TP.HCM / HÀ NỘI – KANSAI / OSAKA (Nghỉ đêm trên máy bay)',
      content:
        'Quý khách tập trung tại sân bay quốc tế Tân Sơn Nhất / Nội Bài. Trưởng đoàn hỗ trợ làm thủ tục check-in chuyến bay thẳng sang Nhật Bản. Quý khách nghỉ ngơi thư giãn trên máy bay với suất ăn nhẹ tiêu chuẩn quốc tế.',
      image: 'https://images.unsplash.com/photo-1542051841857-5f90071e7989?q=80&w=800&auto=format&fit=crop',
    },
    {
      day: 2,
      title: 'CỐ ĐÔ KYOTO – RỪNG TRÚC SAGANO – CHÙA VÀNG KINKAKUJI',
      content:
        'Đáp xuống sân bay Kansai, xe đón đoàn đến Cố đô Kyoto. Bách bộ giữa rừng trúc xanh Sagano thanh tịnh, lắng nghe tiếng gió xào xạc. Buổi chiều chiêm ngưỡng Chùa Vàng Kinkakuji soi bóng xuống hồ nước trong vắt. Tối thưởng thức bữa tối kaiseki truyền thống.',
      image: 'https://images.unsplash.com/photo-1492571350019-22de08371fd3?q=80&w=800&auto=format&fit=crop',
    },
    {
      day: 3,
      title: 'LÀNG CỔ SHIRAKAWAGO – THƯỞNG THỨC BÒ HIDA THƯỢNG HẠNG',
      content:
        'Khám phá di sản thế giới làng cổ tích Shirakawago với những mái nhà tranh Gassho-zukuri hàng trăm năm tuổi. Bữa trưa đặc biệt với set bò Hida nướng lá mộc lan thơm lừng tan chảy trong miệng.',
      image: 'https://images.unsplash.com/photo-1503899036084-c55cdd92da26?q=80&w=800&auto=format&fit=crop',
    },
    {
      day: 4,
      title: 'NÚI PHÚ SĨ LINH THIÊNG – LÀNG OSHINO HAKKAI – TẮM ONSEN',
      content:
        'Di chuyển đến biểu tượng núi Phú Sĩ hùng vĩ (lên trạm 5 nếu thời tiết thuận lợi). Tham quan làng cổ Oshino Hakkai dưới chân núi với nguồn nước tuyết tan thanh khiết. Tối trải nghiệm tắm suối khoáng nóng Onsen phục hồi thể lực hoàn hảo.',
      image: 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?q=80&w=800&auto=format&fit=crop',
    },
    {
      day: 5,
      title: 'THỦ ĐÔ TOKYO – CHÙA ASAKUSA KANNON – PHỐ GINZA & SHIBUYA',
      content:
        'Viếng ngôi chùa cổ nhất Tokyo Asakusa Kannon và dạo phố Nakamise nhộn nhịp. Buổi chiều tự do mua sắm tại thánh địa hàng hiệu Ginza và trải nghiệm giao lộ Shibuya huyền thoại.',
      image: 'https://images.unsplash.com/photo-1538485399081-7191377e8241?q=80&w=800&auto=format&fit=crop',
    },
    {
      day: 6,
      title: 'VỊNH TOKYO – ODAIBA – ĐÁP CHUYẾN BAY VỀ VIỆT NAM',
      content:
        'Check-in tượng Nữ Thần Tự Do tại đảo nhân tạo Odaiba, ngắm cầu Cầu Vồng Rainbow Bridge thơ mộng. Xe đưa đoàn ra sân bay Narita / Haneda làm thủ tục đáp chuyến bay thẳng về lại Việt Nam. Kết thúc chuyến đi thảnh thơi trọn vẹn.',
      image: 'https://images.unsplash.com/photo-1508248467877-aec1b08de376?q=80&w=800&auto=format&fit=crop',
    },
  ];

  const valueStackItems = [
    { name: 'Vé máy bay khứ hồi quốc tế thẳng (23kg ký gửi + 10kg xách tay)', val: '14.500.000₫', type: 'core' },
    { name: 'Khách sạn 3-4 sao chuẩn Nhật (2 khách/phòng, vị trí trung tâm)', val: '9.000.000₫', type: 'core' },
    { name: 'Trọn gói các bữa ăn đặc sản: Bò Hida A5, Cua tuyết, Shabu-shabu', val: '5.200.000₫', type: 'core' },
    { name: 'Phí thị thực Visa du lịch Nhật Bản & đội ngũ thụ lý hồ sơ 1:1', val: '2.500.000₫', type: 'core' },
    { name: 'Xe du lịch đời mới máy lạnh đưa đón riêng suốt hành trình', val: '3.800.000₫', type: 'core' },
    { name: '🎁 BONUS 1: Bộ cẩm nang số "Tokyo - Kyoto Hidden Gems" do AI biên soạn', val: '890.000₫', type: 'bonus' },
    { name: '🎁 BONUS 2: 1 Buổi tắm Onsen khoáng nóng cao cấp chuẩn Ryokan truyền thống', val: '1.200.000₫', type: 'bonus' },
    { name: '🎁 BONUS 3: SIM 4G dung lượng không giới hạn tốc độ cao sử dụng 6 ngày', val: '450.000₫', type: 'bonus' },
    { name: '🎁 BONUS 4: Bảo hiểm du lịch quốc tế quyền lợi tối đa 1.000.000.000 VNĐ', val: '750.000₫', type: 'bonus' },
  ];

  const departuresList = [
    { date: '15/10/2026', airline: 'Vietnam Airlines', price: '28.990.000₫', status: 'Còn 4 chỗ', hot: true },
    { date: '25/10/2026', airline: 'Vietjet Air (Bay thẳng)', price: '27.490.000₫', status: 'Chắc chắn bay', hot: false },
    { date: '10/11/2026', airline: 'All Nippon Airways (ANA)', price: '29.990.000₫', status: 'Còn 6 chỗ', hot: true },
    { date: '24/11/2026', airline: 'Vietnam Airlines', price: '28.990.000₫', status: 'Mở đăng ký', hot: false },
  ];

  const faqs = [
    {
      q: 'Nếu tôi bị rớt Visa du lịch Nhật Bản thì chính sách hoàn tiền ra sao?',
      a: 'Chúng tôi áp dụng Cam Kết Godfather tuyệt đối: Nếu quý khách chuẩn bị đầy đủ hồ sơ theo hướng dẫn nhưng bị Lãnh sự quán từ chối cấp visa, hệ thống sẽ HOÀN LẠI 100% TIỀN CỌC ngay trong 3 ngày làm việc mà không trừ bất kỳ khoản phí phạt nào.',
    },
    {
      q: 'Chỉ cần đặt cọc bao nhiêu để giữ suất ưu đãi giá và quà tặng số?',
      a: 'Quý khách chỉ cần thanh toán trước 30% giá trị tour (khoảng 8.697.000₫). Số tiền 70% còn lại chỉ cần thanh toán trước ngày khởi hành 15 ngày sau khi đã có kết quả visa hợp lệ.',
    },
    {
      q: 'Tour này có phù hợp cho người bận rộn không có nhiều thời gian chuẩn bị?',
      a: 'Hoàn toàn phù hợp! Đây là hành trình được thiết kế theo chuẩn "4 Chạm Thảnh Thơi": Chuyên viên và AI Sakura sẽ hỗ trợ điền toàn bộ tờ khai visa, book vé, chuẩn bị hành trình chi tiết. Bạn chỉ việc xách vali và lên máy bay.',
    },
    {
      q: 'Tôi đi một mình có bị phụ thu phòng đơn bắt buộc không?',
      a: 'Hệ thống sẽ chủ động ghép phòng với khách cùng giới tính nếu có nhu cầu để tiết kiệm chi phí. Quý khách chỉ thanh toán phụ thu phòng đơn nếu chủ động yêu cầu ở riêng 1 mình 1 phòng.',
    },
  ];

  return (
    <div className="bg-[#080c14] text-slate-100 min-h-screen selection:bg-amber-500 selection:text-black font-sans">
      {/* TOP ANNOUNCEMENT TICKER */}
      <div className="bg-gradient-to-r from-[#181105] via-[#2a1d07] to-[#181105] border-b border-amber-500/25 py-2 px-4 text-xs sm:text-sm text-amber-400 text-center font-medium flex items-center justify-center gap-2">
        <span className="inline-block w-2 h-2 rounded-full bg-emerald-400 animate-ping" />
        <span>
          🌸 <strong>Ưu Đãi Đặc Biệt:</strong> Tặng Gói Quà Tặng Số AI Trị Giá <strong>3.290.000₫</strong> Cho 10 Khách Đặt Cọc Giữ Chỗ Đầu Tiên
        </span>
        <span className="hidden md:inline text-slate-400">• Kết thúc sau:</span>
        <span className="font-bold text-amber-300 font-mono tracking-wider">23:59:59</span>
      </div>

      {/* MAIN NAVBAR */}
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

          {/* Navigation Links */}
          <nav className="hidden lg:flex items-center gap-7 text-sm font-medium text-slate-300">
            <a href="#itinerary" className="hover:text-amber-400 transition-colors">Lịch Trình</a>
            <a href="#offer" className="hover:text-amber-400 transition-colors">Đặc Quyền Offer</a>
            <a href="#godfather" className="hover:text-amber-400 transition-colors">Cam Kết 3 Lớp</a>
            <a href="#departures" className="hover:text-amber-400 transition-colors">Lịch Bay & Giá</a>
            <a href="#faq" className="hover:text-amber-400 transition-colors">Câu Hỏi FAQ</a>
            <Link
              to="/admin"
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
            <a
              href="#offer"
              className="px-5 py-2.5 rounded-full bg-gradient-to-r from-amber-500 via-amber-400 to-amber-600 text-[#080c14] font-bold text-xs sm:text-sm hover:shadow-lg hover:shadow-amber-500/25 hover:brightness-110 transition-all flex items-center gap-1.5"
            >
              <span>Giữ Chỗ Ngay</span>
              <span>→</span>
            </a>
          </div>
        </div>
      </header>

      {/* [SECTION 1] HERO SECTION */}
      <section className="relative min-h-[90vh] flex items-center justify-center overflow-hidden pt-12 pb-20">
        {/* Background Overlay */}
        <div className="absolute inset-0 z-0">
          <img
            src="https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?q=80&w=1600&auto=format&fit=crop"
            alt="Núi Phú Sĩ và Cổng Torii Nhật Bản"
            className="w-full h-full object-cover object-center transform scale-105 transition-transform duration-1000 ease-out"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-[#080c14] via-[#080c14]/70 to-[#080c14]/50" />
        </div>

        <div className="relative z-10 max-w-5xl mx-auto px-4 sm:px-6 text-center">
          {/* Super Badge */}
          <div className="inline-flex items-center gap-2 px-4 py-1.5 rounded-full bg-amber-500/15 border border-amber-500/40 text-amber-400 text-xs sm:text-sm font-semibold tracking-wide uppercase mb-6 backdrop-blur-md">
            <span>🌸</span>
            <span>Hành Trình Chữa Lành Tâm Hồn • Dành Riêng Cho Dân Văn Phòng</span>
          </div>

          {/* Main Heading */}
          <h1 className="font-display text-4xl sm:text-6xl lg:text-7xl font-bold text-white tracking-tight leading-[1.1] mb-6">
            Cung Đường Vàng Nhật Bản 6N5Đ: <br />
            <span className="text-gold-gradient">Osaka – Kyoto – Phú Sĩ – Tokyo</span>
          </h1>

          {/* Subtitle */}
          <p className="max-w-3xl mx-auto text-base sm:text-xl text-slate-200 font-light leading-relaxed mb-10">
            Tạm gác áp lực deadline để đắm mình giữa sắc hoa Phù Tang, thư giãn suối khoáng Onsen và thưởng thức bò Hida thượng hạng. Trọn gói không phát sinh, hỗ trợ visa đậu 100% hoặc hoàn cọc.
          </p>

          {/* 4 Trust Badges Grid */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 max-w-4xl mx-auto mb-10">
            <div className="glass-panel p-3.5 rounded-2xl flex items-center gap-3 text-left">
              <span className="text-2xl">✈️</span>
              <div>
                <div className="text-xs text-slate-400 font-medium">Bay thẳng quốc tế</div>
                <div className="text-xs sm:text-sm font-bold text-white">VJ / ANA 46kg gửi</div>
              </div>
            </div>
            <div className="glass-panel p-3.5 rounded-2xl flex items-center gap-3 text-left">
              <span className="text-2xl">♨️</span>
              <div>
                <div className="text-xs text-slate-400 font-medium">Nghỉ dưỡng Onsen</div>
                <div className="text-xs sm:text-sm font-bold text-white">Khách sạn 3-4★ chuẩn Nhật</div>
              </div>
            </div>
            <div className="glass-panel p-3.5 rounded-2xl flex items-center gap-3 text-left">
              <span className="text-2xl">🥩</span>
              <div>
                <div className="text-xs text-slate-400 font-medium">Ẩm thực tinh hoa</div>
                <div className="text-xs sm:text-sm font-bold text-white">Bò Hida, Cua tuyết, Shabu</div>
              </div>
            </div>
            <div className="glass-panel p-3.5 rounded-2xl flex items-center gap-3 text-left">
              <span className="text-2xl">🛡️</span>
              <div>
                <div className="text-xs text-slate-400 font-medium">Bảo vệ quyền lợi</div>
                <div className="text-xs sm:text-sm font-bold text-white">100% Hoàn cọc nếu trượt visa</div>
              </div>
            </div>
          </div>

          {/* Price Card & Dual CTAs */}
          <div className="glass-panel-gold max-w-xl mx-auto p-6 sm:p-7 rounded-3xl gold-border-glow">
            <div className="flex flex-col sm:flex-row items-center justify-between gap-4 border-b border-white/10 pb-5 mb-5">
              <div className="text-center sm:text-left">
                <span className="text-xs text-slate-300 block uppercase tracking-wider font-semibold">
                  Giá trọn gói ưu đãi chỉ từ:
                </span>
                <div className="flex items-baseline gap-2 justify-center sm:justify-start">
                  <span className="font-display text-3xl sm:text-4xl font-bold text-amber-400">
                    28.990.000 ₫
                  </span>
                  <span className="text-sm line-through text-slate-400">35.000.000 ₫</span>
                </div>
              </div>
              <div className="text-center sm:text-right">
                <span className="text-xs text-emerald-400 font-semibold block">Chỉ cần cọc 30% giữ chỗ:</span>
                <span className="text-lg sm:text-xl font-bold text-white">8.697.000 ₫</span>
              </div>
            </div>

            <div className="flex flex-col sm:flex-row gap-3">
              <a
                href="#offer"
                className="flex-1 py-3.5 px-6 rounded-2xl bg-gradient-to-r from-amber-500 to-amber-400 text-[#080c14] font-bold text-sm sm:text-base hover:brightness-110 shadow-lg shadow-amber-500/30 transition-all flex items-center justify-center gap-2"
              >
                <span>Nhận Ưu Đãi & Giữ Suất Bay</span>
                <span>→</span>
              </a>
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

      {/* [SECTION 2] SOCIAL PROOF BAR */}
      <section className="py-14 bg-[#0f172a]/60 border-y border-white/5 relative">
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
            <div className="glass-panel p-6 rounded-2xl flex flex-col justify-between">
              <div>
                <div className="flex text-amber-400 text-sm mb-3">★★★★★</div>
                <p className="text-slate-300 text-sm italic leading-relaxed mb-4">
                  "Chuyến đi thật sự chữa lành sau chuỗi ngày OT ngập đầu. Khách sạn Onsen view hồ Kawaguchi đẹp mê mẩn, thịt bò Hida nướng thơm béo ngậy. Hướng dẫn viên chu đáo từng li từng tí."
                </p>
              </div>
              <div className="flex items-center gap-3 pt-3 border-t border-white/10">
                <div className="w-10 h-10 rounded-full bg-amber-500/20 text-amber-400 flex items-center justify-center font-bold text-sm">
                  L
                </div>
                <div>
                  <div className="text-sm font-semibold text-white">Chị Lan Hương</div>
                  <div className="text-xs text-slate-400">Marketing Lead (TP.HCM)</div>
                </div>
              </div>
            </div>

            <div className="glass-panel p-6 rounded-2xl flex flex-col justify-between">
              <div>
                <div className="flex text-amber-400 text-sm mb-3">★★★★★</div>
                <p className="text-slate-300 text-sm italic leading-relaxed mb-4">
                  "Hồ sơ visa của mình làm tự do ban đầu rất lo bị trượt, may nhờ đội ngũ hỗ trợ nhiệt tình và có cam kết hoàn 100% nên rất yên tâm. 5 ngày sau nhận visa trong tay, đi tour suôn sẻ 10/10."
                </p>
              </div>
              <div className="flex items-center gap-3 pt-3 border-t border-white/10">
                <div className="w-10 h-10 rounded-full bg-amber-500/20 text-amber-400 flex items-center justify-center font-bold text-sm">
                  T
                </div>
                <div>
                  <div className="text-sm font-semibold text-white">Anh Tuấn Minh</div>
                  <div className="text-xs text-slate-400">Software Engineer (Hà Nội)</div>
                </div>
              </div>
            </div>

            <div className="glass-panel p-6 rounded-2xl flex flex-col justify-between">
              <div>
                <div className="flex text-amber-400 text-sm mb-3">★★★★★</div>
                <p className="text-slate-300 text-sm italic leading-relaxed mb-4">
                  "Thích nhất là con AI Sakura tư vấn ngay lúc 12h đêm khi mình rảnh tra lịch bay, thông tin trả lời chính xác từng ngày khởi hành. Đặt cọc 30% giữ chỗ nhanh chóng."
                </p>
              </div>
              <div className="flex items-center gap-3 pt-3 border-t border-white/10">
                <div className="w-10 h-10 rounded-full bg-amber-500/20 text-amber-400 flex items-center justify-center font-bold text-sm">
                  H
                </div>
                <div>
                  <div className="text-sm font-semibold text-white">Chị Hoàng Yến</div>
                  <div className="text-xs text-slate-400">HR Manager (Đà Nẵng)</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* [SECTION 3] THE GRAND SLAM OFFER (Alex Hormozi $100M Value Stack) */}
      <section id="offer" className="py-20 max-w-5xl mx-auto px-4 sm:px-6">
        <div className="text-center space-y-3 mb-12">
          <span className="text-xs uppercase tracking-widest text-amber-400 font-semibold px-3 py-1 rounded-full bg-amber-500/10 border border-amber-500/30">
            Lời Đề Nghị Không Thể Từ Chối (Hormozi Grand Slam Offer)
          </span>
          <h2 className="font-display text-3xl sm:text-5xl font-bold text-white">
            Bảng Xếp Chồng Giá Trị Thực Tế
          </h2>
          <p className="text-slate-300 text-sm sm:text-base max-w-2xl mx-auto">
            Chúng tôi không chỉ bán một chuyến đi, chúng tôi đóng gói toàn bộ sự an tâm, trải nghiệm thượng hạng và cam kết bảo vệ rủi ro 100%.
          </p>
        </div>

        <div className="glass-panel-gold rounded-3xl p-6 sm:p-10 gold-border-glow space-y-6">
          <div className="space-y-3">
            {valueStackItems.map((item, idx) => (
              <div
                key={idx}
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
              <span className="text-xs text-slate-400 block">Tổng trị giá niêm yết: <strong className="line-through text-slate-300">38.490.000₫</strong></span>
              <div className="flex items-baseline gap-2">
                <span className="font-display text-4xl sm:text-5xl font-bold text-amber-400">
                  28.990.000 ₫
                </span>
                <span className="text-xs text-emerald-400 font-semibold uppercase px-2 py-0.5 rounded bg-emerald-500/20 border border-emerald-500/30">
                  Tiết kiệm 9.500.000₫
                </span>
              </div>
            </div>

            <button
              onClick={() => {
                alert('Đã ghi nhận yêu cầu đặt chỗ. Chuyên viên tư vấn sẽ liên hệ ngay!');
              }}
              className="w-full sm:w-auto px-8 py-4 rounded-2xl bg-gradient-to-r from-amber-500 to-amber-400 hover:brightness-110 text-[#080c14] font-bold text-base shadow-xl shadow-amber-500/30 transition-all flex items-center justify-center gap-2 active:scale-95"
            >
              <span>Đặt Cọc Giữ Suất Bay (8.697.000₫)</span>
              <span>→</span>
            </button>
          </div>
        </div>
      </section>

      {/* [SECTION 4] ITINERARY SECTION */}
      <section id="itinerary" className="py-20 bg-[#0f172a]/40 border-y border-white/5">
        <div className="max-w-6xl mx-auto px-4 sm:px-6">
          <div className="text-center space-y-3 mb-14">
            <span className="text-xs uppercase tracking-widest text-amber-400 font-semibold">
              Hành Trình Tinh Hoa
            </span>
            <h2 className="font-display text-3xl sm:text-5xl font-bold text-white">
              Chi Tiết Lịch Trình 6 Ngày 5 Đêm
            </h2>
            <p className="text-slate-300 text-sm max-w-xl mx-auto">
              Nhịp độ thư thả, không vội vã, cân bằng giữa thắng cảnh văn hóa, ẩm thực thượng hạng và thời gian nghỉ dưỡng Onsen.
            </p>
          </div>

          <div className="space-y-6">
            {itineraryDays.map((item) => (
              <div
                key={item.day}
                className="glass-panel rounded-2xl p-5 sm:p-7 flex flex-col md:flex-row gap-6 items-center"
              >
                <div className="w-full md:w-5/12 h-52 sm:h-60 rounded-xl overflow-hidden shrink-0 border border-white/10">
                  <img src={item.image} alt={item.title} className="w-full h-full object-cover" />
                </div>
                <div className="flex-1 space-y-3">
                  <div className="flex items-center gap-2">
                    <span className="px-3 py-1 rounded-lg text-xs font-bold bg-amber-500/20 text-amber-400 border border-amber-500/30 font-mono">
                      NGÀY {item.day}
                    </span>
                    <h3 className="font-bold text-white text-base sm:text-lg">{item.title}</h3>
                  </div>
                  <p className="text-slate-300 text-xs sm:text-sm leading-relaxed">{item.content}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* [SECTION 5] GODFATHER GUARANTEE */}
      <section id="godfather" className="py-20 max-w-5xl mx-auto px-4 sm:px-6">
        <div className="glass-panel-gold rounded-3xl p-8 sm:p-12 gold-border-glow text-center space-y-6">
          <div className="w-16 h-16 rounded-2xl bg-amber-500/20 text-amber-400 flex items-center justify-center text-3xl mx-auto border border-amber-500/40">
            🛡️
          </div>
          <h2 className="font-display text-3xl sm:text-5xl font-bold text-white">
            3 Lớp Cam Kết Godfather Tuyệt Đối
          </h2>
          <p className="text-slate-300 text-sm sm:text-base max-w-2xl mx-auto leading-relaxed">
            Chúng tôi loại bỏ 100% rủi ro cho bạn với chính sách Godfather được cam kết bằng văn bản hợp đồng pháp lý:
          </p>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 text-left pt-4">
            <div className="p-5 rounded-2xl bg-slate-900/80 border border-white/5 space-y-2">
              <span className="text-amber-400 font-bold text-lg block">Lớp 1: Rớt Visa Hoàn 100%</span>
              <p className="text-xs text-slate-300 leading-relaxed">
                Hoàn trả toàn bộ 100% tiền cọc và lệ phí visa trong 3 ngày nếu hồ sơ không được duyệt. Không thu bất kỳ khoản phí phạt nào.
              </p>
            </div>
            <div className="p-5 rounded-2xl bg-slate-900/80 border border-white/5 space-y-2">
              <span className="text-amber-400 font-bold text-lg block">Lớp 2: Đúng Chuẩn Khách Sạn</span>
              <p className="text-xs text-slate-300 leading-relaxed">
                Cam kết đúng chuẩn khách sạn 3-4 sao trung tâm, view Onsen truyền thống. Nếu không đúng cam kết, bồi hoàn ngay 5.000.000₫/khách.
              </p>
            </div>
            <div className="p-5 rounded-2xl bg-slate-900/80 border border-white/5 space-y-2">
              <span className="text-amber-400 font-bold text-lg block">Lớp 3: Hỗ Trợ 24/7 Bằng AI & Sale</span>
              <p className="text-xs text-slate-300 leading-relaxed">
                Đồng hành cùng khách hàng từ trước chuyến đi, phiên dịch menu tiếng Nhật, hướng dẫn mua sắm qua kênh chat riêng.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* [SECTION 6] DEPARTURES TABLE */}
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
                  <th className="py-3.5 px-5">Hãng Hàng Không</th>
                  <th className="py-3.5 px-5">Giá Người Lớn</th>
                  <th className="py-3.5 px-5">Tình Trạng</th>
                  <th className="py-3.5 px-5 text-right">Hành Động</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-white/5">
                {departuresList.map((dep, idx) => (
                  <tr key={idx} className="hover:bg-slate-900/50 transition">
                    <td className="py-4 px-5 font-semibold text-white">{dep.date}</td>
                    <td className="py-4 px-5 text-slate-300">{dep.airline}</td>
                    <td className="py-4 px-5 font-bold text-amber-400 font-mono">{dep.price}</td>
                    <td className="py-4 px-5">
                      <span className="px-2.5 py-1 rounded-full text-[11px] font-bold uppercase bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                        {dep.status}
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

      {/* [SECTION 7] FAQ ACCORDION */}
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
          {faqs.map((faq, idx) => (
            <div key={idx} className="glass-panel rounded-2xl overflow-hidden border border-white/10">
              <button
                onClick={() => setActiveFaq(activeFaq === idx ? null : idx)}
                className="w-full p-5 text-left font-semibold text-white flex items-center justify-between gap-4 hover:text-amber-400 transition"
              >
                <span className="text-sm sm:text-base">{faq.q}</span>
                <span className="text-xl font-mono">{activeFaq === idx ? '−' : '+'}</span>
              </button>
              {activeFaq === idx && (
                <div className="px-5 pb-5 text-xs sm:text-sm text-slate-300 leading-relaxed border-t border-white/5 pt-3">
                  {faq.a}
                </div>
              )}
            </div>
          ))}
        </div>
      </section>

      {/* FOOTER */}
      <footer className="border-t border-white/10 bg-[#0e1422] py-10 px-4 sm:px-8 text-center text-xs text-slate-400 space-y-3">
        <div className="flex flex-wrap items-center justify-center gap-6 text-slate-300">
          <a href="#itinerary" className="hover:text-amber-400">Lịch Trình</a>
          <a href="#offer" className="hover:text-amber-400">Grand Slam Offer</a>
          <a href="#godfather" className="hover:text-amber-400">Cam Kết 3 Lớp</a>
          <a href="#departures" className="hover:text-amber-400">Đợt Khởi Hành</a>
          <Link to="/admin" className="text-amber-400 font-semibold hover:underline">
            Quản Trị Hệ Thống (Admin)
          </Link>
        </div>
        <p className="text-slate-500 text-[11px] pt-2">
          © 2026 AI Agent Travel • Hệ Thống Du Lịch 4 Chạm Thảnh Thơi • .NET 10 Clean Architecture Core.
        </p>
      </footer>

      {/* FLOATING AI SAKURA CHAT WIDGET */}
      <FloatingChatWidget
        isOpen={isChatOpen}
        onClose={() => setIsChatOpen(false)}
        selectedTourContext={currentTour}
      />

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
