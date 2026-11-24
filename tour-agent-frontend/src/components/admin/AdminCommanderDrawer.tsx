import React, { useState, useEffect, useRef } from 'react';
import { useLocation } from 'react-router-dom';
import { X, Send, Sparkles, Bot, RefreshCw, Cpu } from 'lucide-react';
import { apiFetch } from '../../api/client';
import { notify } from '../../lib/notify';
import { AGENT_API_ROUTES, ADMIN_WRITE_TOOLS } from '../../constants/agent';

interface Message {
  sender: 'user' | 'agent';
  text: string;
  time?: string;
}

interface LlmSettingInfo {
  driver?: string;
  model?: string;
  isConfigured?: boolean;
}

interface AdminCommanderDrawerProps {
  isOpen: boolean;
  onClose: () => void;
}

/**
 * Hàm tiện ích mở AI Agent Commander từ bất kỳ đâu trong ứng dụng.
 * @param prompt Câu lệnh hoặc câu hỏi gợi ý cần điền sẵn vào ô nhập
 */
export const openAgentCommander = (prompt?: string) => {
  window.dispatchEvent(new CustomEvent('open-agent-commander', { detail: { prompt } }));
};

/**
 * Trợ lý điều hành toàn cục AI Agent Commander (Global Admin Copilot).
 * Tự động nhận diện ngữ cảnh trang (Offers, Products, Orders...) và sử dụng
 * cấu hình Model AI đồng bộ với thiết lập tại /admin/settings/llm.
 */
export const AdminCommanderDrawer: React.FC<AdminCommanderDrawerProps> = ({
  isOpen,
  onClose,
}) => {
  const location = useLocation();
  const path = location.pathname;

  // Lấy thông tin LLM Setting động từ backend
  const [llmInfo, setLlmInfo] = useState<LlmSettingInfo | null>(null);
  const [loadingLlm, setLoadingLlm] = useState<boolean>(false);

  // State hội thoại
  const [messages, setMessages] = useState<Message[]>([
    {
      sender: 'agent',
      text: '🤖 Chào bạn! Tôi là AI Agent Commander. Tôi là trợ lý điều hành thông minh hỗ trợ tra cứu số liệu, tối ưu hóa Grand Slam Offer và quản trị sản phẩm.',
      time: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
    },
  ]);
  const [conversationId, setConversationId] = useState<number | null>(null);
  const [input, setInput] = useState<string>('');
  const [isTyping, setIsTyping] = useState<boolean>(false);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Tải cấu hình LLM để đồng bộ model đang dùng
  const fetchLlmSetting = async () => {
    try {
      setLoadingLlm(true);
      const res = await apiFetch('/api/v1/llm-setting');
      let json: any = null;
      try {
        json = await res.json();
      } catch {
        const text = await res.text().catch(() => '');
        throw new Error(text || `Máy chủ phản hồi mã lỗi HTTP ${res.status}`);
      }

      if (!res.ok) {
        throw new Error(json?.error || json?.message || `Máy chủ phản hồi mã lỗi HTTP ${res.status}`);
      }
      if (json.success && json.data) {
        setLlmInfo({
          driver: json.data.driver,
          model: json.data.model,
          isConfigured: json.data.isConfigured,
        });
      }
    } catch {
      // Giữ fallback nếu không kết nối được API setting
    } finally {
      setLoadingLlm(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      fetchLlmSetting();
    }
  }, [isOpen]);

  // Lắng nghe sự kiện mở kèm prompt từ các component khác
  useEffect(() => {
    const handleOpenWithPrompt = (e: any) => {
      if (e.detail?.prompt) {
        setInput(e.detail.prompt);
      }
    };
    window.addEventListener('open-agent-commander', handleOpenWithPrompt);
    return () => window.removeEventListener('open-agent-commander', handleOpenWithPrompt);
  }, []);

  // Tự động cuộn xuống tin nhắn mới nhất
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isTyping]);

  // Ngữ cảnh trang hiện tại
  const isOffersPage = path.startsWith('/admin/offers');
  const isProductsPage = path.startsWith('/admin/products');
  const isOrdersPage = path.startsWith('/admin/orders');
  const isContactsPage = path.startsWith('/admin/contacts');

  // Gợi ý lệnh nhanh theo ngữ cảnh (Context-aware quick pills)
  const quickPills = isOffersPage
    ? [
        'Đổi giá thành 3.5tr và tăng slot lên 6',
        'Thêm quà tặng bonus Gói Chụp Ảnh 2tr',
        'Đặt trạng thái gói Offer thành Active',
      ]
    : isProductsPage
    ? [
        'Hiện tại có bao nhiêu tour đang mở bán?',
        'Tour Nhật Bản có những ngày khởi hành nào?',
        'Lịch trình ngày 2 tour Nhật Bản đi đâu?',
      ]
    : isOrdersPage
    ? [
        'Tổng hợp các đơn cọc đang chờ làm Visa',
        'Có bao nhiêu đơn sẵn sàng bay trong tháng?',
      ]
    : isContactsPage
    ? [
        'Khách hàng tiềm năng nhất hiện nay là ai?',
        'Lọc các khách hàng từ kênh Telegram',
      ]
    : [
        'Tổng quan tình trạng hệ thống hôm nay',
        'Tra cứu danh mục tour du lịch',
      ];

  // Xử lý gửi tin nhắn / câu lệnh cho AI Agent
  const handleSend = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    const userText = input.trim();
    if (!userText || isTyping) return;

    const timeStr = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
    setInput('');
    setMessages((prev) => [...prev, { sender: 'user', text: userText, time: timeStr }]);
    setIsTyping(true);

    try {
      const res = await apiFetch(AGENT_API_ROUTES.ADMIN_CHAT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          conversationId,
          message: userText,
          pageContext: path,
        }),
      });
      let json: any = null;
      try {
        json = await res.json();
      } catch {
        const text = await res.text().catch(() => '');
        throw new Error(text || `Máy chủ phản hồi mã lỗi HTTP ${res.status}`);
      }

      if (!res.ok) {
        throw new Error(json?.error || json?.message || `Máy chủ phản hồi mã lỗi HTTP ${res.status}`);
      }

      // Lưu lại conversationId để giữ mạch đối thoại đa lượt (Multi-turn disambiguation)
      if (json.conversationId) {
        setConversationId(json.conversationId);
      }

      const replyText =
        json.responseText ||
        json.reply ||
        json.response ||
        json.message ||
        (Array.isArray(json.toolsExecuted) && json.toolsExecuted.length > 0
          ? `⚡ Đã kích hoạt công cụ: ${json.toolsExecuted.join(', ')}.`
          : '⚡ Lệnh đã được tiếp nhận và ghi nhận vào hệ thống.');

      // Tự động phát sự kiện làm mới dữ liệu cho các bảng quản trị (Products / Offers) khi công cụ ghi được gọi
      const executed: string[] = json.toolsExecuted || [];
      const hasWriteToolExecuted = Array.isArray(executed) && executed.some((toolName) =>
        (ADMIN_WRITE_TOOLS as readonly string[]).includes(toolName)
      );

      if (hasWriteToolExecuted) {
        window.dispatchEvent(new CustomEvent('refresh-admin-data'));
        notify.success('AI Commander đã cập nhật dữ liệu thành công!');
      }

      setMessages((prev) => [
        ...prev,
        {
          sender: 'agent',
          text: replyText,
          time: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
        },
      ]);
    } catch (err: unknown) {
      notify.error('Lỗi khi gửi lệnh tới AI Agent Commander.');
      setMessages((prev) => [
        ...prev,
        {
          sender: 'agent',
          text: `⚠️ ${err instanceof Error ? err.message : String(err)}`,
          time: new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
        },
      ]);
    } finally {
      setIsTyping(false);
    }
  };

  if (!isOpen) return null;

  const modelDisplayName = llmInfo?.model || 'Claude / DeepSeek';
  const driverName = (llmInfo?.driver || 'AI').toUpperCase();

  return (
    <div className="fixed inset-0 z-50 overflow-hidden flex justify-end">
      {/* Backdrop mờ nền */}
      <div
        className="fixed inset-0 bg-black/60 backdrop-blur-xs transition-opacity animate-in fade-in duration-200"
        onClick={onClose}
      />

      {/* Slide-over Drawer Panel */}
      <div className="relative w-full max-w-md sm:max-w-lg bg-[#0e1422] border-l border-[#222f49] shadow-2xl flex flex-col h-full z-10 animate-in slide-in-from-right duration-200">
        {/* Drawer Header */}
        <div className="p-4 border-b border-[#222f49] bg-[#111726] flex items-center justify-between shrink-0">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-emerald-500 to-teal-500 flex items-center justify-center text-white shadow-lg shadow-emerald-500/20">
              <Bot className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="font-bold text-sm text-white flex items-center gap-1.5">
                  AI Agent Commander
                  <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
                </h3>
              </div>
              <div className="flex items-center gap-2 mt-0.5">
                <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-emerald-500/15 text-emerald-300 border border-emerald-500/30 flex items-center gap-1">
                  <Cpu className="w-3 h-3" />
                  <span>{modelDisplayName}</span>
                </span>
                <span className="text-[10px] font-mono text-slate-400">
                  {driverName} Gateway
                </span>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-1">
            <button
              onClick={fetchLlmSetting}
              title="Cập nhật thông tin Model mới nhất từ Settings"
              className="p-2 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition"
            >
              <RefreshCw className={`w-4 h-4 ${loadingLlm ? 'animate-spin' : ''}`} />
            </button>
            <button
              onClick={onClose}
              className="p-2 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition"
              aria-label="Đóng Drawer"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Context Indicator Badge */}
        <div className="px-4 py-2 bg-[#0a0f1d] border-b border-[#222f49] flex items-center justify-between text-xs text-slate-400">
          <span className="flex items-center gap-1.5">
            <Sparkles className="w-3.5 h-3.5 text-amber-400" />
            <span>Ngữ cảnh:</span>
            <strong className="text-slate-200">
              {isOffersPage
                ? 'Quản lý Gói Offers (Grand Slam)'
                : isProductsPage
                ? 'Danh mục Sản Phẩm (Tours)'
                : isOrdersPage
                ? 'Pipeline Đơn Hàng & Visa'
                : isContactsPage
                ? 'Hồ Sơ Khách Hàng CRM'
                : 'Trợ lý Quản Trị Hệ Thống'}
            </strong>
          </span>
          <span className="text-[10px] font-mono text-slate-500">Live Context</span>
        </div>

        {/* Message Stream Body */}
        <div className="flex-1 overflow-y-auto p-4 space-y-3.5 custom-scrollbar text-xs">
          {messages.map((msg, idx) => (
            <div
              key={idx}
              className={`flex flex-col ${msg.sender === 'user' ? 'items-end' : 'items-start'}`}
            >
              <div className="text-[10px] text-slate-400 mb-1 px-1">
                {msg.sender === 'user' ? '👤 Quản Trị Viên' : '🤖 AI Commander'}
                {msg.time && <span className="ml-2 font-mono text-slate-500">{msg.time}</span>}
              </div>
              <div
                className={`p-3.5 rounded-2xl max-w-[90%] leading-relaxed whitespace-pre-wrap ${
                  msg.sender === 'user'
                    ? 'bg-emerald-600 text-white rounded-tr-none shadow-md'
                    : 'bg-[#151c2e] border border-[#222f49] text-slate-200 rounded-tl-none shadow-sm'
                }`}
              >
                {msg.text}
              </div>
            </div>
          ))}

          {isTyping && (
            <div className="flex items-center gap-2 p-3 rounded-2xl bg-[#151c2e] border border-[#222f49] text-slate-400 text-xs w-fit animate-pulse">
              <Bot className="w-4 h-4 text-emerald-400" />
              <span>AI đang phân tích lệnh và phản hồi...</span>
            </div>
          )}

          <div ref={messagesEndRef} />
        </div>

        {/* Quick Pills (Gợi ý lệnh thông minh theo ngữ cảnh) */}
        <div className="px-4 py-2.5 bg-[#0a0f1d] border-t border-[#222f49] space-y-1.5">
          <div className="text-[10px] font-bold uppercase tracking-wider text-slate-500">
            Gợi ý lệnh nhanh:
          </div>
          <div className="flex gap-1.5 overflow-x-auto pb-1 text-[11px] custom-scrollbar">
            {quickPills.map((pill, idx) => (
              <button
                key={idx}
                type="button"
                onClick={() => setInput(pill)}
                className="px-2.5 py-1 rounded-full bg-[#151c2e] hover:bg-emerald-500/10 hover:text-emerald-300 hover:border-emerald-500/30 text-slate-300 border border-[#222f49] whitespace-nowrap transition shrink-0"
              >
                {pill}
              </button>
            ))}
          </div>
        </div>

        {/* Input Command Form */}
        <form
          onSubmit={handleSend}
          className="p-3 bg-[#111726] border-t border-[#222f49] flex items-center gap-2"
        >
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Nhập yêu cầu hoặc câu lệnh cho AI Agent Commander..."
            className="flex-1 px-3.5 py-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white text-xs placeholder-slate-500 focus:border-emerald-500 focus:outline-none transition"
          />
          <button
            type="submit"
            disabled={!input.trim() || isTyping}
            className="px-4 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold transition shadow-lg shadow-emerald-600/25 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1.5"
          >
            <Send className="w-3.5 h-3.5" />
            <span>Gửi</span>
          </button>
        </form>
      </div>
    </div>
  );
};
