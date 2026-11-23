import React, { useState, useEffect, useCallback } from 'react';
import { notify } from '../lib/notify';
import { apiFetch } from '../api/client';
import { AdminLayout } from '../components/admin/AdminLayout';

/**
 * Định nghĩa cấu trúc dữ liệu cấu hình LLM Gateway nhận từ API.
 */
interface LlmConfigData {
  driver: string;
  isConfigured: boolean;
  apiKeyMasked: string;
  model: string;
  baseUrl: string;
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
 * Danh sách model cài sẵn cho từng nhà cung cấp.
 */
const CLAUDE_MODELS = [
  { value: 'haiku-4.5', label: 'haiku-4.5 (Fable AI Haiku 4.5 - Tối ưu chi phí & tốc độ)' },
  { value: 'haiku-5.5', label: 'haiku-5.5 (Fable AI Haiku 5.5 - Bản nâng cấp)' },
  { value: 'claude-3-5-haiku-20241022', label: 'claude-3-5-haiku-20241022 (Claude 3.5 Haiku chính thức)' },
  { value: 'claude-3-5-sonnet-20241022', label: 'claude-3-5-sonnet-20241022 (Claude 3.5 Sonnet - Thông minh nhất)' },
  { value: 'claude-3-haiku-20240307', label: 'claude-3-haiku-20240307 (Claude 3 Haiku)' },
];

const DEEPSEEK_MODELS = [
  { value: 'deepseek-reasoner', label: 'deepseek-reasoner (DeepSeek-R1: Suy luận chuỗi tư duy CoT)' },
  { value: 'deepseek-chat', label: 'deepseek-chat (DeepSeek-V3: Tối ưu Function Calling & Đàm thoại)' },
];

/**
 * Trang Quản Trị Cấu Hình LLM Gateway (Hỗ trợ DeepSeek AI và Anthropic Claude / Fable AI).
 */
export const AdminLlmSettingsPage: React.FC = () => {
  // Trạng thái cấu hình nạp từ máy chủ
  const [config, setConfig] = useState<LlmConfigData | null>(null);
  const [loadingStatus, setLoadingStatus] = useState<boolean>(true);

  // Form inputs
  const [selectedDriver, setSelectedDriver] = useState<'claude' | 'deepseek'>('claude');
  const [apiKeyInput, setApiKeyInput] = useState<string>('');
  const [showApiKey, setShowApiKey] = useState<boolean>(false);
  const [selectedModel, setSelectedModel] = useState<string>('haiku-4.5');
  const [customModelInput, setCustomModelInput] = useState<string>('');
  const [isCustomModel, setIsCustomModel] = useState<boolean>(false);
  const [baseUrlInput, setBaseUrlInput] = useState<string>('https://api.fableaiapi.com');
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
      message: 'Sẵn sàng. Bấm nút "Kiểm Tra Kết Nối" để gửi gói tin ping thử nghiệm...',
      type: 'info',
    },
  ]);

  /**
   * Thêm một dòng nhật ký vào bảng điều khiển chẩn đoán.
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
   * Chuyển đổi nhà cung cấp (Driver) và tự động cập nhật URL / Model gợi ý.
   */
  const handleDriverChange = (driver: 'claude' | 'deepseek') => {
    setSelectedDriver(driver);
    if (driver === 'claude') {
      setSelectedModel('haiku-4.5');
      setIsCustomModel(false);
      if (baseUrlInput.includes('deepseek.com')) {
        setBaseUrlInput('https://api.fableaiapi.com');
      }
    } else {
      setSelectedModel('deepseek-reasoner');
      setIsCustomModel(false);
      if (baseUrlInput.includes('fableaiapi.com') || baseUrlInput.includes('anthropic.com')) {
        setBaseUrlInput('https://api.deepseek.com');
      }
    }
  };

  /**
   * Nạp trạng thái cấu hình LLM từ API backend.
   */
  const loadLlmStatus = useCallback(async () => {
    try {
      setLoadingStatus(true);
      const response = await apiFetch('/api/v1/llm-setting');
      const json = await response.json();

      if (json.success && json.data) {
        const d: LlmConfigData = json.data;
        setConfig(d);

        const currentDriver: 'claude' | 'deepseek' =
          d.driver?.toLowerCase() === 'claude' ||
          d.model?.toLowerCase().includes('claude') ||
          d.model?.toLowerCase().includes('haiku') ||
          d.baseUrl?.toLowerCase().includes('fableai')
            ? 'claude'
            : 'deepseek';

        setSelectedDriver(currentDriver);

        // Kiểm tra xem model có nằm trong danh sách cài sẵn không
        const activeList = currentDriver === 'claude' ? CLAUDE_MODELS : DEEPSEEK_MODELS;
        const found = activeList.some((m) => m.value === d.model);

        if (found) {
          setSelectedModel(d.model);
          setIsCustomModel(false);
        } else if (d.model) {
          setSelectedModel('custom');
          setCustomModelInput(d.model);
          setIsCustomModel(true);
        } else {
          setSelectedModel(currentDriver === 'claude' ? 'haiku-4.5' : 'deepseek-reasoner');
          setIsCustomModel(false);
        }

        setBaseUrlInput(
          d.baseUrl || (currentDriver === 'claude' ? 'https://api.fableaiapi.com' : 'https://api.deepseek.com')
        );
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
   */
  const handleSaveSettings = async (event: React.FormEvent) => {
    event.preventDefault();
    setIsSaving(true);

    const finalModel = isCustomModel ? customModelInput.trim() : selectedModel;

    if (!finalModel) {
      notify.warning('Vui lòng chọn hoặc nhập tên mô hình AI (Model name).');
      setIsSaving(false);
      return;
    }

    try {
      const response = await apiFetch('/api/v1/llm-setting', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
        },
        body: JSON.stringify({
          driver: selectedDriver,
          apiKey: apiKeyInput.trim() || undefined,
          model: finalModel,
          baseUrl: baseUrlInput.trim(),
          temperature: temperature,
        }),
      });

      const json = await response.json();
      if (!response.ok || !json.success) {
        throw new Error(json.message || 'Lỗi khi lưu cấu hình.');
      }

      notify.success(json.message || 'Lưu cấu hình LLM thành công!');
      setApiKeyInput('');
      await loadLlmStatus();
      addLog(`Đã cập nhật cấu hình [${selectedDriver.toUpperCase()}] thành công! Model: ${finalModel}`, 'success');
    } catch (err: unknown) {
      const errorMsg = err instanceof Error ? err.message : String(err);
      notify.error('Lỗi: ' + errorMsg);
      addLog(`Lỗi lưu cấu hình: ${errorMsg}`, 'error');
    } finally {
      setIsSaving(false);
    }
  };

  /**
   * Thực hiện Ping kiểm tra kết nối trực tiếp đến Gateway.
   */
  const handleTestConnection = async () => {
    setIsTesting(true);
    const providerName = selectedDriver === 'claude' ? 'Claude / Fable AI' : 'DeepSeek AI';
    addLog(`Bắt đầu kiểm tra kết nối tới ${providerName} Gateway...`, 'warn');

    try {
      const response = await apiFetch('/api/v1/llm-setting/test', {
        method: 'POST',
        headers: { Accept: 'application/json' },
      });

      const json = await response.json();
      const latency = json.latencyMs ?? 0;
      setLatencyMs(latency);

      if (response.ok && json.success) {
        setConnectionStatus('success');
        addLog(`Kết nối thành công (${latency}ms)! Driver: [${json.driver}], Model: [${json.model}].`, 'success');
        addLog(`Phản hồi kiểm tra: "${json.testOutput || 'OK'}"`, 'info');
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

  const isClaudeActive = selectedDriver === 'claude';
  const availableModels = isClaudeActive ? CLAUDE_MODELS : DEEPSEEK_MODELS;

  return (
    <AdminLayout
      title="AI Agent Travel — Quản Trị Hệ Thống"
      subtitle={`Cấu Hình Cổng LLM Gateway (${isClaudeActive ? 'Claude / Fable AI' : 'DeepSeek'})`}
      icon={isClaudeActive ? '✨' : '🧠'}
      headerActions={
        <div className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-slate-800/80 border border-slate-700/60 text-xs shrink-0">
          <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
          <span className="text-slate-300 font-medium">Server Driver:</span>
          <span className="text-emerald-400 font-mono font-semibold uppercase">{config?.driver || 'deepseek'}</span>
        </div>
      }
    >
      {/* Main Workspace */}
      <main className="flex-1 p-6 max-w-6xl w-full mx-auto space-y-6">
        {/* Banner Thông Tin & Resilient Fallback */}
        <div
          className={`p-5 rounded-2xl border flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 shadow-xl transition-all ${
            isClaudeActive
              ? 'bg-gradient-to-r from-purple-950/40 via-slate-900 to-amber-950/30 border-purple-500/30'
              : 'bg-gradient-to-r from-indigo-950/40 via-slate-900 to-blue-950/30 border-indigo-500/30'
          }`}
        >
          <div className="flex items-center space-x-3.5">
            <div
              className={`w-12 h-12 rounded-2xl border flex items-center justify-center text-2xl font-bold ${
                isClaudeActive
                  ? 'bg-purple-500/20 border-purple-500/40 text-purple-300'
                  : 'bg-indigo-500/20 border-indigo-500/40 text-indigo-400'
              }`}
            >
              {isClaudeActive ? '✨' : '⚡'}
            </div>
            <div>
              <h3 className="text-sm font-bold text-white flex items-center gap-2">
                {isClaudeActive ? 'Claude Driver (Anthropic / Fable AI)' : 'DeepSeek Driver & Resilient Fallback'}
                {config?.isConfigured ? (
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
                {isClaudeActive ? (
                  <>
                    Hỗ trợ gọi trực tiếp mô hình <strong>Haiku 4.5 / 5.5</strong> qua gateway{' '}
                    <code className="text-amber-300 font-mono">api.fableaiapi.com</code> hoặc Anthropic Messages API chính
                    thức với tốc độ phản hồi cực nhanh.
                  </>
                ) : (
                  <>
                    Hệ thống ưu tiên kết nối DeepSeek API chính thức. Nếu mất mạng hoặc API Key hết hạn, LlmGateway tự động
                    kích hoạt <strong>Simulator Fallback</strong> để việc tư vấn không bị ngắt quãng.
                  </>
                )}
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={handleTestConnection}
            disabled={isTesting}
            className={`shrink-0 px-4 py-2.5 rounded-xl font-bold text-xs transition flex items-center gap-2 shadow-lg border active:scale-95 disabled:opacity-50 text-white ${
              isClaudeActive
                ? 'bg-purple-600 hover:bg-purple-500 shadow-purple-600/30 border-purple-400/30'
                : 'bg-indigo-600 hover:bg-indigo-500 shadow-indigo-600/30 border-indigo-400/30'
            }`}
          >
            <span>{isTesting ? '⏳ Đang Ping...' : '⚡ Kiểm Tra Kết Nối Ngay'}</span>
          </button>
        </div>

        {/* 2-Column Grid Layout */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Cột Trái: Cấu Hình Tham Số LLM (7/12) */}
          <div className="lg:col-span-7 bg-[#151c2e] border border-[#222f49] rounded-2xl p-6 shadow-xl space-y-6">
            {/* Header + Tabs Chọn Nhà Cung Cấp */}
            <div className="space-y-4 pb-4 border-b border-[#222f49]">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="text-lg">⚙️</span>
                  <h2 className="text-sm font-bold text-white tracking-wide uppercase">Chọn Nhà Cung Cấp AI</h2>
                </div>
                <span className="text-xs text-slate-400">Chuyển đổi provider trực tiếp</span>
              </div>

              {/* Segmented Control Tabs */}
              <div className="grid grid-cols-2 gap-2 p-1.5 bg-[#0b0f19] border border-[#222f49] rounded-xl">
                <button
                  type="button"
                  onClick={() => handleDriverChange('claude')}
                  className={`flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-bold transition ${
                    isClaudeActive
                      ? 'bg-gradient-to-r from-purple-600 to-amber-600 text-white shadow-md shadow-purple-600/20'
                      : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
                  }`}
                >
                  <span>✨ Anthropic Claude / Fable AI</span>
                  <span className="text-[10px] px-1.5 py-0.2 rounded bg-black/30 text-amber-200">Haiku</span>
                </button>

                <button
                  type="button"
                  onClick={() => handleDriverChange('deepseek')}
                  className={`flex items-center justify-center gap-2 py-2 px-3 rounded-lg text-xs font-bold transition ${
                    !isClaudeActive
                      ? 'bg-indigo-600 text-white shadow-md shadow-indigo-600/20'
                      : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
                  }`}
                >
                  <span>🧠 DeepSeek AI</span>
                  <span className="text-[10px] px-1.5 py-0.2 rounded bg-black/30 text-indigo-200">R1 / V3</span>
                </button>
              </div>
            </div>

            <form onSubmit={handleSaveSettings} className="space-y-5">
              {/* API Key Input */}
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <label className="block text-xs font-semibold text-slate-200">
                    {isClaudeActive ? 'Claude API Key / Fable AI Auth Token' : 'DeepSeek API Key'}{' '}
                    <span className="text-rose-400">*</span>
                  </label>
                  <span className="text-[11px] font-mono text-slate-400">
                    {config?.apiKeyMasked ? `Key đã lưu: ${config.apiKeyMasked}` : 'Chưa cài API Key'}
                  </span>
                </div>
                <div className="relative">
                  <input
                    type={showApiKey ? 'text' : 'password'}
                    value={apiKeyInput}
                    onChange={(e) => setApiKeyInput(e.target.value)}
                    placeholder={
                      isClaudeActive
                        ? 'Nhập key sk-ant-fai-... hoặc sk-ant-api... (bỏ trống để giữ key cũ)'
                        : 'Nhập API Key sk-... mới (bỏ trống để giữ key cũ)'
                    }
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
                  {isClaudeActive ? (
                    <>
                      Hỗ trợ token Fable AI (<code className="text-amber-300">sk-ant-fai-...</code>) hoặc Anthropic Official (
                      <code className="text-purple-300">sk-ant-api...</code>).
                    </>
                  ) : (
                    <>
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
                    </>
                  )}
                </p>
              </div>

              {/* Model Selection */}
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <label className="block text-xs font-semibold text-slate-200">Mô Hình Suy Luận (Model Name)</label>
                  <button
                    type="button"
                    onClick={() => {
                      setIsCustomModel(!isCustomModel);
                      if (!isCustomModel) {
                        setCustomModelInput(selectedModel !== 'custom' ? selectedModel : '');
                      }
                    }}
                    className="text-[11px] text-indigo-400 hover:text-indigo-300 hover:underline"
                  >
                    {isCustomModel ? '← Chọn từ danh sách có sẵn' : '✏️ Nhập model tùy biến'}
                  </button>
                </div>

                {!isCustomModel ? (
                  <select
                    value={selectedModel}
                    onChange={(e) => {
                      if (e.target.value === 'custom') {
                        setIsCustomModel(true);
                      } else {
                        setSelectedModel(e.target.value);
                      }
                    }}
                    className="w-full bg-[#0b0f19] border border-[#222f49] rounded-xl px-3.5 py-2.5 text-xs text-slate-100 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 font-mono transition cursor-pointer"
                  >
                    {availableModels.map((m) => (
                      <option key={m.value} value={m.value}>
                        {m.label}
                      </option>
                    ))}
                    <option value="custom">✏️ Nhập model khác...</option>
                  </select>
                ) : (
                  <input
                    type="text"
                    value={customModelInput}
                    onChange={(e) => setCustomModelInput(e.target.value)}
                    placeholder="Ví dụ: haiku-4.5, claude-3-5-haiku-20241022, deepseek-chat..."
                    className="w-full bg-[#0b0f19] border border-amber-500/50 rounded-xl px-3.5 py-2.5 text-xs text-amber-200 placeholder-slate-500 focus:outline-none focus:border-amber-400 focus:ring-1 focus:ring-amber-400 font-mono transition"
                  />
                )}

                <p className="text-[11px] text-slate-400">
                  {isClaudeActive
                    ? 'Bạn có thể chọn Haiku 4.5 / 5.5 theo tài khoản Fable AI hoặc Claude 3.5 Haiku chính thức.'
                    : 'DeepSeek-Chat tối ưu cho tốc độ và Tool Calling; DeepSeek-Reasoner tối ưu phân tích chuyên sâu CoT.'}
                </p>
              </div>

              {/* API Base URL với Preset Buttons */}
              <div className="space-y-1.5">
                <div className="flex items-center justify-between">
                  <label className="block text-xs font-semibold text-slate-200">API Base URL</label>
                  <span className="text-[11px] text-slate-400">Đường dẫn cổng Gateway</span>
                </div>
                <input
                  type="text"
                  value={baseUrlInput}
                  onChange={(e) => setBaseUrlInput(e.target.value)}
                  placeholder={isClaudeActive ? 'https://api.fableaiapi.com' : 'https://api.deepseek.com'}
                  className="w-full bg-[#0b0f19] border border-[#222f49] rounded-xl px-3.5 py-2.5 text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 font-mono transition"
                />

                {/* Quick Presets */}
                <div className="flex flex-wrap items-center gap-2 pt-1">
                  <span className="text-[10px] text-slate-500">Mẫu sẵn:</span>
                  {isClaudeActive ? (
                    <>
                      <button
                        type="button"
                        onClick={() => setBaseUrlInput('https://api.fableaiapi.com')}
                        className={`text-[10px] font-mono px-2 py-0.5 rounded border transition ${
                          baseUrlInput === 'https://api.fableaiapi.com'
                            ? 'bg-amber-500/20 text-amber-300 border-amber-500/40 font-bold'
                            : 'bg-slate-800 text-slate-400 border-slate-700 hover:text-slate-200'
                        }`}
                      >
                        🌐 Fable AI (api.fableaiapi.com)
                      </button>
                      <button
                        type="button"
                        onClick={() => setBaseUrlInput('https://api.anthropic.com')}
                        className={`text-[10px] font-mono px-2 py-0.5 rounded border transition ${
                          baseUrlInput === 'https://api.anthropic.com'
                            ? 'bg-purple-500/20 text-purple-300 border-purple-500/40 font-bold'
                            : 'bg-slate-800 text-slate-400 border-slate-700 hover:text-slate-200'
                        }`}
                      >
                        🏢 Anthropic Official (api.anthropic.com)
                      </button>
                    </>
                  ) : (
                    <button
                      type="button"
                      onClick={() => setBaseUrlInput('https://api.deepseek.com')}
                      className={`text-[10px] font-mono px-2 py-0.5 rounded border transition ${
                        baseUrlInput === 'https://api.deepseek.com'
                          ? 'bg-indigo-500/20 text-indigo-300 border-indigo-500/40 font-bold'
                          : 'bg-slate-800 text-slate-400 border-slate-700 hover:text-slate-200'
                      }`}
                    >
                      🌐 DeepSeek Official (api.deepseek.com)
                    </button>
                  )}
                </div>
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
                  className={`px-5 py-2.5 rounded-xl font-bold text-xs shadow-lg border transition flex items-center gap-2 active:scale-95 disabled:opacity-50 text-white ${
                    isClaudeActive
                      ? 'bg-purple-600 hover:bg-purple-500 shadow-purple-600/30 border-purple-400/30'
                      : 'bg-indigo-600 hover:bg-indigo-500 shadow-indigo-600/30 border-indigo-400/30'
                  }`}
                >
                  <span>{isSaving ? '⏳ Đang Lưu...' : `💾 Lưu Cấu Hình [${selectedDriver.toUpperCase()}]`}</span>
                </button>
              </div>
            </form>
          </div>

          {/* Cột Phải: Chẩn Đoán & Đo Độ Trễ Ping Test (5/12) */}
          <div className="lg:col-span-5 bg-[#151c2e] border border-[#222f49] rounded-2xl p-6 shadow-xl flex flex-col justify-between space-y-6">
            <div className="space-y-5">
              <div className="pb-4 border-b border-[#222f49] flex items-center gap-2">
                <span className="text-lg">⚡</span>
                <h2 className="text-sm font-bold text-white tracking-wide uppercase">
                  Chẩn Đoán & Đo Độ Trễ (Ping Test)
                </h2>
              </div>
              <p className="text-xs text-slate-400">
                Kiểm tra gói tin thực tế tới máy chủ{' '}
                <strong className="text-slate-200">
                  {isClaudeActive ? 'Claude / Fable AI (Messages Endpoint)' : 'DeepSeek AI'}
                </strong>
              </p>

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
                className={`w-full py-2.5 rounded-xl text-white font-bold text-xs border transition flex items-center justify-center gap-2 active:scale-95 disabled:opacity-50 ${
                  isClaudeActive
                    ? 'bg-purple-900/60 hover:bg-purple-800/80 text-purple-200 border-purple-700/50'
                    : 'bg-slate-800 hover:bg-slate-700 text-slate-200 border-slate-700'
                }`}
              >
                <span>
                  {isTesting
                    ? '⏳ Đang gửi yêu cầu ping...'
                    : `⚡ Ping ${isClaudeActive ? 'Claude / Fable AI' : 'DeepSeek AI'} Gateway`}
                </span>
              </button>
            </div>
          </div>
        </div>
      </main>
    </AdminLayout>
  );
};

export default AdminLlmSettingsPage;
