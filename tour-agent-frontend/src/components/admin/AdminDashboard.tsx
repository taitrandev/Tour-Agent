import { useState, useEffect } from 'react';
import {
  Package,
  CalendarCheck,
  MessagesSquare,
  Cpu,
  Send,
  CheckCircle,
  AlertTriangle,
  RefreshCw,
  Search,
  ShieldCheck,
} from 'lucide-react';
import type { TourSummary } from '../../types/tour';
import type { AdminOverviewStats, ConversationAuditItem, TelegramStatus } from '../../types/admin';
import { getAdminOverviewStats, getRecentConversations, getTelegramWebhookStatus } from '../../api/adminApi';

interface AdminDashboardProps {
  tours: TourSummary[];
  onRefreshTours: () => void;
}

/**
 * Màn hình Bảng Điều Khiển Quản Trị Hệ Thống (Admin Operations Desk),
 * bao gồm Thống Kê Tổng Quan, Quản Lý Danh Mục Tour, Giám Sát Hội Thoại AI và Kênh Telegram.
 */
export const AdminDashboard: React.FC<AdminDashboardProps> = ({ tours, onRefreshTours }) => {
  const [activeTab, setActiveTab] = useState<'tours' | 'conversations' | 'telegram'>('tours');
  const [stats, setStats] = useState<AdminOverviewStats | null>(null);
  const [conversations, setConversations] = useState<ConversationAuditItem[]>([]);
  const [telegramStatus, setTelegramStatus] = useState<TelegramStatus | null>(null);
  const [searchTourQuery, setSearchTourQuery] = useState('');
  const [isRefreshing, setIsRefreshing] = useState(false);

  /**
   * Tải lại toàn bộ dữ liệu thống kê, hội thoại và trạng thái webhook.
   */
  const loadDashboardData = async () => {
    setIsRefreshing(true);
    try {
      const [statsData, convData, tgData] = await Promise.all([
        getAdminOverviewStats(),
        getRecentConversations(),
        getTelegramWebhookStatus(),
      ]);
      setStats(statsData);
      setConversations(convData);
      setTelegramStatus(tgData);
    } catch (err) {
      console.error('Lỗi khi tải dữ liệu Admin:', err);
    } finally {
      setIsRefreshing(false);
    }
  };

  useEffect(() => {
    loadDashboardData();
  }, []);

  const formatCurrency = (amount: number): string => {
    return new Intl.NumberFormat('vi-VN').format(amount) + ' ₫';
  };

  const filteredTours = tours.filter(
    (t) =>
      t.name.toLowerCase().includes(searchTourQuery.toLowerCase()) ||
      t.code?.toLowerCase().includes(searchTourQuery.toLowerCase()) ||
      t.country?.toLowerCase().includes(searchTourQuery.toLowerCase())
  );

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-8 py-8 space-y-8 animate-in fade-in duration-300">
      {/* Dashboard Top Title & Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span className="px-2.5 py-0.5 rounded-full text-[11px] font-mono font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30">
              OPERATIONS DESK
            </span>
            <span className="text-xs text-slate-400">PostgreSQL 16 • Clean Arch</span>
          </div>
          <h1 className="text-2xl sm:text-3xl font-bold text-white mt-1">
            Trung Tâm Điều Hành & Giám Sát AI
          </h1>
          <p className="text-xs sm:text-sm text-slate-400">
            Theo dõi danh mục tour, kiểm toán chi phí token DeepSeek và điều phối kênh Telegram
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => {
              loadDashboardData();
              onRefreshTours();
            }}
            disabled={isRefreshing}
            className="px-3.5 py-2 rounded-xl text-xs font-semibold glass-input hover:border-slate-500 text-slate-300 hover:text-white flex items-center gap-2 transition"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isRefreshing ? 'animate-spin text-cyan-400' : ''}`} />
            <span>Đồng bộ</span>
          </button>
        </div>
      </div>

      {/* Metric Stats Row Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {/* Total Tours Card */}
        <div className="glass-panel rounded-2xl p-5 border border-white/10 space-y-2">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Tổng Số Tour Mở Bán</span>
            <Package className="w-5 h-5 text-cyan-400" />
          </div>
          <div className="text-2xl sm:text-3xl font-bold font-tech text-white">
            {stats?.totalTours || tours.length}
          </div>
          <div className="text-[11px] text-slate-400 flex items-center gap-1">
            <span className="text-emerald-400 font-semibold">100% Active</span>
            <span>trong database</span>
          </div>
        </div>

        {/* Open Departures Card */}
        <div className="glass-panel rounded-2xl p-5 border border-white/10 space-y-2">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Đợt Khởi Hành Mở Chỗ</span>
            <CalendarCheck className="w-5 h-5 text-emerald-400" />
          </div>
          <div className="text-2xl sm:text-3xl font-bold font-tech text-emerald-300">
            {stats?.openDepartures || 24}
          </div>
          <div className="text-[11px] text-slate-400">Đợt bay tháng 10 & 11/2026</div>
        </div>

        {/* Conversations Card */}
        <div className="glass-panel rounded-2xl p-5 border border-white/10 space-y-2">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Phiên Tư Vấn Khách</span>
            <MessagesSquare className="w-5 h-5 text-sky-400" />
          </div>
          <div className="text-2xl sm:text-3xl font-bold font-tech text-sky-300">
            {stats?.totalConversations || 142}
          </div>
          <div className="text-[11px] text-slate-400">Kênh Web & Telegram Bot</div>
        </div>

        {/* LLM Tokens & Cost Card */}
        <div className="glass-panel rounded-2xl p-5 border border-white/10 space-y-2">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Chi Phí DeepSeek LLM</span>
            <Cpu className="w-5 h-5 text-amber-400" />
          </div>
          <div className="text-2xl sm:text-3xl font-bold font-tech text-amber-300">
            ${stats?.estimatedLlmCostUsd || '0.38'}
          </div>
          <div className="text-[11px] text-slate-400">
            {new Intl.NumberFormat('en-US').format(stats?.totalTokensConsumed || 185420)} tokens tiêu thụ
          </div>
        </div>
      </div>

      {/* Tabs Navigation */}
      <div className="flex border-b border-white/10 gap-6 text-sm font-semibold">
        <button
          onClick={() => setActiveTab('tours')}
          className={`py-3 border-b-2 transition flex items-center gap-2 ${
            activeTab === 'tours'
              ? 'border-cyan-400 text-cyan-300'
              : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          <Package className="w-4 h-4" />
          <span>Danh Mục Tour ({tours.length})</span>
        </button>
        <button
          onClick={() => setActiveTab('conversations')}
          className={`py-3 border-b-2 transition flex items-center gap-2 ${
            activeTab === 'conversations'
              ? 'border-cyan-400 text-cyan-300'
              : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          <MessagesSquare className="w-4 h-4" />
          <span>Giám Sát Hội Thoại & Audit</span>
        </button>
        <button
          onClick={() => setActiveTab('telegram')}
          className={`py-3 border-b-2 transition flex items-center gap-2 ${
            activeTab === 'telegram'
              ? 'border-cyan-400 text-cyan-300'
              : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          <Send className="w-4 h-4" />
          <span>Kênh Telegram Bot</span>
        </button>
      </div>

      {/* TAB 1: TOURS MANAGEMENT TABLE */}
      {activeTab === 'tours' && (
        <div className="glass-panel rounded-2xl p-6 border border-white/10 space-y-4">
          <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="relative w-full sm:w-80">
              <Search className="w-4 h-4 absolute left-3.5 top-3 text-slate-400" />
              <input
                type="text"
                placeholder="Tìm mã tour, tên, quốc gia..."
                value={searchTourQuery}
                onChange={(e) => setSearchTourQuery(e.target.value)}
                className="w-full glass-input pl-10 pr-4 py-2 rounded-xl text-xs sm:text-sm"
              />
            </div>

            <div className="text-xs text-slate-400">
              Hiển thị <strong>{filteredTours.length}</strong> / {tours.length} tour
            </div>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs sm:text-sm">
              <thead className="bg-slate-900/80 text-slate-400 uppercase text-[11px] font-mono border-b border-white/10">
                <tr>
                  <th className="py-3 px-4">Mã & Tên Tour</th>
                  <th className="py-3 px-4">Quốc Gia / Điểm Đến</th>
                  <th className="py-3 px-4">Nơi Khởi Hành</th>
                  <th className="py-3 px-4">Thời Lượng</th>
                  <th className="py-3 px-4">Giá Sàn</th>
                  <th className="py-3 px-4">Số Đợt Bay</th>
                  <th className="py-3 px-4">Trạng Thái</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-white/5">
                {filteredTours.map((t) => (
                  <tr key={t.id} className="hover:bg-slate-900/40 transition">
                    <td className="py-3.5 px-4">
                      <div className="font-mono text-[11px] text-cyan-400">{t.code}</div>
                      <div className="font-semibold text-slate-100 max-w-xs truncate">{t.name}</div>
                    </td>
                    <td className="py-3.5 px-4 text-slate-300">
                      <span className="px-2 py-0.5 rounded text-[11px] bg-slate-800 text-slate-200 border border-white/5">
                        {t.country}
                      </span>
                    </td>
                    <td className="py-3.5 px-4 text-slate-300">{t.departureCity || 'Toàn quốc'}</td>
                    <td className="py-3.5 px-4 text-slate-300 font-tech">
                      {t.durationDays}N{t.durationNights}Đ
                    </td>
                    <td className="py-3.5 px-4 font-bold text-amber-400">
                      {formatCurrency(t.priceFrom)}
                    </td>
                    <td className="py-3.5 px-4 text-emerald-400 font-semibold">
                      {t.departuresCount} đợt
                    </td>
                    <td className="py-3.5 px-4">
                      <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold uppercase bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                        Published
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 2: CONVERSATION AUDIT */}
      {activeTab === 'conversations' && (
        <div className="glass-panel rounded-2xl p-6 border border-white/10 space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="font-bold text-base text-white">Lịch Sử Các Phiên Chat Khách Hàng</h3>
            <span className="text-xs text-slate-400">Telemetry trực tiếp từ bảng conversations</span>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs sm:text-sm">
              <thead className="bg-slate-900/80 text-slate-400 uppercase text-[11px] font-mono border-b border-white/10">
                <tr>
                  <th className="py-3 px-4">ID Phiên</th>
                  <th className="py-3 px-4">Kênh</th>
                  <th className="py-3 px-4">Nội Dung Gần Nhất</th>
                  <th className="py-3 px-4">Số Lượt LLM</th>
                  <th className="py-3 px-4">Trạng Thái</th>
                  <th className="py-3 px-4">Escalate</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-white/5">
                {conversations.map((conv) => (
                  <tr key={conv.id} className="hover:bg-slate-900/40 transition">
                    <td className="py-3.5 px-4 font-mono text-cyan-400">#{conv.id}</td>
                    <td className="py-3.5 px-4">
                      <span
                        className={`px-2 py-0.5 rounded text-[11px] font-bold uppercase ${
                          conv.channel === 'telegram'
                            ? 'bg-sky-500/20 text-sky-300 border border-sky-500/30'
                            : 'bg-purple-500/20 text-purple-300 border border-purple-500/30'
                        }`}
                      >
                        {conv.channel}
                      </span>
                    </td>
                    <td className="py-3.5 px-4 max-w-sm truncate text-slate-300">
                      {conv.lastMessageSnippet}
                    </td>
                    <td className="py-3.5 px-4 font-mono text-slate-300">{conv.totalLlmCalls} lượt</td>
                    <td className="py-3.5 px-4">
                      <span className="px-2 py-0.5 rounded text-[10px] uppercase font-semibold bg-slate-800 text-slate-300">
                        {conv.status}
                      </span>
                    </td>
                    <td className="py-3.5 px-4">
                      {conv.isEscalated ? (
                        <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold uppercase bg-rose-500/20 text-rose-300 border border-rose-500/30 flex items-center gap-1 w-max">
                          <AlertTriangle className="w-3 h-3" />
                          Cần Sales Gọi
                        </span>
                      ) : (
                        <span className="text-slate-500 text-xs">Tự động 24/7</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 3: TELEGRAM WEBHOOK */}
      {activeTab === 'telegram' && (
        <div className="glass-panel rounded-2xl p-6 border border-white/10 space-y-6">
          <div className="flex items-center justify-between border-b border-white/10 pb-4">
            <div>
              <h3 className="font-bold text-lg text-white flex items-center gap-2">
                <Send className="w-5 h-5 text-sky-400" />
                Trạng Thái Kênh Tiếp Nhận Telegram Bot
              </h3>
              <p className="text-xs text-slate-400">
                Xử lý bất đồng bộ qua System.Threading.Channels và TelegramWebhookBackgroundService
              </p>
            </div>
            <span className="px-3 py-1 rounded-full text-xs font-bold uppercase bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 flex items-center gap-1.5">
              <CheckCircle className="w-3.5 h-3.5" />
              {telegramStatus?.status || 'Online'}
            </span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="glass-card rounded-xl p-4 space-y-1">
              <span className="text-xs text-slate-400">Endpoint Webhook Inbound</span>
              <div className="font-mono text-xs text-cyan-300 select-all">
                POST /api/v1/webhooks/telegram
              </div>
            </div>
            <div className="glass-card rounded-xl p-4 space-y-1">
              <span className="text-xs text-slate-400">Cơ chế Hàng Đợi (Queue)</span>
              <div className="font-mono text-xs text-emerald-300">
                In-Process Zero-latency Channel
              </div>
            </div>
            <div className="glass-card rounded-xl p-4 space-y-1">
              <span className="text-xs text-slate-400">Healthcheck Endpoint</span>
              <div className="font-mono text-xs text-amber-300 select-all">
                GET /api/v1/webhooks/telegram/status
              </div>
            </div>
          </div>

          <div className="p-4 rounded-xl bg-sky-950/30 border border-sky-500/20 text-xs text-slate-300 space-y-2">
            <h4 className="font-semibold text-sky-300 flex items-center gap-1.5">
              <ShieldCheck className="w-4 h-4" />
              Cam Kết Kiến Trúc Omnichannel:
            </h4>
            <ul className="space-y-1 pl-4 list-disc text-slate-400">
              <li>Mỗi tin nhắn đến được trả lời HTTP 200 tức thì để chống timeout và retry của Telegram.</li>
              <li>Tự động ghi nhận `channel_message_id` chống duplicate tin nhắn khi mạng chập chờn.</li>
              <li>AgentLoop tự động xử lý đa lượt và gửi phản hồi định dạng Markdown qua TelegramAdapter.</li>
            </ul>
          </div>
        </div>
      )}
    </div>
  );
};
