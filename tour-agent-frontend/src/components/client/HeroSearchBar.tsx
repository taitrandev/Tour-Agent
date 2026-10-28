import React from 'react';
import { Search, MapPin, DollarSign, Calendar, RefreshCcw } from 'lucide-react';
import type { SearchToursFilter } from '../../types/tour';

interface HeroSearchBarProps {
  filter: SearchToursFilter;
  onFilterChange: (newFilter: SearchToursFilter) => void;
  onReset: () => void;
  totalCount: number;
}

/**
 * Thanh tìm kiếm Hero phong cách Glassmorphism với bộ lọc đa tiêu chí trực quan.
 */
export const HeroSearchBar: React.FC<HeroSearchBarProps> = ({
  filter,
  onFilterChange,
  onReset,
  totalCount,
}) => {
  const popularDestinations = ['Tất cả', 'Nhật Bản', 'Hàn Quốc', 'Đài Loan', 'Thái Lan', 'Singapore'];

  return (
    <div className="relative z-10 -mt-6 sm:-mt-10 max-w-5xl mx-auto px-4">
      <div className="glass-panel rounded-2xl p-5 sm:p-7 shadow-2xl backdrop-blur-2xl border border-white/15">
        {/* Main Search Inputs Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* 1. Điểm đến & Từ khóa */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
              <MapPin className="w-3.5 h-3.5 text-cyan-400" />
              Điểm đến / Tên tour
            </label>
            <div className="relative">
              <input
                type="text"
                placeholder="Tokyo, Seoul, Jiufen..."
                value={filter.search || ''}
                onChange={(e) => onFilterChange({ ...filter, search: e.target.value })}
                className="w-full glass-input px-3.5 py-2.5 rounded-xl text-sm placeholder-slate-500"
              />
            </div>
          </div>

          {/* 2. Thành phố khởi hành */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
              <Calendar className="w-3.5 h-3.5 text-cyan-400" />
              Nơi khởi hành
            </label>
            <select
              value={filter.departureCity || ''}
              onChange={(e) => onFilterChange({ ...filter, departureCity: e.target.value || undefined })}
              className="w-full glass-input px-3.5 py-2.5 rounded-xl text-sm bg-slate-900 text-slate-200"
            >
              <option value="">Tất cả điểm bay</option>
              <option value="TP.HCM">TP. Hồ Chí Minh</option>
              <option value="Hà Nội">Hà Nội</option>
              <option value="Đà Nẵng">Đà Nẵng</option>
            </select>
          </div>

          {/* 3. Ngân sách trần */}
          <div className="space-y-1.5">
            <label className="text-xs font-semibold text-slate-300 flex items-center gap-1.5">
              <DollarSign className="w-3.5 h-3.5 text-amber-400" />
              Ngân sách tối đa
            </label>
            <select
              value={filter.maxPrice || ''}
              onChange={(e) => onFilterChange({ ...filter, maxPrice: e.target.value ? Number(e.target.value) : undefined })}
              className="w-full glass-input px-3.5 py-2.5 rounded-xl text-sm bg-slate-900 text-slate-200"
            >
              <option value="">Mọi mức giá</option>
              <option value="15000000">Dưới 15 Triệu VNĐ</option>
              <option value="25000000">Dưới 25 Triệu VNĐ</option>
              <option value="35000000">Dưới 35 Triệu VNĐ</option>
            </select>
          </div>

          {/* 4. Thao tác lọc & Xóa */}
          <div className="space-y-1.5 flex flex-col justify-end">
            <div className="flex gap-2">
              <button
                type="button"
                className="flex-1 py-2.5 px-4 rounded-xl font-semibold text-sm bg-gradient-to-r from-cyan-500 to-sky-500 hover:from-cyan-400 hover:to-sky-400 text-slate-950 flex items-center justify-center gap-2 shadow-lg shadow-cyan-500/25 transition active:scale-95"
              >
                <Search className="w-4 h-4 stroke-[2.5]" />
                <span>Tìm ({totalCount})</span>
              </button>
              <button
                type="button"
                onClick={onReset}
                title="Đặt lại bộ lọc"
                className="p-2.5 rounded-xl glass-input hover:border-slate-500 text-slate-400 hover:text-white transition flex items-center justify-center"
              >
                <RefreshCcw className="w-4 h-4" />
              </button>
            </div>
          </div>
        </div>

        {/* Quick Country Pills */}
        <div className="mt-4 pt-4 border-t border-white/10 flex flex-wrap items-center gap-2">
          <span className="text-xs text-slate-400 mr-1 font-medium">Khám phá nhanh:</span>
          {popularDestinations.map((country) => {
            const isSelected = country === 'Tất cả' ? !filter.country : filter.country === country;
            return (
              <button
                key={country}
                type="button"
                onClick={() =>
                  onFilterChange({
                    ...filter,
                    country: country === 'Tất cả' ? undefined : country,
                  })
                }
                className={`px-3 py-1 rounded-full text-xs font-medium transition ${
                  isSelected
                    ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/50 shadow-sm'
                    : 'bg-slate-800/60 hover:bg-slate-800 text-slate-400 hover:text-slate-200 border border-white/5'
                }`}
              >
                {country}
              </button>
            );
          })}
        </div>
      </div>
    </div>
  );
};
