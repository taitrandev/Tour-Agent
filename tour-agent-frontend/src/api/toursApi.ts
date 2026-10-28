import { fetchJson } from './client';
import type { SearchToursFilter, SearchToursResult, TourDetail, TourSummary } from '../types/tour';

/**
 * Danh sách Tour mẫu chất lượng cao chuẩn bị cho trường hợp Backend đang khởi động hoặc offline.
 */
export const MOCK_TOURS: TourSummary[] = [
  {
    id: 1,
    code: 'TOUR-JP-6N5D-OSAKA-TYO',
    name: 'Cung Đường Vàng Nhật Bản: Osaka - Kyoto - Phú Sĩ - Tokyo',
    destination: 'Osaka, Kyoto, Núi Phú Sĩ, Tokyo',
    country: 'Japan',
    departureCity: 'TP.HCM',
    durationDays: 6,
    durationNights: 5,
    priceFrom: 28990000,
    currency: 'VND',
    highlights: 'Bay Vietnam Airlines, Tắm suối Onsen truyền thống, Thưởng thức bò Kobe thượng hạng, Cam kết hoàn tiền 100% nếu trượt visa',
    nextDepartureDate: '2026-10-15',
    departuresCount: 4,
    imageUrl: 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?q=80&w=1200&auto=format&fit=crop',
  },
  {
    id: 2,
    code: 'TOUR-KR-5N4D-SEOUL-NAMI',
    name: 'Mùa Thu Vàng Xứ Kim Chi: Seoul - Đảo Nami - Lotte World',
    destination: 'Seoul, Đảo Nami, Tháp Namsan',
    country: 'South Korea',
    departureCity: 'Hà Nội',
    durationDays: 5,
    durationNights: 4,
    priceFrom: 16990000,
    currency: 'VND',
    highlights: 'Check-in rừng ngân hạnh Nami rực rỡ, Trải nghiệm mặc Hanbok cổ trang, Khách sạn 4 sao trung tâm Seoul',
    nextDepartureDate: '2026-10-22',
    departuresCount: 6,
    imageUrl: 'https://images.unsplash.com/photo-1538485399081-7191377e8241?q=80&w=1200&auto=format&fit=crop',
  },
  {
    id: 3,
    code: 'TOUR-TW-5N4D-TPE-KHH',
    name: 'Đài Loan Tinh Hoa: Cao Hùng - Hồ Nhật Nguyệt - Làng Cổ Jiufen',
    destination: 'Đài Bắc, Cao Hùng, Đài Trung',
    country: 'Taiwan',
    departureCity: 'TP.HCM',
    durationDays: 5,
    durationNights: 4,
    priceFrom: 13880000,
    currency: 'VND',
    highlights: 'Du thuyền thưởng ngoạn Nhật Nguyệt Đàm, Thả đèn trời phố cổ Thập Phần cầu may mắn, Tặng trà sữa chính gốc Đài Loan',
    nextDepartureDate: '2026-11-05',
    departuresCount: 3,
    imageUrl: 'https://images.unsplash.com/photo-1508248467877-aec1b08de376?q=80&w=1200&auto=format&fit=crop',
  },
  {
    id: 4,
    code: 'TOUR-TH-4N3D-BKK-PTY',
    name: 'Thái Lan Thiên Đường Nghỉ Dưỡng: Bangkok - Pattaya - Đảo Coral',
    destination: 'Bangkok, Pattaya',
    country: 'Thailand',
    departureCity: 'TP.HCM',
    durationDays: 4,
    durationNights: 3,
    priceFrom: 6990000,
    currency: 'VND',
    highlights: 'Buffet du thuyền 5 sao trên sông Chao Phraya, Chợ nổi Bốn Miền, Massage Thái cổ truyền phục hồi sinh lực',
    nextDepartureDate: '2026-10-18',
    departuresCount: 8,
    imageUrl: 'https://images.unsplash.com/photo-1508009603885-50cf7c579365?q=80&w=1200&auto=format&fit=crop',
  },
  {
    id: 5,
    code: 'TOUR-SG-MY-5N4D',
    name: 'Hành Trình 1 Chuyến Đi 2 Quốc Gia: Singapore - Malaysia',
    destination: 'Singapore, Kuala Lumpur, Malacca, Genting',
    country: 'Singapore',
    departureCity: 'Hà Nội',
    durationDays: 5,
    durationNights: 4,
    priceFrom: 12490000,
    currency: 'VND',
    highlights: 'Show ánh sáng Gardens by the Bay, Cáp treo đỉnh cao nguyên Genting, Tháp đôi Petronas biểu tượng',
    nextDepartureDate: '2026-11-12',
    departuresCount: 5,
    imageUrl: 'https://images.unsplash.com/photo-1525625293386-3f8f99389edd?q=80&w=1200&auto=format&fit=crop',
  },
  {
    id: 6,
    code: 'TOUR-CN-6N5D-TRUONGGIAGIOI',
    name: 'Kỳ Quan Avatar: Trương Gia Giới - Phượng Hoàng Cổ Trấn',
    destination: 'Trương Gia Giới, Phượng Hoàng Cổ Trấn, Vũ Lăng Nguyên',
    country: 'China',
    departureCity: 'TP.HCM',
    durationDays: 6,
    durationNights: 5,
    priceFrom: 18990000,
    currency: 'VND',
    highlights: 'Cầu kính Thiên Vân Độ ngoạn mục, Ngồi thuyền sông Đà Giang ngắm cổ trấn về đêm, Thang máy Bách Long kỳ vĩ',
    nextDepartureDate: '2026-10-30',
    departuresCount: 4,
    imageUrl: 'https://images.unsplash.com/photo-1528728329032-2972f65dfb3f?q=80&w=1200&auto=format&fit=crop',
  }
];

/**
 * Tra cứu và lọc danh sách Tour từ backend API (với fallback mock data nếu lỗi mạng).
 * @param filter Các tiêu chí tìm kiếm
 */
export async function searchTours(filter: SearchToursFilter = {}): Promise<SearchToursResult> {
  const params = new URLSearchParams();
  if (filter.destination) params.set('destination', filter.destination);
  if (filter.country) params.set('country', filter.country);
  if (filter.departureCity) params.set('departureCity', filter.departureCity);
  if (filter.maxPrice) params.set('maxPrice', filter.maxPrice.toString());
  if (filter.search) params.set('search', filter.search);
  if (filter.page) params.set('page', filter.page.toString());
  if (filter.pageSize) params.set('pageSize', filter.pageSize.toString());

  const queryString = params.toString() ? `?${params.toString()}` : '';

  try {
    const result = await fetchJson<SearchToursResult>(`/tours${queryString}`);
    if (result && result.items && result.items.length > 0) {
      return result;
    }
  } catch (error) {
    console.warn('Backend API /tours không phản hồi hoặc trả về rỗng, dùng dữ liệu hiển thị dự phòng:', error);
  }

  // Fallback lọc trên mock data để demo luôn trơn tru
  let filtered = [...MOCK_TOURS];
  if (filter.country) {
    filtered = filtered.filter(t => t.country?.toLowerCase().includes(filter.country!.toLowerCase()));
  }
  if (filter.departureCity) {
    filtered = filtered.filter(t => t.departureCity?.toLowerCase().includes(filter.departureCity!.toLowerCase()));
  }
  if (filter.maxPrice) {
    filtered = filtered.filter(t => t.priceFrom <= filter.maxPrice!);
  }
  if (filter.search) {
    const q = filter.search.toLowerCase();
    filtered = filtered.filter(t => t.name.toLowerCase().includes(q) || t.destination?.toLowerCase().includes(q));
  }

  return {
    items: filtered,
    totalCount: filtered.length,
    page: filter.page || 1,
    pageSize: filter.pageSize || 10,
  };
}

/**
 * Lấy chi tiết lịch trình và đợt khởi hành của một tour cụ thể.
 * @param tourId Mã định danh tour
 */
export async function getTourDetail(tourId: number): Promise<TourDetail> {
  const baseTour = MOCK_TOURS.find(t => t.id === tourId) || MOCK_TOURS[0];

  return {
    ...baseTour,
    itinerary: [
      {
        day: 1,
        title: 'TP.HCM / HÀ NỘI – OSAKA / TOKYO (Nghỉ đêm trên máy bay)',
        content: 'Quý khách tập trung tại ga quốc tế. Hướng dẫn viên làm thủ tục đáp chuyến bay thẳng hàng không quốc tế. Bắt đầu hành trình khám phá tinh hoa Nhật Bản.',
        images: ['https://images.unsplash.com/photo-1542051841857-5f90071e7989?q=80&w=800&auto=format&fit=crop'],
      },
      {
        day: 2,
        title: 'CỐ ĐÔ KYOTO – RỪNG TRÚC SAGANO – CHÙA VÀNG KINKAKUJI',
        content: 'Khám phá Cố đô Kyoto thanh bình, bách bộ giữa rừng trúc Sagano xanh mướt và chiêm ngưỡng kiến trúc dát vàng kỳ vĩ tại Kinkakuji. Thưởng thức bữa trưa ẩm thực truyền thống.',
        images: ['https://images.unsplash.com/photo-1492571350019-22de08371fd3?q=80&w=800&auto=format&fit=crop'],
      },
      {
        day: 3,
        title: 'NÚI PHÚ SĨ – LÀNG CỔ OSHINO HAKKAI – TẮM ONSEN',
        content: 'Di chuyển đến biểu tượng núi Phú Sĩ linh thiêng. Dạo quanh ngôi làng cổ Oshino Hakkai dưới chân núi với nguồn nước tuyết tan thanh khiết. Trải nghiệm tắm khoáng nóng Onsen phục hồi năng lượng.',
        images: ['https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?q=80&w=800&auto=format&fit=crop'],
      },
      {
        day: 4,
        title: 'THỦ ĐÔ TOKYO – CHÙA ASAKUSA KANNON – KHU PHỐ ĐIỆN TỬ AKIHABARA',
        content: 'Tham quan ngôi chùa cổ nhất Tokyo Asakusa Kannon. Mua sắm tự do tại thiên đường sầm uất Ginza và khu phố điện tử công nghệ Akihabara.',
        images: ['https://images.unsplash.com/photo-1503899036084-c55cdd92da26?q=80&w=800&auto=format&fit=crop'],
      }
    ],
    departures: [
      { id: 101, departureDate: '2026-10-15', priceAdult: 28990000, priceChild: 24500000, singleSupplement: 6500000, status: 'open', notes: 'Giờ bay đẹp sáng sớm, Vietnam Airlines' },
      { id: 102, departureDate: '2026-10-25', priceAdult: 29500000, priceChild: 25000000, singleSupplement: 6500000, status: 'guaranteed', notes: 'Chắc chắn khởi hành, còn 4 chỗ cuối' },
      { id: 103, departureDate: '2026-11-10', priceAdult: 28990000, priceChild: 24500000, singleSupplement: 6500000, status: 'open', notes: 'Ngắm lá vàng rực rỡ cao điểm' },
      { id: 104, departureDate: '2026-11-24', priceAdult: 27990000, priceChild: 23800000, singleSupplement: 6500000, status: 'open', notes: 'Ưu đãi đặt sớm cho nhóm từ 4 khách' },
    ],
    inclusions: [
      'Vé máy bay khứ hồi quốc tế bao gồm 23kg hành lý ký gửi + 10kg xách tay',
      'Khách sạn tiêu chuẩn 4-5 sao quốc tế (2 khách/phòng)',
      'Xe du lịch đời mới đưa đón suốt hành trình',
      'Bảo hiểm du lịch quốc tế hạn mức lên tới 1.000.000.000 VNĐ',
      'Vé tham quan tất cả các địa điểm trong lịch trình',
    ],
    exclusions: [
      'Tiền bồi dưỡng (Tip) cho lái xe và HDV địa phương (khoảng 8 USD/ngày/khách)',
      'Chi phí làm hộ chiếu và các chi tiêu cá nhân ngoài chương trình',
    ],
    cancellationPolicy: 'Cam kết hoàn lại 100% tiền cọc và phí visa nếu khách hàng bị từ chối cấp thị thực (Rớt visa không mất phí).',
    paymentPolicy: 'Đặt cọc 30% khi ký hợp đồng giữ chỗ. Thanh toán 70% còn lại trước ngày khởi hành 15 ngày làm việc.',
  };
}
