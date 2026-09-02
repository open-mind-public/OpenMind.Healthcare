using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Features.DietAchievements;

public record DietAchievementDto(
    Guid Id,
    string Name,
    string Description,
    string Icon,
    AchievementCriterion Criterion,
    int Threshold,
    bool Unlocked,
    DateOnly? EarnedOn,
    int Remaining);

public record DietAchievementListResponse(IReadOnlyList<DietAchievementDto> Achievements);

public record NewlyUnlockedResponse(IReadOnlyList<DietAchievementDto> NewlyUnlocked);

public static class DietAchievementMapper
{
    public static DietAchievementDto ToDto(DietAchievementStatus status) =>
        new(status.Achievement.Id,
            status.Achievement.Name,
            status.Achievement.Description,
            status.Achievement.Icon,
            status.Achievement.Criterion,
            status.Achievement.Threshold,
            status.Unlocked,
            status.EarnedOn,
            status.Remaining);
}
