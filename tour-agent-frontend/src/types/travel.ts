/**
 * Kiểu dữ liệu một đợt khởi hành của Tour du lịch.
 */
export interface TourDeparture {
  id?: number;
  departure_date: string;
  price_adult: number;
  price_child?: number | null;
  single_supplement?: number | null;
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
  travel_id: number;
  reviewer_name: string;
  reviewer_avatar_url?: string | null;
  rating: number;
  content: string;
  travel_date?: string | null;
  is_visible: boolean;
  created_at?: string;
}

/**
 * Kiểu dữ liệu câu hỏi thường gặp (FAQ) gắn với Tour.
 */
export interface TravelFaq {
  id: number;
  travel_id: number;
  question: string;
  answer: string;
  sort_order: number;
  is_visible: boolean;
  created_at?: string;
}

/**
 * Kiểu dữ liệu Sản phẩm Tour du lịch hoàn chỉnh (Product Base).
 */
export interface TravelProduct {
  id: number;
  title: string;
  slug?: string;
  code: string;
  duration_days: number;
  duration_nights: number;
  route?: string;
  country: string;
  destinations: string[];
  departure_city: string;
  tags?: string[];
  price_from: number;
  original_price?: number;
  status: 'published' | 'draft';
  is_featured: boolean;
  itinerary?: ItineraryDay[];
  inclusions?: string[] | string;
  exclusions?: string[] | string;
  child_policy?: string;
  payment_policy?: string;
  cancellation_policy?: string;
  visa_policy?: string;
  notes?: string;
  departures?: TourDeparture[];
  offers_count?: number;
  reviews_count?: number;
  faqs_count?: number;
}
