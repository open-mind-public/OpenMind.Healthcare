namespace DietApi.Domain.ValueObjects;

/// <summary>
/// How the activity catalogue is grouped. Broad enough that every seeded activity has an obvious
/// home, narrow enough that a member scanning a category finds what they did.
/// </summary>
/// <remarks>
/// Intensity is deliberately not a member here. "Running, 8 km/h" and "Running, 12 km/h" are two
/// catalogue entries in the same category, each carrying its own MET value, rather than one entry
/// plus an intensity field on the log - see research.md R-003.
/// </remarks>
public enum ActivityCategory
{
    Walking,
    Running,
    Cycling,
    Swimming,
    Gym,
    Sport,
    HomeAndGarden,
    Everyday
}
