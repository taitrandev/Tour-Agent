using System.Text.Json;

namespace TourAgent.Application.Agent.Contracts;

/// <summary>
/// Giao diện trừu tượng định nghĩa một công cụ hỗ trợ cho AI Agent (Agent Tool).
/// Cho phép AI chủ động thực thi các hành động tra cứu dữ liệu, kiểm tra lịch khởi hành hoặc chuyển tiếp nhân viên.
/// </summary>
public interface IAgentTool
{
    /// <summary>
    /// Tên định danh duy nhất của công cụ (ví dụ: 'search_travels', 'check_departures').
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Mô tả chi tiết chức năng để mô hình ngôn ngữ lớn (LLM) hiểu rõ ngữ cảnh kích hoạt công cụ.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Bản đặc tả JSON Schema mô tả cấu trúc các tham số đầu vào của công cụ.
    /// </summary>
    JsonDocument ParametersSchema { get; }

    /// <summary>
    /// Thực thi logic của công cụ dựa trên chuỗi tham số định dạng JSON do AI truyền vào.
    /// </summary>
    /// <param name="argumentsJson">Chuỗi JSON chứa các đối số gọi hàm từ mô hình AI.</param>
    /// <param name="cancellationToken">Token hủy bỏ tác vụ bất đồng bộ.</param>
    /// <returns>Chuỗi JSON biểu diễn kết quả trả về để đưa lại vào ngữ cảnh của AI.</returns>
    Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken = default);
}
