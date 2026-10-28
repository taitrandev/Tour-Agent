import React from 'react';
import { Calendar, Clock, MapPin, Sparkles, ArrowRight, CheckCircle2 } from 'lucide-react';
import type { TourSummary } from '../../types/tour';

interface TourCardProps {
  tour: TourSummary;
  onSelectTour: (tourId: number) => void;
  onConsultWithAi: (tour: TourSummary) => void;
}

/**
 * Thẻ hiển thị sản phẩm Tour du lịch phong cách Dark Glassmorphism cao cấp.
 */
export const TourCard: React.FC<TourCardProps> = ({ tour, onSelectTour, onConsultWithAi }) => {
  /**
   * Định dạng tiền tệ VND chuyên nghiệp.
   */
  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('vi-VN').format(amount) + ' ₫';
  };

  return (
    <div className="glass-card rounded-2xl overflow-hidden flex flex-col group border border-white/10 hover:border-cyan-500/40">
      {/* Tour Cover Image & Badges */}
      <div className="relative h-52 sm:h-56 overflow-hidden">
        <img
          src={tour.imageUrl || 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?q=80&w=800&auto=format&fit=crop'}
          alt={tour.name}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
          loading="lazy"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-slate-950 via-slate-950/20 to-transparent" />

        {/* Top Badges */}
        <div className="absolute top-3 left-3 flex items-center gap-2">
          <span className="px-2.5 py-1 rounded-lg text-xs font-semibold bg-slate-950/80 backdrop-blur-md text-cyan-300 border border-cyan-500/30">
            {tour.country || 'Quốc tế'}
          </span>
          {tour.departuresCount > 0 && (
            <span className="px-2.5 py-1 rounded-lg text-xs font-semibold bg-emerald-500/20 backdrop-blur-md text-emerald-300 border border-emerald-500/30 flex items-center gap-1">
              <CheckCircle2 className="w-3 h-3" />
              {tour.departuresCount} đợt bay
            </span>
          )}
        </div>

        {/* Duration Badge */}
        <div className="absolute top-3 right-3">
          <span className="px-2.5 py-1 rounded-lg text-xs font-bold bg-slate-900/90 backdrop-blur-md text-slate-200 border border-white/15 flex items-center gap-1">
            <Clock className="w-3 h-3 text-cyan-400" />
            {tour.durationDays}N{tour.durationNights}Đ
          </span>
        </div>

        {/* Code Tag */}
        {tour.code && (
          <div className="absolute bottom-2 left-3">
            <span className="text-[10px] font-mono uppercase tracking-wider text-slate-400 bg-slate-950/60 px-2 py-0.5 rounded border border-white/10">
              {tour.code}
            </span>
          </div>
        )}
      </div>

      {/* Tour Content */}
      <div className="p-5 flex-1 flex flex-col justify-between space-y-4">
        <div>
          {/* Destination */}
          <div className="flex items-center gap-1.5 text-xs text-slate-400 mb-1.5">
            <MapPin className="w-3.5 h-3.5 text-cyan-400 shrink-0" />
            <span className="truncate">{tour.destination || tour.country}</span>
            {tour.departureCity && (
              <span className="text-slate-500"> • Khởi hành từ {tour.departureCity}</span>
            )}
          </div>

          {/* Tour Title */}
          <h3 className="font-semibold text-slate-100 text-base leading-snug line-clamp-2 group-hover:text-cyan-300 transition-colors">
            {tour.name}
          </h3>

          {/* Highlights */}
          {tour.highlights && (
            <p className="mt-2 text-xs text-slate-400 line-clamp-2 leading-relaxed">
              {tour.highlights}
            </p>
          )}
        </div>

        {/* Next Departure Info */}
        <div className="pt-3 border-t border-white/10 flex items-center justify-between text-xs text-slate-400">
          <span className="flex items-center gap-1.5">
            <Calendar className="w-3.5 h-3.5 text-cyan-400" />
            Khởi hành gần nhất:
          </span>
          <span className="font-medium text-slate-300">
            {tour.nextDepartureDate || 'Liên hệ'}
          </span>
        </div>

        {/* Price & Action Buttons */}
        <div className="pt-2 flex items-center justify-between gap-3">
          <div>
            <span className="text-[11px] text-slate-400 block">Giá từ</span>
            <div className="text-lg font-bold text-gold-gradient font-tech">
              {formatCurrency(tour.priceFrom)}
            </div>
          </div>

          <div className="flex items-center gap-2">
            {/* Ask AI Button */}
            <button
              onClick={() => onConsultWithAi(tour)}
              className="p-2.5 rounded-xl bg-cyan-500/10 hover:bg-cyan-500/20 text-cyan-400 border border-cyan-500/30 transition shadow-sm hover:shadow-cyan-500/20"
              title="Hỏi AI tư vấn riêng về tour này"
            >
              <Sparkles className="w-4 h-4" />
            </button>

            {/* View Details Button */}
            <button
              onClick={() => onSelectTour(tour.id)}
              className="px-3.5 py-2 rounded-xl text-xs font-semibold bg-white/10 hover:bg-white/20 text-white border border-white/20 flex items-center gap-1.5 transition active:scale-95"
            >
              <span>Chi tiết</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
