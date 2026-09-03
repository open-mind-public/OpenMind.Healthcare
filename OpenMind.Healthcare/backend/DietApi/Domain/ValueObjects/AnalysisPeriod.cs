using DDD.BuildingBlocks;
using DietApi.Domain.Rules;

namespace DietApi.Domain.ValueObjects;

/// <summary>
/// The resolved window every figure in analytics is computed against.
/// </summary>
/// <remarks>
/// <para>
/// Carries both denominators, because analytics has two honest ones and picking silently between
/// them is how a reporting feature lies. <see cref="LoggedDays"/> is what an intake average
/// divides by; <see cref="TotalDays"/> is what a count of missed days is measured against. Every
/// figure that uses one says which (FR-003).
/// </para>
/// <para>
/// <see cref="HasComparison"/> is false rather than the previous window being reported as zeros.
/// A window before the member's plan started does not exist, and zeros would assert they did
/// nothing in it.
/// </para>
/// <para>
/// Built in two steps by design: the range has to be decided before the store can be asked how
/// many of its days were logged. <see cref="WithLoggedDays"/> completes it once that count is
/// known, and revalidates nothing because the range it copies was already validated.
/// </para>
/// </remarks>
public class AnalysisPeriod : ValueObject
{
    public PeriodPreset Preset { get; private set; }

    public DateOnly From { get; private set; }
    public DateOnly To { get; private set; }

    /// <summary>True when clamping to the plan or to today changed the requested window.</summary>
    public bool WasNarrowed { get; private set; }

    /// <summary>Calendar days in the window, logged or not.</summary>
    public int TotalDays => To.DayNumber - From.DayNumber + 1;

    /// <summary>Days in the window carrying at least one entry.</summary>
    public int LoggedDays { get; private set; }

    public DateOnly? PreviousFrom { get; private set; }
    public DateOnly? PreviousTo { get; private set; }

    public bool HasComparison => PreviousFrom.HasValue && PreviousTo.HasValue;

    private AnalysisPeriod() { }

    public static AnalysisPeriod Create(
        PeriodPreset preset,
        DateOnly from,
        DateOnly to,
        DateOnly planStartDate,
        DateOnly today,
        bool wasNarrowed,
        DateOnly? previousFrom = null,
        DateOnly? previousTo = null)
    {
        Check(new PeriodMustNotBeEmptyRule(from, to));
        Check(new PeriodMustFallWithinPlanRule(from, to, planStartDate, today));

        if (previousFrom.HasValue != previousTo.HasValue)
            throw new DomainException("A comparison window needs both ends or neither");

        return Build(preset, from, to, wasNarrowed, loggedDays: 0, previousFrom, previousTo);
    }

    /// <summary>
    /// The same window with its logged-day count filled in, once the store has been asked.
    /// </summary>
    public AnalysisPeriod WithLoggedDays(int loggedDays)
    {
        if (loggedDays < 0 || loggedDays > TotalDays)
            throw new DomainException($"A {TotalDays} day period cannot have {loggedDays} logged days");

        return Build(Preset, From, To, WasNarrowed, loggedDays, PreviousFrom, PreviousTo);
    }

    private static AnalysisPeriod Build(
        PeriodPreset preset,
        DateOnly from,
        DateOnly to,
        bool wasNarrowed,
        int loggedDays,
        DateOnly? previousFrom,
        DateOnly? previousTo) =>
        new()
        {
            Preset = preset,
            From = from,
            To = to,
            WasNarrowed = wasNarrowed,
            LoggedDays = loggedDays,
            PreviousFrom = previousFrom,
            PreviousTo = previousTo
        };

    /// <summary>
    /// <c>CheckRule</c> lives on <c>AggregateRoot</c>, and this is a value object - so the same
    /// guard is spelled out here rather than reaching for a base class that does not apply.
    /// </summary>
    private static void Check(IBusinessRule rule)
    {
        if (rule.IsBroken())
            throw new BusinessRuleValidationException(rule);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Preset;
        yield return From;
        yield return To;
        yield return WasNarrowed;
        yield return LoggedDays;
        yield return PreviousFrom;
        yield return PreviousTo;
    }
}
