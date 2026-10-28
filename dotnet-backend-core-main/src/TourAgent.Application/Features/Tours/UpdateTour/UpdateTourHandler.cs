using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TourAgent.Application.Abstractions;
using TourAgent.Application.Common;
using TourAgent.Application.Features.Tours.GetAllTours;
using TourAgent.Application.Features.Tours.GetTourById;
using TourAgent.Domain.Common;
using TourAgent.Domain.Tours;

namespace TourAgent.Application.Features.Tours.UpdateTour;

/// <summary>
/// Handler xử lý cập nhật thông tin tour sản phẩm và tái đồng bộ các đợt khởi hành.
/// </summary>
public sealed class UpdateTourHandler : ICommandHandler<UpdateTourCommand, Result<TourAdminItemDto>>
{
    private readonly IAppDbContext _dbContext;

    /// <summary>
    /// Khởi tạo UpdateTourHandler với IAppDbContext.
    /// </summary>
    /// <param name="dbContext">Đối tượng abstraction truy cập cơ sở dữ liệu.</param>
    public UpdateTourHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Xử lý cập nhật dữ liệu tour.
    /// </summary>
    /// <param name="command">Command chứa thông tin cần cập nhật.</param>
    /// <param name="cancellationToken">Token hủy thao tác bất đồng bộ.</param>
    /// <returns>Result chứa TourAdminItemDto sau khi cập nhật thành công.</returns>
    public async Task<Result<TourAdminItemDto>> HandleAsync(
        UpdateTourCommand command,
        CancellationToken cancellationToken = default)
    {
        var tour = await _dbContext.Tours
            .Include(t => t.Departures)
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (tour is null)
        {
            return Result.Failure<TourAdminItemDto>(TourErrors.NotFound(command.Id));
        }

        var destination = !string.IsNullOrWhiteSpace(command.Route)
            ? command.Route.Trim()
            : (command.Destinations is { Count: > 0 } ? string.Join(", ", command.Destinations) : command.Country);

        decimal priceFrom = tour.PriceFrom;
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

        var destinationsJson = command.Destinations is not null ? JsonSerializer.Serialize(command.Destinations) : tour.DestinationsJson;
        var tagsJson = command.Tags is not null ? JsonSerializer.Serialize(command.Tags) : tour.TagsJson;
        var itineraryJson = SerializeObject(command.Itinerary) ?? tour.Itinerary;
        var inclusionsJson = SerializeObject(command.Inclusions) ?? tour.Inclusions;
        var exclusionsJson = SerializeObject(command.Exclusions) ?? tour.Exclusions;

        var isPublished = command.Status != "draft";

        var updateResult = tour.Update(
            command.Title.Trim(),
            destination,
            command.Country.Trim(),
            command.DepartureCity.Trim(),
            command.DurationDays,
            command.DurationNights,
            priceFrom,
            command.Notes ?? tour.Highlights,
            inclusionsJson,
            exclusionsJson,
            command.VisaPolicy ?? tour.VisaPolicy,
            command.CancellationPolicy ?? tour.CancellationPolicy,
            command.ChildPolicy ?? tour.ChildPolicy,
            command.PaymentPolicy ?? tour.PaymentPolicy,
            itineraryJson,
            isPublished,
            tour.Slug,
            command.IsFeatured,
            destinationsJson,
            tagsJson);

        if (updateResult.IsFailure)
        {
            return Result.Failure<TourAdminItemDto>(updateResult.Error);
        }

        if (command.Departures is not null)
        {
            tour.ClearDepartures();

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
