using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Tours.DeleteTour;

/// <summary>
/// Command yêu cầu xóa một Tour du lịch khỏi hệ thống theo Id.
/// </summary>
/// <param name="Id">Mã định danh của Tour cần xóa.</param>
public sealed record DeleteTourCommand(long Id) : ICommand<Result>;
