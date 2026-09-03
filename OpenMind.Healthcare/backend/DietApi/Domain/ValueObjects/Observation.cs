using DDD.BuildingBlocks;
using DietApi.Domain.Observations;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// One thing the programme noticed in a member's own data.
/// </summary>
/// <remarks>
/// <para>
/// A statement of fact with its figure attached, never a verdict. Nothing here diagnoses a
/// condition, calls a member's eating good or bad, or tells them what to eat - that line is
/// FR-019, and it is held by reviewing every rule's wording before release rather than by hoping.
/// </para>
/// <para>
/// <see cref="Figure"/> is carried separately from <see cref="Text"/> deliberately. It lets the
/// screen emphasise the number inside the sentence, and it lets a test assert the arithmetic
/// without matching prose (FR-017).
/// </para>
/// </remarks>
public class Observation : ValueObject
{
    public ObservationFamily Family { get; private set; }

    /// <summary>Fixed wording with the figure interpolated. Never generated freely.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>The number the claim rests on, as the member would read it.</summary>
    public string Figure { get; private set; } = string.Empty;

    /// <summary>
    /// How far past its threshold the rule fired, from 0 to 1. Orders the list, and makes the
    /// determinism FR-020 requires a property of the arithmetic rather than of discipline.
    /// </summary>
    public decimal Strength { get; private set; }

    /// <summary>The logged days behind the claim, so a member can weigh it (FR-017).</summary>
    public int BasedOnDays { get; private set; }

    private Observation() { }

    public static Observation Create(
        ObservationFamily family, string text, string figure, decimal strength, int basedOnDays)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("An observation needs something to say");

        if (string.IsNullOrWhiteSpace(figure))
            throw new DomainException("An observation without its figure is not checkable");

        if (basedOnDays <= 0)
            throw new DomainException("An observation must rest on at least one logged day");

        return new Observation
        {
            Family = family,
            Text = text.Trim(),
            Figure = figure.Trim(),
            Strength = Math.Clamp(strength, 0m, 1m),
            BasedOnDays = basedOnDays
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Family;
        yield return Text;
        yield return Figure;
        yield return Strength;
        yield return BasedOnDays;
    }
}
