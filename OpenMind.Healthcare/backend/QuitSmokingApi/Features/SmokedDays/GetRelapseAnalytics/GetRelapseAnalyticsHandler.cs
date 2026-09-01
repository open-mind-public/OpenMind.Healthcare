using MediatR;
using QuitSmokingApi.Domain.Repositories;
using QuitSmokingApi.Domain.ValueObjects;
using QuitSmokingApi.Services;

namespace QuitSmokingApi.Features.SmokedDays.GetRelapseAnalytics;

public record GetRelapseAnalyticsQuery : IRequest<RelapseAnalyticsDto>;

public class GetRelapseAnalyticsHandler(
    IQuitJourneyRepository journeyRepository,
    IUserService userService) : IRequestHandler<GetRelapseAnalyticsQuery, RelapseAnalyticsDto>
{
    public async Task<RelapseAnalyticsDto> Handle(GetRelapseAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var journey = await journeyRepository.GetByUserIdAsync(userId, cancellationToken);

        var analytics = journey is null
            ? RelapseAnalytics.Empty()
            : journey.GetRelapseAnalytics();

        return SmokedDayMapper.ToDto(analytics);
    }
}
