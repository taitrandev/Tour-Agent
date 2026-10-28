namespace TourAgent.Domain.UnitTests.Orders;

using TourAgent.Domain.Orders;
using Xunit;

/// <summary>
/// Bộ kiểm thử đơn vị cho thực thể TourOrder (Đơn hàng tour và tiến trình visa).
/// </summary>
public sealed class TourOrderTests
{
    /// <summary>
    /// Kiểm tra tạo mới đơn hàng với thông tin hợp lệ sẽ tính đúng tiền cọc 50% và tiền còn lại.
    /// </summary>
    [Fact]
    public void Create_WithValidParameters_CalculatesDepositAndRemainingCorrectly()
    {
        // Arrange
        const decimal totalPrice = 50000000m;
        const int paxAdults = 2;

        // Act
        var result = TourOrder.Create(
            contactId: 1,
            travelId: 10,
            travelDepartureId: null,
            conversationId: null,
            channel: "telegram",
            paxAdults: paxAdults,
            paxChildren: 0,
            totalPrice: totalPrice,
            depositPercent: 50,
            departureDate: new DateOnly(2026, 10, 15),
            paymentAccountNote: "STK: 1903688888",
            aiSummary: "Tóm tắt từ AI",
            notes: "Khách VIP");

        // Assert
        Assert.True(result.IsSuccess);
        var order = result.Value;
        Assert.StartsWith("ORD-", order.OrderCode);
        Assert.Equal(25000000m, order.DepositAmount);
        Assert.Equal(25000000m, order.RemainingAmount);
        Assert.Equal("pending", order.DepositStatus);
        Assert.Equal("pending", order.RemainingStatus);
        Assert.Equal("deposit_pending", order.Status);
    }

    /// <summary>
    /// Kiểm tra xác nhận cọc đợt 1 sẽ chuyển trạng thái đơn sang visa_processing nếu tour có visa.
    /// </summary>
    [Fact]
    public void MarkDepositPaid_WhenTourRequiresVisa_TransitionsToVisaProcessing()
    {
        // Arrange
        var result = TourOrder.Create(
            contactId: 1,
            travelId: 10,
            travelDepartureId: null,
            conversationId: null,
            channel: "telegram",
            paxAdults: 2,
            paxChildren: 0,
            totalPrice: 40000000m,
            depositPercent: 50,
            departureDate: null,
            paymentAccountNote: null,
            aiSummary: null,
            notes: null);

        var order = result.Value;

        // Act
        order.MarkDepositPaid("Ref: VCB.123456");

        // Assert
        Assert.Equal("paid", order.DepositStatus);
        Assert.NotNull(order.DepositPaidAt);
        Assert.Equal("visa_processing", order.Status);
        Assert.Equal("awaiting_docs", order.VisaStatus);
    }

    /// <summary>
    /// Kiểm tra cập nhật visa thành approved sẽ tự động chuyển đơn sang final_payment_pending.
    /// </summary>
    [Fact]
    public void UpdateVisaStatus_WhenApproved_TransitionsToFinalPaymentPending()
    {
        // Arrange
        var order = TourOrder.Create(1, 10, null, null, "web", 2, 0, 40000000m, 50, null, null, null, null).Value;
        order.MarkDepositPaid();

        // Act
        order.UpdateVisaStatus("approved", "Đã dán visa", "Saigontourist", "SGT-999");

        // Assert
        Assert.Equal("approved", order.VisaStatus);
        Assert.Equal("final_payment_pending", order.Status);
        Assert.Equal("Saigontourist", order.OperatorName);
    }

    /// <summary>
    /// Kiểm tra xác nhận thanh toán đủ 100% sẽ chuyển trạng thái sang ready_to_depart.
    /// </summary>
    [Fact]
    public void MarkRemainingPaid_TransitionsToReadyToDepart()
    {
        // Arrange
        var order = TourOrder.Create(1, 10, null, null, "web", 2, 0, 40000000m, 50, null, null, null, null).Value;
        order.MarkDepositPaid();
        order.UpdateVisaStatus("approved");

        // Act
        order.MarkRemainingPaid();

        // Assert
        Assert.Equal("paid", order.RemainingStatus);
        Assert.NotNull(order.RemainingPaidAt);
        Assert.Equal("ready_to_depart", order.Status);
    }

    /// <summary>
    /// Kiểm tra soạn tin nhắn Zalo T-3 chứa đầy đủ thông tin tên khách, tour và số điện thoại Hướng Dẫn Viên.
    /// </summary>
    [Fact]
    public void GetZaloT3DepartureMessage_GeneratesCompleteMessage()
    {
        // Arrange
        var order = TourOrder.Create(1, 10, null, null, "web", 2, 0, 40000000m, 50, new DateOnly(2026, 10, 20), null, null, null).Value;
        order.MarkT3Notified("Nguyễn Văn Tuấn", "0912.888.999", "Cột 10 sân bay TSN lúc 05:00");

        // Act
        var message = order.GetZaloT3DepartureMessage("Anh Nghĩa", "Cung Đường Vàng Nhật Bản 6N5Đ");

        // Assert
        Assert.Contains("Anh Nghĩa", message);
        Assert.Contains("Cung Đường Vàng Nhật Bản 6N5Đ", message);
        Assert.Contains("Nguyễn Văn Tuấn", message);
        Assert.Contains("0912.888.999", message);
        Assert.Contains("Cột 10 sân bay TSN lúc 05:00", message);
    }
}
