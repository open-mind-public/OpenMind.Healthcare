using DietApi.Domain.ValueObjects;
using DDD.BuildingBlocks;

namespace DietApi.Domain.Entities;

/// <summary>
/// One way of measuring a food, with the nutrition values <em>for that serving</em>.
/// </summary>
/// <remarks>
/// Nutrition belongs on the serving rather than the food because "1 medium banana" and
/// "100 g of banana" are different numbers, and the member picks one of them.
/// </remarks>
public class ServingSize : Entity
{
    public Guid FoodLibraryItemId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public decimal GramWeight { get; private set; }
    public NutritionValues Nutrition { get; private set; } = null!;

    // Private parameterless constructor for EF Core
    private ServingSize() { }

    public static ServingSize Create(string label, decimal gramWeight, NutritionValues nutrition)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new DomainException("A serving size needs a label");

        if (gramWeight <= 0)
            throw new DomainException("A serving size must weigh more than zero");

        return new ServingSize
        {
            Label = label.Trim(),
            GramWeight = Math.Round(gramWeight, 2),
            Nutrition = nutrition
        };
    }
}
