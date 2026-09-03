using DietApi.Domain.Aggregates;
using DietApi.Domain.Entities;
using DietApi.Domain.Repositories;

namespace DietApi.Features.Exercise;

/// <summary>
/// One recorded session. <c>EstimatedKcal</c> is an estimate, and every screen showing it says
/// so - it is never presented as calories available to eat (FR-008, FR-016).
/// </summary>
public record ExerciseEntryDto(
    Guid Id,
    Guid ActivityTypeId,
    string ActivityName,
    decimal Met,
    int DurationMinutes,
    int EstimatedKcal,
    DateTime RecordedAt);

/// <summary>
/// One date's activity, whether or not anything was recorded. <c>Version</c> is null when no
/// exercise day exists yet for the date; otherwise it must be echoed back on any write.
/// </summary>
/// <remarks>
/// Carries no calorie target, no remaining allowance and no day state. That is the point: this
/// shape cannot be combined with the eating assessment by a client trying to be helpful
/// (FR-016, SC-009).
/// </remarks>
public record ExerciseDayDto(
    DateOnly Date,
    Guid? Version,
    int TotalMinutes,
    int TotalKilocalories,
    IReadOnlyList<ExerciseEntryDto> Entries);

/// <summary>
/// One row per day <em>that has activity</em>. Days with none are simply absent - the calendar
/// treats absence as no exercise rather than inventing a state for it (FR-021).
/// </summary>
public record ExerciseDaySummaryDto(DateOnly Date, int TotalMinutes, int TotalKilocalories, int EntryCount);

public record ExerciseRangeResponse(DateOnly From, DateOnly To, IReadOnlyList<ExerciseDaySummaryDto> Days);

public record ActivitySummaryDto(
    int WindowDays,
    int ActiveDays,
    int TotalMinutes,
    int TotalKilocalories,
    int PreviousWindowActiveDays,
    int PreviousWindowMinutes);

/// <summary><c>Version</c> is omitted when the date has no exercise day yet, required otherwise.</summary>
public record AddExerciseEntryRequest(Guid ActivityTypeId, int DurationMinutes, Guid? Version);

public record UpdateExerciseEntryRequest(Guid ActivityTypeId, int DurationMinutes, Guid Version);

public static class ExerciseMapper
{
    public static ExerciseEntryDto ToDto(ExerciseEntry entry) =>
        new(entry.Id,
            entry.ActivityTypeId,
            entry.ActivityName,
            entry.Met,
            entry.DurationMinutes,
            entry.EstimatedKcal,
            entry.RecordedAt);

    public static ExerciseDayDto ToDto(ExerciseDay day) =>
        new(day.Date,
            day.Version,
            day.Totals.Minutes,
            day.Totals.Kilocalories,
            [.. day.EntriesInOrder().Select(ToDto)]);

    /// <summary>A date with no activity recorded. Not an error, and not a zero-minute session.</summary>
    public static ExerciseDayDto EmptyDay(DateOnly date) => new(date, null, 0, 0, []);

    public static ExerciseDaySummaryDto ToDto(ExerciseDaySummary summary) =>
        new(summary.Date, summary.TotalMinutes, summary.TotalKilocalories, summary.EntryCount);
}
