/**
 * Chuẩn hóa base URL: bỏ dấu / ở cuối nếu có.
 * Mặc định: VITE_API_BASE_URL nếu có, fallback về 'http://localhost:5132/api/v1'.
 */
const rawBaseUrl = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5132/api/v1').replace(/\/+$/, '');

// Đảm bảo baseUrl luôn kết thúc bằng /api/v1
export const API_BASE_URL = rawBaseUrl.endsWith('/api/v1')
  ? rawBaseUrl
  : `${rawBaseUrl}/api/v1`;

/**
 * Xây dựng URL API hoàn chỉnh từ một endpoint bất kỳ.
 * Tự động gắn API_BASE_URL và chống trùng lặp '/api/v1'.
 * Hỗ trợ cả:
 *   buildApiUrl('/offers')            -> 'https://.../api/v1/offers'
 *   buildApiUrl('/api/v1/offers')     -> 'https://.../api/v1/offers'
 *   buildApiUrl('api/v1/offers')      -> 'https://.../api/v1/offers'
 */
export function buildApiUrl(endpoint: string): string {
  if (endpoint.startsWith('http://') || endpoint.startsWith('https://')) {
    return endpoint;
  }
  const cleanEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
  if (cleanEndpoint.startsWith('/api/v1/')) {
    const subPath = cleanEndpoint.substring('/api/v1'.length);
    return `${API_BASE_URL}${subPath}`;
  }
  if (cleanEndpoint === '/api/v1') {
    return API_BASE_URL;
  }
  return `${API_BASE_URL}${cleanEndpoint}`;
}

/**
 * Wrapper cho fetch chuẩn: Tự động phân giải URL đầy đủ thông qua buildApiUrl.
 * @param endpoint Endpoint tương đối (vd: '/api/v1/offers' hoặc '/offers')
 * @param options Cấu hình RequestInit
 * @returns Response
 */
export async function apiFetch(endpoint: string, options?: RequestInit): Promise<Response> {
  const url = buildApiUrl(endpoint);
  return fetch(url, options);
}

/**
 * Thực hiện yêu cầu HTTP fetch với xử lý lỗi chuẩn hóa.
 * @param endpoint Đường dẫn endpoint tương đối (vd: '/tours')
 * @param options Cấu hình RequestInit
 * @returns Dữ liệu JSON kiểu T
 */
export async function fetchJson<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const url = buildApiUrl(endpoint);
  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
      ...options?.headers,
    },
  });

  if (!response.ok) {
    let errorDetail = 'Lỗi không xác định';
    try {
      const errorJson = await response.json();
      errorDetail = errorJson.message || errorJson.error || JSON.stringify(errorJson);
    } catch {
      errorDetail = response.statusText;
    }
    throw new Error(`API Error [${response.status}]: ${errorDetail}`);
  }

  return response.json();
}

/**
 * Đọc luồng dữ liệu Server-Sent Events (SSE) theo chuẩn chunked stream.
 * @param endpoint Endpoint phát SSE (vd: '/agent/chat/stream')
 * @param body Dữ liệu request gửi lên
 * @param onEvent Hàm callback nhận từng gói tin sự kiện
 * @param signal AbortSignal để hủy request nếu cần
 */
export async function streamSSE<T>(
  endpoint: string,
  body: unknown,
  onEvent: (event: T) => void,
  signal?: AbortSignal
): Promise<void> {
  const url = buildApiUrl(endpoint);
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'text/event-stream',
    },
    body: JSON.stringify(body),
    signal,
  });

  if (!response.ok) {
    throw new Error(`Lỗi kết nối SSE [${response.status}]: ${response.statusText}`);
  }

  if (!response.body) {
    throw new Error('Trình duyệt không nhận được luồng phản hồi từ server.');
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder('utf-8');
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';

    for (const line of lines) {
      const trimmed = line.trim();
      if (!trimmed) continue;

      // Hỗ trợ chuẩn SSE "data: {...}" hoặc raw JSON streams
      let jsonStr = trimmed;
      if (trimmed.startsWith('data:')) {
        jsonStr = trimmed.slice(5).trim();
      }

      if (jsonStr === '[DONE]') return;

      try {
        const parsed = JSON.parse(jsonStr) as T;
        onEvent(parsed);
      } catch (err) {
        console.warn('Bỏ qua chunk JSON không hợp lệ:', jsonStr, err);
      }
    }
  }

  if (buffer.trim()) {
    try {
      const parsed = JSON.parse(buffer.trim()) as T;
      onEvent(parsed);
    } catch {
      // Ignored
    }
  }
}
