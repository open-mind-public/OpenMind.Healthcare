using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.DietGuidance.GetDailyEncouragement;

public record GetDailyEncouragementQuery : IRequest<DailyEncouragementDto?>;

/// <summary>
/// A short message that reflects how the member's plan is actually going.
/// </summary>
/// <remarks>
/// A member with nothing logged gets a getting-started message rather than an error - an empty
/// history is a beginning, not a failure, and the wording says so.
/// </remarks>
public class GetDailyEncouragementHandler(
    IDietPlanRepository planRepository,
    ILoggedDayRepository dayRepository,
    StreakCalculator streakCalculator,
    IUserService userService) : IRequestHandler<GetDailyEncouragementQuery, DailyEncouragementDto?>
{
    public async Task<DailyEncouragementDto?> Handle(
        GetDailyEncouragementQuery request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken);
        if (plan is null)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summaries = await dayRepository.GetRangeAsync(userId, plan.StartDate, today, cancellationToken);
        var stats = streakCalculator.Calculate(summaries, plan.StartDate);

        var (message, tone) = Compose(stats.CurrentStreakDays, stats.TotalDaysLogged);

        return new DailyEncouragementDto(message, stats.CurrentStreakDays, tone);
    }

    private static (string Message, string Tone) Compose(int streak, int daysLogged) => (streak, daysLogged) switch
    {
        (0, 0) => ("Nothing logged yet. Add one thing you ate today - that is the whole first step.", "GettingStarted"),
        (0, _) => ("Yesterday is done. Log something today and the run starts again from here.", "Restart"),
        (1, _) => ("One day on target. That is how every streak begins.", "Streak"),
        ( < 7, _) => ($"{streak} days on target. That is a habit forming.", "Streak"),
        ( < 30, _) => ($"{streak} days in a row. This is no longer an accident.", "Streak"),
        _ => ($"{streak} days on target. That is genuinely remarkable consistency.", "Milestone")
    };
}
