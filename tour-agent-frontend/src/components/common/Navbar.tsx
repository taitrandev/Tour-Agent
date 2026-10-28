import React from 'react';
import { Compass, ShieldCheck, Sparkles, LayoutDashboard, UserCheck } from 'lucide-react';

interface NavbarProps {
  currentView: 'client' | 'admin';
  onViewChange: (view: 'client' | 'admin') => void;
  openChatWidget: () => void;
}

/**
 * Thanh điều hướng toàn cục của hệ thống với chuyển đổi chế độ Khách ⟷ Quản trị.
 */
export const Navbar: React.FC<NavbarProps> = ({ currentView, onViewChange, openChatWidget }) => {
  return (
    <header className="sticky top-0 z-50 glass-panel border-b border-white/10 px-4 sm:px-8 py-3.5 backdrop-blur-xl">
      <div className="max-w-7xl mx-auto flex items-center justify-between">
        {/* Brand Logo & Tagline */}
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-cyan-500 to-sky-400 flex items-center justify-center shadow-lg shadow-cyan-500/25">
            <Compass className="w-6 h-6 text-slate-950 stroke-[2.5]" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="font-tech text-lg font-bold tracking-tight text-white">
                TOUR<span className="text-cyan-400">AGENT</span>
              </span>
              <span className="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded-full bg-cyan-500/15 text-cyan-300 border border-cyan-500/30">
                .NET 10 CORE
              </span>
            </div>
            <p className="text-[11px] text-slate-400 hidden sm:block">
              Hệ Thống Du Lịch 4 Chạm Thảnh Thơi • AI-Human Hybrid Protocol
            </p>
          </div>
        </div>

        {/* Action Controls & Mode Switcher */}
        <div className="flex items-center gap-3">
          {/* Quick AI Consultant Trigger */}
          <button
            onClick={openChatWidget}
            className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg text-xs font-semibold bg-cyan-500/10 hover:bg-cyan-500/20 text-cyan-300 border border-cyan-500/30 transition shadow-sm hover:shadow-cyan-500/20"
            title="Mở hộp thoại tư vấn AI DeepSeek V3"
          >
            <Sparkles className="w-3.5 h-3.5 text-cyan-400 animate-pulse" />
            <span className="hidden md:inline">AI Consultant</span>
          </button>

          {/* Mode Switcher Button */}
          <div className="flex items-center p-1 rounded-xl bg-slate-900/80 border border-white/10">
            <button
              onClick={() => onViewChange('client')}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold transition ${
                currentView === 'client'
                  ? 'bg-gradient-to-r from-cyan-500 to-sky-500 text-slate-950 font-bold shadow-md'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <UserCheck className="w-3.5 h-3.5" />
              <span>Du Khách</span>
            </button>
            <button
              onClick={() => onViewChange('admin')}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold transition ${
                currentView === 'admin'
                  ? 'bg-gradient-to-r from-amber-500 to-yellow-500 text-slate-950 font-bold shadow-md'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              <LayoutDashboard className="w-3.5 h-3.5" />
              <span>Quản Trị</span>
            </button>
          </div>

          {/* Core Health Indicator */}
          <div className="hidden lg:flex items-center gap-2 pl-2 border-l border-white/10 text-xs text-slate-400">
            <div className="w-2 h-2 rounded-full bg-emerald-400 animate-ping" />
            <ShieldCheck className="w-4 h-4 text-emerald-400" />
            <span className="font-mono text-[11px] text-emerald-300">Clean Arch 4 Layers</span>
          </div>
        </div>
      </div>
    </header>
  );
};
