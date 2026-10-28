import React, { useState, useEffect, useCallback } from 'react';
import { Link, useSearchParams } from 'react-router-dom';

/**
 * Định nghĩa cấu trúc dữ liệu một phiên hội thoại tóm tắt từ backend.
 */
interface ConversationItem {
  id: number;
  channel: string;
  channel_user_id: string;
  status: string;
  contact_id?: number | null;
  last_message_at?: string | null;
  created_at?: string;
  metadata?: any;
  messages_count?: number;
  llm_calls_count?: number;
}

/**
 * Định nghĩa cấu trúc dữ liệu tin nhắn trong hội thoại.
 */
interface MessageItem {
  id: number;
  conversation_id: number;
  role: 'user' | 'assistant' | 'tool' | string;
  content?: string | null;
  tool_name?: string | null;
  tool_payload?: any;
  channel_message_id?: string | null;
  created_at: string;
}

/**
 * Định nghĩa cấu trúc dữ liệu lượt gọi LLM kiểm toán (Observability Telemetry).
 */
interface LlmCallItem {
  id: number;
  conversation_id?: number | null;
  model: string;
  provider?: string | null;
  prompt_tokens: number;
  completion_tokens: number;
  cost_usd: number;
  latency_ms: number;
  tools_called?: string[] | null;
  finish_reason?: string | null;
  status_bar_snapshot?: string | null;
  state_snapshot?: any;
  tool_payloads?: any;
  created_at: string;
}

/**
 * Định nghĩa dữ liệu trả về từ API chi tiết một hội thoại.
 */
interface ConversationDetailData {
  conversation: {
    id: number;
    channel: string;
    channel_user_id: string;
    status: string;
    contact_id?: number | null;
    metadata?: any;
    created_at?: string;
    updated_at?: string;
  };
  messages: MessageItem[];
  llm_calls: LlmCallItem[];
  system_prompt: string;
}

/**
 * Hàm phân tích định dạng Markdown đơn giản sang chuỗi HTML an toàn.
 * Tái hiện phương thức formatMarkdown() của file conversations.blade.php.
 * @param raw Chuỗi markdown gốc
 * @returns Chuỗi HTML đã format
 */
function renderMarkdown(raw?: string | null): string {
  if (!raw) return '';

  let html = raw
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');

  // In đậm: **text**
  html = html.replace(/\*\*(.*?)\*\*/g, '<strong class="font-bold text-white">$1</strong>');

  // Tiêu đề: ### và ##
  html = html.replace(/^### (.*?)$/gm, '<h4 class="font-bold text-sky-400 mt-2 mb-0.5 text-xs">$1</h4>');
  html = html.replace(/^## (.*?)$/gm, '<h3 class="font-bold text-purple-300 text-sm mt-2 mb-1 pb-0.5 border-b border-[#1f2937]/60">$1</h3>');

  // Đường kẻ ngang: ---
  html = html.replace(/^---$/gm, '<hr class="my-2 border-[#1f2937]/60">');

  // Gạch đầu dòng: - item hoặc * item
  html = html.replace(/^[-\*] (.*?)$/gm, '<div class="flex items-start gap-1.5 my-0.5 ml-2"><span class="text-sky-400">•</span><span>$1</span></div>');

  // Xuống dòng: \n
  html = html.replace(/\n/g, '<br>');

  return html;
}

/**
 * Trang Giám Sát Hội Thoại & Nhật Ký LLM (AI Observability Live Inspector)
 * Tái hiện 100% giao diện và nghiệp vụ từ resources/views/admin/conversations.blade.php của PHP core.
 */
export const AdminConversationsPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();

  // State danh sách hội thoại
  const [conversations, setConversations] = useState<ConversationItem[]>([]);
  const [loadingList, setLoadingList] = useState<boolean>(true);
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [channelFilter, setChannelFilter] = useState<'all' | 'telegram' | 'web_widget' | 'zalo'>('all');

  // Query param `?id=` trên URL là nguồn chân lý duy nhất (Single Source of Truth) cho phiên hội thoại được chọn
  const paramId = searchParams.get('id');
  const selectedConvId = paramId ? parseInt(paramId, 10) : null;

  const [loadingDetail, setLoadingDetail] = useState<boolean>(false);
  const [detailData, setDetailData] = useState<ConversationDetailData | null>(null);
  const [activeTab, setActiveTab] = useState<'msgs' | 'llm' | 'prompt'>('msgs');

  // State modal xem Snapshot (Status Bar XML, Agent State JSON, Tools JSON)
  const [snapshotModalOpen, setSnapshotModalOpen] = useState<boolean>(false);
  const [snapshotModalTitle, setSnapshotModalTitle] = useState<string>('');
  const [snapshotModalIcon, setSnapshotModalIcon] = useState<string>('📋');
  const [snapshotModalContent, setSnapshotModalContent] = useState<string>('');
  const [copyFeedback, setCopyFeedback] = useState<boolean>(false);

  /**
   * Gọi API tải danh sách toàn bộ phiên hội thoại từ .NET backend.
   * @param showLoading Có bật hiệu ứng skeleton loading danh sách không
   */
  const loadConversations = useCallback(async (showLoading = false): Promise<ConversationItem[]> => {
    if (showLoading) setLoadingList(true);
    try {
      const response = await fetch('/api/v1/agent/consultant/conversations');
      const json = await response.json();
      if (json.success && Array.isArray(json.data)) {
        setConversations(json.data);
        return json.data;
      }
    } catch (error) {
      console.error('Lỗi khi tải danh sách hội thoại:', error);
    } finally {
      if (showLoading) setLoadingList(false);
    }
    return [];
  }, []);

  /**
   * Gọi API tải chi tiết tin nhắn, logs LLM và System Prompt của phiên hội thoại đang chọn.
   * @param conversationId Mã ID phiên hội thoại
   */
  const loadConversationDetail = useCallback(async (conversationId: number) => {
    setLoadingDetail(true);
    try {
      const response = await fetch(`/api/v1/agent/consultant/conversations/${conversationId}`);
      const json = await response.json();
      if (json.success) {
        setDetailData({
          conversation: json.conversation,
          messages: json.messages || [],
          llm_calls: json.llm_calls || [],
          system_prompt: json.system_prompt || '',
        });
      } else {
        setDetailData(null);
      }
    } catch (error) {
      console.error(`Lỗi khi tải chi tiết hội thoại #${conversationId}:`, error);
      setDetailData(null);
    } finally {
      setLoadingDetail(false);
    }
  }, []);

  /**
   * Effect tự động tải danh sách hội thoại khi render trang lần đầu.
   * Nếu URL chưa có tham số ?id= thì tự động chọn hội thoại đầu tiên.
   */
  useEffect(() => {
    let isMounted = true;
    const init = async () => {
      setLoadingList(true);
      const data = await loadConversations(false);
      if (!isMounted) return;
      setLoadingList(false);

      const currentParam = searchParams.get('id');
      if (!currentParam && data.length > 0) {
        setSearchParams({ id: data[0].id.toString() }, { replace: true });
      }
    };

    init();
    return () => {
      isMounted = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  /**
   * Effect nạp chi tiết hội thoại mỗi khi selectedConvId trên URL thay đổi.
   * Đảm bảo chỉ gửi ĐÚNG 1 request duy nhất tới API chi tiết khi click chuyển session.
   */
  useEffect(() => {
    if (selectedConvId !== null) {
      loadConversationDetail(selectedConvId);
    } else {
      setDetailData(null);
    }
  }, [selectedConvId, loadConversationDetail]);

  /**
   * Xử lý chọn một phiên hội thoại từ danh sách bên cột trái.
   * @param convId Mã ID hội thoại
   */
  const handleSelectConversation = (convId: number) => {
    if (selectedConvId === convId) return;
    setSearchParams({ id: convId.toString() }, { replace: true });
  };

  /**
   * Xử lý xác nhận và xóa một phiên hội thoại an toàn khỏi hệ thống.
   * @param convId Mã ID hội thoại cần xóa
   */
  const handleDeleteConversation = async (convId: number, event?: React.MouseEvent) => {
    if (event) {
      event.stopPropagation();
    }

    const confirmed = window.confirm(
      `Bạn có chắc chắn muốn xóa vĩnh viễn cuộc hội thoại #${convId} cùng toàn bộ tin nhắn và nhật ký LLM liên quan?`
    );
    if (!confirmed) return;

    try {
      const response = await fetch(`/api/v1/agent/consultant/conversations/${convId}`, {
        method: 'DELETE',
        headers: { Accept: 'application/json' },
      });
      const json = await response.json();
      if (json.success) {
        if (selectedConvId === convId) {
          setDetailData(null);
          setSearchParams({}, { replace: true });
        }
        await loadConversations(false);
      } else {
        alert(json.message || 'Không thể xóa hội thoại.');
      }
    } catch (error: any) {
      alert('Lỗi kết nối khi xóa hội thoại: ' + error.message);
    }
  };

  /**
   * Mở modal xem dữ liệu kiểm toán (XML Status Bar, State JSON hoặc Tools Payload).
   * @param type Loại snapshot cần xem
   * @param call Bản ghi lượt gọi LLM
   */
  const handleOpenSnapshotModal = (type: 'status_bar' | 'state' | 'tools', call: LlmCallItem) => {
    const timeFormatted = new Date(call.created_at).toLocaleTimeString('vi-VN');

    if (type === 'status_bar') {
      setSnapshotModalIcon('📋');
      setSnapshotModalTitle(`Status Bar XML Snapshot — Lượt gọi #${call.id} (${timeFormatted})`);
      setSnapshotModalContent(call.status_bar_snapshot || '(Không có nội dung XML)');
    } else if (type === 'state') {
      setSnapshotModalIcon('📦');
      setSnapshotModalTitle(`Agent State Snapshot — Lượt gọi #${call.id} (${timeFormatted})`);
      setSnapshotModalContent(
        call.state_snapshot ? JSON.stringify(call.state_snapshot, null, 2) : '(Không có dữ liệu state)'
      );
    } else if (type === 'tools') {
      setSnapshotModalIcon('⚡');
      setSnapshotModalTitle(`Tool Arguments & Execution Results — Lượt gọi #${call.id} (${timeFormatted})`);
      setSnapshotModalContent(
        call.tool_payloads ? JSON.stringify(call.tool_payloads, null, 2) : '(Không có payload tool)'
      );
    }

    setCopyFeedback(false);
    setSnapshotModalOpen(true);
  };

  /**
   * Đóng modal snapshot.
   */
  const handleCloseSnapshotModal = () => {
    setSnapshotModalOpen(false);
  };

  /**
   * Sao chép nội dung snapshot vào Clipboard của người dùng.
   */
  const handleCopySnapshotContent = () => {
    if (!snapshotModalContent) return;
    navigator.clipboard
      .writeText(snapshotModalContent)
      .then(() => {
        setCopyFeedback(true);
        setTimeout(() => setCopyFeedback(false), 2000);
      })
      .catch(() => {
        alert('Không thể sao chép vào bộ nhớ đệm.');
      });
  };

  // Lọc danh sách hội thoại theo kênh và từ khóa tìm kiếm
  const filteredConversations = conversations.filter((c) => {
    if (channelFilter !== 'all' && c.channel !== channelFilter) {
      return false;
    }
    if (searchQuery.trim()) {
      const keyword = searchQuery.toLowerCase().trim();
      const matchId = c.id.toString().includes(keyword);
      const matchUser = (c.channel_user_id || '').toLowerCase().includes(keyword);
      const matchChannel = (c.channel || '').toLowerCase().includes(keyword);
      return matchId || matchUser || matchChannel;
    }
    return true;
  });

  const selectedConv = detailData?.conversation;
  const agentState = selectedConv?.metadata?.agent_state;
  const factsCount = Array.isArray(agentState?.confirmed_facts) ? agentState.confirmed_facts.length : 0;

  return (
    <div className="bg-[#0b0f19] text-slate-100 font-sans antialiased min-h-screen flex flex-col selection:bg-purple-500/30 selection:text-purple-200">
      {/* Top Navigation Header */}
      <header className="border-b border-[#1f2937] bg-[#111827]/80 backdrop-blur sticky top-0 z-40 px-4 sm:px-6 py-3 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Link to="/admin/products" className="flex items-center gap-2 group">
            <div className="w-9 h-9 rounded-xl bg-purple-500/20 border border-purple-500/40 flex items-center justify-center text-purple-400 font-bold group-hover:scale-105 transition">
              💬
            </div>
            <div>
              <h1 className="text-sm font-bold text-white flex items-center gap-2">
                Giám Sát Hội Thoại & Nhật Ký LLM
                <span className="text-[10px] font-mono font-bold uppercase px-2 py-0.5 rounded bg-purple-500/20 text-purple-300 border border-purple-500/30">
                  Live Inspector
                </span>
              </h1>
              <p className="text-[11px] text-slate-400 hidden sm:block">
                Theo dõi chi tiết từng lượt chat, tool executions, prompt DeepSeek và chi phí token
              </p>
            </div>
          </Link>
        </div>

        <div className="flex items-center gap-2">
          <Link
            to="/admin/products"
            className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold transition flex items-center gap-1.5 border border-slate-700"
          >
            <span>✈️ Sản Phẩm Tour</span>
          </Link>
          <Link
            to="/admin/offers"
            className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold transition flex items-center gap-1.5 border border-slate-700"
          >
            <span>🎁 Grand Slam Offers</span>
          </Link>
          <Link
            to="/admin/contacts"
            className="px-3 py-1.5 rounded-xl bg-teal-500/10 hover:bg-teal-500/20 text-teal-300 text-xs font-semibold transition flex items-center gap-1.5 border border-teal-500/30"
          >
            <span>👥 Khách Hàng (CRM)</span>
          </Link>
          <Link
            to="/admin/orders"
            className="px-3 py-1.5 rounded-xl bg-sky-500/10 hover:bg-sky-500/20 text-sky-300 text-xs font-semibold transition flex items-center gap-1.5 border border-sky-500/30"
          >
            <span>📦 Đơn Tour & Visa</span>
          </Link>
          <Link
            to="/admin/escalations"
            className="px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold transition flex items-center gap-1.5 border border-slate-700"
          >
            <span>⚡ Bàn Quyết Định</span>
          </Link>
          <Link
            to="/admin/channels/telegram"
            className="px-3 py-1.5 rounded-xl bg-sky-500/10 hover:bg-sky-500/20 text-sky-400 text-xs font-semibold transition flex items-center gap-1.5 border border-sky-500/30"
          >
            <span>📱 Kênh Telegram</span>
          </Link>
        </div>
      </header>

      {/* Main Inspector Workspace (Two-Column Layout) */}
      <main className="flex-1 p-3 sm:p-4 flex flex-col md:flex-row gap-4 overflow-hidden min-h-[calc(100vh-60px)]">
        {/* Left Column: Conversations List & Filters */}
        <aside className="w-full md:w-80 lg:w-96 flex flex-col rounded-2xl bg-[#111827] border border-[#1f2937] overflow-hidden shadow-xl">
          {/* Filter & Search Bar */}
          <div className="p-3 border-b border-[#1f2937] bg-[#0e1422] space-y-2">
            <div className="flex items-center justify-between">
              <span className="font-bold text-slate-200 flex items-center gap-1.5 uppercase text-[11px] tracking-wider">
                <span>📋 Phiên Chat</span>
                <span className="px-1.5 py-0.2 rounded bg-slate-800 text-slate-400 font-mono text-[10px]">
                  {filteredConversations.length}
                </span>
              </span>
              <button
                onClick={() => loadConversations(true)}
                disabled={loadingList}
                className="px-2 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 transition text-[11px] flex items-center gap-1 border border-slate-700"
              >
                <span>{loadingList ? '⏳' : '🔄'} Làm mới</span>
              </button>
            </div>

            <div className="relative">
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Tìm User ID, SĐT, nội dung..."
                className="w-full px-3 py-1.5 pl-8 rounded-xl bg-[#0b0f19] border border-[#1f2937] text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-purple-500 transition"
              />
              <span className="absolute left-2.5 top-2 text-slate-500 text-xs">🔍</span>
            </div>

            {/* Channel Filter Chips */}
            <div className="flex items-center gap-1 text-[10px]">
              <button
                onClick={() => setChannelFilter('all')}
                className={`px-2 py-0.5 rounded-md font-bold transition ${
                  channelFilter === 'all'
                    ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40'
                    : 'bg-slate-800 text-slate-400 hover:text-white border border-[#1f2937]'
                }`}
              >
                Tất cả
              </button>
              <button
                onClick={() => setChannelFilter('telegram')}
                className={`px-2 py-0.5 rounded-md transition ${
                  channelFilter === 'telegram'
                    ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40 font-bold'
                    : 'bg-slate-800 text-slate-400 hover:text-white border border-[#1f2937]'
                }`}
              >
                📱 Telegram
              </button>
              <button
                onClick={() => setChannelFilter('web_widget')}
                className={`px-2 py-0.5 rounded-md transition ${
                  channelFilter === 'web_widget'
                    ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40 font-bold'
                    : 'bg-slate-800 text-slate-400 hover:text-white border border-[#1f2937]'
                }`}
              >
                🌐 Web Chat
              </button>
              <button
                onClick={() => setChannelFilter('zalo')}
                className={`px-2 py-0.5 rounded-md transition ${
                  channelFilter === 'zalo'
                    ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40 font-bold'
                    : 'bg-slate-800 text-slate-400 hover:text-white border border-[#1f2937]'
                }`}
              >
                💬 Zalo
              </button>
            </div>
          </div>

          {/* Scrollable Conversations List */}
          <div className="flex-1 overflow-y-auto custom-scrollbar p-2 space-y-2">
            {loadingList ? (
              <div className="text-center py-12 text-slate-500 text-xs">Đang nạp danh sách hội thoại...</div>
            ) : filteredConversations.length === 0 ? (
              <div className="text-center py-8 text-slate-500 text-xs">Không tìm thấy phiên hội thoại phù hợp.</div>
            ) : (
              filteredConversations.map((conv) => {
                const isSelected = conv.id === selectedConvId;
                let channelIcon = '🌐';
                let channelName = 'Web Chat';
                if (conv.channel === 'telegram') {
                  channelIcon = '📱';
                  channelName = 'Telegram';
                } else if (conv.channel === 'zalo') {
                  channelIcon = '💬';
                  channelName = 'Zalo';
                }

                const activeClass = isSelected
                  ? 'bg-purple-500/15 border-purple-500/50 shadow-md ring-1 ring-purple-500/30'
                  : 'bg-[#0f1422] hover:bg-[#161d30] border-[#1f2937]';

                const timeStr = conv.last_message_at
                  ? new Date(conv.last_message_at).toLocaleTimeString('vi-VN', {
                      hour: '2-digit',
                      minute: '2-digit',
                      day: '2-digit',
                      month: '2-digit',
                    })
                  : 'Vừa tạo';

                return (
                  <div
                    key={conv.id}
                    onClick={() => handleSelectConversation(conv.id)}
                    className={`p-2.5 rounded-xl border ${activeClass} cursor-pointer transition space-y-1.5 group`}
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-1.5">
                        <span>{channelIcon}</span>
                        <strong className="text-white text-xs font-semibold">
                          #{conv.id} • {channelName}
                        </strong>
                      </div>
                      <div className="flex items-center gap-1">
                        {conv.status === 'bot_active' ? (
                          <span className="text-[9px] font-mono px-1.5 py-0.5 rounded bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                            Bot Active
                          </span>
                        ) : (
                          <span className="text-[9px] font-mono px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-300 border border-amber-500/30">
                            Human Active
                          </span>
                        )}
                        <button
                          onClick={(e) => handleDeleteConversation(conv.id, e)}
                          title="Xóa phiên này"
                          className="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-rose-500/20 text-slate-400 hover:text-rose-300 transition"
                        >
                          <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth="2"
                              d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                            />
                          </svg>
                        </button>
                      </div>
                    </div>
                    <div className="text-[11px] text-slate-400 font-mono truncate">
                      ID: {conv.channel_user_id || 'N/A'}
                    </div>
                    <div className="flex items-center justify-between text-[10px] text-slate-500 pt-1 border-t border-[#1f2937]/60">
                      <span>
                        💬 {conv.messages_count || 0} tin • 🧠 {conv.llm_calls_count || 0} LLM
                      </span>
                      <span>{timeStr}</span>
                    </div>
                  </div>
                );
              })
            )}
          </div>
        </aside>

        {/* Right Column: Inspector Details */}
        <section className="flex-1 flex flex-col rounded-2xl bg-[#111827] border border-[#1f2937] overflow-hidden shadow-xl">
          {/* Header & Action Tabs */}
          <div className="p-3 border-b border-[#1f2937] flex flex-wrap items-center justify-between gap-2 bg-[#0e1422]">
            <div className="flex items-center gap-2">
              <span className="text-lg">
                {selectedConv?.channel === 'telegram' ? '📱' : selectedConv?.channel === 'zalo' ? '💬' : '🌐'}
              </span>
              <div>
                <span className="font-bold text-white text-sm">
                  {selectedConv
                    ? `Hội thoại #${selectedConv.id} • ${selectedConv.channel.toUpperCase()} (${selectedConv.channel_user_id})`
                    : 'Chọn một phiên hội thoại để kiểm tra'}
                </span>
                {selectedConv && (
                  <div className="flex items-center gap-2 mt-0.5">
                    <span
                      className={`text-[10px] font-mono px-2 py-0.5 rounded border ${
                        selectedConv.status === 'bot_active'
                          ? 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30'
                          : 'bg-amber-500/20 text-amber-300 border-amber-500/30'
                      }`}
                    >
                      {selectedConv.status === 'bot_active' ? '● Bot Active' : '● Human Handover'}
                    </span>

                    {selectedConv.contact_id && (
                      <span className="text-[10px] text-sky-400 font-semibold hover:underline cursor-pointer">
                        👤 Hồ sơ Contact #{selectedConv.contact_id}
                      </span>
                    )}
                  </div>
                )}

                {/* Hiển thị Agent State Badge */}
                {agentState && (
                  <div className="flex items-center gap-1.5 flex-wrap mt-1">
                    <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-purple-500/20 text-purple-300 border border-purple-500/30 font-bold">
                      State v{agentState.version || 1}
                    </span>
                    <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-sky-500/20 text-sky-300 border border-sky-500/30">
                      🎯 {agentState.current_goal || 'consulting'}
                    </span>
                    <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                      ⚡ {agentState.workflow_status || 'in_progress'}
                    </span>
                    <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-300 border border-slate-700">
                      📌 {factsCount} facts
                    </span>
                  </div>
                )}
              </div>
            </div>

            <div className="flex items-center gap-1.5">
              {selectedConvId && (
                <button
                  onClick={() => handleDeleteConversation(selectedConvId)}
                  className="px-2.5 py-1.5 rounded-lg bg-rose-500/15 text-rose-300 hover:bg-rose-500/25 border border-rose-500/30 text-xs font-bold transition flex items-center gap-1"
                >
                  <span>🗑️ Xóa phiên</span>
                </button>
              )}
              <button
                onClick={() => setActiveTab('msgs')}
                className={`px-3 py-1.5 rounded-lg text-xs font-bold transition ${
                  activeTab === 'msgs'
                    ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40'
                    : 'bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700'
                }`}
              >
                💬 Tin Nhắn
              </button>
              <button
                onClick={() => setActiveTab('llm')}
                className={`px-3 py-1.5 rounded-lg text-xs font-bold transition ${
                  activeTab === 'llm'
                    ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40'
                    : 'bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700'
                }`}
              >
                🧠 Lượt Gọi LLM
              </button>
              <button
                onClick={() => setActiveTab('prompt')}
                className={`px-3 py-1.5 rounded-lg text-xs font-bold transition ${
                  activeTab === 'prompt'
                    ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40'
                    : 'bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700'
                }`}
              >
                📜 System Prompt
              </button>
            </div>
          </div>

          {/* Loading detail state */}
          {loadingDetail ? (
            <div className="flex-1 flex items-center justify-center p-8 text-slate-500 text-xs">
              Đang tải chi tiết phiên #{selectedConvId}...
            </div>
          ) : !selectedConvId ? (
            <div className="flex-1 flex items-center justify-center p-8 text-slate-500 text-xs">
              👈 Vui lòng bấm chọn một cuộc hội thoại bên danh sách trái để xem toàn bộ tin nhắn & tool results.
            </div>
          ) : (
            <>
              {/* Tab 1: Messages History */}
              {activeTab === 'msgs' && (
                <div className="flex-1 overflow-y-auto custom-scrollbar p-4 space-y-3">
                  {/* Clean Chat Notice */}
                  <div className="p-2.5 rounded-xl bg-purple-500/10 border border-purple-500/20 text-purple-300 text-xs flex items-center justify-between">
                    <span>
                      ✨ <strong>Kênh Chat Hội Thoại Sạch:</strong> Bảng messages chỉ lưu trữ trao đổi giữa Khách Hàng
                      và AI. Toàn bộ telemetry & tool debug được lưu tại tab <strong>🧠 Lượt Gọi LLM</strong>.
                    </span>
                  </div>

                  {detailData?.messages.length === 0 ? (
                    <div className="text-center py-16 text-slate-500 text-xs">
                      Cuộc hội thoại chưa có tin nhắn nào.
                    </div>
                  ) : (
                    detailData?.messages.map((m) => {
                      const timeStr = new Date(m.created_at).toLocaleTimeString('vi-VN', {
                        hour: '2-digit',
                        minute: '2-digit',
                        second: '2-digit',
                      });

                      if (m.role === 'user') {
                        return (
                          <div key={m.id} className="flex justify-end">
                            <div className="max-w-[85%] rounded-2xl rounded-tr-sm bg-sky-600/90 text-white p-3 space-y-1 shadow">
                              <div className="flex items-center justify-between text-[10px] text-sky-200 border-b border-sky-500/40 pb-1 gap-4">
                                <span className="font-bold">👤 Khách Hàng</span>
                                <span className="font-mono">{timeStr}</span>
                              </div>
                              <div className="text-xs whitespace-pre-wrap leading-relaxed">{m.content || ''}</div>
                            </div>
                          </div>
                        );
                      } else if (m.role === 'assistant') {
                        return (
                          <div key={m.id} className="flex justify-start">
                            <div className="max-w-[85%] rounded-2xl rounded-tl-sm bg-[#151c2e] border border-[#1f2937] text-slate-200 p-3.5 space-y-1.5 shadow">
                              <div className="flex items-center justify-between text-[10px] text-purple-300 border-b border-[#1f2937] pb-1 gap-4 font-mono">
                                <span className="font-bold font-sans">🤖 AI Consultant (DeepSeek)</span>
                                <span>{timeStr}</span>
                              </div>
                              <div
                                className="text-xs leading-relaxed"
                                dangerouslySetInnerHTML={{ __html: renderMarkdown(m.content) }}
                              />
                            </div>
                          </div>
                        );
                      } else if (m.role === 'tool') {
                        const payloadJson = m.tool_payload
                          ? JSON.stringify(m.tool_payload, null, 2)
                          : m.content || '{}';
                        return (
                          <div
                            key={m.id}
                            className="p-2.5 rounded-xl bg-amber-500/10 border border-amber-500/30 text-slate-300 space-y-1 font-mono text-[11px]"
                          >
                            <div className="flex items-center justify-between text-amber-400 font-sans font-bold">
                              <span className="flex items-center gap-1.5">
                                <span>⚙️ Tool Execution Result:</span>
                                <code className="px-1.5 py-0.2 rounded bg-[#0b0f19] text-amber-300 font-mono">
                                  {m.tool_name || 'unknown'}
                                </code>
                              </span>
                              <span className="text-[10px] font-mono text-slate-400">{timeStr}</span>
                            </div>
                            <details className="pt-1">
                              <summary className="cursor-pointer text-[10px] text-amber-300/80 hover:text-amber-200">
                                🔍 Bấm để xem payload JSON trả về cho LLM
                              </summary>
                              <pre className="mt-1.5 p-2 rounded-lg bg-[#0b0f19] border border-[#1f2937] overflow-x-auto text-[10px] text-slate-300 custom-scrollbar">
                                {payloadJson}
                              </pre>
                            </details>
                          </div>
                        );
                      }
                      return null;
                    })
                  )}
                </div>
              )}

              {/* Tab 2: LLM Calls & Observability Table */}
              {activeTab === 'llm' && (
                <div className="flex-1 overflow-y-auto custom-scrollbar p-3">
                  <div className="rounded-xl border border-[#1f2937] overflow-hidden">
                    <table className="w-full text-left text-xs border-collapse">
                      <thead>
                        <tr className="border-b border-[#1f2937] text-slate-400 bg-[#0a0e17] uppercase text-[10px] font-semibold">
                          <th className="py-2.5 px-3">Thời Gian</th>
                          <th className="py-2.5 px-3">Model</th>
                          <th className="py-2.5 px-3">Tokens (P / C)</th>
                          <th className="py-2.5 px-3">Chi Phí USD</th>
                          <th className="py-2.5 px-3">Độ Trễ</th>
                          <th className="py-2.5 px-3">Tools Kích Hoạt</th>
                          <th className="py-2.5 px-3">Kiểm Toán & Snapshots (Quá Khứ)</th>
                          <th className="py-2.5 px-3">Kết Thúc</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-[#1f2937] font-mono bg-[#0b0f19]">
                        {detailData?.llm_calls.length === 0 ? (
                          <tr>
                            <td colSpan={8} className="py-8 text-center text-slate-500 font-sans text-xs">
                              Chưa ghi nhận lượt gọi LLM nào trong phiên này.
                            </td>
                          </tr>
                        ) : (
                          detailData?.llm_calls.map((call) => {
                            const callTime = new Date(call.created_at).toLocaleTimeString('vi-VN', {
                              hour: '2-digit',
                              minute: '2-digit',
                              second: '2-digit',
                            });

                            const costFormatted = call.cost_usd
                              ? `$${Number(call.cost_usd).toFixed(6)}`
                              : '$0.000000';

                            return (
                              <tr key={call.id} className="hover:bg-[#151d30] transition text-[11px]">
                                <td className="py-2.5 px-3 text-slate-400">{callTime}</td>
                                <td className="py-2.5 px-3 font-bold text-sky-400">
                                  {call.model || 'deepseek-chat'}
                                </td>
                                <td className="py-2.5 px-3 text-slate-300">
                                  {call.prompt_tokens || 0} / {call.completion_tokens || 0}
                                </td>
                                <td className="py-2.5 px-3 text-emerald-400 font-bold">{costFormatted}</td>
                                <td className="py-2.5 px-3 text-amber-300 font-bold">{call.latency_ms || 0} ms</td>
                                <td className="py-2.5 px-3">
                                  {Array.isArray(call.tools_called) && call.tools_called.length > 0 ? (
                                    <div className="flex items-center gap-1 flex-wrap">
                                      {call.tools_called.map((toolName, idx) => (
                                        <span
                                          key={idx}
                                          className="px-1.5 py-0.5 rounded bg-amber-500/20 text-amber-300 font-bold border border-amber-500/30 text-[10px]"
                                        >
                                          {toolName}
                                        </span>
                                      ))}
                                    </div>
                                  ) : (
                                    <span className="text-slate-500 text-[10px]">None</span>
                                  )}
                                </td>
                                <td className="py-2.5 px-3">
                                  <div className="flex items-center gap-1.5 flex-wrap">
                                    {call.status_bar_snapshot ? (
                                      <button
                                        onClick={() => handleOpenSnapshotModal('status_bar', call)}
                                        className="px-2 py-0.5 rounded bg-purple-500/20 hover:bg-purple-500/30 text-purple-300 border border-purple-500/40 text-[10px] font-bold transition flex items-center gap-1"
                                        title="Xem XML Status Bar tại lượt này"
                                      >
                                        <span>📋 Status Bar XML</span>
                                      </button>
                                    ) : (
                                      <span className="text-slate-600 text-[10px]" title="Không có snapshot">
                                        —
                                      </span>
                                    )}

                                    {call.state_snapshot ? (
                                      <button
                                        onClick={() => handleOpenSnapshotModal('state', call)}
                                        className="px-2 py-0.5 rounded bg-sky-500/20 hover:bg-sky-500/30 text-sky-300 border border-sky-500/40 text-[10px] font-bold transition flex items-center gap-1"
                                        title="Xem State JSON tại lượt này"
                                      >
                                        <span>📦 State Snapshot</span>
                                      </button>
                                    ) : (
                                      <span className="text-slate-600 text-[10px]" title="Không có snapshot">
                                        —
                                      </span>
                                    )}

                                    {call.tool_payloads ? (
                                      <button
                                        onClick={() => handleOpenSnapshotModal('tools', call)}
                                        className="px-2 py-0.5 rounded bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 border border-amber-500/40 text-[10px] font-bold transition flex items-center gap-1"
                                        title="Xem Arguments & Results của Tools"
                                      >
                                        <span>⚡ Tools</span>
                                      </button>
                                    ) : (
                                      <span className="text-slate-600 text-[10px]">—</span>
                                    )}
                                  </div>
                                </td>
                                <td className="py-2.5 px-3">
                                  <span className="px-1.5 py-0.5 rounded bg-slate-800 text-slate-300 text-[10px]">
                                    {call.finish_reason || 'stop'}
                                  </span>
                                </td>
                              </tr>
                            );
                          })
                        )}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {/* Tab 3: System Prompt & Guardrails */}
              {activeTab === 'prompt' && (
                <div className="flex-1 overflow-y-auto custom-scrollbar p-4 space-y-3 font-mono text-[11px]">
                  <div className="text-slate-300 font-sans font-bold flex items-center justify-between">
                    <span className="flex items-center gap-1.5">
                      <span>🛡️ System Prompt Hoạt Động Của AI Consultant Agent:</span>
                    </span>
                    <span className="text-[10px] text-purple-400 font-mono px-2 py-0.5 rounded bg-purple-500/10 border border-purple-500/30">
                      Strict Grounding Active
                    </span>
                  </div>
                  <pre className="p-4 rounded-xl bg-[#0b0f19] border border-[#1f2937] text-slate-300 whitespace-pre-wrap leading-relaxed custom-scrollbar overflow-x-auto">
                    {detailData?.system_prompt || 'Không có System Prompt tùy chỉnh.'}
                  </pre>
                </div>
              )}
            </>
          )}
        </section>
      </main>

      {/* Modal Reusable cho Snapshots & Payloads */}
      {snapshotModalOpen && (
        <div
          className="fixed inset-0 z-50 bg-black/75 backdrop-blur-sm flex items-center justify-center p-4"
          onClick={handleCloseSnapshotModal}
        >
          <div
            className="bg-[#111827] border border-[#1f2937] rounded-2xl w-full max-w-3xl max-h-[85vh] flex flex-col shadow-2xl overflow-hidden"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="p-4 border-b border-[#1f2937] flex items-center justify-between bg-[#0e1422]">
              <div className="flex items-center gap-2">
                <span className="text-base">{snapshotModalIcon}</span>
                <h3 className="font-bold text-sm text-white">{snapshotModalTitle}</h3>
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={handleCopySnapshotContent}
                  className="px-2.5 py-1 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold flex items-center gap-1 border border-slate-700 transition"
                >
                  <span>{copyFeedback ? '✓ Đã chép' : '📋 Sao chép'}</span>
                </button>
                <button
                  onClick={handleCloseSnapshotModal}
                  className="p-1 rounded-lg hover:bg-slate-800 text-slate-400 hover:text-white transition"
                >
                  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </div>
            <div className="p-4 overflow-y-auto custom-scrollbar flex-1 bg-[#0b0f19]">
              <pre className="text-xs font-mono text-slate-200 whitespace-pre-wrap leading-relaxed select-text">
                {snapshotModalContent}
              </pre>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminConversationsPage;
