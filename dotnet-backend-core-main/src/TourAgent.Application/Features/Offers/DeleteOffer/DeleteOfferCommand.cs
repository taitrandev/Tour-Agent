using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Offers.DeleteOffer;

/// <summary>
/// Command yêu cầu xóa một gói Grand Slam Offer theo Id (hỗ trợ xóa mềm hoặc xóa hoàn toàn).
/// </summary>
public sealed record DeleteOfferCommand(long Id) : ICommand<Result>;
