using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.Exercise.GetActivitySummary;

/// <summary>
/// The weekly picture. Returns null only when the member has no plan; a member with a plan and
/// no activity gets zeros, which is an answer rather than an error (FR-024).
/// </summary>
public record GetActivitySummaryQuery : IRequest<ActivitySummaryDto?>;

public class GetActivitySummaryHandler(
    IDietPlanRepository planRepository,
    IExerciseDayRepository exerciseRepository,
    ActivitySummaryCalculator calculator,
    IUserService userService) : IRequestHandler<GetActivitySummaryQuery, ActivitySummaryDto?>
{
    public async Task<ActivitySummaryDto?> Handle(GetActivitySummaryQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Two windows back is everything the comparison needs - not the member's whole history.
        var from = today.AddDays(-(ActivitySummaryCalculator.WindowDays * 2 - 1));
        var days = await exerciseRepository.GetRangeAsync(userId, from, today, cancellationToken);

        var summary = calculator.Summarise(days, plan.StartDate);

        return new ActivitySummaryDto(
            summary.WindowDays,
            summary.ActiveDays,
            summary.TotalMinutes,
            summary.TotalKilocalories,
            summary.PreviousWindowActiveDays,
            summary.PreviousWindowMinutes);
    }
}
