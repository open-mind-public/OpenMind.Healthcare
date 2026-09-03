namespace DietApi.Domain.Observations;

/// <summary>
/// What an observation is fundamentally about.
/// </summary>
/// <remarks>
/// This is what FR-022 de-duplicates on. "A third of your intake is after 21:00" and "your
/// evening meal is 45% of your intake" are the same observation wearing two hats; only the
/// stronger of a family is shown, so the list does not repeat itself in different words.
/// </remarks>
public enum ObservationFamily
{
    Timing,
    Composition,
    Targets,
    Consistency
}
