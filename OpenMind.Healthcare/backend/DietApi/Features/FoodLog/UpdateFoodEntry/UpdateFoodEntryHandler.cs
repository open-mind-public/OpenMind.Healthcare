using DDD.BuildingBlocks;
using DietApi.Domain;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.FoodLog.UpdateFoodEntry;

public record UpdateFoodEntryCommand(Guid EntryId, UpdateFoodEntryRequest Request) : IRequest<LoggedDayDto?>;

/// <summary>
/// Revises an entry the member already logged.
/// </summary>
/// <remarks>
/// The serving's nutrition is re-read from the library and re-snapshotted. A member's own edit is
/// a deliberate act, unlike a background correction to the catalogue, and re-reading keeps the
/// entry consistent with the serving it now names.
/// </remarks>
public class UpdateFoodEntryHandler(
    ILoggedDayRepository dayRepository,
    IFoodLibraryRepository libraryRepository,
    IUserService userService) : IRequestHandler<UpdateFoodEntryCommand, LoggedDayDto?>
{
    public async Task<LoggedDayDto?> Handle(UpdateFoodEntryCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var day = await dayRepository.GetByEntryIdAsync(userId, request.EntryId, cancellationToken);
        if (day is null)
            return null;

        if (request.Request.Version != day.Version)
            throw ConcurrencyConflictException.ForDay(day.Date);

        var entry = day.Entries.Single(e => e.Id == request.EntryId);

        var food = await libraryRepository.GetByIdAsync(entry.FoodLibraryItemId, cancellationToken)
            ?? throw new DomainException("That food is no longer in the library");

        var serving = food.ServingSize(request.Request.ServingSizeId)
            ?? throw new DomainException("That serving size is not available for this food");

        day.UpdateEntry(
            request.EntryId,
            serving.Id,
            serving.Label,
            request.Request.Quantity,
            request.Request.MealType,
            serving.Nutrition);

        await dayRepository.UpdateAsync(day, cancellationToken);

        return FoodLogMapper.ToDto(day);
    }
}
