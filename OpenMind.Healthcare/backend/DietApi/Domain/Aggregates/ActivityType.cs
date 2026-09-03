using DDD.BuildingBlocks;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Aggregates;

/// <summary>
/// A curated activity with a known metabolic equivalent. Seeded reference data - members pick
/// from the catalogue rather than typing a MET value, so every recorded session carries a figure
/// that can be defended.
/// </summary>
/// <remarks>
/// Intensity lives here rather than on the log: "Running, 8 km/h" and "Running, 12 km/h" are two
/// rows with their own MET values, not one row plus an intensity field a member has to interpret
/// (research.md R-003).
/// </remarks>
public class ActivityType : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;

    /// <summary>Lowercased and accent-stripped. Indexed; search matches on this, not on Name.</summary>
    public string SearchName { get; private set; } = string.Empty;

    public ActivityCategory Category { get; private set; }

    /// <summary>
    /// Metabolic equivalent of task, from the Compendium of Physical Activities. Decimal because
    /// one significant fractional digit matters at this scale, and safe as decimal because it is
    /// never aggregated in SQL - only multiplied in memory (ADR 0002).
    /// </summary>
    public decimal Met { get; private set; }

    private ActivityType() { }

    public static ActivityType Create(string name, ActivityCategory category, decimal met)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("An activity needs a name");

        if (met <= 0)
            throw new DomainException($"'{name}' needs a positive MET value");

        return new ActivityType
        {
            Name = name.Trim(),
            SearchName = Normalise(name),
            Category = category,
            Met = met
        };
    }

    /// <summary>
    /// Lowercases and strips accents so "café workout" is found by typing "cafe".
    /// </summary>
    /// <remarks>
    /// Delegates to the food library's implementation rather than copying it. Two catalogues
    /// normalising search text differently would be a search bug nobody would think to look for.
    /// </remarks>
    public static string Normalise(string value) => FoodLibraryItem.Normalise(value);
}
