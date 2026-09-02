using System.Globalization;
using System.Text;
using DDD.BuildingBlocks;
using DietApi.Domain.Entities;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Aggregates;

/// <summary>
/// A curated food with known nutrition values. Seeded reference data - members select from the
/// library rather than typing nutrition values, so every logged entry carries trustworthy
/// numbers.
/// </summary>
public class FoodLibraryItem : AggregateRoot
{
    private readonly List<ServingSize> _servingSizes = [];

    public string Name { get; private set; } = string.Empty;

    /// <summary>Lowercased and accent-stripped. Indexed; search matches on this, not on Name.</summary>
    public string SearchName { get; private set; } = string.Empty;

    public FoodCategory Category { get; private set; }

    public IReadOnlyCollection<ServingSize> ServingSizes => _servingSizes;

    private FoodLibraryItem() { }

    public static FoodLibraryItem Create(string name, FoodCategory category, params ServingSize[] servingSizes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A food needs a name");

        if (servingSizes.Length == 0)
            throw new DomainException($"'{name}' needs at least one serving size");

        var item = new FoodLibraryItem
        {
            Name = name.Trim(),
            SearchName = Normalise(name),
            Category = category
        };

        item._servingSizes.AddRange(servingSizes);
        return item;
    }

    public ServingSize? ServingSize(Guid servingSizeId) =>
        _servingSizes.SingleOrDefault(s => s.Id == servingSizeId);

    /// <summary>
    /// Lowercases and strips accents so "crème fraîche" is found by typing "creme".
    /// </summary>
    public static string Normalise(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
