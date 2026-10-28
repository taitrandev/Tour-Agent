/**
 * Định nghĩa kiểu dữ liệu đại diện cho tóm tắt thông tin Tour du lịch hiển thị trên danh mục.
 */
export interface TourSummary {
  id: number;
  code?: string;
  name: string;
  destination?: string;
  country?: string;
  departureCity?: string;
  durationDays: number;
  durationNights: number;
  priceFrom: number;
  currency: string;
  highlights?: string;
  nextDepartureDate?: string;
  departuresCount: number;
  imageUrl?: string;
}

/**
 * Kết quả phân trang danh sách Tour trả về từ Backend API.
 */
export interface SearchToursResult {
  items: TourSummary[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/**
 * Bộ lọc tìm kiếm Tour du lịch.
 */
export interface SearchToursFilter {
  destination?: string;
  country?: string;
  departureCity?: string;
  maxPrice?: number;
  search?: string;
  page?: number;
  pageSize?: number;
}

/**
 * Chi tiết đợt khởi hành cụ thể của Tour.
 */
export interface TourDepartureItem {
  id: number;
  departureDate: string;
  priceAdult: number;
  priceChild?: number;
  singleSupplement?: number;
  status: 'open' | 'guaranteed' | 'full' | 'closed';
  notes?: string;
}

/**
 * Chi tiết một ngày trong lịch trình tham quan.
 */
export interface ItineraryDay {
  day: number;
  title: string;
  content: string;
  images?: string[];
}

/**
 * Chi tiết đầy đủ của một Tour phục vụ xem chi tiết & đặt chỗ.
 */
export interface TourDetail extends TourSummary {
  itinerary?: ItineraryDay[];
  departures?: TourDepartureItem[];
  inclusions?: string[];
  exclusions?: string[];
  childPolicy?: string;
  paymentPolicy?: string;
  cancellationPolicy?: string;
  visaPolicy?: string;
  notes?: string;
}
