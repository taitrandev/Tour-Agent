using TourAgent.Domain.Contacts;
using Xunit;

namespace TourAgent.Domain.UnitTests.Contacts;

/// <summary>
/// Các bài kiểm thử đơn vị cho thực thể CrmContact và các quy tắc nghiệp vụ liên quan đến vòng đời khách hàng CRM.
/// </summary>
public class CrmContactTests
{
    /// <summary>
    /// Kiểm tra tạo khách hàng mới không có SĐT thì mặc định stage là prospect.
    /// </summary>
    [Fact]
    public void Create_WithoutPhone_ShouldDefaultToProspectStage()
    {
        // Act
        var result = CrmContact.Create(
            name: "Lê Văn Tám",
            phone: null,
            channel: "telegram",
            telegramUsername: "tamle");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Lê Văn Tám", result.Value.Name);
        Assert.Equal(CrmContact.StageProspect, result.Value.Stage);
        Assert.False(result.Value.IsHot());
        Assert.Equal("tamle", result.Value.TelegramUsername);
    }

    /// <summary>
    /// Kiểm tra tạo khách hàng có SĐT mà không chỉ định stage thì tự động nâng thành hot lead.
    /// </summary>
    [Fact]
    public void Create_WithPhoneAndNoStage_ShouldAutoPromoteToHotStage()
    {
        // Act
        var result = CrmContact.Create(
            name: "Trần Thị Lan",
            phone: "0909123456",
            channel: "web_widget");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(CrmContact.StageHot, result.Value.Stage);
        Assert.True(result.Value.IsHot());
    }

    /// <summary>
    /// Kiểm tra tạo khách hàng với stage không hợp lệ thì trả về Result thất bại.
    /// </summary>
    [Fact]
    public void Create_WithInvalidStage_ShouldReturnFailureResult()
    {
        // Act
        var result = CrmContact.Create(
            name: "Nguyễn Văn X",
            phone: "0911222333",
            stage: "invalid_stage_xyz");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("CrmContact.InvalidStage", result.Error.Code);
    }

    /// <summary>
    /// Kiểm tra cập nhật thông tin khách hàng và nâng stage lên customer thành công.
    /// </summary>
    [Fact]
    public void Update_WithValidData_ShouldUpdatePropertiesAndTimestamp()
    {
        // Arrange
        var contact = CrmContact.Create(
            name: "Hoàng Minh",
            phone: "0933444555",
            channel: "zalo").Value;

        // Act
        var updateResult = contact.Update(
            name: "Hoàng Minh Đức",
            phone: "0933444999",
            email: "duc.hoang@gmail.com",
            stage: CrmContact.StageCustomer,
            notes: "Đã thanh toán đủ tour Hàn Quốc",
            tagsJson: "[\"vip\"]");

        // Assert
        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Hoàng Minh Đức", contact.Name);
        Assert.Equal("0933444999", contact.Phone);
        Assert.Equal("duc.hoang@gmail.com", contact.Email);
        Assert.Equal(CrmContact.StageCustomer, contact.Stage);
        Assert.Equal("Đã thanh toán đủ tour Hàn Quốc", contact.Notes);
    }

    /// <summary>
    /// Kiểm tra phương thức MarkAsHot cập nhật trạng thái của contact sang StageHot.
    /// </summary>
    [Fact]
    public void MarkAsHot_ShouldUpdateStageToHot()
    {
        // Arrange
        var contact = CrmContact.Create(
            name: "Khách Hỏi Giá",
            phone: null,
            stage: CrmContact.StageProspect).Value;

        Assert.False(contact.IsHot());

        // Act
        contact.MarkAsHot();

        // Assert
        Assert.True(contact.IsHot());
        Assert.Equal(CrmContact.StageHot, contact.Stage);
    }

    /// <summary>
    /// Kiểm tra xóa mềm (SoftDelete) cập nhật thuộc tính DeletedAtUtc.
    /// </summary>
    [Fact]
    public void SoftDelete_ShouldSetDeletedAtUtc()
    {
        // Arrange
        var contact = CrmContact.Create("Khách Cần Xóa", "0900000000").Value;
        Assert.Null(contact.DeletedAtUtc);

        // Act
        contact.SoftDelete();

        // Assert
        Assert.NotNull(contact.DeletedAtUtc);
    }
}
