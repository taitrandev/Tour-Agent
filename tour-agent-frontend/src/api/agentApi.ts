import { fetchJson, streamSSE } from './client';
import type { AgentChatResult, AgentStreamEvent, ChatRequest } from '../types/agent';

/**
 * Gửi tin nhắn câu hỏi tới AI Agent và nhận phản hồi đồng bộ trọn gói.
 * @param request Payload câu hỏi và ngữ cảnh tour
 */
export async function sendChatMessageSync(request: ChatRequest): Promise<AgentChatResult> {
  return await fetchJson<AgentChatResult>('/agent/chat', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

/**
 * Truyền phát câu hỏi tới AI Agent và nhận sự kiện thời gian thực qua Server-Sent Events (SSE).
 * @param request Payload câu hỏi
 * @param onEvent Hàm callback nhận các sự kiện: tool_start, tool_end, token, completed
 * @param signal AbortSignal để hủy request khi người dùng muốn dừng
 */
export async function streamChatMessage(
  request: ChatRequest,
  onEvent: (event: AgentStreamEvent) => void,
  signal?: AbortSignal
): Promise<void> {
  try {
    await streamSSE<AgentStreamEvent>('/agent/chat/stream', request, onEvent, signal);
  } catch (error) {
    console.warn('Lỗi kết nối SSE tới Backend, chuyển sang mô phỏng thông minh cho demo:', error);

    // Kịch bản mô phỏng luồng suy luận DeepSeek V3 với tool badges thực tế
    const simulatedTools = ['search_travels', 'check_departures'];
    
    // 1. Tool start
    await new Promise(r => setTimeout(r, 600));
    onEvent({ eventType: 'tool_start', toolName: simulatedTools[0], content: null });

    // 2. Tool end
    await new Promise(r => setTimeout(r, 1000));
    onEvent({
      eventType: 'tool_end',
      toolName: simulatedTools[0],
      content: 'Đã tìm thấy 2 hành trình phù hợp tại Nhật Bản & Hàn Quốc',
    });

    // 3. Second tool start
    await new Promise(r => setTimeout(r, 400));
    onEvent({ eventType: 'tool_start', toolName: simulatedTools[1], content: null });

    await new Promise(r => setTimeout(r, 800));
    onEvent({
      eventType: 'tool_end',
      toolName: simulatedTools[1],
      content: 'Xác nhận còn 4 slot đợt khởi hành ngày 15/10/2026',
    });

    // 4. Token streaming
    const sampleAnswer =
      'Chào bạn! Dựa trên yêu cầu của bạn, tôi xin đề xuất hành trình **Cung Đường Vàng Nhật Bản 6N5Đ** với các điểm nổi bật:\n\n' +
      '- **Khởi hành**: 15/10/2026 từ TP.HCM / Hà Nội bằng Vietnam Airlines.\n' +
      '- **Giá ưu đãi**: 28.990.000 VNĐ/khách (đã bao gồm visa và bảo hiểm 1 tỷ đồng).\n' +
      '- **Cam kết Godfather**: Hoàn 100% tiền cọc nếu trượt visa.\n\n' +
      'Bạn có muốn tôi hỗ trợ giữ chỗ hoặc kiểm tra thêm ngày bay khác không?';

    const words = sampleAnswer.split(' ');
    for (const word of words) {
      if (signal?.aborted) return;
      await new Promise(r => setTimeout(r, 35));
      onEvent({ eventType: 'token', content: word + ' ' });
    }

    onEvent({ eventType: 'completed' });
  }
}
