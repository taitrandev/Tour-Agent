using System.Text.Json;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.GetAllTours;
using TourAgent.Application.Features.Tours.GetTourById;
using TourAgent.Domain.Common;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.CreateTour;

/// <summary>
/// Handler xử lý tạo mới tour sản phẩm và lưu vào cơ sở dữ liệu PostgreSQL.
/// </summary>
public sealed class CreateTourHandler : ICommandHandler<CreateTourCommand, Result<TourAdminItemDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo CreateTourHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public CreateTourHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Xử lý tạo mới tour, chuẩn hóa dữ liệu và thêm các đợt khởi hành.
    /// </summary>
    /// <param name="command">Command chứa thông tin tour cần tạo.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result chứa TourAdminItemDto nếu thành công, hoặc lỗi nghiệp vụ.</returns>
    public async Task<Result<TourAdminItemDto>> HandleAsync(
        CreateTourCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            return Result.Failure<TourAdminItemDto>(TourErrors.EmptyName);
        }

        var code = string.IsNullOrWhiteSpace(command.Code)
            ? $"TOUR-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}"
            : command.Code.Trim().ToUpperInvariant();

        var destination = !string.IsNullOrWhiteSpace(command.Route)
            ? command.Route.Trim()
            : (command.Destinations is { Count: > 0 } ? string.Join(", ", command.Destinations) : command.Country);

        decimal priceFrom = 1;
        if (command.PriceFrom.HasValue && command.PriceFrom.Value > 0)
        {
            priceFrom = command.PriceFrom.Value;
        }
        else if (command.Departures is { Count: > 0 })
        {
            var validPrices = command.Departures.Where(d => d.PriceAdult > 0).Select(d => d.PriceAdult).ToList();
            if (validPrices.Count > 0)
            {
                priceFrom = validPrices.Min();
            }
        }

        var destinationsJson = command.Destinations is not null ? JsonSerializer.Serialize(command.Destinations) : null;
        var tagsJson = command.Tags is not null ? JsonSerializer.Serialize(command.Tags) : null;
        var itineraryJson = SerializeObject(command.Itinerary);
        var inclusionsJson = SerializeObject(command.Inclusions);
        var exclusionsJson = SerializeObject(command.Exclusions);

        var isPublished = command.Status != "draft";
        var slug = code.ToLowerInvariant();

        var tourResult = Tour.Create(
            code,
            command.Title.Trim(),
            destination,
            command.Country.Trim(),
            command.DepartureCity.Trim(),
            command.DurationDays,
            command.DurationNights,
            priceFrom,
            "VND",
            command.Notes,
            inclusionsJson,
            exclusionsJson,
            command.VisaPolicy,
            command.CancellationPolicy,
            command.ChildPolicy,
            command.PaymentPolicy,
            itineraryJson,
            isPublished,
            0,
            slug,
            command.IsFeatured,
            destinationsJson,
            tagsJson);

        if (tourResult.IsFailure)
        {
            return Result.Failure<TourAdminItemDto>(tourResult.Error);
        }

        var tour = tourResult.Value;

        if (command.Departures is { Count: > 0 })
        {
            foreach (var depDto in command.Departures)
            {
                if (DateOnly.TryParse(depDto.DepartureDate, out var depDate) && depDto.PriceAdult > 0)
                {
                    var depStatus = depDto.Status?.ToLowerInvariant() switch
                    {
                        "guaranteed" => TourDepartureStatus.Guaranteed,
                        "full" => TourDepartureStatus.Full,
                        "closed" => TourDepartureStatus.Closed,
                        _ => TourDepartureStatus.Open
                    };

                    var departureResult = TourDeparture.Create(
                        tour.Id,
                        depDate,
                        depDto.PriceAdult,
                        depDto.PriceChild ?? (depDto.PriceAdult * 0.85m),
                        depDto.SingleSupplement ?? 0,
                        20,
                        20,
                        depStatus);

                    if (departureResult.IsSuccess)
                    {
                        tour.AddDeparture(departureResult.Value);
                    }
                }
            }
        }

        _dbContext.Tours.Add(tour);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var returnDepartures = tour.Departures
            .OrderBy(d => d.DepartureDate)
            .Select(d => new TourDepartureDto(
                d.Id,
                d.DepartureDate.ToString("yyyy-MM-dd"),
                d.PriceAdult,
                d.PriceChild,
                d.SingleSupplement,
                d.Status.ToString().ToLowerInvariant(),
                null))
            .ToList();

        var resultDto = new TourAdminItemDto(
            tour.Id,
            tour.Name,
            tour.Slug,
            tour.Code,
            tour.DurationDays,
            tour.DurationNights,
            tour.Destination,
            tour.Country,
            command.Destinations ?? [],
            tour.DepartureCity,
            command.Tags ?? [],
            tour.PriceFrom,
            tour.PriceFrom,
            tour.IsActive ? "published" : "draft",
            tour.IsFeatured,
            ParseJsonElement(tour.Itinerary),
            ParseJsonElement(tour.Inclusions),
            ParseJsonElement(tour.Exclusions),
            tour.ChildPolicy,
            tour.PaymentPolicy,
            tour.CancellationPolicy,
            tour.VisaPolicy,
            tour.Highlights,
            returnDepartures,
            0,
            0,
            0);

        return Result.Success(resultDto);
    }

    private static string? SerializeObject(object? obj)
    {
        if (obj is null) return null;
        if (obj is string str) return str;
        return JsonSerializer.Serialize(obj);
    }

    private static JsonElement? ParseJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }
}
