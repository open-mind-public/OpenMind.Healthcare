using MediatR;
using QuitSmokingApi.Domain.Repositories;
using QuitSmokingApi.Services;

namespace QuitSmokingApi.Features.SmokedDays.GetSmokedDays;

/// <summary>
/// Returns the days the user marked as smoked. From/To are optional and restrict the window,
/// which the calendar uses to fetch a single month at a time.
/// </summary>
public record GetSmokedDaysQuery(DateOnly? From = null, DateOnly? To = null) : IRequest<List<SmokedDayDto>>;

public class GetSmokedDaysHandler(
    IQuitJourneyRepository journeyRepository,
    IUserService userService) : IRequestHandler<GetSmokedDaysQuery, List<SmokedDayDto>>
{
    public async Task<List<SmokedDayDto>> Handle(GetSmokedDaysQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var journey = await journeyRepository.GetByUserIdAsync(userId, cancellationToken);
        if (journey is null) return [];

        var from = request.From ?? DateOnly.MinValue;
        var to = request.To ?? DateOnly.MaxValue;

        var pricePerCigarette = journey.SmokingHabits.PricePerCigarette;

        return journey.GetSmokedDaysBetween(from, to)
            .Select(day => SmokedDayMapper.ToDto(day, pricePerCigarette))
            .ToList();
    }
}
