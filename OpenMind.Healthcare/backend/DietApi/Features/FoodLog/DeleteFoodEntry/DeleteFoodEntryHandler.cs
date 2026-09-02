using DietApi.Domain;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.FoodLog.DeleteFoodEntry;

/// <summary>
/// Result of removing an entry. <c>Day</c> is null once the day has no entries left - the date
/// reverts to not logged rather than becoming a zero-calorie, perfectly compliant day.
/// </summary>
public record DeleteFoodEntryResult(bool Found, LoggedDayDto? Day);

public record DeleteFoodEntryCommand(Guid EntryId, Guid Version) : IRequest<DeleteFoodEntryResult>;

public class DeleteFoodEntryHandler(
    ILoggedDayRepository dayRepository,
    IUserService userService) : IRequestHandler<DeleteFoodEntryCommand, DeleteFoodEntryResult>
{
    public async Task<DeleteFoodEntryResult> Handle(DeleteFoodEntryCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var day = await dayRepository.GetByEntryIdAsync(userId, request.EntryId, cancellationToken);
        if (day is null)
            return new DeleteFoodEntryResult(Found: false, Day: null);

        if (request.Version != day.Version)
            throw ConcurrencyConflictException.ForDay(day.Date);

        day.RemoveEntry(request.EntryId);

        if (day.IsEmpty)
        {
            await dayRepository.DeleteAsync(day, cancellationToken);
            return new DeleteFoodEntryResult(Found: true, Day: null);
        }

        await dayRepository.UpdateAsync(day, cancellationToken);
        return new DeleteFoodEntryResult(Found: true, Day: FoodLogMapper.ToDto(day));
    }
}
