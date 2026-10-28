using TourAgent.Application.Common;
using TourAgent.Domain.Common;

namespace TourAgent.Application.Features.Offers.GetOfferById;

/// <summary>
/// Query tra cứu thông tin chi tiết một Grand Slam Offer theo Id.
/// </summary>
public sealed record GetOfferByIdQuery(long Id) : IQuery<Result<OfferDto>>;
