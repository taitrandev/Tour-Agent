import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';

/**
 * Định nghĩa cấu trúc dữ liệu Bot Account nhận về từ Telegram API (getMe).
 */
interface TelegramBotInfo {
  id: number;
  is_bot: boolean;
  first_name: string;
  username: string;
  can_join_groups?: boolean;
  can_read_all_group_messages?: boolean;
  supports_inline_queries?: boolean;
}

/**
 * Định nghĩa cấu trúc dữ liệu trạng thái Webhook từ Telegram API (getWebhookInfo).
 */
interface TelegramWebhookInfo {
  url: string;
  has_custom_certificate: boolean;
  pending_update_count: number;
  last_error_date?: number;
  last_error_message?: string;
  max_connections?: number;
  ip_address?: string;
}

/**
 * Cấu trúc phản hồi tổng hợp từ endpoint /api/v1/agent/telegram/status.
 */
interface TelegramConfigStatus {
  configured: boolean;
  bot_token_masked: string;
  bot_info?: {
    ok: boolean;
    result?: TelegramBotInfo;
    description?: string;
  } | null;
  webhook_info?: {
    ok: boolean;
    result?: TelegramWebhookInfo;
    description?: string;
  } | null;
  suggested_webhook_url: string;
  admin_chat_id: string;
}

/**
 * Trang Quản Trị Kênh Telegram Bot & Thông Báo Quản Lý
 * Tái hiện 100% giao diện và nghiệp vụ từ resources/views/admin/telegram.blade.php của PHP core.
 */
export const AdminTelegramPage: React.FC = () => {
  // State cấu hình Telegram
  const [config, setConfig] = useState<TelegramConfigStatus | null>(null);
  const [loadingStatus, setLoadingStatus] = useState<boolean>(true);

  // State Mục 1: Bot Token
  const [botTokenInput, setBotTokenInput] = useState<string>('');
  const [showTokenPassword, setShowTokenPassword] = useState<boolean>(false);
  const [savingToken, setSavingToken] = useState<boolean>(false);

  // State Mục 2: Webhook URL
  const [webhookUrlInput, setWebhookUrlInput] = useState<string>('');
  const [activatingWebhook, setActivatingWebhook] = useState<boolean>(false);
  const [removingWebhook, setRemovingWebhook] = useState<boolean>(false);

  // State Mục 3: Admin Chat ID & Duyệt Deal
  const [adminChatIdInput, setAdminChatIdInput] = useState<string>('');
  const [savingAdminChatId, setSavingAdminChatId] = useState<boolean>(false);
  const [sendingEscalationTest, setSendingEscalationTest] = useState<boolean>(false);
  const [escalationResult, setEscalationResult] = useState<{ success: boolean; message: string } | null>(null);

  // State Mục 4: Test Message
  const [testChatIdInput, setTestChatIdInput] = useState<string>('');
  const [sendingTestMessage, setSendingTestMessage] = useState<boolean>(false);
  const [testMessageResult, setTestMessageResult] = useState<{ success: boolean; message: string } | null>(null);

  /**
   * Tải toàn bộ trạng thái Telegram từ .NET backend API.
   */
  const loadTelegramStatus = useCallback(async () => {
    setLoadingStatus(true);
    try {
      const response = await fetch('/api/v1/agent/telegram/status');
      const data: TelegramConfigStatus = await response.json();
      setConfig(data);

      if (data.admin_chat_id) {
        setAdminChatIdInput(data.admin_chat_id);
        setTestChatIdInput(data.admin_chat_id);
      }

      if (data.webhook_info?.result?.url) {
        setWebhookUrlInput(data.webhook_info.result.url);
      } else if (data.suggested_webhook_url) {
        setWebhookUrlInput(data.suggested_webhook_url);
      }
    } catch (error) {
      console.error('Lỗi khi tải trạng thái Telegram:', error);
    } finally {
      setLoadingStatus(false);
    }
  }, []);

  /**
   * Tự động tải trạng thái Telegram khi mở trang lần đầu.
   */
  useEffect(() => {
    loadTelegramStatus();
  }, [loadTelegramStatus]);

  /**
   * Ẩn/hiện mật khẩu input token.
   */
  const toggleTokenVisibility = () => {
    setShowTokenPassword((prev) => !prev);
  };

  /**
   * Điền URL Webhook gợi ý mặc định vào ô input.
   */
  const handleUseSuggestedWebhookUrl = () => {
    if (config?.suggested_webhook_url) {
      setWebhookUrlInput(config.suggested_webhook_url);
    }
  };

  /**
   * Lưu chuỗi token Telegram Bot mới và kiểm tra xác thực getMe.
   */
  const handleSaveToken = async () => {
    const trimmed = botTokenInput.trim();
    if (!trimmed) {
      alert('Vui lòng nhập Telegram Bot Token.');
      return;
    }

    setSavingToken(true);
    try {
      const res = await fetch('/api/v1/agent/telegram/save-token', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({ bot_token: trimmed }),
      });
      const data = await res.json();
      if (!res.ok || !data.success) {
        throw new Error(data.message || 'Lỗi khi lưu token.');
      }

      alert('🎉 ' + data.message);
      setBotTokenInput('');
      await loadTelegramStatus();
    } catch (err: any) {
      alert('❌ ' + err.message);
    } finally {
      setSavingToken(false);
    }
  };

  /**
   * Kích hoạt Webhook URL lên máy chủ Telegram API.
   */
  const handleActivateWebhook = async () => {
    const trimmed = webhookUrlInput.trim();
    if (!trimmed) {
      alert('Vui lòng nhập Webhook URL.');
      return;
    }

    setActivatingWebhook(true);
    try {
      const res = await fetch('/api/v1/agent/telegram/set-webhook', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({ webhook_url: trimmed }),
      });
      const data = await res.json();
      if (!res.ok || !data.success) {
        throw new Error(data.message || 'Lỗi khi kích hoạt Webhook.');
      }

      alert('✅ ' + data.message);
      await loadTelegramStatus();
    } catch (err: any) {
      alert('❌ ' + err.message);
    } finally {
      setActivatingWebhook(false);
    }
  };

  /**
   * Hủy bỏ Webhook Telegram đang hoạt động.
   */
  const handleRemoveWebhook = async () => {
    const confirmed = window.confirm('Bạn có chắc chắn muốn hủy Webhook này không?');
    if (!confirmed) return;

    setRemovingWebhook(true);
    try {
      const res = await fetch('/api/v1/agent/telegram/delete-webhook', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      });
      const data = await res.json();
      if (!res.ok || !data.success) {
        throw new Error(data.message || 'Lỗi khi gỡ Webhook.');
      }

      alert('✅ ' + data.message);
      await loadTelegramStatus();
    } catch (err: any) {
      alert('❌ ' + err.message);
    } finally {
      setRemovingWebhook(false);
    }
  };

  /**
   * Lưu Admin Chat ID nhận thông báo duyệt deal đoàn.
   */
  const handleSaveAdminChatId = async () => {
    const trimmed = adminChatIdInput.trim();
    if (!trimmed) {
      alert('Vui lòng nhập Admin Chat ID.');
      return;
    }

    setSavingAdminChatId(true);
    try {
      const res = await fetch('/api/v1/agent/telegram/save-admin-chat-id', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({ admin_chat_id: trimmed }),
      });
      const data = await res.json();
      if (!res.ok || !data.success) {
        throw new Error(data.message || 'Lỗi khi lưu Chat ID.');
      }

      alert('🎉 ' + data.message);
      setTestChatIdInput(trimmed);
      await loadTelegramStatus();
    } catch (err: any) {
      alert('❌ ' + err.message);
    } finally {
      setSavingAdminChatId(false);
    }
  };

  /**
   * Bắn thử nghiệm tin nhắn duyệt deal mẫu kèm 2 nút bấm Inline Keyboard về Telegram.
   */
  const handleSendTestEscalation = async () => {
    const trimmed = adminChatIdInput.trim();
    setSendingEscalationTest(true);
    setEscalationResult(null);

    try {
      const res = await fetch('/api/v1/agent/telegram/test-escalation', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({ admin_chat_id: trimmed }),
      });
      const data = await res.json();
      if (!res.ok || !data.success) {
        throw new Error(data.message || 'Lỗi khi gửi tin duyệt deal mẫu.');
      }

      setEscalationResult({
        success: true,
        message: '✅ ' + data.message,
      });
    } catch (err: any) {
      setEscalationResult({
        success: false,
        message: '❌ ' + err.message,
      });
    } finally {
      setSendingEscalationTest(false);
    }
  };

  /**
   * Gửi tin nhắn text thử nghiệm thông thường tới Chat ID.
   */
  const handleSendTestMessage = async () => {
    const trimmed = testChatIdInput.trim();
    if (!trimmed) {
      alert('Vui lòng nhập Chat ID.');
      return;
    }

    setSendingTestMessage(true);
    setTestMessageResult(null);

    try {
      const res = await fetch('/api/v1/agent/telegram/test-message', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({ chat_id: trimmed }),
      });
      const data = await res.json();
      if (!res.ok || !data.success) {
        throw new Error(data.message || 'Không thể gửi tin nhắn.');
      }

      setTestMessageResult({
        success: true,
        message: '✅ Tin nhắn đã được gửi thành công! Hãy kiểm tra Telegram của bạn.',
      });
    } catch (err: any) {
      setTestMessageResult({
        success: false,
        message: '❌ ' + err.message,
      });
    } finally {
      setSendingTestMessage(false);
    }
  };

  const isBotOnline = config?.configured && config.bot_info?.ok;
  const botData = config?.bot_info?.result;
  const isWebhookActive = config?.webhook_info?.ok && Boolean(config.webhook_info?.result?.url);
  const webhookData = config?.webhook_info?.result;

  return (
    <div className="min-h-screen flex flex-col font-sans bg-slate-950 text-slate-100 selection:bg-sky-500 selection:text-white">
      {/* Top Navigation Header */}
      <header className="h-16 border-b border-slate-800 bg-slate-900/80 backdrop-blur px-6 flex items-center justify-between shrink-0 z-20 sticky top-0">
        <div className="flex items-center gap-4">
          <Link
            to="/admin/products"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-semibold border border-slate-700/80 transition shadow-sm"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            <span>← Dashboard Quản Trị</span>
          </Link>

          <div className="h-6 w-px bg-slate-800" />

          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-sky-500 to-blue-600 flex items-center justify-center font-bold text-white text-base shadow-lg shadow-sky-500/20">
              📱
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-sm sm:text-base font-bold text-white tracking-tight">
                  Kênh Telegram Bot & Thông Báo Quản Lý
                </h1>
                {loadingStatus ? (
                  <span className="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded bg-slate-800 text-slate-400">
                    Đang kiểm tra...
                  </span>
                ) : isBotOnline ? (
                  <span className="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                    ONLINE
                  </span>
                ) : (
                  <span className="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded bg-amber-500/20 text-amber-300 border border-amber-500/30">
                    Chưa Kết Nối
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-400 hidden sm:block">
                Kết nối bot tư vấn tour du lịch 24/7 và cấu hình Chat ID Quản lý nhận duyệt deal
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <Link
            to="/admin/settings/llm"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-indigo-500/15 hover:bg-indigo-500/25 text-indigo-300 text-xs font-semibold border border-indigo-500/30 transition"
          >
            <span>🧠 Cấu Hình LLM</span>
          </Link>
          <Link
            to="/admin/conversations"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-purple-500/15 hover:bg-purple-500/25 text-purple-300 text-xs font-semibold border border-purple-500/30 transition"
          >
            <span>💬 Giám Sát Chat</span>
          </Link>
          <Link
            to="/admin/escalations"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-amber-500/15 hover:bg-amber-500/25 text-amber-300 text-xs font-semibold border border-amber-500/30 transition"
          >
            <span>🛎️ Bàn Quyết Định</span>
          </Link>
          <Link
            to="/admin/offers"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-500/15 hover:bg-emerald-500/25 text-emerald-300 text-xs font-semibold border border-emerald-500/30 transition"
          >
            <span>🎁 Grand Slam Offers</span>
          </Link>
        </div>
      </header>

      {/* Main Workspace */}
      <main className="flex-1 overflow-y-auto p-6 max-w-6xl w-full mx-auto space-y-6">
        {/* 3-Step Quick Guide Alert */}
        <div className="p-4 rounded-2xl bg-[#0e1526] border border-sky-500/30 space-y-2 text-xs">
          <div className="text-[11px] font-bold text-sky-400 uppercase tracking-wider flex items-center gap-1.5">
            <span>💡 Hướng Dẫn Nhanh 3 Bước Tích Hợp Telegram</span>
          </div>
          <ol className="list-decimal list-inside space-y-1 text-slate-300 text-[11px] leading-relaxed">
            <li>
              Mở Telegram, chat với <strong>@BotFather</strong> và tạo bot bằng lệnh{' '}
              <code className="px-1.5 py-0.5 rounded bg-[#0b0f19] text-sky-300 font-mono">/newbot</code>. Dán chuỗi
              Token vào <strong>Mục 1</strong>.
            </li>
            <li>
              Nhập URL HTTPS công khai (Cloudflare Tunnel hoặc Ngrok) vào <strong>Mục 2</strong> và bấm{' '}
              <strong>Kích Hoạt Webhook</strong>.
            </li>
            <li>
              Chat với <strong>@userinfobot</strong> trên Telegram để lấy <strong>Id</strong> của bạn, dán vào{' '}
              <strong>Mục 3</strong> để nhận thông báo bấm nút duyệt deal đoàn khi khách hỏi giảm giá!
            </li>
          </ol>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Left Column: Settings Panels */}
          <div className="lg:col-span-7 space-y-6">
            {/* 1. Bot Token Configuration */}
            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-4 shadow-xl text-xs">
              <div className="flex items-center justify-between border-b border-[#222f49] pb-3">
                <h2 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                  <span>🔑 1. Telegram Bot Token</span>
                </h2>
                {config?.configured ? (
                  <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                    Token: {config.bot_token_masked}
                  </span>
                ) : (
                  <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-400">
                    Chưa cấu hình
                  </span>
                )}
              </div>

              <div className="space-y-1.5">
                <label className="block font-semibold text-slate-300">HTTP API Token từ @BotFather:</label>
                <div className="flex items-center gap-2">
                  <input
                    type={showTokenPassword ? 'text' : 'password'}
                    value={botTokenInput}
                    onChange={(e) => setBotTokenInput(e.target.value)}
                    placeholder="123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ"
                    className="flex-1 px-3.5 py-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white font-mono focus:border-sky-500 focus:outline-none text-xs"
                  />
                  <button
                    type="button"
                    onClick={toggleTokenVisibility}
                    className="px-3 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-semibold transition"
                    title="Ẩn/Hiện token"
                  >
                    👁️
                  </button>
                  <button
                    type="button"
                    disabled={savingToken}
                    onClick={handleSaveToken}
                    className="px-4 py-2.5 rounded-xl bg-sky-500 hover:bg-sky-600 disabled:opacity-50 text-white font-bold transition whitespace-nowrap"
                  >
                    {savingToken ? 'Đang kiểm tra...' : '💾 Lưu & Kết Nối'}
                  </button>
                </div>
              </div>

              {/* Bot Info Profile Box */}
              {isBotOnline && botData && (
                <div className="p-3.5 rounded-xl bg-[#0b0f19] border border-sky-500/30 space-y-1.5 font-mono text-[11px]">
                  <div className="text-emerald-400 font-bold flex items-center gap-1.5 font-sans">
                    <span>✅ Bot Đang Hoạt Động Trực Tuyến!</span>
                  </div>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-slate-300 pt-1">
                    <div>
                      • Tên hiển thị: <strong className="text-white">{botData.first_name}</strong>
                    </div>
                    <div>
                      • Username: <strong className="text-sky-300">@{botData.username}</strong>
                    </div>
                    <div>
                      • Bot ID: <span className="text-slate-400">{botData.id}</span>
                    </div>
                    <div>
                      • Link chat:{' '}
                      <a
                        href={`https://t.me/${botData.username}`}
                        target="_blank"
                        rel="noreferrer"
                        className="text-sky-400 underline font-bold"
                      >
                        Mở Telegram Chat ↗
                      </a>
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* 2. Webhook Configuration */}
            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-4 shadow-xl text-xs">
              <div className="flex items-center justify-between border-b border-[#222f49] pb-3">
                <h2 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                  <span>⚡ 2. Cài Đặt Webhook (Kênh Nhận Tin Nhắn)</span>
                </h2>
                {isWebhookActive ? (
                  <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                    Webhook Active
                  </span>
                ) : (
                  <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-400">
                    Chưa kích hoạt
                  </span>
                )}
              </div>

              <p className="text-slate-400 leading-relaxed">
                Telegram cần URL HTTPS công khai để chuyển tiếp tin nhắn của khách về webhook hệ thống.
              </p>

              <div className="space-y-1.5">
                <label className="block font-semibold text-slate-300">Webhook URL:</label>
                <div className="flex items-center gap-2">
                  <input
                    type="url"
                    value={webhookUrlInput}
                    onChange={(e) => setWebhookUrlInput(e.target.value)}
                    placeholder="https://your-domain.com/api/v1/agent/webhook/telegram"
                    className="flex-1 px-3.5 py-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white font-mono focus:border-sky-500 focus:outline-none text-xs"
                  />
                  <button
                    type="button"
                    onClick={handleUseSuggestedWebhookUrl}
                    className="px-3 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-semibold transition whitespace-nowrap"
                    title="Dùng URL mặc định"
                  >
                    Mặc định
                  </button>
                </div>
              </div>

              <div className="flex items-center gap-2 pt-1">
                <button
                  type="button"
                  disabled={activatingWebhook}
                  onClick={handleActivateWebhook}
                  className="px-4 py-2 rounded-xl bg-emerald-500 hover:bg-emerald-600 disabled:opacity-50 text-white font-bold transition flex items-center gap-1.5"
                >
                  <span>{activatingWebhook ? 'Đang kích hoạt...' : '⚡ Kích Hoạt Webhook'}</span>
                </button>
                <button
                  type="button"
                  disabled={removingWebhook}
                  onClick={handleRemoveWebhook}
                  className="px-3 py-2 rounded-xl bg-rose-500/20 hover:bg-rose-500/30 disabled:opacity-50 text-rose-300 border border-rose-500/40 font-semibold transition"
                >
                  <span>{removingWebhook ? 'Đang gỡ...' : '❌ Gỡ Webhook'}</span>
                </button>
              </div>

              {/* Diagnostics Box */}
              {isWebhookActive && webhookData && (
                <div className="p-3.5 rounded-xl bg-[#0b0f19] border border-[#222f49] space-y-1 font-mono text-[11px] text-slate-400">
                  <div className="text-slate-300 font-bold font-sans">🔍 Trạng thái Webhook Telegram:</div>
                  <div>
                    • Active URL: <span className="text-sky-300 break-all">{webhookData.url}</span>
                  </div>
                  <div>
                    • Tin nhắn đang chờ (pending):{' '}
                    <span className="text-amber-400">{webhookData.pending_update_count || 0}</span>
                  </div>
                  {webhookData.last_error_message && (
                    <div className="text-rose-400">
                      • Lỗi gần nhất: {webhookData.last_error_message} (
                      {webhookData.last_error_date
                        ? new Date(webhookData.last_error_date * 1000).toLocaleString('vi-VN')
                        : ''}
                      )
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>

          {/* Right Column: Admin Chat ID & Deal Approval Test Panel */}
          <div className="lg:col-span-5 space-y-6">
            {/* 3. Admin Chat ID for Deal Approval */}
            <div className="p-5 rounded-2xl bg-[#151c2e] border border-amber-500/40 space-y-4 shadow-xl text-xs">
              <div className="flex items-center justify-between border-b border-[#222f49] pb-3">
                <h2 className="text-sm font-bold text-amber-300 uppercase tracking-wider flex items-center gap-2">
                  <span>🛎️ 3. Quản Lý Nhận Duyệt Deal</span>
                </h2>
                <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-amber-500/20 text-amber-400 font-bold">
                  Human-in-the-Loop
                </span>
              </div>

              <p className="text-slate-300 leading-relaxed">
                Khi khách hàng hỏi giảm giá đoàn đông (&gt;5 người), AI Agent sẽ gửi yêu cầu duyệt deal kèm{' '}
                <strong>2 nút bấm Inline Keyboard [✅ Duyệt] [❌ Từ chối]</strong> trực tiếp tới Chat ID này.
              </p>

              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <label className="block font-semibold text-slate-200">
                    Admin Chat ID / Group ID: <span className="text-amber-400">*</span>
                  </label>
                  <span className="text-[10px] text-slate-400">Lấy từ @userinfobot</span>
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="text"
                    value={adminChatIdInput}
                    onChange={(e) => setAdminChatIdInput(e.target.value)}
                    placeholder="Ví dụ: 987654321 hoặc -10012345678"
                    className="flex-1 px-3.5 py-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white font-mono focus:border-amber-500 focus:outline-none text-xs"
                  />
                  <button
                    type="button"
                    disabled={savingAdminChatId}
                    onClick={handleSaveAdminChatId}
                    className="px-4 py-2.5 rounded-xl bg-amber-500 hover:bg-amber-600 disabled:opacity-50 text-white font-bold transition whitespace-nowrap"
                  >
                    {savingAdminChatId ? 'Đang lưu...' : '💾 Lưu Chat ID'}
                  </button>
                </div>
              </div>

              {/* Live Escalation Test Button */}
              <div className="pt-2 border-t border-[#222f49]/60 space-y-2">
                <label className="block font-semibold text-slate-300">Thử nghiệm nhận thông báo duyệt deal:</label>
                <button
                  type="button"
                  disabled={sendingEscalationTest}
                  onClick={handleSendTestEscalation}
                  className="w-full py-2.5 rounded-xl bg-gradient-to-r from-amber-500 to-rose-500 hover:from-amber-600 hover:to-rose-600 disabled:opacity-50 text-white font-bold text-xs shadow-lg shadow-amber-500/20 transition flex items-center justify-center gap-2"
                >
                  <span>{sendingEscalationTest ? 'Đang gửi...' : '📨 Bắn Tin Duyệt Deal Mẫu (Có Nút Bấm)'}</span>
                </button>
                <p className="text-[10px] text-slate-400 italic text-center">
                  Bấm nút trên, bot sẽ gửi tin nhắn thử nghiệm có 2 nút [✅ Duyệt giảm 10%] và [❌ Giữ nguyên giá] về
                  Telegram của bạn để kiểm tra.
                </p>
              </div>

              {escalationResult && (
                <div
                  className={`p-3 rounded-xl text-xs font-mono border ${
                    escalationResult.success
                      ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30'
                      : 'bg-rose-500/20 text-rose-300 border-rose-500/30'
                  }`}
                >
                  {escalationResult.message}
                </div>
              )}
            </div>

            {/* 4. Direct Customer Message Test */}
            <div className="p-5 rounded-2xl bg-[#151c2e] border border-[#222f49] space-y-4 shadow-xl text-xs">
              <div className="flex items-center justify-between border-b border-[#222f49] pb-3">
                <h2 className="text-sm font-bold text-white uppercase tracking-wider flex items-center gap-2">
                  <span>📨 4. Thử Gửi Tin Nhắn Văn Bản</span>
                </h2>
              </div>

              <p className="text-slate-400">
                Nhập Telegram Chat ID của bạn để kiểm tra khả năng gửi tin nhắn văn bản thông thường từ Bot.
              </p>

              <div className="flex items-center gap-2">
                <input
                  type="text"
                  value={testChatIdInput}
                  onChange={(e) => setTestChatIdInput(e.target.value)}
                  placeholder="Chat ID (VD: 987654321)"
                  className="w-48 px-3.5 py-2.5 rounded-xl bg-[#0b0f19] border border-[#222f49] text-white font-mono focus:border-sky-500 focus:outline-none text-xs"
                />
                <button
                  type="button"
                  disabled={sendingTestMessage}
                  onClick={handleSendTestMessage}
                  className="px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 disabled:opacity-50 text-slate-200 font-bold transition flex items-center gap-1.5 whitespace-nowrap"
                >
                  <span>{sendingTestMessage ? 'Đang gửi...' : '📨 Gửi Tin Test'}</span>
                </button>
              </div>

              {testMessageResult && (
                <div
                  className={`text-xs p-3 rounded-xl font-mono border ${
                    testMessageResult.success
                      ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30'
                      : 'bg-rose-500/20 text-rose-300 border-rose-500/30'
                  }`}
                >
                  {testMessageResult.message}
                </div>
              )}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default AdminTelegramPage;
