namespace DietApi.Features.BeerDays;

/// <summary>
/// The beer days in a window, for the calendar. <c>Days</c> holds only the dates that are beer days
/// and fall within the plan - absence of a date means "not a beer day", not an unknown state.
/// </summary>
public record BeerDayRangeResponse(DateOnly From, DateOnly To, IReadOnlyList<DateOnly> Days);

/// <summary>The state of one date after a mark or unmark.</summary>
public record BeerDayResponse(DateOnly Date, bool IsBeerDay);
