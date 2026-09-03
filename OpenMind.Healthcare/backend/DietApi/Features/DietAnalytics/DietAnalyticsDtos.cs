using DietApi.Domain.Observations;
using DietApi.Domain.ValueObjects;

namespace DietApi.Features.DietAnalytics;

/// <summary>
/// The window a set of figures was computed over. Present on every analytics response, because no
/// figure here means anything without it.
/// </summary>
public record AnalysisPeriodDto(
    PeriodPreset Preset,
    DateOnly From,
    DateOnly To,
    bool WasNarrowed,
    int TotalDays,
    int LoggedDays,
    bool HasComparison,
    DateOnly? PreviousFrom,
    DateOnly? PreviousTo);

// --- Intake (US1) ---------------------------------------------------------

public record IntakeSummaryDto(
    int TotalKilocalories,
    int AverageDailyKilocalories,
    int AveragedOverDays,
    AveragedOver AveragedOver,
    int? PreviousAverageDailyKilocalories,
    int OnTargetDays,
    int OverTargetDays,
    int NotLoggedDays);

public record MealShareDto(MealType Meal, int Kilocalories, decimal ShareOfTotal, int EntryCount);

public record CategoryShareDto(FoodCategory Category, int Kilocalories, decimal ShareOfTotal);

public record FoodContributionDto(
    Guid FoodLibraryItemId, string FoodName, int Kilocalories, decimal ShareOfTotal, int TimesLogged);

/// <summary>
/// <c>Meals</c> and <c>Categories</c> are exhaustive, so their parts sum to the total.
/// <c>TopFoods</c> is a top ten and deliberately does not.
/// </summary>
public record IntakeAnalysisResponse(
    AnalysisPeriodDto Period,
    IntakeSummaryDto Summary,
    IReadOnlyList<MealShareDto> Meals,
    IReadOnlyList<FoodContributionDto> TopFoods,
    IReadOnlyList<CategoryShareDto> Categories);

// --- Macronutrients (US2) -------------------------------------------------

public record MacroAmountsDto(decimal ProteinG, decimal CarbsG, decimal FatG);

public record MacroSharesDto(decimal Protein, decimal Carbs, decimal Fat);

/// <summary>
/// <c>Target</c> is null when the plan carries no macronutrient targets. The client presents the
/// split alone in that case and must not substitute the plan's present target (FR-012).
/// </summary>
public record MacroAnalysisResponse(
    AnalysisPeriodDto Period,
    int AveragedOverDays,
    bool HasTargets,
    MacroAmountsDto Actual,
    MacroAmountsDto? Target,
    MacroSharesDto ShareOfEnergy);

// --- Patterns (US3) -------------------------------------------------------

public record WeekdayShareDto(DayOfWeek DayOfWeek, int AverageKilocalories, int LoggedDays);

public record HourShareDto(int Hour, int Kilocalories, decimal ShareOfTotal);

/// <summary>
/// <c>IsApproximate</c> is always true here and always accompanied by its reason. The times shown
/// are when an entry was recorded, not necessarily when the food was eaten (FR-015).
/// </summary>
public record EatingPatternsResponse(
    AnalysisPeriodDto Period,
    int UtcOffsetMinutes,
    bool IsApproximate,
    string ApproximationReason,
    IReadOnlyList<WeekdayShareDto> ByWeekday,
    IReadOnlyList<HourShareDto> ByHour);

// --- Daily trend ----------------------------------------------------------

/// <summary>
/// One calendar day on the trend.
/// </summary>
/// <remarks>
/// <c>Logged</c> is what a chart must read first. On an unlogged day the intake figures are
/// placeholders, not measurements, and a line must break rather than pass through them. The target
/// is meaningful either way - it was in force whether or not the member logged.
/// </remarks>
public record DailyIntakePointDto(
    DateOnly Date,
    bool Logged,
    int Kilocalories,
    int TargetKilocalories,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    decimal? TargetProteinG,
    decimal? TargetCarbsG,
    decimal? TargetFatG);

public record IntakeTrendResponse(
    AnalysisPeriodDto Period,
    int LoggedDays,
    int PeakKilocalories,
    IReadOnlyList<DailyIntakePointDto> Points);

// --- Observations (US4) ---------------------------------------------------

public record ObservationDto(
    ObservationFamily Family, string Text, string Figure, int BasedOnDays, decimal Strength);

/// <summary>
/// <c>NothingStoodOut</c> is a stated answer rather than something a client infers from an empty
/// list, and <c>MinimumDaysForAnyObservation</c> lets a member with too little history be told why
/// they see nothing (FR-018, FR-021).
/// </summary>
public record ObservationsResponse(
    AnalysisPeriodDto Period,
    IReadOnlyList<ObservationDto> Observations,
    bool NothingStoodOut,
    int MinimumDaysForAnyObservation);

public static class DietAnalyticsMapper
{
    public static AnalysisPeriodDto ToDto(AnalysisPeriod period) =>
        new(period.Preset,
            period.From,
            period.To,
            period.WasNarrowed,
            period.TotalDays,
            period.LoggedDays,
            period.HasComparison,
            period.PreviousFrom,
            period.PreviousTo);

    public static DailyIntakePointDto ToDto(DailyIntakePoint point) =>
        new(point.Date,
            point.Logged,
            point.Calories,
            point.TargetCalories,
            point.ProteinG,
            point.CarbsG,
            point.FatG,
            point.TargetProteinG,
            point.TargetCarbsG,
            point.TargetFatG);

    public static ObservationDto ToDto(Observation observation) =>
        new(observation.Family,
            observation.Text,
            observation.Figure,
            observation.BasedOnDays,
            observation.Strength);
}
