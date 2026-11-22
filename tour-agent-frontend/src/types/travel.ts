/**
 * Kiểu dữ liệu một đợt khởi hành của Tour du lịch (chuẩn camelCase).
 */
export interface TourDeparture {
  id?: number;
  departureDate?: string;
  priceAdult?: number;
  priceChild?: number | null;
  singleSupplement?: number | null;
  status: 'open' | 'guaranteed' | 'full' | 'closed';
  notes?: string | null;
}

/**
 * Kiểu dữ liệu một ngày trong lịch trình tour.
 */
export interface ItineraryDay {
  day: number;
  title: string;
  route?: string;
  content: string;
  images?: string[];
}

/**
 * Kiểu dữ liệu một đánh giá của khách hàng cho Tour.
 */
export interface TravelReview {
  id: number;
  travelId: number;
  reviewerName: string;
  reviewerAvatarUrl?: string | null;
  rating: number;
  content: string;
  travelDate?: string | null;
  isVisible: boolean;
  createdAt?: string;
}

/**
 * Kiểu dữ liệu câu hỏi thường gặp (FAQ) gắn với Tour.
 */
export interface TravelFaq {
  id: number;
  travelId: number;
  question: string;
  answer: string;
  sortOrder: number;
  isVisible: boolean;
  createdAt?: string;
}

/**
 * Kiểu dữ liệu Sản phẩm Tour du lịch hoàn chỉnh (chuẩn camelCase).
 */
export interface TravelProduct {
  id: number;
  title: string;
  slug?: string;
  code: string;
  durationDays: number;
  durationNights: number;
  route?: string;
  country: string;
  destinations: string[];
  departureCity: string;
  tags?: string[];
  priceFrom: number;
  originalPrice?: number;
  status: 'published' | 'draft';
  isFeatured: boolean;
  itinerary?: ItineraryDay[];
  inclusions?: string[] | string;
  exclusions?: string[] | string;
  childPolicy?: string;
  paymentPolicy?: string;
  cancellationPolicy?: string;
  visaPolicy?: string;
  notes?: string;
  departures?: TourDeparture[];
  offersCount?: number;
  reviewsCount?: number;
  faqsCount?: number;
}
