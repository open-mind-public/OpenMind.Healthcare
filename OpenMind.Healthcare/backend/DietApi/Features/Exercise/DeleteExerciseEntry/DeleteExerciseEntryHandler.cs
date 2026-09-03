using DietApi.Domain;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.Exercise.DeleteExerciseEntry;

/// <summary>
/// Result of removing a session. <c>Day</c> is null once the day has none left - the date reverts
/// to no exercise recorded, rather than becoming a zero-minute day that did nothing.
/// </summary>
public record DeleteExerciseEntryResult(bool Found, ExerciseDayDto? Day);

public record DeleteExerciseEntryCommand(Guid EntryId, Guid Version) : IRequest<DeleteExerciseEntryResult>;

public class DeleteExerciseEntryHandler(
    IExerciseDayRepository exerciseRepository,
    IUserService userService) : IRequestHandler<DeleteExerciseEntryCommand, DeleteExerciseEntryResult>
{
    public async Task<DeleteExerciseEntryResult> Handle(
        DeleteExerciseEntryCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var day = await exerciseRepository.GetByEntryIdAsync(userId, request.EntryId, cancellationToken);
        if (day is null)
            return new DeleteExerciseEntryResult(Found: false, Day: null);

        if (request.Version != day.Version)
            throw ConcurrencyConflictException.ForDay(day.Date);

        day.RemoveEntry(request.EntryId);

        if (day.IsEmpty)
        {
            await exerciseRepository.DeleteAsync(day, cancellationToken);
            return new DeleteExerciseEntryResult(Found: true, Day: null);
        }

        await exerciseRepository.UpdateAsync(day, cancellationToken);
        return new DeleteExerciseEntryResult(Found: true, Day: ExerciseMapper.ToDto(day));
    }
}
