namespace QuitSmokingApi.Features.Progress;

/// <summary>
/// DTO for quit journey/progress response
/// </summary>
public record QuitJourneyDto(
    Guid Id,
    DateTime QuitDate,
    int CigarettesPerDay,
    int CigarettesPerPack,
    decimal PricePerPack,
    string Currency,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>
/// DTO for progress statistics response
/// </summary>
public record ProgressStatsDto(
    int DaysSmokeFree,
    int HoursSmokeFree,
    int MinutesSmokeFree,
    int CigarettesNotSmoked,
    decimal MoneySaved,
    int LifeRegainedMinutes,
    string LifeRegainedFormatted,
    double ProgressPercentage,
    string CurrentMilestone,
    string? NextMilestone,
    int DaysToNextMilestone,
    int TotalDaysInJourney,
    int SmokedDays,
    int CigarettesSmoked,
    decimal MoneySpentOnRelapses,
    int CurrentStreak,
    int LongestStreak,
    double SmokeFreeRate
);
