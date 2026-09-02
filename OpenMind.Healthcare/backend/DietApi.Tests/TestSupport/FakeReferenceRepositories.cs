using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.TestSupport;

/// <summary>In-memory stand-in for the seeded achievement definitions.</summary>
public sealed class FakeDietAchievementRepository : IDietAchievementRepository
{
    private readonly List<DietAchievement> _achievements = [];

    public static FakeDietAchievementRepository Containing(params DietAchievement[] achievements)
    {
        var repository = new FakeDietAchievementRepository();
        repository._achievements.AddRange(achievements);
        return repository;
    }

    public static DietAchievement WeekOnTarget() =>
        DietAchievement.Create("A week on target", "Seven days in a row.", "🥗",
            AchievementCriterion.ConsecutiveOnTargetDays, 7);

    public static DietAchievement FirstDayLogged() =>
        DietAchievement.Create("First day logged", "Everything starts here.", "🌱",
            AchievementCriterion.TotalDaysLogged, 1);

    public Task<IReadOnlyList<DietAchievement>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DietAchievement>>(_achievements);
}

/// <summary>In-memory stand-in for the seeded eating tips.</summary>
public sealed class FakeEatingTipRepository : IEatingTipRepository
{
    private readonly List<EatingTip> _tips = [];

    public static FakeEatingTipRepository WithSampleTips()
    {
        var repository = new FakeEatingTipRepository();
        repository._tips.AddRange(
        [
            EatingTip.Create("Wait ten minutes", "Most cravings pass.", "⏳", TipCategory.Craving),
            EatingTip.Create("Use a smaller plate", "The same food reads as more.", "🍽️", TipCategory.PortionControl),
            EatingTip.Create("One day is just one day", "The pattern matters.", "🌤️", TipCategory.Mindset)
        ]);
        return repository;
    }

    public Task<IReadOnlyList<EatingTip>> GetAsync(
        TipCategory? category = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EatingTip> tips = category is null
            ? _tips
            : [.. _tips.Where(t => t.Category == category)];

        return Task.FromResult(tips);
    }
}
