using DietApi.Domain.Aggregates;
using DietApi.Domain.Entities;
using DietApi.Domain.ValueObjects;

namespace DietApi.Features.FoodLibrary;

public record NutritionValuesDto(int Calories, decimal ProteinG, decimal CarbsG, decimal FatG);

public record ServingSizeDto(Guid Id, string Label, decimal GramWeight, NutritionValuesDto Nutrition);

public record FoodLibraryItemDto(Guid Id, string Name, FoodCategory Category, IReadOnlyList<ServingSizeDto> ServingSizes);

/// <summary>
/// An empty <c>Matches</c> is how a member learns the food is not in the library. The client says
/// so plainly rather than offering to invent an entry with no nutrition values.
/// </summary>
public record FoodSearchResponse(string Query, IReadOnlyList<FoodLibraryItemDto> Matches);

public static class FoodLibraryMapper
{
    public static NutritionValuesDto ToDto(NutritionValues values) =>
        new(values.Calories, values.ProteinG, values.CarbsG, values.FatG);

    public static ServingSizeDto ToDto(ServingSize serving) =>
        new(serving.Id, serving.Label, serving.GramWeight, ToDto(serving.Nutrition));

    public static FoodLibraryItemDto ToDto(FoodLibraryItem item) =>
        new(item.Id, item.Name, item.Category, [.. item.ServingSizes.Select(ToDto)]);
}
