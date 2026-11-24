using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.GetTourById;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.GetTourBySlug;

/// <summary>
/// Query yêu cầu lấy thông tin chi tiết đầy đủ của một Tour du lịch theo Slug (hoặc Code duy nhất).
/// </summary>
/// <param name="Slug">Slug URL hoặc Code duy nhất của Tour.</param>
public sealed record GetTourBySlugQuery(string Slug) : IQuery<Result<TourDetailDto>>;

