namespace DietApi.Domain.ValueObjects;

/// <summary>
/// Grouping for the curated food library, used to keep the seed balanced and to filter search.
/// </summary>
public enum FoodCategory
{
    Staple,
    Protein,
    Dairy,
    Fruit,
    Vegetable,
    PreparedMeal,
    Snack,
    Drink
}
