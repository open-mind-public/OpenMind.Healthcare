using DDD.BuildingBlocks;
using DietApi.Domain.ValueObjects;

namespace DietApi.Domain.Aggregates;

/// <summary>
/// A piece of curated guidance shown to a member who needs support.
/// </summary>
/// <remarks>
/// General wellbeing information, never clinical advice. Nothing here diagnoses, treats, or
/// prescribes.
/// </remarks>
public class EatingTip : AggregateRoot
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public TipCategory Category { get; private set; }

    private EatingTip() { }

    public static EatingTip Create(string title, string description, string icon, TipCategory category)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("A tip needs a title");

        return new EatingTip
        {
            Title = title,
            Description = description,
            Icon = icon,
            Category = category
        };
    }
}
