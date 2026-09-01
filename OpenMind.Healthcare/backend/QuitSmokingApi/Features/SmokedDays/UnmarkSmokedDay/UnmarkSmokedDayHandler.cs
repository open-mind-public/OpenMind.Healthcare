using MediatR;
using QuitSmokingApi.Domain.Repositories;
using QuitSmokingApi.Services;

namespace QuitSmokingApi.Features.SmokedDays.UnmarkSmokedDay;

public record UnmarkSmokedDayCommand(DateOnly Date) : IRequest<bool>;

public class UnmarkSmokedDayHandler(
    IQuitJourneyRepository journeyRepository,
    IUserService userService) : IRequestHandler<UnmarkSmokedDayCommand, bool>
{
    public async Task<bool> Handle(UnmarkSmokedDayCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var journey = await journeyRepository.GetByUserIdAsync(userId, cancellationToken);
        if (journey is null) return false;

        if (!journey.UnmarkSmokedDay(request.Date)) return false;

        await journeyRepository.UpdateAsync(journey, cancellationToken);
        return true;
    }
}
