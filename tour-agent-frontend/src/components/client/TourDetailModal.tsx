import React, { useState } from 'react';
import { X, Calendar, MapPin, CheckCircle, AlertCircle, ShieldAlert, Sparkles, PhoneCall } from 'lucide-react';
import type { TourDetail } from '../../types/tour';

interface TourDetailModalProps {
  tour: TourDetail | null;
  isOpen: boolean;
  onClose: () => void;
  onConsultWithAi: (tour: TourDetail) => void;
}

/**
 * Modal hiển thị chi tiết lịch trình, đợt bay, chính sách và cam kết Godfather của Tour.
 */
export const TourDetailModal: React.FC<TourDetailModalProps> = ({
  tour,
  isOpen,
  onClose,
  onConsultWithAi,
}) => {
  const [activeTab, setActiveTab] = useState<'itinerary' | 'departures' | 'policies'>('itinerary');

  if (!isOpen || !tour) return null;

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('vi-VN').format(amount) + ' ₫';
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-6 overflow-y-auto backdrop-blur-md bg-black/70 animate-in fade-in duration-200">
      <div className="glass-panel w-full max-w-4xl rounded-2xl overflow-hidden shadow-2xl border border-white/20 my-auto flex flex-col max-h-[90vh]">
        {/* Modal Header */}
        <div className="relative p-6 border-b border-white/10 bg-slate-900/60 flex items-start justify-between">
          <div className="space-y-1 pr-6">
            <div className="flex items-center gap-2">
              <span className="text-xs font-mono uppercase px-2.5 py-0.5 rounded bg-cyan-500/20 text-cyan-300 border border-cyan-500/30">
                {tour.code || 'TOUR-EXCLUSIVE'}
              </span>
              <span className="text-xs text-slate-400 flex items-center gap-1">
                <MapPin className="w-3.5 h-3.5 text-cyan-400" />
                {tour.destination || tour.country}
              </span>
            </div>
            <h2 className="text-xl sm:text-2xl font-bold text-white leading-snug">
              {tour.name}
            </h2>
            <div className="flex items-center gap-4 text-xs text-slate-300 pt-1">
              <span>Khởi hành: <strong>{tour.departureCity || 'TP.HCM / Hà Nội'}</strong></span>
              <span>Thời lượng: <strong>{tour.durationDays} Ngày {tour.durationNights} Đêm</strong></span>
              <span className="text-gold-gradient font-bold text-sm">
                Giá từ {formatCurrency(tour.priceFrom)}
              </span>
            </div>
          </div>

          <button
            onClick={onClose}
            className="p-2 rounded-xl glass-input hover:border-slate-500 text-slate-400 hover:text-white transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Tab Navigation */}
        <div className="flex border-b border-white/10 bg-slate-950/40 px-6 gap-6 text-sm font-semibold">
          <button
            onClick={() => setActiveTab('itinerary')}
            className={`py-3 border-b-2 transition ${
              activeTab === 'itinerary'
                ? 'border-cyan-400 text-cyan-300'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            Lịch Trình Chi Tiết ({tour.itinerary?.length || 0} Ngày)
          </button>
          <button
            onClick={() => setActiveTab('departures')}
            className={`py-3 border-b-2 transition ${
              activeTab === 'departures'
                ? 'border-cyan-400 text-cyan-300'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            Lịch Khởi Hành & Giá Vé ({tour.departures?.length || 0})
          </button>
          <button
            onClick={() => setActiveTab('policies')}
            className={`py-3 border-b-2 transition ${
              activeTab === 'policies'
                ? 'border-cyan-400 text-cyan-300'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            Cam Kết & Chính Sách
          </button>
        </div>

        {/* Modal Scrollable Body */}
        <div className="p-6 overflow-y-auto space-y-6 flex-1 text-slate-200 text-sm">
          {/* TAB 1: ITINERARY */}
          {activeTab === 'itinerary' && (
            <div className="space-y-6">
              {tour.itinerary && tour.itinerary.length > 0 ? (
                tour.itinerary.map((day) => (
                  <div key={day.day} className="glass-card rounded-xl p-4 sm:p-5 space-y-3">
                    <div className="flex items-center gap-2">
                      <span className="w-8 h-8 rounded-lg bg-cyan-500/20 text-cyan-300 font-bold flex items-center justify-center text-xs border border-cyan-500/30">
                        N{day.day}
                      </span>
                      <h4 className="font-semibold text-slate-100 text-base">{day.title}</h4>
                    </div>
                    <p className="text-slate-300 leading-relaxed text-xs sm:text-sm pl-10">
                      {day.content}
                    </p>
                    {day.images && day.images.length > 0 && (
                      <div className="pl-10 grid grid-cols-2 gap-3 pt-2">
                        {day.images.map((img, idx) => (
                          <img
                            key={idx}
                            src={img}
                            alt={`Hành trình ngày ${day.day}`}
                            className="rounded-lg h-36 w-full object-cover border border-white/10"
                          />
                        ))}
                      </div>
                    )}
                  </div>
                ))
              ) : (
                <p className="text-slate-400">Đang cập nhật lịch trình chi tiết cho tour này.</p>
              )}
            </div>
          )}

          {/* TAB 2: DEPARTURES */}
          {activeTab === 'departures' && (
            <div className="space-y-4">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs sm:text-sm">
                  <thead className="bg-slate-900/80 text-slate-400 uppercase text-[11px] font-mono border-b border-white/10">
                    <tr>
                      <th className="py-3 px-4">Ngày Bay</th>
                      <th className="py-3 px-4">Giá Người Lớn</th>
                      <th className="py-3 px-4">Giá Trẻ Em</th>
                      <th className="py-3 px-4">Trạng Thái</th>
                      <th className="py-3 px-4">Ghi Chú</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/5">
                    {tour.departures && tour.departures.length > 0 ? (
                      tour.departures.map((dep) => (
                        <tr key={dep.id} className="hover:bg-slate-900/40 transition">
                          <td className="py-3.5 px-4 font-semibold text-slate-100 flex items-center gap-1.5">
                            <Calendar className="w-3.5 h-3.5 text-cyan-400" />
                            {dep.departureDate}
                          </td>
                          <td className="py-3.5 px-4 font-bold text-amber-400">
                            {formatCurrency(dep.priceAdult)}
                          </td>
                          <td className="py-3.5 px-4 text-slate-300">
                            {dep.priceChild ? formatCurrency(dep.priceChild) : 'Theo quy định'}
                          </td>
                          <td className="py-3.5 px-4">
                            <span
                              className={`px-2 py-0.5 rounded text-[11px] font-semibold uppercase ${
                                dep.status === 'open'
                                  ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/30'
                                  : dep.status === 'guaranteed'
                                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/30'
                                  : 'bg-rose-500/20 text-rose-300 border border-rose-500/30'
                              }`}
                            >
                              {dep.status === 'open' ? 'Còn chỗ' : dep.status === 'guaranteed' ? 'Chắc chắn bay' : 'Hết chỗ'}
                            </span>
                          </td>
                          <td className="py-3.5 px-4 text-slate-400 text-xs">{dep.notes || '—'}</td>
                        </tr>
                      ))
                    ) : (
                      <tr>
                        <td colSpan={5} className="py-4 text-center text-slate-500">
                          Chưa có đợt khởi hành nào được cấu hình.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* TAB 3: POLICIES & GUARANTEE */}
          {activeTab === 'policies' && (
            <div className="space-y-4">
              {/* Godfather Guarantee Highlight */}
              <div className="p-4 rounded-xl bg-amber-500/10 border border-amber-500/30 flex items-start gap-3">
                <ShieldAlert className="w-6 h-6 text-amber-400 shrink-0 mt-0.5" />
                <div>
                  <h4 className="font-bold text-amber-300 text-sm">
                    3 Lớp Cam Kết Godfather — An Tâm Tuyệt Đối 100%
                  </h4>
                  <p className="text-xs text-amber-200/80 mt-1 leading-relaxed">
                    {tour.cancellationPolicy ||
                      'Cam kết hoàn lại 100% tiền cọc và toàn bộ lệ phí visa nếu hồ sơ của bạn bị từ chối thị thực mà không có bất kỳ khoản phí phạt nào.'}
                  </p>
                </div>
              </div>

              {/* Inclusions & Exclusions */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2">
                <div className="glass-card rounded-xl p-4 space-y-2">
                  <h5 className="font-semibold text-emerald-400 flex items-center gap-1.5 text-xs uppercase tracking-wide">
                    <CheckCircle className="w-4 h-4" />
                    Giá Tour Bao Gồm
                  </h5>
                  <ul className="text-xs text-slate-300 space-y-1.5 pl-1">
                    {tour.inclusions?.map((inc, i) => (
                      <li key={i} className="flex items-start gap-2">
                        <span className="text-emerald-400">•</span>
                        <span>{inc}</span>
                      </li>
                    )) || <li>Vé máy bay, khách sạn 4-5 sao, bảo hiểm du lịch.</li>}
                  </ul>
                </div>

                <div className="glass-card rounded-xl p-4 space-y-2">
                  <h5 className="font-semibold text-rose-400 flex items-center gap-1.5 text-xs uppercase tracking-wide">
                    <AlertCircle className="w-4 h-4" />
                    Giá Tour Không Bao Gồm
                  </h5>
                  <ul className="text-xs text-slate-300 space-y-1.5 pl-1">
                    {tour.exclusions?.map((exc, i) => (
                      <li key={i} className="flex items-start gap-2">
                        <span className="text-rose-400">•</span>
                        <span>{exc}</span>
                      </li>
                    )) || <li>Tiền tip HDV, chi tiêu mua sắm cá nhân ngoài chương trình.</li>}
                  </ul>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Modal Footer Controls */}
        <div className="p-4 sm:p-5 border-t border-white/10 bg-slate-950/60 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <button
              onClick={() => {
                onClose();
                onConsultWithAi(tour);
              }}
              className="px-4 py-2.5 rounded-xl font-semibold text-xs bg-cyan-500/10 hover:bg-cyan-500/20 text-cyan-300 border border-cyan-500/30 flex items-center gap-2 transition"
            >
              <Sparkles className="w-4 h-4 text-cyan-400" />
              <span>Hỏi AI tư vấn tour này</span>
            </button>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={() => {
                alert(`Đã ghi nhận yêu cầu đặt cọc cho tour: ${tour.name}. Chuyên viên tư vấn sẽ liên hệ ngay!`);
                onClose();
              }}
              className="px-6 py-2.5 rounded-xl font-bold text-xs bg-gradient-to-r from-amber-500 to-yellow-500 hover:from-amber-400 hover:to-yellow-400 text-slate-950 flex items-center gap-2 shadow-lg shadow-amber-500/25 transition active:scale-95"
            >
              <PhoneCall className="w-4 h-4" />
              <span>Đặt Cọc Giữ Chỗ (30%)</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
