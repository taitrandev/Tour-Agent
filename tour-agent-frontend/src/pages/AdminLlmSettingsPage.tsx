import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';

/**
 * Định nghĩa cấu trúc dữ liệu cấu hình LLM Gateway.
 */
interface LlmConfigData {
  driver: string;
  is_configured: boolean;
  api_key_masked: string;
  model: string;
  base_url: string;
  temperature: number;
}

/**
 * Định nghĩa cấu trúc dòng nhật ký chẩn đoán.
 */
interface DiagnosticLogItem {
  id: string;
  time: string;
  message: string;
  type: 'info' | 'success' | 'warn' | 'error';
}

/**
 * Trang Quản Trị Cấu Hình LLM Gateway (DeepSeek AI).
 * Tái hiện 100% giao diện và nghiệp vụ từ resources/views/admin/llm-settings.blade.php của PHP core.
 */
export const AdminLlmSettingsPage: React.FC = () => {
  // Trạng thái cấu hình nạp từ máy chủ
  const [config, setConfig] = useState<LlmConfigData | null>(null);
  const [loadingStatus, setLoadingStatus] = useState<boolean>(true);

  // Form inputs
  const [apiKeyInput, setApiKeyInput] = useState<string>('');
  const [showApiKey, setShowApiKey] = useState<boolean>(false);
  const [selectedModel, setSelectedModel] = useState<string>('deepseek-reasoner');
  const [baseUrlInput, setBaseUrlInput] = useState<string>('https://api.deepseek.com');
  const [temperature, setTemperature] = useState<number>(0.2);

  // Trạng thái lưu cấu hình
  const [isSaving, setIsSaving] = useState<boolean>(false);

  // Trạng thái kiểm tra kết nối (Ping test)
  const [isTesting, setIsTesting] = useState<boolean>(false);
  const [latencyMs, setLatencyMs] = useState<number | null>(null);
  const [connectionStatus, setConnectionStatus] = useState<'idle' | 'success' | 'failed'>('idle');

  // Nhật ký chẩn đoán
  const [logs, setLogs] = useState<DiagnosticLogItem[]>([
    {
      id: 'init-1',
      time: new Date().toLocaleTimeString('vi-VN'),
      message: 'Bấm nút "Kiểm Tra Kết Nối" để gửi gói tin ping thử nghiệm...',
      type: 'info',
    },
  ]);

  /**
   * Thêm một dòng nhật ký vào bảng điều khiển chẩn đoán.
   * @param message Nội dung thông điệp
   * @param type Phân loại cấp độ log (info, success, warn, error)
   */
  const addLog = useCallback((message: string, type: 'info' | 'success' | 'warn' | 'error' = 'info') => {
    const timeString = new Date().toLocaleTimeString('vi-VN');
    setLogs((prev) => [
      ...prev,
      {
        id: `${Date.now()}-${Math.random()}`,
        time: timeString,
        message,
        type,
      },
    ]);
  }, []);

  /**
   * Nạp trạng thái cấu hình LLM từ API backend.
   */
  const loadLlmStatus = useCallback(async () => {
    try {
      setLoadingStatus(true);
      const response = await fetch('/api/v1/llm-setting');
      const json = await response.json();

      if (json.success && json.data) {
        const d: LlmConfigData = json.data;
        setConfig(d);
        setSelectedModel(d.model || 'deepseek-reasoner');
        setBaseUrlInput(d.base_url || 'https://api.deepseek.com');
        setTemperature(d.temperature ?? 0.2);
      }
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : String(err);
      addLog(`Lỗi khi tải cấu hình LLM: ${errorMsg}`, 'error');
    } finally {
      setLoadingStatus(false);
    }
  }, [addLog]);

  useEffect(() => {
    loadLlmStatus();
  }, [loadLlmStatus]);

  /**
   * Xử lý lưu cấu hình LLM mới lên backend.
   * @param event Sự kiện Submit Form
   */
  const handleSaveSettings = async (event: React.FormEvent) => {
    event.preventDefault();
    setIsSaving(true);

    try {
      const response = await fetch('/api/v1/llm-setting', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
        },
        body: JSON.stringify({
          api_key: apiKeyInput.trim() || undefined,
          model: selectedModel,
          base_url: baseUrlInput.trim(),
          temperature: temperature,
        }),
      });

      const json = await response.json();
      if (!response.ok || !json.success) {
        throw new Error(json.message || 'Lỗi khi lưu cấu hình.');
      }

      alert('✅ ' + json.message);
      setApiKeyInput('');
      await loadLlmStatus();
      addLog('Đã cập nhật cấu hình LLM thành công.', 'success');
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : String(err);
      alert('❌ Lỗi: ' + errorMsg);
      addLog(`Lỗi lưu cấu hình: ${errorMsg}`, 'error');
    } finally {
      setIsSaving(false);
    }
  };

  /**
   * Thực hiện Ping kiểm tra kết nối trực tiếp đến DeepSeek AI Gateway.
   */
  const handleTestConnection = async () => {
    setIsTesting(true);
    addLog('Bắt đầu kiểm tra kết nối tới DeepSeek API Gateway...', 'warn');

    try {
      const response = await fetch('/api/v1/llm-setting/test', {
        method: 'POST',
        headers: { Accept: 'application/json' },
      });

      const json = await response.json();
      const latency = json.latency_ms ?? 0;
      setLatencyMs(latency);

      if (response.ok && json.success) {
        setConnectionStatus('success');
        addLog(`Kết nối thành công (${latency}ms)! Driver: [${json.driver}], Model: [${json.model}].`, 'success');
        addLog(`Phản hồi kiểm tra: "${json.test_output || 'OK'}"`, 'info');
      } else {
        setConnectionStatus('failed');
        addLog(`Kết nối thất bại (${latency}ms): ${json.message || 'Lỗi kiểm tra API'}`, 'error');
      }
    } catch (err: unknown) {
      setConnectionStatus('failed');
      const errorMsg = err instanceof Error ? err.message : String(err);
      addLog(`Lỗi kết nối máy chủ: ${errorMsg}`, 'error');
    } finally {
      setIsTesting(false);
    }
  };

  return (
    <div className="min-h-screen flex flex-col font-sans bg-slate-950 text-slate-100">
      {/* Top Navigation Header */}
      <header className="h-16 border-b border-slate-800 bg-slate-900/80 backdrop-blur px-6 flex items-center justify-between shrink-0 z-20 sticky top-0">
        <div className="flex items-center gap-4">
          <Link
            to="/admin"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-semibold border border-slate-700/80 transition shadow-sm"
          >
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            <span>← Dashboard Quản Trị</span>
          </Link>

          <div className="h-6 w-px bg-slate-800" />

          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-gradient-to-tr from-indigo-500 to-purple-600 flex items-center justify-center font-bold text-white text-base shadow-lg shadow-indigo-500/20">
              🧠
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h1 className="text-sm sm:text-base font-bold text-white tracking-tight">Cấu Hình LLM Gateway (DeepSeek)</h1>
                <span className="text-[10px] font-semibold tracking-wider uppercase px-2 py-0.5 rounded-full bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">
                  Module LlmSetting
                </span>
              </div>
              <p className="text-xs text-slate-400">Quản trị API Keys, lựa chọn Model suy luận, nhiệt độ và đo kiểm tra độ trễ (Latency)</p>
            </div>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <Link
            to="/admin/escalations"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-amber-500/15 hover:bg-amber-500/25 text-amber-300 text-xs font-semibold border border-amber-500/30 transition"
          >
            <span>🛎️ Bàn Quyết Định</span>
          </Link>
          <Link
            to="/admin/channels/telegram"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-sky-500/15 hover:bg-sky-500/25 text-sky-300 text-xs font-semibold border border-sky-500/30 transition"
          >
            <span>📱 Kênh Telegram</span>
          </Link>
          <div className="flex items-center gap-2 px-3 py-1 rounded-lg bg-slate-800/80 border border-slate-700/60 text-xs">
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
            <span className="text-slate-300 font-medium">Driver:</span>
            <span className="text-emerald-400 font-mono font-semibold">{config?.driver || 'deepseek'}</span>
          </div>
        </div>
      </header>

      {/* Main Workspace */}
      <main className="flex-1 p-6 max-w-6xl w-full mx-auto space-y-6">
        {/* Status & Banner Notification */}
        <div className="p-5 rounded-2xl bg-gradient-to-r from-indigo-950/40 via-slate-900 to-purple-950/30 border border-indigo-500/30 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 shadow-xl">
          <div className="flex items-center space-x-3.5">
            <div className="w-12 h-12 rounded-2xl bg-indigo-500/20 border border-indigo-500/40 flex items-center justify-center text-indigo-400 text-2xl font-bold">
              ⚡
            </div>
            <div>
              <h3 className="text-sm font-bold text-white flex items-center gap-2">
                DeepSeek Driver & Resilient Fallback
                {config?.is_configured ? (
                  <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-emerald-500/20 text-emerald-400 font-bold border border-emerald-500/30">
                    Đã Cấu Hình Key
                  </span>
                ) : (
                  <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-amber-500/20 text-amber-300 font-bold border border-amber-500/30">
                    Chưa Nhập Key
                  </span>
                )}
              </h3>
              <p className="text-xs text-slate-300 mt-0.5 leading-relaxed">
                Hệ thống ưu tiên kết nối DeepSeek API chính thức. Nếu mất mạng hoặc API Key hết hạn, LlmGateway tự động kích hoạt chế độ{' '}
                <strong>Simulator Fallback</strong> để tư vấn tour không bị gián đoạn.
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={handleTestConnection}
            disabled={isTesting}
            className="shrink-0 px-4 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 active:scale-95 text-white font-bold text-xs transition flex items-center gap-2 shadow-lg shadow-indigo-600/30 border border-indigo-400/30 disabled:opacity-50"
          >
            <span>{isTesting ? '⏳ Đang Ping...' : '⚡ Kiểm Tra Kết Nối Ngay'}</span>
          </button>
        </div>

        {/* 2-Column Grid Layout */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Cột Trái: Cấu Hình Tham Số DeepSeek (7/12) */}
          <div className="lg:col-span-7 bg-[#151c2e] border border-[#222f49] rounded-2xl p-6 shadow-xl space-y-6">
            <div className="flex items-center justify-between pb-4 border-b border-[#222f49]">
              <div className="flex items-center gap-2">
                <span className="text-lg">🔑</span>
                <h2 className="text-sm font-bold text-white tracking-wide uppercase">Cấu Hình Tham Số DeepSeek</h2>
              </div>
              <span className="text-xs text-slate-400">Các thông số này được lưu an toàn vào tệp môi trường của hệ thống</span>
            </div>

            <form onSubmit={handleSaveSettings} className="space-y-5">
              {/* DeepSeek API Key */}
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <label className="block text-xs font-semibold text-slate-200">
                    DeepSeek API Key <span className="text-rose-400">*</span>
                  </label>
                  <span className="text-[11px] font-mono text-slate-400">
                    {config?.api_key_masked ? `Key: ${config.api_key_masked}` : 'Chưa cài API Key'}
                  </span>
                </div>
                <div className="relative">
                  <input
                    type={showApiKey ? 'text' : 'password'}
                    value={apiKeyInput}
                    onChange={(e) => setApiKeyInput(e.target.value)}
                    placeholder="Nhập API Key mới (bỏ trống nếu muốn giữ key cũ)..."
                    className="w-full bg-[#0b0f19] border border-[#222f49] rounded-xl px-3.5 py-2.5 text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 pr-10 font-mono transition"
                  />
                  <button
                    type="button"
                    onClick={() => setShowApiKey(!showApiKey)}
                    className="absolute right-3 top-2.5 text-slate-400 hover:text-slate-200 transition"
                    title="Hiện/Ẩn Key"
                  >
                    {showApiKey ? '👁️' : '👁️‍🗨️'}
                  </button>
                </div>
                <p className="text-[11px] text-slate-400">
                  Lấy API Key từ{' '}
                  <a
                    href="https://platform.deepseek.com/api_keys"
                    target="_blank"
                    rel="noreferrer"
                    className="text-indigo-400 hover:underline font-semibold"
                  >
                    DeepSeek Platform
                  </a>{' '}
                  (Bắt đầu bằng <code className="text-amber-300">sk-...</code>).
                </p>
              </div>

              {/* Model Selection */}
              <div className="space-y-1.5">
                <label className="block text-xs font-semibold text-slate-200">Model Suy Luận (Model Name)</label>
                <select
                  value={selectedModel}
                  onChange={(e) => setSelectedModel(e.target.value)}
                  className="w-full bg-[#0b0f19] border border-[#222f49] rounded-xl px-3.5 py-2.5 text-xs text-slate-100 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 font-mono transition cursor-pointer"
                >
                  <option value="deepseek-reasoner">deepseek-reasoner (DeepSeek-R1: Suy luận chuỗi tư duy CoT)</option>
                  <option value="deepseek-chat">deepseek-chat (DeepSeek-V3: Tối ưu Function Calling & Đàm thoại tốc độ cao)</option>
                </select>
                <p className="text-[11px] text-slate-400">
                  Hệ thống tự động sử dụng <code className="text-indigo-300 font-semibold">deepseek-chat</code> khi cần thực thi công cụ
                  (Function Calling) và <code className="text-purple-300 font-semibold">deepseek-reasoner</code> cho phân tích chuyên sâu.
                </p>
              </div>

              {/* API Base URL */}
              <div className="space-y-1.5">
                <label className="block text-xs font-semibold text-slate-200">API Base URL</label>
                <input
                  type="text"
                  value={baseUrlInput}
                  onChange={(e) => setBaseUrlInput(e.target.value)}
                  placeholder="https://api.deepseek.com"
                  className="w-full bg-[#0b0f19] border border-[#222f49] rounded-xl px-3.5 py-2.5 text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 font-mono transition"
                />
              </div>

              {/* Temperature Slider */}
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <label className="block text-xs font-semibold text-slate-200">Nhiệt Độ (Temperature):</label>
                  <span className="text-xs font-mono font-bold text-indigo-400">{temperature.toFixed(1)}</span>
                </div>
                <input
                  type="range"
                  min="0"
                  max="1"
                  step="0.05"
                  value={temperature}
                  onChange={(e) => setTemperature(parseFloat(e.target.value))}
                  className="w-full accent-indigo-500 cursor-pointer"
                />
                <div className="flex justify-between text-[10px] text-slate-500 font-medium">
                  <span>0.0 (Chính xác, trung thực nhất)</span>
                  <span>0.5</span>
                  <span>1.0 (Sáng tạo cao)</span>
                </div>
              </div>

              {/* Actions Footer */}
              <div className="pt-4 border-t border-[#222f49] flex items-center justify-between">
                <button
                  type="button"
                  onClick={loadLlmStatus}
                  disabled={loadingStatus}
                  className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 font-semibold text-xs border border-slate-700 transition flex items-center gap-1.5"
                >
                  <span>🔄 Làm mới dữ liệu</span>
                </button>

                <button
                  type="submit"
                  disabled={isSaving}
                  className="px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 active:scale-95 text-white font-bold text-xs shadow-lg shadow-indigo-600/30 border border-indigo-400/30 transition flex items-center gap-2 disabled:opacity-50"
                >
                  <span>{isSaving ? '⏳ Đang Lưu...' : '💾 Lưu Cấu Hình LLM'}</span>
                </button>
              </div>
            </form>
          </div>

          {/* Cột Phải: Chẩn Đoán & Đo Độ Trễ Ping Test (5/12) */}
          <div className="lg:col-span-5 bg-[#151c2e] border border-[#222f49] rounded-2xl p-6 shadow-xl flex flex-col justify-between space-y-6">
            <div className="space-y-5">
              <div className="pb-4 border-b border-[#222f49] flex items-center gap-2">
                <span className="text-lg">⚡</span>
                <h2 className="text-sm font-bold text-white tracking-wide uppercase">Chẩn Đoán & Đo Độ Trễ (Ping Test)</h2>
              </div>
              <p className="text-xs text-slate-400">Kiểm tra kết nối thực tế tới máy chủ DeepSeek</p>

              {/* 2 Stat Boxes */}
              <div className="grid grid-cols-2 gap-4">
                <div className="p-4 rounded-xl bg-[#0b0f19] border border-[#222f49]">
                  <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">ĐỘ TRỄ API (LATENCY)</div>
                  <div className="text-xl font-bold font-mono text-white mt-1">
                    {latencyMs !== null ? `${latencyMs} ms` : '-- ms'}
                  </div>
                </div>

                <div className="p-4 rounded-xl bg-[#0b0f19] border border-[#222f49]">
                  <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">TRẠNG THÁI KẾT NỐI</div>
                  <div className="text-xs font-semibold mt-2">
                    {connectionStatus === 'idle' && <span className="text-slate-400">Chưa kiểm tra</span>}
                    {connectionStatus === 'success' && <span className="text-emerald-400">● Hoạt Động (OK)</span>}
                    {connectionStatus === 'failed' && <span className="text-rose-400">● Thất Bại</span>}
                  </div>
                </div>
              </div>

              {/* Diagnostic Console */}
              <div className="space-y-1.5">
                <label className="block font-semibold text-slate-300 text-xs">Nhật Ký Chẩn Đoán:</label>
                <div className="p-3.5 rounded-xl bg-[#080c14] border border-[#222f49] font-mono text-[11px] space-y-1 min-h-[160px] max-h-[220px] overflow-y-auto">
                  {logs.map((item) => {
                    let colorClass = 'text-slate-300';
                    if (item.type === 'success') colorClass = 'text-emerald-400 font-semibold';
                    if (item.type === 'error') colorClass = 'text-rose-400 font-semibold';
                    if (item.type === 'warn') colorClass = 'text-amber-400';

                    return (
                      <div key={item.id} className={colorClass}>
                        <span className="text-slate-500">[{item.time}]</span> {item.message}
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>

            {/* Ping Action Button */}
            <div className="pt-3 border-t border-[#222f49]">
              <button
                type="button"
                onClick={handleTestConnection}
                disabled={isTesting}
                className="w-full py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 font-bold text-xs border border-slate-700 transition flex items-center justify-center gap-2 disabled:opacity-50"
              >
                <span>{isTesting ? '⏳ Đang gửi yêu cầu ping...' : '⚡ Ping DeepSeek AI Gateway'}</span>
              </button>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};
export default AdminLlmSettingsPage;
