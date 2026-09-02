using DDD.BuildingBlocks;
using DietApi.Domain;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using DietApi.Services;
using MediatR;

namespace DietApi.Features.FoodLog.AddFoodEntry;

public record AddFoodEntryCommand(DateOnly Date, AddFoodEntryRequest Request) : IRequest<LoggedDayDto>;

/// <summary>
/// Adds an entry, creating the day if this is the date's first one.
/// </summary>
/// <remarks>
/// Two snapshots are taken here and never taken again. The plan's current targets are copied onto
/// a newly created day, so changing the target tomorrow cannot re-judge today. The serving's
/// nutrition is copied onto the entry, so correcting the library later cannot rewrite what the
/// member already saw.
/// </remarks>
public class AddFoodEntryHandler(
    IDietPlanRepository planRepository,
    ILoggedDayRepository dayRepository,
    IFoodLibraryRepository libraryRepository,
    IUserService userService) : IRequestHandler<AddFoodEntryCommand, LoggedDayDto>
{
    public async Task<LoggedDayDto> Handle(AddFoodEntryCommand request, CancellationToken cancellationToken)
    {
        var userId = userService.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var plan = await planRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Set up your diet plan before logging food");

        var food = await libraryRepository.GetByIdAsync(request.Request.FoodLibraryItemId, cancellationToken)
            ?? throw new DomainException("That food is not in the library");

        var serving = food.ServingSize(request.Request.ServingSizeId)
            ?? throw new DomainException("That serving size is not available for this food");

        var day = await dayRepository.GetByDateAsync(userId, request.Date, cancellationToken);
        var isNewDay = day is null;

        if (day is null)
        {
            day = LoggedDay.StartDay(plan.Id, userId, request.Date, plan.Targets, plan.StartDate);
        }
        else if (request.Request.Version != day.Version)
        {
            throw ConcurrencyConflictException.ForDay(request.Date);
        }

        day.AddEntry(
            food.Id,
            serving.Id,
            food.Name,
            serving.Label,
            request.Request.Quantity,
            request.Request.MealType,
            serving.Nutrition);

        if (isNewDay)
            await dayRepository.AddAsync(day, cancellationToken);
        else
            await dayRepository.UpdateAsync(day, cancellationToken);

        return FoodLogMapper.ToDto(day);
    }
}
