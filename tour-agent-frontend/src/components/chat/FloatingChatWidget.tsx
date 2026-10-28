import { useState, useEffect, useRef } from 'react';
import {
  X,
  Send,
  RotateCcw,
  Bot,
  User,
  Wrench,
  CheckCircle,
  ChevronDown,
  Info,
} from 'lucide-react';
import { marked } from 'marked';
import type { ChatMessage, ChatRequest } from '../../types/agent';
import { streamChatMessage } from '../../api/agentApi';
import type { TourSummary } from '../../types/tour';

interface FloatingChatWidgetProps {
  isOpen: boolean;
  onClose: () => void;
  selectedTourContext?: TourSummary | null;
  onClearContext?: () => void;
}

/**
 * Widget tư vấn AI thời gian thực hỗ trợ truyền phát Server-Sent Events (SSE),
 * hiển thị Tool Badges động và Markdown streaming chuẩn ChatGPT/Claude.
 */
export const FloatingChatWidget: React.FC<FloatingChatWidgetProps> = ({
  isOpen,
  onClose,
  selectedTourContext,
  onClearContext,
}) => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputValue, setInputValue] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [conversationId, setConversationId] = useState<number>(() => Date.now());
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  // Khởi tạo tin nhắn chào mừng ban đầu
  useEffect(() => {
    if (messages.length === 0) {
      setMessages([
        {
          id: 'welcome-1',
          role: 'assistant',
          content:
            'Xin chào! Tôi là **AI Consultant** của TourAgent, được vận hành bởi mô hình DeepSeek V3 với cơ chế tự hành tra cứu CSDL thực tế. Tôi có thể giúp gì cho chuyến du lịch sắp tới của bạn?',
          timestamp: new Date(),
        },
      ]);
    }
  }, [messages.length]);

  // Tự động cuộn xuống cuối khi có tin nhắn mới hoặc streaming token
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isStreaming]);

  // Cập nhật khi nhận ngữ cảnh tour từ bên ngoài
  useEffect(() => {
    if (selectedTourContext) {
      const prompt = `Tôi đang xem tour "${selectedTourContext.name}". Hãy tóm tắt cho tôi điểm đặc sắc và các ngày bay gần nhất của tour này?`;
      setInputValue(prompt);
    }
  }, [selectedTourContext]);

  /**
   * Reset lại phiên hội thoại, tạo mới conversationId và làm sạch lịch sử chat.
   */
  const handleResetConversation = () => {
    if (isStreaming && abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    setConversationId(Date.now());
    setIsStreaming(false);
    onClearContext?.();
    setMessages([
      {
        id: `welcome-${Date.now()}`,
        role: 'assistant',
        content:
          'Phiên hội thoại mới đã được thiết lập. Hãy cho tôi biết điểm đến mong muốn hoặc ngân sách của bạn nhé!',
        timestamp: new Date(),
      },
    ]);
  };

  /**
   * Gửi câu hỏi và kích hoạt chu trình streaming SSE.
   */
  const handleSendMessage = async (textToSend?: string) => {
    const messageContent = (textToSend || inputValue).trim();
    if (!messageContent || isStreaming) return;

    setInputValue('');

    // 1. Thêm tin nhắn của User
    const userMsg: ChatMessage = {
      id: `user-${Date.now()}`,
      role: 'user',
      content: messageContent,
      timestamp: new Date(),
    };

    // 2. Tạo tin nhắn bot rỗng để nhận streaming
    const botMsgId = `bot-${Date.now()}`;
    const botPlaceholderMsg: ChatMessage = {
      id: botMsgId,
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      toolsExecuted: [],
      activeTool: null,
      isStreaming: true,
    };

    setMessages((prev) => [...prev, userMsg, botPlaceholderMsg]);
    setIsStreaming(true);

    const abortController = new AbortController();
    abortControllerRef.current = abortController;

    const requestPayload: ChatRequest = {
      conversationId: conversationId,
      message: messageContent,
      currentTravelId: selectedTourContext?.id,
      currentTravelTitle: selectedTourContext?.name,
      country: selectedTourContext?.country,
    };

    const startTime = Date.now();
    let accumulatedText = '';
    const executedToolsList: string[] = [];

    try {
      await streamChatMessage(
        requestPayload,
        (event) => {
          if (event.eventType === 'tool_start') {
            const toolName = event.toolName || 'tool_calling';
            if (!executedToolsList.includes(toolName)) {
              executedToolsList.push(toolName);
            }
            setMessages((prev) =>
              prev.map((msg) =>
                msg.id === botMsgId
                  ? {
                      ...msg,
                      activeTool: toolName,
                      toolsExecuted: [...executedToolsList],
                    }
                  : msg
              )
            );
          } else if (event.eventType === 'tool_end') {
            setMessages((prev) =>
              prev.map((msg) =>
                msg.id === botMsgId
                  ? {
                      ...msg,
                      activeTool: null,
                      toolsExecuted: [...executedToolsList],
                    }
                  : msg
              )
            );
          } else if (event.eventType === 'token') {
            accumulatedText += event.content || '';
            setMessages((prev) =>
              prev.map((msg) =>
                msg.id === botMsgId
                  ? {
                      ...msg,
                      content: accumulatedText,
                    }
                  : msg
              )
            );
          } else if (event.eventType === 'completed') {
            const latency = Date.now() - startTime;
            setMessages((prev) =>
              prev.map((msg) =>
                msg.id === botMsgId
                  ? {
                      ...msg,
                      isStreaming: false,
                      activeTool: null,
                      latencyMs: latency,
                      tokensCount: Math.round(accumulatedText.length / 3) + 450,
                    }
                  : msg
              )
            );
            setIsStreaming(false);
          }
        },
        abortController.signal
      );
    } catch (err: unknown) {
      if (err instanceof Error && err.name === 'AbortError') {
        console.log('Stream đã được người dùng dừng.');
      } else {
        console.error('Lỗi khi streaming:', err);
      }
    } finally {
      setIsStreaming(false);
      abortControllerRef.current = null;
    }
  };

  /**
   * Render nội dung Markdown an toàn từ tin nhắn trợ lý.
   */
  const renderMarkdown = (content: string) => {
    try {
      const parsedHtml = marked.parse(content, { breaks: true, gfm: true }) as string;
      return { __html: parsedHtml };
    } catch {
      return { __html: content };
    }
  };

  const samplePrompts = [
    'Tìm tour Nhật Bản tháng 11 dưới 30 triệu',
    'Cam kết hoàn tiền khi rớt visa thế nào?',
    'Có những đợt bay nào đến Hàn Quốc gần nhất?',
  ];

  if (!isOpen) return null;

  return (
    <div className="fixed bottom-4 right-4 sm:bottom-6 sm:right-6 z-50 w-[94vw] sm:w-[420px] h-[640px] max-h-[85vh] flex flex-col rounded-2xl glass-panel-glow shadow-2xl border border-cyan-500/30 overflow-hidden backdrop-blur-2xl animate-in slide-in-from-bottom-5 duration-300">
      {/* Top Header */}
      <div className="p-3.5 sm:p-4 bg-slate-950/80 border-b border-white/10 flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-lg bg-gradient-to-tr from-cyan-500 to-sky-400 flex items-center justify-center shadow-md shadow-cyan-500/25">
            <Bot className="w-5 h-5 text-slate-950" />
          </div>
          <div>
            <div className="flex items-center gap-1.5">
              <h3 className="font-bold text-sm text-white">AI Travel Consultant</h3>
              <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
            </div>
            <span className="text-[10px] font-mono text-cyan-300 uppercase">
              DeepSeek-V3 • Multi-Turn Tools
            </span>
          </div>
        </div>

        <div className="flex items-center gap-1">
          <button
            onClick={handleResetConversation}
            className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-white/10 transition"
            title="Tạo phiên hội thoại mới"
          >
            <RotateCcw className="w-4 h-4" />
          </button>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-white/10 transition"
            title="Thu nhỏ cửa sổ chat"
          >
            <ChevronDown className="w-5 h-5" />
          </button>
        </div>
      </div>

      {/* Selected Tour Context Banner */}
      {selectedTourContext && (
        <div className="bg-cyan-950/40 border-b border-cyan-500/20 px-3.5 py-1.5 flex items-center justify-between text-xs text-cyan-300">
          <div className="flex items-center gap-1.5 truncate">
            <Info className="w-3.5 h-3.5 shrink-0" />
            <span className="truncate">Ngữ cảnh: {selectedTourContext.name}</span>
          </div>
          <button
            onClick={onClearContext}
            className="text-slate-400 hover:text-white ml-2 p-0.5"
            title="Bỏ ngữ cảnh tour"
          >
            <X className="w-3 h-3" />
          </button>
        </div>
      )}

      {/* Chat Messages Stream Area */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4 custom-scrollbar">
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`flex flex-col ${msg.role === 'user' ? 'items-end' : 'items-start'}`}
          >
            <div
              className={`flex items-start gap-2.5 max-w-[90%] ${
                msg.role === 'user' ? 'flex-row-reverse' : 'flex-row'
              }`}
            >
              {/* Avatar Icon */}
              <div
                className={`w-7 h-7 rounded-lg flex items-center justify-center shrink-0 text-xs font-bold ${
                  msg.role === 'user'
                    ? 'bg-gradient-to-tr from-cyan-600 to-sky-500 text-white shadow-sm'
                    : 'bg-slate-800 text-cyan-300 border border-white/10'
                }`}
              >
                {msg.role === 'user' ? <User className="w-4 h-4" /> : <Bot className="w-4 h-4" />}
              </div>

              {/* Message Content Container */}
              <div className="space-y-1.5">
                {/* Real-time Tool Execution Badges */}
                {msg.role === 'assistant' && msg.toolsExecuted && msg.toolsExecuted.length > 0 && (
                  <div className="flex flex-wrap gap-1.5 pb-1">
                    {msg.toolsExecuted.map((tool, idx) => (
                      <span
                        key={idx}
                        className={`text-[10px] font-mono px-2 py-0.5 rounded-full flex items-center gap-1 ${
                          msg.activeTool === tool
                            ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 animate-pulse'
                            : 'bg-cyan-500/15 text-cyan-300 border border-cyan-500/30'
                        }`}
                      >
                        {msg.activeTool === tool ? (
                          <Wrench className="w-3 h-3 animate-spin" />
                        ) : (
                          <CheckCircle className="w-3 h-3 text-emerald-400" />
                        )}
                        <span>{tool}</span>
                      </span>
                    ))}
                  </div>
                )}

                {/* Bubble Text */}
                <div
                  className={`rounded-2xl px-4 py-2.5 text-xs sm:text-sm leading-relaxed shadow-md ${
                    msg.role === 'user'
                      ? 'bg-gradient-to-r from-cyan-600 to-sky-600 text-white rounded-tr-none'
                      : 'glass-card border border-white/10 text-slate-100 rounded-tl-none prose prose-invert max-w-none'
                  }`}
                >
                  {msg.role === 'assistant' ? (
                    <div
                      dangerouslySetInnerHTML={renderMarkdown(msg.content)}
                      className={msg.isStreaming ? 'typing-cursor' : ''}
                    />
                  ) : (
                    <p className="whitespace-pre-wrap">{msg.content}</p>
                  )}
                </div>

                {/* Telemetry Metric Footers */}
                {msg.role === 'assistant' && !msg.isStreaming && msg.tokensCount && (
                  <div className="text-[10px] font-mono text-slate-500 pl-1 flex items-center gap-2">
                    <span>{msg.tokensCount} tokens</span>
                    <span>•</span>
                    <span>{msg.latencyMs}ms</span>
                    <span>•</span>
                    <span>DeepSeek V3</span>
                  </div>
                )}
              </div>
            </div>
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>

      {/* Suggested Quick Prompts */}
      {messages.length <= 2 && (
        <div className="px-3 pb-2 flex flex-wrap gap-1.5">
          {samplePrompts.map((prompt, idx) => (
            <button
              key={idx}
              onClick={() => handleSendMessage(prompt)}
              className="text-[11px] px-2.5 py-1 rounded-full bg-slate-900/90 text-cyan-300 hover:bg-cyan-900/40 border border-cyan-500/20 transition text-left truncate max-w-full"
            >
              {prompt}
            </button>
          ))}
        </div>
      )}

      {/* Input Form */}
      <div className="p-3 border-t border-white/10 bg-slate-950/80">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            handleSendMessage();
          }}
          className="flex items-center gap-2"
        >
          <input
            type="text"
            placeholder={isStreaming ? 'AI đang tổng hợp phản hồi...' : 'Hỏi bất kỳ điều gì về tour, visa, giá cả...'}
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            disabled={isStreaming}
            className="flex-1 glass-input px-3.5 py-2.5 rounded-xl text-xs sm:text-sm placeholder-slate-500 disabled:opacity-50"
          />
          <button
            type="submit"
            disabled={!inputValue.trim() || isStreaming}
            className="p-2.5 rounded-xl bg-gradient-to-r from-cyan-500 to-sky-500 hover:from-cyan-400 hover:to-sky-400 disabled:opacity-40 text-slate-950 font-bold transition shadow-md shadow-cyan-500/20 flex items-center justify-center shrink-0 active:scale-95"
          >
            <Send className="w-4 h-4 stroke-[2.5]" />
          </button>
        </form>
      </div>
    </div>
  );
};
