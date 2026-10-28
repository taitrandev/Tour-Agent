using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Offers.GetAllOffers;

/// <summary>
/// Query lấy danh sách toàn bộ các Grand Slam Offer có phân trang hoặc lọc theo từ khóa, trạng thái, nổi bật.
/// </summary>
public sealed record GetAllOffersQuery(
    string? Search = null,
    string? Status = null,
    bool? IsFeatured = null) : IQuery<Result<IReadOnlyList<OfferDto>>>;
